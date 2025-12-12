using System.Numerics;
using Server.DB;
using Server.Infra;
using static Server.DB.DataModel;
using static Server.Infra.LeaderboardService;

namespace Server {

    #region 정의
    public enum GameMode : int { Survive = 0, Respawn = 1, RankSurvive = 2 }

    public enum SupplyEffect : int {
        HealFull = 1, RangeUp = 2, SpeedUp = 3, DamageUp = 4,
        Vomit = 5, Invincible = 6, Giant = 7
    }

    public class Supply {
        public int id;
        public float x, y, z;
        public SupplyEffect effect;
        public long spawnMs;
        public long expireMs;
        public bool alive = true;
    }

    public class Bot {
        public int botId;
        public float posX, posY, posZ;
        public float yaw;
        public int MaxHP = 100;
        public int HP = 100;
        public bool Alive = true;
        public int NextAttackTick = 0;
        public int Kill = 0;
        public int Death = 0;
    }
    #endregion

    public sealed class Game {
        private readonly Func<IReadOnlyList<ClientSession>> _getSessions;
        private readonly Action<ArraySegment<byte>> _broadcast;
        private readonly Action<Action> _push;

        // 게임
        private DateTime _gameStartUtc = DateTime.UtcNow;

        public int _serverTick { get; private set; } = 0;
        private int _startTick = -1;
        private bool _isCountDownCompleted = false;
        private bool _isGameOver = false;
        private int aliveNums = 0;

        // 공격
        private readonly List<(int tick, int attackerId, bool attackerIsBot)> _pendingMelee = new();

        // 리스폰
        private readonly HashSet<int> _respawnHumans = new();
        private readonly HashSet<int> _respawnBots = new();
        private readonly Dictionary<int, int> _humanWake = new();
        private readonly Dictionary<int, int> _botWake = new();
        private long _respawnEndMs = -1;
        private int _lastTimerSentSec = -1;
        private const int RESPAWN_MATCH_MS = 90_000;
        private readonly int RESPAWN_DELAY_TICKS = (int)(2 * LevelData.TICK_RATE);
        private readonly int RESPAWN_SPAWN_INV_MS = 1500;

        // 봇/봇 소유
        private readonly Dictionary<int, Bot> _bots = new();
        private int _botOwnerSessionId = -1;

        // 보급품/버프
        private long _nextSupplySpawnMs = -1;
        private readonly Dictionary<int, Supply> _supplies = new();
        private int _nextSupplyId = 1;

        private class TimedBuff { public SupplyEffect eff; public long endMs; }
        private readonly Dictionary<ClientSession, List<TimedBuff>> _buffs = new();
        private readonly Dictionary<int, List<TimedBuff>> _botBuffs = new();

        private struct PendingVomit { public int ownerId; public long triggerMs; public bool ownerIsBot; }
        private readonly List<PendingVomit> _vomits = new();

        private static readonly Random _rng = new();

        private readonly HashSet<int> _seasonSaved = new();
        private readonly HashSet<string> _recentSavedAccs = new();

        private class ResultSnap {
            public int id;
            public string name;
            public bool isBot;
            public int kills;
            public int deaths;
            public int rank; // 0이면 미책정
            public string accountId;
        }
        private readonly Dictionary<int, ResultSnap> _resultById = new();

        public GameMode Mode { get; }

        internal Game(GameMode mode,
                      Func<IReadOnlyList<ClientSession>> getSessions,
                      Action<ArraySegment<byte>> broadcast,
                      Action<Action> push) {
            Mode = mode;
            _getSessions = getSessions;
            _broadcast = broadcast;
            _push = push;
        }

        private readonly Dictionary<int, int> _rankByPlayer = new();
        private readonly Dictionary<int, int> _rankByBot = new();

        public void SetupBots(int ownerSessionId, IReadOnlyList<int> botIds,
                              IReadOnlyList<(float x, float y, float z, float yaw)> spawns) {
            _botOwnerSessionId = ownerSessionId;
            for (int i = 0; i < botIds.Count; i++) {
                int id = botIds[i];
                var bot = new Bot { botId = id };
                if (spawns != null && i < spawns.Count) {
                    var p = spawns[i];
                    bot.posX = p.x; bot.posY = p.y; bot.posZ = p.z; bot.yaw = p.yaw;
                }
                _bots[id] = bot;
            }
        }

        internal void OnPlayerEnter(ClientSession s) {
            s.Game.MaxHP = 100; s.Game.HP = 100;
            s.Game.Kill = s.Game.Death = 0;
            s.Game.Rank = 0;
            s.Game.isAlive = true;
            s.Game.LastAttackSeq = s.Game.LastProcessedSeq = 0;
            s.Game.NextAttackTick = 0;
        }

        internal void OnPlayerLeave(ClientSession s) {
            if (s.SessionID == _botOwnerSessionId) ElectBotOwner();

            if (_isCountDownCompleted && !_isGameOver) {
                if (Mode != GameMode.Respawn) {
                    if (!_rankByPlayer.ContainsKey(s.SessionID)) {
                        s.Game.isAlive = false;
                        s.Game.Death++;

                        int aliveHumansNow = _getSessions().Count(p => p != s && p.Game.isAlive);
                        int aliveBotsNow = _bots.Values.Count(b => b.Alive);
                        int rank = Math.Max(1, aliveHumansNow + aliveBotsNow + 1);

                        s.Game.Rank = rank;
                        _rankByPlayer[s.SessionID] = rank;
                        Snap_SetRank_Player(s.SessionID, rank);
                    }

                    SaveRecentGameForOne(s);
                }

                if (Mode == GameMode.RankSurvive) {
                    SaveOnePlayerSeason(s);
                }
            }

            Snap_SyncHumanStats(s);
        }

        internal void OnBotSnapshot(ClientSession sender, C_BotSnapshot pkt) {
            if (sender.SessionID != _botOwnerSessionId) return;
            foreach (var b in pkt.botss) {
                if (_bots.TryGetValue(b.botId, out var bot) && bot.Alive) {
                    float vx = 0f, vz = 0f;
                    float x = b.posX, z = b.posZ;
                    ClampToMapAndSlide(ref x, ref z, ref vx, ref vz);
                    bot.posX = x; bot.posY = b.posY; bot.posZ = z; bot.yaw = b.yaw;
                }
            }
        }

        private int _lastCountdownSent = 0;

        public void Tick() {
            _serverTick++;

            // 카운트다운 단계
            if (!_isCountDownCompleted && _startTick >= 0) {
                int remain = _startTick - _serverTick;
                if (remain > 0) {
                    int R = LevelData.TICK_RATE;
                    int sec = (remain + R - 1) / R;
                    if (sec > 3) sec = 3;
                    if (sec != _lastCountdownSent) {
                        _lastCountdownSent = sec;
                        _broadcast(new S_GameReady { startServerTick = _startTick, countdownSec = sec }.Write());
                    }
                    if ((_serverTick % 2) == 0) { BroadcastSnapshots(); BroadcastWorldSnapshot(); }
                    return;
                }
                _broadcast(new S_GameReady { startServerTick = _startTick, countdownSec = 0 }.Write());
                _isCountDownCompleted = true;
                _gameStartUtc = DateTime.UtcNow;

                SnapshotRoster();

                if (Mode == GameMode.Respawn) {
                    _respawnEndMs = TimeUtil.NowMs() + RESPAWN_MATCH_MS;
                    _lastTimerSentSec = -1;
                }
            }

            if (_nextSupplySpawnMs < 0) _nextSupplySpawnMs = TimeUtil.NowMs() + LevelData.SUPPLY_INTERVAL_MS;

            var sessions = _getSessions();

            // 입력 소비/이동
            foreach (var s in sessions) {
                int consumed = 0;
                int lastSeq = s.Game.LastProcessedSeq;

                while (s.Game.InputInbox.Count > 0 && consumed < LevelData.MAX_CONSUME_PER_TICK) {
                    var cmd = s.Game.InputInbox.Dequeue();
                    SimulateAuthoritative(s, cmd, LevelData.DT);
                    lastSeq = cmd.seq;
                    consumed++;
                }

                if (consumed == 0) {
                    var neutral = new C_Input { seq = s.Game.LastProcessedSeq, moveX = 0, moveZ = 0, yaw = s.Game.Yaw };
                    SimulateAuthoritative(s, neutral, LevelData.DT);
                }

                s.Game.LastProcessedSeq = lastSeq;
            }

            // 지연 히트 처리 (플레이어/봇 공통)
            for (int i = _pendingMelee.Count - 1; i >= 0; --i) {
                var h = _pendingMelee[i];
                if (h.tick <= _serverTick) {
                    ResolveMeleeHit(h.attackerId, h.attackerIsBot);
                    _pendingMelee.RemoveAt(i);
                }
            }

            // 상태 브로드캐스트
            var status = new S_GameStatus();
            int aliveHumans = 0, aliveBots = 0;
            int connectedHumans = 0;
            foreach (var s in sessions) {
                connectedHumans++;
                if (s.Game.isAlive) aliveHumans++;
                status.players.Add(new S_GameStatus.Player {
                    playerId = s.SessionID,
                    kills = s.Game.Kill,
                    deaths = s.Game.Death
                });
            }
            foreach (var b in _bots.Values) {
                if (b.Alive) aliveBots++;
                status.botss.Add(new S_GameStatus.Bots {
                    botId = b.botId,
                    kills = b.Kill,
                    deaths = b.Death
                });
            }
            aliveNums = aliveHumans + aliveBots;
            status.aliveCount = aliveNums;
            _broadcast(status.Write());


            if ((_serverTick % 2) == 0) { BroadcastSnapshots(); BroadcastWorldSnapshot(); }

            if (!_isGameOver) {
                HandleSupplySpawnSchedule();
                HandleSupplyPickupChecks();
                HandleSupplyExpiry();
                HandleVomitTriggers();
                HandleBotCombatAI();         // 공격 개시만 예약
                HandleRespawnMatchTimer();   // 리스폰 모드 타이머
                HandleRespawns();            // 깨어나기
            }

            // 모든 사람이 떠난 경우(봇만 남음): 바로 종료 및 저장
            if (_isCountDownCompleted && !_isGameOver && connectedHumans == 0) {
                _isGameOver = true;

                // 랭크 미책정 인간에게 기본 랭크 부여(봇 수 + 1등부터)
                int baseRank = Math.Max(1, aliveBots + 1);
                foreach (var e in _resultById.Values) {
                    if (!e.isBot && (e.rank <= 0)) {
                        e.rank = baseRank;
                    }
                }

                SaveRecentGamesForAll(respawnByKDOrder: false, kdOrdered: null);
                return; // 이 틱은 여기서 종료
            }

            // 게임오버(서바이벌)
            if (Mode != GameMode.Respawn && aliveNums <= 1 && _isCountDownCompleted && !_isGameOver) {
                _isGameOver = true;
                foreach (var s in _getSessions())
                    if (s.Game.isAlive && !_rankByPlayer.ContainsKey(s.SessionID)) {
                        _rankByPlayer[s.SessionID] = 1;
                        Snap_SetRank_Player(s.SessionID, 1);
                    }
                foreach (var b in _bots.Values)
                    if (b.Alive && !_rankByBot.ContainsKey(b.botId)) {
                        _rankByBot[b.botId] = 1;
                        Snap_SetRank_Bot(b.botId, 1);
                    }

                // 결과 패킷 스냅샷 기준
                var over = new S_GameOver();
                over.totalPlayers = _resultById.Count;

                foreach (var e in _resultById.Values.OrderBy(v => v.rank == 0 ? int.MaxValue : v.rank)) {
                    int rank = (e.rank <= 0) ? 1 : e.rank;
                    over.resultss.Add(new S_GameOver.Results {
                        playerId = e.id,
                        nickName = e.name,
                        isBot = e.isBot,
                        rank = rank,
                        kills = e.kills,
                        deaths = e.deaths
                    });
                }
                _broadcast(over.Write());

                try {
                    var accountIdBySid = _getSessions().ToDictionary(s => s.SessionID, s => s.Account.accountId);

                    var stats = new Dictionary<string, RankedStat>();
                    foreach (var s in sessions) {
                        if (_seasonSaved.Contains(s.SessionID)) continue; // 이미 저장한 사람 스킵
                        int rank = _rankByPlayer.TryGetValue(s.SessionID, out var r) ? r : 1;
                        if (accountIdBySid.TryGetValue(s.SessionID, out var accId)) {
                            stats[accId] = new RankedStat { Rank = rank, Kills = s.Game.Kill };
                        }
                    }

                    SaveRankedSeasonResults(stats);
                } catch (Exception ex) {
                    Log.Error($"[SeasonStat] save failed (Survive): {ex.Message}");
                }

                SaveRecentGamesForAll(respawnByKDOrder: false, kdOrdered: null);
            }

        }

        // === 이동/공격 입력 처리 ===
        private void SimulateAuthoritative(ClientSession s, C_Input cmd, float dt) {
            if (!s.Game.isAlive) { s.Game.VelX = s.Game.VelZ = 0; return; }

            const float MOVE_SPEED = 8f;
            const float INV_SQRT2 = 0.70710678f;

            int ix = (int)cmd.moveX, iz = (int)cmd.moveZ;
            float dx = ix, dz = iz;
            if (ix != 0 && iz != 0) { dx *= INV_SQRT2; dz *= INV_SQRT2; }

            float mul = GetSpeedMultiplier(s);
            s.Game.VelX = dx * MOVE_SPEED * mul;
            s.Game.VelZ = dz * MOVE_SPEED * mul;

            s.Game.PosX += s.Game.VelX * dt;
            s.Game.PosY += s.Game.VelY * dt;
            s.Game.PosZ += s.Game.VelZ * dt;

            ClampToMapAndSlide(ref s.Game.PosX, ref s.Game.PosZ, ref s.Game.VelX, ref s.Game.VelZ);
            s.Game.Yaw = MathUtil.NormalizeYaw(cmd.yaw);

            const byte ACTION_ATTACK = 1 << 0;
            const byte ACTION_DANCE = 1 << 1;

            if ((cmd.action & ACTION_ATTACK) != 0 && cmd.seq > s.Game.LastAttackSeq && _serverTick >= s.Game.NextAttackTick) {
                s.Game.LastAttackSeq = cmd.seq;
                s.Game.NextAttackTick = _serverTick + LevelData.ATTACK_COOLDOWN_TICKS;

                _broadcast(new S_BroadcastAttack { playerId = s.SessionID, serverTick = _serverTick }.Write());
                _pendingMelee.Add((_serverTick + LevelData.ATTACK_HIT_DELAY_TICKS, s.SessionID, false));
            }
            if ((cmd.action & ACTION_DANCE) != 0) {
                string slotName = $"dance{cmd.emoteSlot}";
                if (s.Game.EquippedBySlot.TryGetValue(slotName, out var emoteSku) && !string.IsNullOrEmpty(emoteSku)) {
                    //Console.WriteLine(emoteSku);
                    _broadcast(new S_BroadcastEmote { playerId = s.SessionID, emoteId = cmd.emoteSlot, emoteSku = emoteSku, serverTick = _serverTick }.Write());
                }
            }
        }

        private void ResolveMeleeHit(int attackerId, bool attackerIsBot) {
            if (!attackerIsBot) {
                var attacker = _getSessions().FirstOrDefault(s => s.SessionID == attackerId);
                if (attacker == null || !attacker.Game.isAlive) return;

                bool giant = HasBuff(attacker, SupplyEffect.Giant);
                float range = GetMeleeRange(attacker);
                int damage = (int)MathF.Round(LevelData.BASE_DAMAGE * GetDamageMultiplier(attacker));
                Vector3 pos = new(attacker.Game.PosX, 0, attacker.Game.PosZ);
                Vector3 fwd = new(MathF.Sin(attacker.Game.Yaw * (MathF.PI / 180f)), 0, MathF.Cos(attacker.Game.Yaw * (MathF.PI / 180f)));

                HitInArc(attackerId, false, range, damage, pos, fwd);
                return;
            }

            if (!_bots.TryGetValue(attackerId, out var bot) || !bot.Alive) return;

            bool giantB = BotHasBuff(attackerId, SupplyEffect.Giant);
            float rangeB = 2.6f;
            int damageB = (int)MathF.Round(LevelData.BASE_DAMAGE * 1f);

            Vector3 posB = new(bot.posX, 0, bot.posZ);
            Vector3 fwdB = new(MathF.Sin(bot.yaw * (MathF.PI / 180f)), 0, MathF.Cos(bot.yaw * (MathF.PI / 180f)));

            HitInArc(attackerId, true, rangeB, damageB, posB, fwdB);
        }

        private void HitInArc(int attackerId, bool attackerIsBot, float range, int damage, Vector3 pos, Vector3 fwd) {
            const float ARC = 150f;
            float range2 = range * range;

            var hitsP = new List<ClientSession>();
            var hitsB = new List<Bot>();

            // 플레이어 후보
            foreach (var t in _getSessions()) {
                if (!attackerIsBot && t.SessionID == attackerId) continue; // self 제외(플레이어 공격자)
                if (!t.Game.isAlive) continue;

                Vector3 to = new(t.Game.PosX - pos.X, 0, t.Game.PosZ - pos.Z);
                float d2 = to.X * to.X + to.Z * to.Z; if (d2 > range2) continue;
                float len = MathF.Sqrt(d2); if (len < 1e-4f) continue;

                float dot = (to.X / len) * fwd.X + (to.Z / len) * fwd.Z;
                float ang = MathF.Acos(Math.Clamp(dot, -1f, 1f)) * (180f / MathF.PI);
                if (ang > ARC * 0.5f) continue;

                if (!IsInvincible(t)) hitsP.Add(t);
            }

            // 봇 후보
            foreach (var b in _bots.Values) {
                if (attackerIsBot && b.botId == attackerId) continue; // self 제외(봇 공격자)
                if (!b.Alive) continue;

                Vector3 to = new(b.posX - pos.X, 0, b.posZ - pos.Z);
                float d2 = to.X * to.X + to.Z * to.Z; if (d2 > range2) continue;
                float len = MathF.Sqrt(d2); if (len < 1e-4f) continue;

                float dot = (to.X / len) * fwd.X + (to.Z / len) * fwd.Z;
                float ang = MathF.Acos(Math.Clamp(dot, -1f, 1f)) * (180f / MathF.PI);
                if (ang > ARC * 0.5f) continue;

                if (!BotHasInvincible(b.botId)) hitsB.Add(b);
            }

            if (hitsP.Count + hitsB.Count == 0) return;

            // 거인 여부를 내부에서 확인
            bool giant = HasGiant(attackerId, attackerIsBot);

            if (giant) {
                foreach (var v in hitsP) ApplyDamageToPlayer(attackerId, attackerIsBot, v, damage);
                foreach (var vb in hitsB) ApplyDamageToBot(attackerId, attackerIsBot, vb, damage);
                return;
            }

            // 가장 가까운 1명만
            float best = float.MaxValue;
            int who = 0; // 1 player, 2 bot
            ClientSession bp = null; Bot bb = null;

            foreach (var v in hitsP) {
                float dx = v.Game.PosX - pos.X, dz = v.Game.PosZ - pos.Z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best) { best = d2; who = 1; bp = v; bb = null; }
            }
            foreach (var b in hitsB) {
                float dx = b.posX - pos.X, dz = b.posZ - pos.Z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best) { best = d2; who = 2; bp = null; bb = b; }
            }

            if (who == 1) ApplyDamageToPlayer(attackerId, attackerIsBot, bp, damage);
            else if (who == 2) ApplyDamageToBot(attackerId, attackerIsBot, bb, damage);
        }


        private bool HasGiant(int id, bool isBot) {
            if (isBot) return BotHasBuff(id, SupplyEffect.Giant);
            var attacker = _getSessions().FirstOrDefault(s => s.SessionID == id);
            if (attacker == null) return false;
            return HasBuff(attacker, SupplyEffect.Giant);
        }


        private void ApplyDamageToPlayer(int attackerId, bool attackerIsBot, ClientSession target, int damage) {
            if (IsInvincible(target)) return;
            target.Game.HP = Math.Max(0, target.Game.HP - damage);
            bool killed = target.Game.HP == 0;
            if (killed) {
                target.Game.isAlive = false;
                target.Game.Death++;
                if (attackerIsBot) _bots[attackerId].Kill++;
                else _getSessions().First(s => s.SessionID == attackerId).Game.Kill++;

                if (Mode == GameMode.Respawn) {
                    StartRespawnHuman(target, RESPAWN_DELAY_TICKS);
                } else {
                    AssignRank_ForPlayer(target);
                    SaveOnePlayerSeason(target);
                }
            }

            Snap_SyncHumanStats(target);
            if (attackerIsBot) { if (_bots.TryGetValue(attackerId, out var ab)) Snap_SyncBotStats(ab); } else {
                var atk = _getSessions().FirstOrDefault(s => s.SessionID == attackerId);
                if (atk != null) Snap_SyncHumanStats(atk);
            }

            _broadcast(new S_Hit {
                attackerId = attackerId,
                targetId = target.SessionID,
                damage = damage,
                hpAfter = target.Game.HP,
                killed = killed,
                serverTick = _serverTick
            }.Write());
        }

        private void ApplyDamageToBot(int attackerId, bool attackerIsBot, Bot target, int damage) {
            if (BotHasInvincible(target.botId)) return;
            target.HP = Math.Max(0, target.HP - damage);
            bool killed = target.HP == 0;
            if (killed) {
                target.Alive = false;
                target.Death++;
                if (attackerIsBot) _bots[attackerId].Kill++;
                else _getSessions().First(s => s.SessionID == attackerId).Game.Kill++;

                if (Mode == GameMode.Respawn) StartRespawnBot(target, RESPAWN_DELAY_TICKS);
                else AssignRank_ForBot(target);
            }

            Snap_SyncBotStats(target);
            if (attackerIsBot) { if (_bots.TryGetValue(attackerId, out var ab)) Snap_SyncBotStats(ab); } else {
                var atk = _getSessions().FirstOrDefault(s => s.SessionID == attackerId);
                if (atk != null) Snap_SyncHumanStats(atk);
            }

            _broadcast(new S_Hit {
                attackerId = attackerId,
                targetId = target.botId,
                damage = damage,
                hpAfter = target.HP,
                killed = killed,
                serverTick = _serverTick
            }.Write());
        }

        // === 스냅샷 ===
        private void BroadcastSnapshots() {
            foreach (var cli in _getSessions()) {
                cli.Send(new S_Snapshot {
                    serverTick = _serverTick,
                    lastProcessedInputSeq = cli.Game.LastProcessedSeq,
                    posX = cli.Game.PosX,
                    posY = cli.Game.PosY,
                    posZ = cli.Game.PosZ,
                    yaw = cli.Game.Yaw
                }.Write());
            }
        }
        private void BroadcastWorldSnapshot() {
            var ws = new S_WorldSnapshot { serverTick = _serverTick };
            foreach (var s in _getSessions()) {
                ws.entitiess.Add(new S_WorldSnapshot.Entities {
                    playerId = s.SessionID,
                    posX = s.Game.PosX,
                    posY = s.Game.PosY,
                    posZ = s.Game.PosZ,
                    yaw = s.Game.Yaw
                });
            }
            foreach (var b in _bots.Values) {
                if (!b.Alive) continue;
                ws.entitiess.Add(new S_WorldSnapshot.Entities {
                    playerId = b.botId,
                    posX = b.posX,
                    posY = b.posY,
                    posZ = b.posZ,
                    yaw = b.yaw
                });
            }
            _broadcast(ws.Write());
        }

        // === 보급 ===
        private void HandleSupplySpawnSchedule() {
            long now = TimeUtil.NowMs();
            if (_nextSupplySpawnMs >= 0 && now >= _nextSupplySpawnMs) {
                SpawnSupply();
                _nextSupplySpawnMs = now + LevelData.SUPPLY_INTERVAL_MS;
            }
        }
        private void SpawnSupply() {
            var p = LevelData.RandomPointInMap(_rng);
            float x = p.x, z = p.z, y = 0f;

            SupplyEffect eff;
            int r = _rng.Next(0, 100);
            if (r < 15) eff = SupplyEffect.HealFull;
            else if (r < 30) eff = SupplyEffect.RangeUp;
            else if (r < 45) eff = SupplyEffect.SpeedUp;
            else if (r < 60) eff = SupplyEffect.DamageUp;
            else if (r < 75) eff = SupplyEffect.Vomit;
            else if (r < 90) eff = SupplyEffect.Invincible;
            else eff = SupplyEffect.Giant;

            int id = _nextSupplyId++;
            long now = TimeUtil.NowMs();
            _supplies[id] = new Supply {
                id = id,
                x = x,
                y = y,
                z = z,
                effect = eff,
                spawnMs = now,
                expireMs = now + LevelData.SUPPLY_LIFETIME_MS,
                alive = true
            };

            _broadcast(new S_SupplySpawn { dropId = id, effect = (int)eff, posX = x, posY = y, posZ = z }.Write());
        }
        private void HandleSupplyExpiry() {
            long now = TimeUtil.NowMs();
            foreach (var s in _supplies.Values) {
                if (s.alive && now >= s.expireMs) {
                    s.alive = false;
                    _broadcast(new S_SupplyGone { dropId = s.id, reason = 1 }.Write());
                }
            }
        }
        private void HandleSupplyPickupChecks() {
            foreach (var s in _supplies.Values) {
                if (!s.alive) continue;

                int bestType = 0; // 1 human, 2 bot
                ClientSession bp = null; Bot bb = null;
                float best = float.MaxValue;

                foreach (var p in _getSessions()) {
                    if (!p.Game.isAlive) continue;
                    float dx = p.Game.PosX - s.x, dz = p.Game.PosZ - s.z, d2 = dx * dx + dz * dz;
                    if (d2 <= LevelData.SUPPLY_PICKUP_RADIUS * LevelData.SUPPLY_PICKUP_RADIUS && d2 < best) {
                        best = d2; bestType = 1; bp = p; bb = null;
                    }
                }
                foreach (var b in _bots.Values) {
                    if (!b.Alive) continue;
                    float dx = b.posX - s.x, dz = b.posZ - s.z, d2 = dx * dx + dz * dz;
                    if (d2 <= LevelData.SUPPLY_PICKUP_RADIUS * LevelData.SUPPLY_PICKUP_RADIUS && d2 < best) {
                        best = d2; bestType = 2; bp = null; bb = b;
                    }
                }
                if (bestType == 0) continue;

                s.alive = false;
                int amount = 0, durationMs = 0;

                switch (s.effect) {
                    case SupplyEffect.HealFull:
                        if (bestType == 1) {
                            amount = bp.Game.MaxHP - bp.Game.HP; bp.Game.HP = bp.Game.MaxHP;
                            _broadcast(new S_Health { playerId = bp.SessionID, hp = bp.Game.HP, maxHp = bp.Game.MaxHP }.Write());
                        } else {
                            amount = bb.MaxHP - bb.HP; bb.HP = bb.MaxHP;
                            _broadcast(new S_Health { playerId = bb.botId, hp = bb.HP, maxHp = bb.MaxHP }.Write());
                        }
                        break;
                    case SupplyEffect.RangeUp: durationMs = LevelData.DURATION_MS_10S; if (bestType == 1) AddBuff(bp, SupplyEffect.RangeUp, durationMs); else AddBotBuff(bb.botId, SupplyEffect.RangeUp, durationMs); break;
                    case SupplyEffect.SpeedUp: durationMs = LevelData.DURATION_MS_10S; if (bestType == 1) AddBuff(bp, SupplyEffect.SpeedUp, durationMs); else AddBotBuff(bb.botId, SupplyEffect.SpeedUp, durationMs); break;
                    case SupplyEffect.DamageUp: durationMs = LevelData.DURATION_MS_10S; if (bestType == 1) AddBuff(bp, SupplyEffect.DamageUp, durationMs); else AddBotBuff(bb.botId, SupplyEffect.DamageUp, durationMs); break;
                    case SupplyEffect.Vomit:
                        durationMs = LevelData.DURATION_MS_10S; long t = TimeUtil.NowMs() + durationMs;
                        _vomits.Add(new PendingVomit { ownerId = (bestType == 1 ? bp.SessionID : bb.botId), ownerIsBot = (bestType == 2), triggerMs = t }); break;
                    case SupplyEffect.Invincible: durationMs = LevelData.DURATION_MS_10S; if (bestType == 1) AddBuff(bp, SupplyEffect.Invincible, durationMs); else AddBotBuff(bb.botId, SupplyEffect.Invincible, durationMs); break;
                    case SupplyEffect.Giant: durationMs = LevelData.DURATION_MS_10S; if (bestType == 1) AddBuff(bp, SupplyEffect.Giant, durationMs); else AddBotBuff(bb.botId, SupplyEffect.Giant, durationMs); break;
                }

                _broadcast(new S_SupplyApplied {
                    serverTick = _serverTick,
                    playerId = (bestType == 1 ? bp.SessionID : bb.botId),
                    effect = (int)s.effect,
                    amount = amount,
                    durationMs = durationMs
                }.Write());
                _broadcast(new S_SupplyGone { dropId = s.id, reason = 0 }.Write());
            }

            // 버프 만료
            long now = TimeUtil.NowMs();
            foreach (var p in _buffs.Keys.ToList()) {
                var list = _buffs[p];
                for (int i = list.Count - 1; i >= 0; --i) if (now >= list[i].endMs) list.RemoveAt(i);
            }
            foreach (var k in _botBuffs.Keys.ToList()) {
                var list = _botBuffs[k];
                for (int i = list.Count - 1; i >= 0; --i) if (now >= list[i].endMs) list.RemoveAt(i);
            }
        }

        private void HandleVomitTriggers() {
            long now = TimeUtil.NowMs();

            for (int i = _vomits.Count - 1; i >= 0; --i) {
                var v = _vomits[i];
                if (now < v.triggerMs) continue;

                // 소유자 현재 위치
                Vector3 pos;
                bool ownerAlive = false;

                if (!v.ownerIsBot) {
                    var owner = _getSessions().FirstOrDefault(s => s.SessionID == v.ownerId && s.Game.isAlive);
                    if (owner != null) {
                        pos = new Vector3(owner.Game.PosX, owner.Game.PosY, owner.Game.PosZ);
                        ownerAlive = true;
                    } else {
                        _vomits.RemoveAt(i);
                        continue;
                    }
                } else {
                    if (_bots.TryGetValue(v.ownerId, out var b) && b.Alive) {
                        pos = new Vector3(b.posX, b.posY, b.posZ);
                        ownerAlive = true;
                    } else {
                        _vomits.RemoveAt(i);
                        continue;
                    }
                }

                if (ownerAlive) {
                    _broadcast(new S_VomitExplode {
                        serverTick = _serverTick,
                        ownerId = v.ownerId,
                        posX = pos.X,
                        posY = pos.Y,
                        posZ = pos.Z,
                        radius = LevelData.VOMIT_RADIUS
                    }.Write());

                    // 피해 적용(플레이어)
                    foreach (var s in _getSessions()) {
                        if (!s.Game.isAlive || s.SessionID == v.ownerId) continue;
                        float dx = s.Game.PosX - pos.X, dz = s.Game.PosZ - pos.Z;
                        if (dx * dx + dz * dz <= LevelData.VOMIT_RADIUS * LevelData.VOMIT_RADIUS) {
                            if (IsInvincible(s)) continue;
                            s.Game.HP = Math.Max(0, s.Game.HP - LevelData.VOMIT_DAMAGE);
                            bool killed = s.Game.HP == 0;
                            if (killed) {
                                s.Game.isAlive = false;
                                s.Game.Death++;
                                // 킬 크레딧
                                if (!v.ownerIsBot) {
                                    var owner = _getSessions().FirstOrDefault(p => p.SessionID == v.ownerId);
                                    if (owner != null) owner.Game.Kill++;
                                } else {
                                    if (_bots.TryGetValue(v.ownerId, out var ob)) ob.Kill++;
                                }
                                if (Mode == GameMode.Respawn) {
                                    StartRespawnHuman(s, RESPAWN_DELAY_TICKS);
                                } else {
                                    AssignRank_ForPlayer(s);
                                    SaveOnePlayerSeason(s);
                                }

                                Snap_SyncHumanStats(s);                         // 피해자 동기화
                                if (!v.ownerIsBot) {
                                    var owner = _getSessions().FirstOrDefault(p => p.SessionID == v.ownerId);
                                    if (owner != null) Snap_SyncHumanStats(owner); // 가해자(사람) 동기화
                                } else {
                                    if (_bots.TryGetValue(v.ownerId, out var ob)) Snap_SyncBotStats(ob); // 가해자(봇) 동기화
                                }

                            }
                            _broadcast(new S_Hit {
                                attackerId = v.ownerId,
                                targetId = s.SessionID,
                                damage = LevelData.VOMIT_DAMAGE,
                                hpAfter = s.Game.HP,
                                killed = killed,
                                serverTick = _serverTick
                            }.Write());
                        }
                    }

                    // 피해 적용(봇)
                    foreach (var bot in _bots.Values) {
                        if (!bot.Alive || bot.botId == v.ownerId) continue;
                        float dx = bot.posX - pos.X, dz = bot.posZ - pos.Z;
                        if (dx * dx + dz * dz <= LevelData.VOMIT_RADIUS * LevelData.VOMIT_RADIUS) {
                            if (BotHasInvincible(bot.botId)) continue;
                            bot.HP = Math.Max(0, bot.HP - LevelData.VOMIT_DAMAGE);
                            bool killed = bot.HP == 0;
                            if (killed) {
                                bot.Alive = false;
                                bot.Death++;
                                // 킬 크레딧
                                if (!v.ownerIsBot) {
                                    var owner = _getSessions().FirstOrDefault(p => p.SessionID == v.ownerId);
                                    if (owner != null) owner.Game.Kill++;
                                } else {
                                    if (_bots.TryGetValue(v.ownerId, out var ob)) ob.Kill++;
                                }
                                if (Mode == GameMode.Respawn) StartRespawnBot(bot, RESPAWN_DELAY_TICKS);
                                else AssignRank_ForBot(bot);

                                Snap_SyncBotStats(bot);
                                if (!v.ownerIsBot) {
                                    var owner = _getSessions().FirstOrDefault(p => p.SessionID == v.ownerId);
                                    if (owner != null) Snap_SyncHumanStats(owner);
                                } else {
                                    if (_bots.TryGetValue(v.ownerId, out var ob)) Snap_SyncBotStats(ob);
                                }
                            }
                            _broadcast(new S_Hit {
                                attackerId = v.ownerId,
                                targetId = bot.botId,
                                damage = LevelData.VOMIT_DAMAGE,
                                hpAfter = bot.HP,
                                killed = killed,
                                serverTick = _serverTick
                            }.Write());
                        }
                    }
                }

                _vomits.RemoveAt(i);
            }
        }

        private float GetSpeedMultiplier(ClientSession s) {
            if (_buffs.TryGetValue(s, out var list)) foreach (var b in list) if (b.eff == SupplyEffect.SpeedUp) return 2f;
            return 1f;
        }
        private float GetMeleeRange(ClientSession s) {
            if (_buffs.TryGetValue(s, out var list)) foreach (var b in list) if (b.eff == SupplyEffect.RangeUp) return LevelData.ONSUPPLY_RANGE;
            return LevelData.DEFAULT_RANGE;
        }
        private float GetDamageMultiplier(ClientSession s) {
            if (_buffs.TryGetValue(s, out var list)) foreach (var b in list) if (b.eff == SupplyEffect.DamageUp) return LevelData.ONSUPPLY_DAMAGE_MUL;
            return 1f;
        }
        private bool IsInvincible(ClientSession s) {
            if (_buffs.TryGetValue(s, out var list)) foreach (var b in list) if (b.eff == SupplyEffect.Invincible) return true;
            return false;
        }
        private bool HasBuff(ClientSession s, SupplyEffect eff) {
            if (_buffs.TryGetValue(s, out var list)) foreach (var b in list) if (b.eff == eff) return true;
            return false;
        }
        private bool BotHasInvincible(int botId) {
            if (_botBuffs.TryGetValue(botId, out var list)) foreach (var b in list) if (b.eff == SupplyEffect.Invincible) return true;
            return false;
        }
        private bool BotHasBuff(int botId, SupplyEffect eff) {
            if (_botBuffs.TryGetValue(botId, out var list)) foreach (var b in list) if (b.eff == eff) return true;
            return false;
        }
        private void AddBuff(ClientSession p, SupplyEffect eff, int durationMs) {
            if (!_buffs.TryGetValue(p, out var list)) { list = new List<TimedBuff>(); _buffs[p] = list; }
            list.Add(new TimedBuff { eff = eff, endMs = TimeUtil.NowMs() + durationMs });
        }
        private void AddBotBuff(int botId, SupplyEffect eff, int durationMs) {
            if (!_botBuffs.TryGetValue(botId, out var list)) { list = new List<TimedBuff>(); _botBuffs[botId] = list; }
            list.Add(new TimedBuff { eff = eff, endMs = TimeUtil.NowMs() + durationMs });
        }

        private bool ElectBotOwner() {
            int? next = null;
            foreach (var s in _getSessions()) if (s.Game.isAlive && (next == null || s.SessionID < next.Value)) next = s.SessionID;
            int newOwner = next ?? -1;
            if (newOwner == _botOwnerSessionId) return false;

            _botOwnerSessionId = newOwner;
            var noti = new S_BotOwnerChanged { ownerId = _botOwnerSessionId };
            foreach (var id in _bots.Keys) noti.botIdss.Add(new S_BotOwnerChanged.BotIds { botId = id });
            _broadcast(noti.Write());
            Log.Info($"[BotOwner] changed -> {_botOwnerSessionId}");
            return true;
        }

        private void HandleBotCombatAI() {
            foreach (var bot in _bots.Values) {
                if (!bot.Alive) continue;

                int bestType = 0; ClientSession bestP = null; Bot bestB = null; float bestD2 = float.MaxValue;

                foreach (var s in _getSessions()) {
                    if (!s.Game.isAlive) continue;
                    float dx = s.Game.PosX - bot.posX, dz = s.Game.PosZ - bot.posZ;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < bestD2) { bestD2 = d2; bestType = 1; bestP = s; bestB = null; }
                }
                foreach (var ob in _bots.Values) {
                    if (!ob.Alive || ob.botId == bot.botId) continue;
                    float dx = ob.posX - bot.posX, dz = ob.posZ - bot.posZ;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < bestD2) { bestD2 = d2; bestType = 2; bestP = null; bestB = ob; }
                }
                if (bestType == 0) continue;

                float range = 2.6f;
                if (bestD2 > range * range) continue;

                float yawRad = bot.yaw * (MathF.PI / 180f);
                Vector3 fwd = new(MathF.Sin(yawRad), 0, MathF.Cos(yawRad));
                Vector3 to = (bestType == 1)
                    ? new(bestP.Game.PosX - bot.posX, 0, bestP.Game.PosZ - bot.posZ)
                    : new(bestB.posX - bot.posX, 0, bestB.posZ - bot.posZ);

                float len = to.Length(); if (len < 1e-4f) continue;
                float dot = Vector3.Dot(to / len, fwd);
                float ang = MathF.Acos(Math.Clamp(dot, -1f, 1f)) * (180f / MathF.PI);
                if (ang > LevelData.MELEE_ARC_DEG * 0.5f) continue;

                if (_serverTick < bot.NextAttackTick) continue;

                bot.NextAttackTick = _serverTick + LevelData.BOT_ATTACK_COOLDOWN_TICKS;
                _broadcast(new S_BroadcastAttack { playerId = bot.botId, serverTick = _serverTick }.Write());
                _pendingMelee.Add((_serverTick + LevelData.ATTACK_HIT_DELAY_TICKS, bot.botId, true));
            }
        }

        // === Respawn ===
        private void StartRespawnHuman(ClientSession target, int delayTicks) {
            if (!_respawnHumans.Add(target.SessionID)) return;
            _humanWake[target.SessionID] = _serverTick + delayTicks;
        }
        private void StartRespawnBot(Bot b, int delayTicks) {
            if (!_respawnBots.Add(b.botId)) return;
            _botWake[b.botId] = _serverTick + delayTicks;
        }

        private void HandleRespawns() {
            if (_isGameOver) { _respawnHumans.Clear(); _respawnBots.Clear(); _humanWake.Clear(); _botWake.Clear(); return; }

            var sessions = _getSessions().ToDictionary(s => s.SessionID, s => s);

            // 사람
            var doneH = new List<int>();
            foreach (var (sid, wake) in _humanWake) {
                if (_serverTick < wake) continue;
                if (!sessions.TryGetValue(sid, out var target)) { doneH.Add(sid); _respawnHumans.Remove(sid); continue; }

                var p = LevelData.RandomPointInMap(_rng);
                float yaw = MathUtil.YawDegLookAt(p.x, p.z, 0f, 0f);

                target.Game.PosX = p.x; target.Game.PosY = 0f; target.Game.PosZ = p.z;
                target.Game.Yaw = yaw; target.Game.VelX = target.Game.VelY = target.Game.VelZ = 0f;
                target.Game.HP = target.Game.MaxHP; target.Game.isAlive = true;

                if (RESPAWN_SPAWN_INV_MS > 0) AddBuff(target, SupplyEffect.Invincible, RESPAWN_SPAWN_INV_MS);

                _broadcast(new S_Health { playerId = target.SessionID, hp = target.Game.HP, maxHp = target.Game.MaxHP }.Write());
                _broadcast(new S_Respawn { playerId = target.SessionID, posX = target.Game.PosX, posY = target.Game.PosY, posZ = target.Game.PosZ, yaw = target.Game.Yaw }.Write());

                doneH.Add(sid); _respawnHumans.Remove(sid);
            }
            foreach (var sid in doneH) _humanWake.Remove(sid);

            // 봇
            var doneB = new List<int>();
            foreach (var (bid, wake) in _botWake) {
                if (_serverTick < wake) continue;
                if (!_bots.TryGetValue(bid, out var bot)) { doneB.Add(bid); _respawnBots.Remove(bid); continue; }

                var p = LevelData.RandomPointInMap(_rng);
                float yaw = MathUtil.YawDegLookAt(p.x, p.z, 0f, 0f);

                bot.posX = p.x; bot.posY = 0f; bot.posZ = p.z; bot.yaw = yaw;
                bot.HP = bot.MaxHP; bot.Alive = true;

                if (RESPAWN_SPAWN_INV_MS > 0) AddBotBuff(bid, SupplyEffect.Invincible, RESPAWN_SPAWN_INV_MS);

                _broadcast(new S_Health { playerId = bid, hp = bot.HP, maxHp = bot.MaxHP }.Write());
                _broadcast(new S_Respawn { playerId = bid, posX = bot.posX, posY = bot.posY, posZ = bot.posZ, yaw = bot.yaw }.Write());

                doneB.Add(bid); _respawnBots.Remove(bid);
            }
            foreach (var bid in doneB) _botWake.Remove(bid);
        }

        private void HandleRespawnMatchTimer() {
            if (!(Mode == GameMode.Respawn && _isCountDownCompleted && !_isGameOver)) return;

            long now = TimeUtil.NowMs();
            long remainMs = (_respawnEndMs > 0) ? Math.Max(0, _respawnEndMs - now) : 0;

            int sec = (int)((remainMs + 999) / 1000);
            if (sec != _lastTimerSentSec) {
                _lastTimerSentSec = sec;
                _broadcast(new S_MatchTimer { remainingSec = sec }.Write());
            }

            if (remainMs <= 0) EndMatchByTime();
        }

        private void BuildAndBroadcastGameOver_ByKD() {
            if (_isGameOver) return;
            _isGameOver = true;

            // 스냅샷을 K/D 정렬
            var all = _resultById.Values
                .OrderByDescending(e => e.kills)
                .ThenBy(e => e.deaths)
                .ToList();

            var over = new S_GameOver { totalPlayers = all.Count };
            for (int i = 0; i < all.Count; i++) {
                var e = all[i];
                over.resultss.Add(new S_GameOver.Results {
                    playerId = e.id,
                    nickName = e.name,
                    isBot = e.isBot,
                    rank = i + 1,     // Respawn은 K/D 순위로 등수 부여
                    kills = e.kills,
                    deaths = e.deaths
                });
            }
            _broadcast(over.Write());
            SaveRecentGamesForAll(respawnByKDOrder: true, kdOrdered: over.resultss.ToList());
        }

        private void EndMatchByTime() {
            if (_isGameOver) return;
            Log.Info("[GameOver] by time limit (Respawn)");
            BuildAndBroadcastGameOver_ByKD();
        }

        private static void ClampToMapAndSlide(ref float x, ref float z, ref float vx, ref float vz) {
            if (x <= LevelData.MAP_MIN_X && vx < 0f) vx = 0f;
            if (x >= LevelData.MAP_MAX_X && vx > 0f) vx = 0f;
            if (z <= LevelData.MAP_MIN_Z && vz < 0f) vz = 0f;
            if (z >= LevelData.MAP_MAX_Z && vz > 0f) vz = 0f;

            if (x < LevelData.MAP_MIN_X) x = LevelData.MAP_MIN_X;
            else if (x > LevelData.MAP_MAX_X) x = LevelData.MAP_MAX_X;
            if (z < LevelData.MAP_MIN_Z) z = LevelData.MAP_MIN_Z;
            else if (z > LevelData.MAP_MAX_Z) z = LevelData.MAP_MAX_Z;
        }

        public void ArmStart(int delayTicks) {
            _startTick = _serverTick + Math.Max(0, delayTicks);
            _isCountDownCompleted = false;
            _lastCountdownSent = 0;
        }
        private void AssignRank_ForPlayer(ClientSession target) {
            if (_rankByPlayer.ContainsKey(target.SessionID)) return;
            int rank = aliveNums;
            target.Game.Rank = rank;
            _rankByPlayer[target.SessionID] = rank;
            Snap_SetRank_Player(target.SessionID, rank);
            target.Send(new S_SetRank { rank = rank }.Write());
        }
        private void AssignRank_ForBot(Bot b) {
            if (_rankByBot.ContainsKey(b.botId)) return;
            int rank = aliveNums;
            _rankByBot[b.botId] = rank;
            Snap_SetRank_Bot(b.botId, rank);
        }

        private static string CurrentSeasonKeyLocal() => DateTime.UtcNow.Year.ToString();

        private static SeasonStat GetOrCreateCurrentSeasonLocal(AppDBContext db, Account acc) {
            string key = CurrentSeasonKeyLocal();
            var cur = db.Set<SeasonStat>().FirstOrDefault(s => s.AccountId == acc.Id && s.seasonKey == key);
            if (cur == null) {
                foreach (var o in db.Set<SeasonStat>().Where(s => s.AccountId == acc.Id && s.isCurrent)) o.isCurrent = false;

                cur = new SeasonStat {
                    AccountId = acc.Id,
                    seasonKey = key,
                    isCurrent = true,
                    totalGames = 0,
                    totalRankSum = 0,
                    winCount = 0,
                    rank = 0,
                    rankScore = 0,
                    totalKills = 0,
                    updatedAt = DateTime.UtcNow
                };
                db.Set<SeasonStat>().Add(cur);
                db.SaveChanges();
            }
            return cur;
        }

        private static void ApplyExpAndLevelRewards(
            AppDBContext db,
            Account acc,
            int expGain,
            ClientSession? sess,
            DateTime now
        ) {
            if (expGain <= 0)
                return;

            var (lvBefore, lvAfter, rewardGold, rewardStar) = AddAccountExpAndLevel(db, acc, expGain);

            if (lvAfter > lvBefore) {
                // 1) 골드 지급
                if (rewardGold > 0) {
                    acc.gold += rewardGold;
                    db.Set<CurrencyLedger>().Add(new CurrencyLedger {
                        AccountId = acc.Id,
                        currency = "GOLD",
                        delta = rewardGold,
                        balanceAfter = acc.gold,
                        reason = "LEVEL_UP",
                        refType = "LEVEL",
                        refId = lvAfter.ToString(),
                        createdAt = now
                    });
                }

                // 2) 스타 지급
                if (rewardStar > 0) {
                    acc.star += rewardStar;
                    db.Set<CurrencyLedger>().Add(new CurrencyLedger {
                        AccountId = acc.Id,
                        currency = "STAR",
                        delta = rewardStar,
                        balanceAfter = acc.star,
                        reason = "LEVEL_UP",
                        refType = "LEVEL",
                        refId = lvAfter.ToString(),
                        createdAt = now
                    });
                }

                // 3) 세션에 PendingLevelUp 기록
                if (sess != null) {
                    var st = sess.PendingLevelUp;
                    if (st == null) {
                        sess.PendingLevelUp = new LevelUpState {
                            FromLevel = lvBefore,
                            ToLevel = lvAfter,
                            RewardGold = rewardGold,
                            RewardStar = rewardStar
                        };
                    } else {
                        st.ToLevel = lvAfter;
                        st.RewardGold += rewardGold;
                        st.RewardStar += rewardStar;
                    }
                }
            }
        }

        private struct RankedStat { public int Rank; public int Kills; }

        // 사람 플레이어들에 대한 (accountId -> rank) 묶음을 받아 한번에 저장
        private void SaveRankedSeasonResults(Dictionary<string, RankedStat> statsByAccountId) {
            if (Mode != GameMode.RankSurvive || statsByAccountId == null || statsByAccountId.Count == 0) return;

            using var db = new AppDBContext();
            var sessionByAccId = _getSessions().ToDictionary(s => s.Account.accountId, s => s);
            var now = DateTime.UtcNow;

            foreach (var kv in statsByAccountId) {
                string accountId = kv.Key;
                var stat = kv.Value;

                var acc = db.Accounts.FirstOrDefault(a => a.accountId == accountId);
                if (acc == null) continue;

                var cur = GetOrCreateCurrentSeasonLocal(db, acc);

                // --- 저장 전 스냅샷(클라 통보용) ---
                int beforeRank = cur.rank;
                int beforeScore = cur.rankScore;

                // --- 누적 스탯 ---
                cur.totalGames += 1;
                cur.totalRankSum += stat.Rank;     // 1~N위 합
                cur.totalKills += stat.Kills;
                if (stat.Rank == 1) cur.winCount += 1;

                // --- 점수 계산 ---
                int change = (6 - stat.Rank) + stat.Kills;
                int newRank = cur.rank;
                int newScore = cur.rankScore + change;

                // 다이아(5) 이하에서만 승급/강등 처리
                if (newRank <= 5) {
                    // 승급 루프
                    while (newScore >= 100 && newRank < 6) {
                        newScore -= 100;
                        newRank += 1;
                    }
                    // 강등 루프
                    while (newScore < 0 && newRank > 0) {
                        newRank -= 1;
                        newScore += 100;
                    }
                    // 언랭(0) 바닥 보정
                    if (newRank == 0 && newScore < 0) newScore = 0;
                }

                // --- 실제 반영 ---
                cur.rank = (byte)newRank;
                cur.rankScore = newScore;
                cur.updatedAt = now;

                // 1등=10, 10등=1 (최소 1 보장)
                int expGain = Math.Clamp(11 - stat.Rank, 1, 10);
                sessionByAccId.TryGetValue(accountId, out var sess);
                ApplyExpAndLevelRewards(db, acc, expGain, sess, now);

                // --- 본인에게만 랭크 갱신 알림 ---
                if (sessionByAccId.TryGetValue(accountId, out var sessRank)) {
                    sessRank.Send(new S_RankScoreUpdate {
                        change = change,
                        beforeRank = beforeRank,
                        beforeRankScore = beforeScore,
                        afterRank = cur.rank,
                        afterRankScore = cur.rankScore
                    }.Write());
                    Log.Info($"[Game Recorded] {sessRank.Account.nickName}: {change}point");
                }
            }
            db.SaveChanges();
        }

        private void SaveOnePlayerSeason(ClientSession target) {
            if (Mode != GameMode.RankSurvive || target == null) return;
            if (_seasonSaved.Contains(target.SessionID)) return;

            // 랭크 확보
            int rank = _rankByPlayer.TryGetValue(target.SessionID, out var r) ? r : target.Game.Rank;
            if (rank <= 0) rank = Math.Max(1, aliveNums); // 안전장치

            var one = new Dictionary<string, RankedStat> {
        { target.Account.accountId, new RankedStat { Rank = rank, Kills = target.Game.Kill } }
    };

            try {
                SaveRankedSeasonResults(one);
                _seasonSaved.Add(target.SessionID); // 중복 저장 방지
            } catch (Exception ex) {
                Log.Error($"[SeasonStat] early save failed: {ex.Message}");
            }
        }

        private void SnapshotRoster() {
            _resultById.Clear();
            foreach (var s in _getSessions()) {
                _resultById[s.SessionID] = new ResultSnap {
                    id = s.SessionID,
                    name = s.Account.nickName,
                    isBot = false,
                    kills = 0,
                    deaths = 0,
                    rank = 0,
                    accountId = s.Account.accountId
                };
            }
            foreach (var b in _bots.Values) {
                _resultById[b.botId] = new ResultSnap {
                    id = b.botId,
                    name = b.botId.ToString(),
                    isBot = true,
                    kills = 0,
                    deaths = 0,
                    rank = 0,
                    accountId = null
                };
            }
        }

        private void Snap_SetRank_Player(int sessionId, int rank) {
            if (_resultById.TryGetValue(sessionId, out var s)) s.rank = rank;
        }
        private void Snap_SetRank_Bot(int botId, int rank) {
            if (_resultById.TryGetValue(botId, out var s)) s.rank = rank;
        }
        private void Snap_SyncHumanStats(ClientSession h) {
            if (_resultById.TryGetValue(h.SessionID, out var s)) {
                s.kills = h.Game.Kill;
                s.deaths = h.Game.Death;
                s.name = h.Account.nickName; // 닉변 방어
            }
        }
        private void Snap_SyncBotStats(Bot b) {
            if (_resultById.TryGetValue(b.botId, out var s)) {
                s.kills = b.Kill;
                s.deaths = b.Death;
            }
        }

        private void SaveRecentGamesForAll(bool respawnByKDOrder, List<S_GameOver.Results>? kdOrdered = null) {
            try {
                using var db = new AppDBContext();

                // 세션 매핑 (온라인인 사람만)
                var sessionByAccId = _getSessions().ToDictionary(s => s.Account.accountId, s => s);
                var now = DateTime.UtcNow;

                // 스냅샷에서 사람(playerId -> accountId) 매핑
                var accountBySid = _resultById.Values
                    .Where(v => !v.isBot && !string.IsNullOrEmpty(v.accountId))
                    .ToDictionary(v => v.id, v => v.accountId);

                Dictionary<int, (int rank, int kills, int deaths)> finalStats = new();

                if (respawnByKDOrder && kdOrdered != null) {
                    for (int i = 0; i < kdOrdered.Count; i++) {
                        var e = kdOrdered[i];
                        if (!e.isBot) finalStats[e.playerId] = (i + 1, e.kills, e.deaths);
                    }
                } else {
                    foreach (var e in _resultById.Values) {
                        if (e.isBot) continue;
                        int r = (e.rank <= 0) ? 1 : e.rank;
                        finalStats[e.id] = (r, e.kills, e.deaths);
                    }
                }

                foreach (var kv in finalStats) {
                    int sid = kv.Key;
                    if (!accountBySid.TryGetValue(sid, out var accStr)) continue;

                    // 이미 개별 저장된 계정은 스킵
                    if (_recentSavedAccs.Contains(accStr)) continue;

                    var acc = db.Accounts.FirstOrDefault(a => a.accountId == accStr);
                    if (acc == null) continue;

                    if (Mode != GameMode.RankSurvive) {
                        int rank = kv.Value.rank;
                        int expGain = Math.Clamp(11 - rank, 1, 10); // 1~10 사이
                        sessionByAccId.TryGetValue(accStr, out var sess);
                        ApplyExpAndLevelRewards(db, acc, expGain, sess, now);
                    }

                    db.RecentGames.Add(new RecentGame {
                        AccountId = acc.Id,
                        mode = (int)Mode,
                        rank = kv.Value.rank,
                        kills = kv.Value.kills,
                        deaths = kv.Value.deaths,
                        startedAt = _gameStartUtc
                    });
                    db.SaveChanges();

                    var old = db.RecentGames
                                .Where(r => r.AccountId == acc.Id)
                                .OrderByDescending(r => r.startedAt)
                                .Skip(20)
                                .ToList();
                    if (old.Count > 0) {
                        db.RecentGames.RemoveRange(old);
                        db.SaveChanges();
                    }
                }
            } catch (Exception ex) {
                Log.Error($"[RecentGames] save failed: {ex.Message}");
            }
        }

        private static (int levelBefore, int levelAfter, int rewardGold, int rewardStar)
    AddAccountExpAndLevel(AppDBContext db, Account acc, int expGain) {

            int levelBefore = acc.level;
            if (expGain <= 0)
                return (levelBefore, levelBefore, 0, 0);

            long total = (long)acc.exp + expGain; // 오버플로우 방지용 long
            int maxExp = (int)LevelData.MAX_ACCOUNT_EXP;

            int levelsGained = 0;
            while (total >= maxExp) {
                acc.level += 1;
                levelsGained++;
                total -= maxExp;
            }

            acc.exp = (int)Math.Clamp(total, 0, int.MaxValue);
            int levelAfter = acc.level;

            // 레벨당 500 골드
            int rewardGold = levelsGained * LevelData.LEVELUP_GOLD;

            // 10레벨마다 100 스타
            int beforeBucket = levelBefore / 10;
            int afterBucket = levelAfter / 10;
            int bucketDiff = afterBucket - beforeBucket;
            int rewardStar = (bucketDiff > 0) ? bucketDiff * LevelData.LEVELUP_STAR : 0;

            return (levelBefore, levelAfter, rewardGold, rewardStar);
        }

        private void SaveRecentGameForOne(ClientSession s) {
            if (s == null) return;
            if (!_isCountDownCompleted) return; // 경기 시작 전 탈주는 저장 안 함

            try {
                string accId = s.Account.accountId;
                if (string.IsNullOrEmpty(accId)) return;
                if (_recentSavedAccs.Contains(accId)) return; // 중복 방지

                // 랭크 확정
                int rank = s.Game.Rank;
                if (rank <= 0) {
                    int aliveHumansNow = _getSessions().Count(p => p.Game.isAlive);
                    int aliveBotsNow = _bots.Values.Count(b => b.Alive);
                    rank = Math.Max(1, aliveHumansNow + aliveBotsNow + 1);
                }

                using var db = new AppDBContext();
                var now = DateTime.UtcNow;
                var acc = db.Accounts.FirstOrDefault(a => a.accountId == accId);
                if (acc == null) return;

                if (Mode != GameMode.RankSurvive) {
                    int expGain = Math.Clamp(11 - rank, 1, 10);
                    ApplyExpAndLevelRewards(db, acc, expGain, s, now);
                }

                db.RecentGames.Add(new RecentGame {
                    AccountId = acc.Id,
                    mode = (int)Mode,
                    rank = rank,
                    kills = s.Game.Kill,
                    deaths = s.Game.Death,
                    startedAt = _gameStartUtc
                });
                db.SaveChanges();

                // 최신 20개 유지
                var old = db.RecentGames
                            .Where(r => r.AccountId == acc.Id)
                            .OrderByDescending(r => r.startedAt)
                            .Skip(20)
                            .ToList();
                if (old.Count > 0) {
                    db.RecentGames.RemoveRange(old);
                    db.SaveChanges();
                }

                _recentSavedAccs.Add(accId); // 이후 GameOver 저장에서 스킵하기 위함
                Log.Info($"[RecentGames] saved early for {s.Account.nickName}");
            } catch (Exception ex) {
                Log.Error($"[RecentGames] early save failed: {ex.Message}");
            }
        }

    }
}
