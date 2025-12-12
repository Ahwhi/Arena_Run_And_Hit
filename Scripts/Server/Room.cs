using Microsoft.EntityFrameworkCore;
using Server.DB;
using ServerCore;
using Server.Infra;
using static Server.DB.DataModel;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Server {
    public enum RoomKind { Login, Lobby, Matchmaking, Game, Group }

    class Room : IJobQueue {
        private readonly List<ClientSession> _sessions = new();
        private readonly JobQueue _jobQueue = new();
        private readonly List<ArraySegment<byte>> _pendingList = new();

        public IReadOnlyList<ClientSession> Sessions => _sessions;
        public RoomKind Kind { get; }
        public GameMode Mode { get; private set; } = (GameMode)(-1);

        // 매칭/게임 상태
        private Game _game;

        // 매칭 대기시간
        private readonly Dictionary<ClientSession, long> _mmEnterMs = new();

        // 시작 배리어(전원 C_EnterGame 도달 대기)
        private readonly HashSet<int> _expectedEnterIds = new();
        private bool _countdownArmed = false;

        // 봇 보류 정보(전원 입장 뒤 Game에 적용)
        private List<int> _pendingBotIds;
        private List<(float x, float y, float z, float yaw)> _pendingBotSpawns;
        private int _pendingBotOwnerId;
        private List<byte> _pendingBotColors;

        // 로스터(게임룸에서만 보유)
        private readonly Dictionary<int, (string nick, byte color, float x, float y, float z, float yaw)> _roster = new();

        // 플레이어(사람)별 코스메틱 캐시
        private readonly Dictionary<int, (string characterSku, string trailSku)> _cosmetics = new();

        private const string DEFAULT_CHAR = "CHAR_BASIC_01";
        private const string DEFAULT_TRAIL = "TRAIL_BASIC_01";

        public Room(RoomKind kind, GameMode mode = (GameMode)(-1)) {
            Kind = kind;
            if (kind == RoomKind.Matchmaking || kind == RoomKind.Game)
                Mode = mode;
        }

        public Room(RoomKind kind) {
            Kind = kind;
        }

        public string groupLeaderName = "";

        class GroupInvite {
            public string inviterNick;
            public string targetNick;
            public long expireMs;
            public bool consumed;
        }
        private readonly List<GroupInvite> _groupInvites = new();

        // ===== 메인 루프 =====
        public void Tick() {
            if (Kind == RoomKind.Matchmaking)
                TryStartMatchIfReady();

            _game?.Tick();


            if (this == Program.LobbyRoom) {
                Broadcast(new S_Population { total = Program.totalConnect }.Write());
                TickGroupInvites();
            }
        }

        private void TickGroupInvites() {
            if (_groupInvites.Count == 0) return;

            long now = TimeUtil.NowMs();

            for (int i = _groupInvites.Count - 1; i >= 0; --i) {
                var inv = _groupInvites[i];
                if (inv.consumed) {
                    _groupInvites.RemoveAt(i);
                    continue;
                }

                if (now >= inv.expireMs) {
                    inv.consumed = true;
                    _groupInvites.RemoveAt(i);

                    // 초대한 사람 세션 찾기
                    var inviter = _sessions.FirstOrDefault(s => s.Account.nickName == inv.inviterNick);
                    if (inviter != null) {
                        inviter.Send(new S_InviteGroupResult {
                            isAvailable = false,
                            isAccepted = false,
                            failReason = 5,   // 5 = 초대 시간 만료 (새로 정의)
                            InviterNickName = inv.inviterNick,
                            replierNickName = inv.targetNick
                        }.Write());
                    }

                    Log.Info($"[InviteGroup Timeout] {inv.inviterNick} -> {inv.targetNick}");
                }
            }
        }

        public void AttachGame(Game g) => _game = g;

        // ===== IJobQueue =====
        public void Push(Action job) => _jobQueue.Push(job);

        // ===== Flush =====
        public void Broadcast(ArraySegment<byte> seg) => _pendingList.Add(seg);
        public void Flush() {
            if (_pendingList.Count == 0) return;
            foreach (var s in _sessions)
                s.Send(_pendingList);
            _pendingList.Clear();
        }

        // ===== 핑 =====
        public void Ping(ClientSession s, C_Ping req) {
            var resp = new S_Pong {
                seq = req.seq,
                serverTick = _game?._serverTick ?? 0
            };
            s.Send(resp.Write());
        }

        // ===== 회원/로그인 =====
        public void Register(ClientSession session, C_Register packet) {
            string id = packet.accountId;
            string pw = packet.accountPw;
            string name = packet.nickName;

            using var db = new AppDBContext();

            if (db.Accounts.Any(u => u.accountId == id)) {
                session.Send(new S_RegisterResult { isSuccess = false, failReason = 1 }.Write());
                Log.Warn($"[Register Failed] accountId exists: {id}");
                return;
            }
            if (db.Accounts.Any(u => u.nickName == name)) {
                session.Send(new S_RegisterResult { isSuccess = false, failReason = 2 }.Write());
                Log.Warn($"[Register Failed] nick exists: {name}");
                return;
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(pw, workFactor: 12);
            var account = new Account {
                accountId = id,
                passwordHash = hash,
                nickName = name,
                recentIp = session.Account.IPAddress,
                level = 1,
                exp = 0,
                isNickSet = true,
                banStatus = AccountBan.None
            };
            db.Accounts.Add(account);
            db.SaveChanges();

            // === 기본 아이템 지급 & 자동 장착 ===
            try {
                GrantDefaultItemsAndEquip(db, account);
            } catch (Exception ex) {
                Log.Error($"[Register] default grants failed: {ex.Message}");
            }

            try {
                StatProfiler.GetOrCreateCurrentSeason(db, account); // 현재 시즌 보장
            } catch (Exception ex) {
                Log.Error($"[Register] ensure current season failed: {ex.Message}");
            }

            session.Send(new S_RegisterResult { isSuccess = true }.Write());
            Log.Info($"[Register] New user: Id={account.Id}, accountId={account.accountId}, nick={account.nickName}");
        }

        public void Login(ClientSession session, C_Login packet) {
            using var db = new AppDBContext();
            var account = db.Accounts.FirstOrDefault(u => u.accountId == packet.accountId);

            if (account == null) {
                session.Send(new S_LoginResult {
                    isSuccess = false,
                    failReason = 1,
                    accountId = string.Empty,
                    nickName = string.Empty,
                    accessToken = string.Empty
                }.Write());
                Log.Warn($"[Login Failed] not found: {packet.accountId}");
                return;
            }
            if (!BCrypt.Net.BCrypt.Verify(packet.accountPw, account.passwordHash)) {
                session.Send(new S_LoginResult {
                    isSuccess = false,
                    failReason = 2,
                    accountId = string.Empty,
                    nickName = string.Empty,
                    accessToken = string.Empty
                }.Write());
                Log.Warn($"[Login Failed] wrong pw: {packet.accountId}");
                return;
            }

            string token = Guid.NewGuid().ToString();
            if (packet.isAutoLogin) {
                account.accessToken = token;
                account.tokenExpireAt = DateTime.UtcNow.AddHours(1);
            } else {
                account.accessToken = null;
                account.tokenExpireAt = null;
            }
            account.recentIp = session.Account.IPAddress;
            db.SaveChanges();

            session.Account.accountId = account.accountId;
            session.Account.nickName = account.nickName;

            session.Send(new S_LoginResult {
                isSuccess = true,
                failReason = 0,
                accountId = account.accountId,
                nickName = account.nickName,
                accessToken = packet.isAutoLogin ? token : string.Empty
            }.Write());

            Log.Info($"[Login Success] id={account.accountId}, nick={account.nickName}, ip={session.Account.IPAddress}, auto={packet.isAutoLogin}");

            Program.LoginRoom.Push(() => Program.LoginRoom.DisAccess(session));
        }

        public void AutoLogin(ClientSession session, C_AutoLogin packet) {
            using var db = new AppDBContext();
            var account = db.Accounts.FirstOrDefault(u => u.accessToken == packet.accessToken);

            if (account != null && account.tokenExpireAt > DateTime.UtcNow) {
                session.Account.accountId = account.accountId;
                session.Account.nickName = account.nickName;

                session.Send(new S_AutoLoginResult { isSuccess = true, accountId = account.accountId, nickName = account.nickName }.Write());
                Log.Info($"[AutoLogin Success] accountId={account.accountId}");
            } else {
                int reason = (account == null) ? 1 : 2;
                session.Send(new S_AutoLoginResult { isSuccess = false, failReason = reason, accountId = "", nickName = "" }.Write());
                Log.Warn($"[AutoLogin Failed] reason={reason}");
            }
        }

        public void Logout(ClientSession s, C_Logout packet) {
            try {
                // 1) 자동로그인 토큰 무효화
                using (var db = new AppDBContext()) {
                    var acc = db.Accounts.FirstOrDefault(a => a.accountId == s.Account.accountId);
                    if (acc != null) {
                        acc.accessToken = null;
                        acc.tokenExpireAt = null;
                        db.SaveChanges();
                    }
                }

                // 2) 현재 룸 상태에서 정리
                switch (Kind) {
                    case RoomKind.Game:
                        Leave(s);
                        _expectedEnterIds.Remove(s.SessionID);
                        break;

                    case RoomKind.Matchmaking:
                        // 매칭 대기열/타이머 제거
                        _mmEnterMs.Remove(s);
                        break;

                    case RoomKind.Lobby:
                        // 로비는 따로 할 것 없음
                        break;

                    case RoomKind.Login:
                    default:
                        break;
                }

                // 3) 현재 룸에서 제거
                DisAccess(s);

                // 4) 세션 런타임 상태 리셋(계정/게임 캐시 초기화)
                s.Game = new SessionGame();
                s.Account.accountId = string.Empty;
                s.Account.nickName = string.Empty;

                // 5) 로그인 룸으로 이동
                Program.LoginRoom.Access(s);

                // 6) 클라이언트에 통지
                s.Send(new S_LogoutResult { isSuccess = true }.Write());

                Log.Info($"[Logout] SID={s.SessionID} -> LoginRoom");
            } catch (Exception ex) {
                Log.Error($"[Logout] error: {ex.Message}");
                try { s.Send(new S_LogoutResult { isSuccess = false }.Write()); } catch { }
            }
        }

        // ===== 룸 접근 =====
        public void Access(ClientSession s) {
            _sessions.Add(s);
            s.Room = this;

            switch (Kind) {
                case RoomKind.Login: 
                    //Log.Info($"[Access:Login] SID={s.SessionID}"); 
                    break;
                case RoomKind.Lobby: 
                    //Log.Info($"[Access:Lobby] SID={s.SessionID}, Nick={s.Account.nickName}"); 
                    break;
                case RoomKind.Matchmaking: 
                    _mmEnterMs[s] = TimeUtil.NowMs(); 
                    //Log.Info($"[Access:MM:{Mode}] SID={s.SessionID}, Nick={s.Account.nickName}"); 
                    break;
                default: 
                    //Log.Info($"[Access] SID={s.SessionID}"); 
                    break;
            }
        }

        public void DisAccess(ClientSession s) {
            _sessions.Remove(s);
            _mmEnterMs.Remove(s);

            switch (Kind) {
                case RoomKind.Login: 
                    //Log.Info($"[DisAccess:Login] SID={s.SessionID}"); 
                    break;
                case RoomKind.Lobby: 
                    //Log.Info($"[DisAccess:Lobby] SID={s.SessionID}"); 
                    break;
                case RoomKind.Matchmaking: 
                    //Log.Info($"[DisAccess:MM:{Mode}] SID={s.SessionID}"); 
                    break;
                default: 
                    //Log.Info($"[DisAccess] SID={s.SessionID}"); 
                    break;
            }
        }

        public void Enter(ClientSession s, C_EnterGame _unused) {
            // 게임룸에서만 유효
            if (Kind != RoomKind.Game) return;

            _sessions.Add(s);
            s.Room = this;

            _game?.OnPlayerEnter(s);
            s.Send(BuildRosterListFor(s).Write());

            if (_expectedEnterIds.Count > 0) {
                _expectedEnterIds.Remove(s.SessionID);
                if (_expectedEnterIds.Count == 0)
                    ArmCountdownAndStart();
            }
        }

        public void Leave(ClientSession s) {
            _sessions.Remove(s);
            _game?.OnPlayerLeave(s);
            Broadcast(new S_BroadcastLeaveGame { playerId = s.SessionID }.Write());
        }

        public void Chat(ClientSession s, C_Chat pkt) {
            Log.Info($"[GameChat] {s.Account.nickName}: {pkt.message}");
            Broadcast(new S_BroadcastChat {
                playerId = s.SessionID,
                nickName = s.Account.nickName,
                message = pkt.message
            }.Write());
        }

        public void LobbyChat(ClientSession s, C_LobbyChat pkt) {
            if (pkt.type == 0) {
                Log.Info($"[LobbyChat] [전체] {s.Account.nickName}: {pkt.message}");
            } else if (pkt.type == 1) {
                Log.Info($"[LobbyChat] [그룹] {s.Account.nickName}: {pkt.message}");
            }

            var seg = new S_BroadcastLobbyChat {
                type = pkt.type,            // 0=전체, 1=그룹
                playerId = s.SessionID,
                nickName = s.Account.nickName,
                message = pkt.message
            }.Write();

            if (pkt.type == 0) {
                //Broadcast(seg);
                Program.LobbyRoom.Broadcast(seg);
                Program.MatchmakingSurvive.Broadcast(seg);
                Program.MatchmakingRespawn.Broadcast(seg);
                Program.MatchmakingRank.Broadcast(seg);
            } else if (pkt.type == 1) {
                if (s.GroupRoom != null) {
                    foreach (var m in _sessions) {
                        if (m.GroupRoom == s.GroupRoom) {
                            m.Send(seg);
                        }
                    }
                } else {
                    Log.Error("로비 채팅 오류 : 그룹이 없는데 그룹 채팅 패킷을 전송함.");
                }
            } else {
                Log.Error("로비 채팅 오류 : 예외 발생");
            }
            
        }

        public void Input(ClientSession s, C_Input pkt) {
            if (s.Game.InputInbox.Count > 60) s.Game.InputInbox.Dequeue();
            s.Game.InputInbox.Enqueue(pkt);
        }

        public void OnBotSnapshot(ClientSession sender, C_BotSnapshot pkt) {
            _game?.OnBotSnapshot(sender, pkt);
        }

        public void intoMatchingRoom(ClientSession s, C_TryFindMatch pkt) {
            Log.Info($"[FindMatch] {s.SessionID}.{s.Account.nickName} : {(GameMode)pkt.gameMode}");
        }
        public void cancelMatchingRoom(ClientSession s) {
            Log.Info($"[CancelMatch] {s.SessionID}.{s.Account.nickName}");
        }

        private void TryStartMatchIfReady() {
            if (Kind != RoomKind.Matchmaking)
                return;

            int n = _sessions.Count;
            if (n == 0)
                return;

            long now = TimeUtil.NowMs();

            if (Mode == GameMode.RankSurvive && !LevelData.RankAllowBots) {
                var sorted = new List<ClientSession>(_sessions);
                sorted.Sort((a, b) => {
                    _mmEnterMs.TryGetValue(a, out long ta);
                    _mmEnterMs.TryGetValue(b, out long tb);
                    return ta.CompareTo(tb);
                });

                int maxSlots = LevelData.MAX_PER_ROOM;

                if (n >= maxSlots) {
                    var picked = sorted.GetRange(0, maxSlots);
                    StartGameWith(picked, allowBots: false);
                }
                return;
            }

            var parties = new List<(List<ClientSession> members, long enterMs)>();
            var visited = new HashSet<ClientSession>();

            foreach (var s in _sessions) {
                if (!visited.Add(s))
                    continue;

                List<ClientSession> members;
                var gRoom = s.GroupRoom;

                if (gRoom == null) {
                    // 그룹이 없는 솔로 큐
                    members = new List<ClientSession> { s };
                } else {
                    // 같은 GroupRoom을 가진 대기중 멤버들만 한 파티로 묶음
                    members = _sessions.Where(m => m.GroupRoom == gRoom).ToList();
                    foreach (var m in members)
                        visited.Add(m);
                }

                long enterMs = long.MaxValue;
                foreach (var m in members) {
                    if (_mmEnterMs.TryGetValue(m, out long t) && t < enterMs)
                        enterMs = t;
                }
                if (enterMs == long.MaxValue)
                    enterMs = now; // 방어 코드

                parties.Add((members, enterMs));
            }

            if (parties.Count == 0)
                return;

            // 먼저 누른 파티가 앞에 오도록 정렬
            parties.Sort((a, b) => a.enterMs.CompareTo(b.enterMs));

            int totalPlayers = parties.Sum(p => p.members.Count);

            int minPlayers = LevelData.MIN_START_PLAYERS;
            int maxPlayers = LevelData.MAX_PER_ROOM;

            var oldestParty = parties[0];
            long oldestWait = now - oldestParty.enterMs;

            if (totalPlayers >= minPlayers) {
                var pickedParties = new List<(List<ClientSession> members, long enterMs)>();
                int used = 0;

                foreach (var p in parties) {
                    int sz = p.members.Count;
                    if (used + sz > maxPlayers)
                        continue; // 넣으면 인원 초과

                    pickedParties.Add(p);
                    used += sz;

                    if (used == maxPlayers)
                        break; // 딱 맞으면 종료
                }

                if (pickedParties.Count > 0 && used >= minPlayers) {
                    var pickedSessions = new List<ClientSession>();
                    foreach (var p in pickedParties)
                        pickedSessions.AddRange(p.members);

                    StartGameWith(pickedSessions); // allowBots = 기본값 true
                    return;
                }
            }

            if (totalPlayers == 0)
                return;

            if (oldestWait < LevelData.WAIT_MS_BACKOFF)
                return;

            if (totalPlayers <= maxPlayers) {
                var everyone = new List<ClientSession>();
                foreach (var p in parties)
                    everyone.AddRange(p.members);

                // 사람 수가 MIN_START보다 적어도, 예전 allWaited 분기처럼
                // 오래 기다렸으니 봇으로 채워서라도 시작
                StartGameWith(everyone);
                return;
            }


            {
                var pickedParties2 = new List<(List<ClientSession> members, long enterMs)>();
                int used = 0;

                // 먼저 가장 오래 기다린 파티를 넣음
                pickedParties2.Add(oldestParty);
                used += oldestParty.members.Count;

                foreach (var p in parties) {
                    if (ReferenceEquals(p.members, oldestParty.members))
                        continue; // 이미 넣은 파티

                    int sz = p.members.Count;
                    if (used + sz > maxPlayers)
                        continue;

                    pickedParties2.Add(p);
                    used += sz;

                    if (used == maxPlayers)
                        break;
                }

                if (pickedParties2.Count > 0) {
                    var pickedSessions = new List<ClientSession>();
                    foreach (var p in pickedParties2)
                        pickedSessions.AddRange(p.members);

                    StartGameWith(pickedSessions);
                }
            }
        }


        private static List<(float x, float y, float z)> MakeSpawnRing(int count, float radius, float jitter = 1.25f) {
            var list = new List<(float, float, float)>(count);
            const float TWO_PI = (float)(Math.PI * 2.0);

            for (int i = 0; i < count; i++) {
                float t = (float)i / Math.Max(1, count);
                float ang = t * TWO_PI;
                float x = radius * (float)Math.Cos(ang);
                float z = radius * (float)Math.Sin(ang);
                float jx = RandUtil.Range(-jitter, jitter);
                float jz = RandUtil.Range(-jitter, jitter);
                list.Add((x + jx, 0f, z + jz));
            }
            return list;
        }

        private void StartGameWith(List<ClientSession> picked, bool allowBots = true) {
            if (picked == null || picked.Count == 0) return;

            var gameRoom = new Room(RoomKind.Game, Mode);
            Program.GameRooms.Add(gameRoom);

            foreach (var s in picked) {
                DisAccess(s);
                s.Room = gameRoom;
            }

            // === 봇 채우기 ===
            int human = picked.Count;

            //int needBots = Math.Max(0, LevelData.MAX_PER_ROOM - human);
            int needBots = allowBots? Math.Max(0, LevelData.MAX_PER_ROOM - human): 0; // 랭크인 경우 0으로 고정

            //int botOwnerId = picked.Min(p => p.SessionID);
            int botOwnerId = (needBots > 0)? picked.Min(p => p.SessionID): -1;

            var newBotIds = new List<int>(needBots);
            for (int i = 0; i < needBots; i++) newBotIds.Add(1000 + i); // 고정 오프셋.

            // === 스폰 위치 ===
            var allSpawns = MakeSpawnRing(human + needBots, LevelData.SPAWN_RADIUS, LevelData.SPAWN_JITTER);
            RandUtil.Shuffle(allSpawns);

            // === 색상(0~9 섞고 10명 초과는 9 고정) ===
            var palette = new List<byte>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            RandUtil.Shuffle(palette);

            var allIds = new List<int>(human + needBots);
            foreach (var s in picked) allIds.Add(s.SessionID);
            foreach (var b in newBotIds) allIds.Add(b);

            var colorOf = new Dictionary<int, byte>(allIds.Count);
            for (int i = 0; i < allIds.Count; i++) {
                byte col = (i < 10) ? palette[i % 10] : (byte)9;
                colorOf[allIds[i]] = col;
            }

            // === 사람 배치/로스터/대기배리어 ===
            int si = 0;
            foreach (var s in picked) {
                var p = allSpawns[si++];
                s.Game.PosX = p.x; s.Game.PosY = p.y; s.Game.PosZ = p.z;
                s.Game.Yaw = MathUtil.YawDegLookAt(s.Game.PosX, s.Game.PosZ, 0f, 0f);
                s.Game.ColorIndex = colorOf[s.SessionID];

                var (ch, tr) = LoadEquippedFor(s.Account.accountId);
                gameRoom._cosmetics[s.SessionID] = (ch, tr);

                gameRoom._roster[s.SessionID] = (s.Account.nickName, s.Game.ColorIndex, s.Game.PosX, s.Game.PosY, s.Game.PosZ, s.Game.Yaw);
                gameRoom._expectedEnterIds.Add(s.SessionID);
            }

            // === 봇 스폰/색상 ===
            var botSpawns = new List<(float x, float y, float z, float yaw)>();
            var botColors = new List<byte>(newBotIds.Count);
            for (int i = 0; i < newBotIds.Count; i++) {
                var p = allSpawns[si++];
                float yaw = MathUtil.YawDegLookAt(p.x, p.z, 0f, 0f);
                botSpawns.Add((p.x, p.y, p.z, yaw));
                botColors.Add(colorOf[newBotIds[i]]);
            }

            // === Game 인스턴스 ===
            var game = new Game(
                mode: gameRoom.Mode,
                getSessions: () => gameRoom.Sessions,
                broadcast: gameRoom.Broadcast,
                push: gameRoom.Push
            );
            gameRoom.AttachGame(game);

            // 봇 보류(전원 입장 후 1회 Init)
            gameRoom._pendingBotIds = newBotIds;
            gameRoom._pendingBotSpawns = botSpawns;
            gameRoom._pendingBotOwnerId = botOwnerId;
            gameRoom._pendingBotColors = botColors;

            // 매치 성사 통지
            foreach (var s in picked) {
                var mf = new S_MatchFound {
                    gameMode = (int)gameRoom.Mode,
                    playerCount = human,
                    botCount = newBotIds.Count,
                    botOwnerId = botOwnerId
                };
                foreach (var id in newBotIds)
                    mf.botIdss.Add(new S_MatchFound.BotIds { botId = id });
                s.Send(mf.Write());
            }

            Log.Info($"[MatchStart] {gameRoom.Mode} : human={human} bots={newBotIds.Count} owner={botOwnerId}");
        }

        private void ArmCountdownAndStart() {
            if (_countdownArmed) return;
            _countdownArmed = true;
            if (_game == null) return;

            if (_pendingBotIds != null && _pendingBotIds.Count > 0)
                _game.SetupBots(_pendingBotOwnerId, _pendingBotIds, _pendingBotSpawns);

            if (_pendingBotIds != null && _pendingBotIds.Count > 0) {
                var init = new S_BotInit();
                for (int i = 0; i < _pendingBotIds.Count; i++) {
                    init.infos.Add(new S_BotInit.Info {
                        botId = _pendingBotIds[i],
                        posX = _pendingBotSpawns[i].x,
                        posY = _pendingBotSpawns[i].y,
                        posZ = _pendingBotSpawns[i].z,
                        yaw = _pendingBotSpawns[i].yaw,
                        colorIndex = (_pendingBotColors != null && i < _pendingBotColors.Count) ? _pendingBotColors[i] : (byte)9
                    });
                }
                Broadcast(init.Write());
            }

            int delayTicks = (int)(LevelData.GAMEREADY_TICK_COUNT * LevelData.TICK_RATE);
            _game.ArmStart(delayTicks);

            Broadcast(new S_GameReady { startServerTick = _game._serverTick + delayTicks }.Write());
            //Log.Info($"[GameReady] startTick={_game._serverTick + delayTicks}");
        }

        private S_PlayerList BuildRosterListFor(ClientSession target) {
            var list = new S_PlayerList();
            foreach (var kv in _roster) {
                int id = kv.Key;
                var r = kv.Value;
                _cosmetics.TryGetValue(id, out var cos);
                string ch = cos.characterSku ?? DEFAULT_CHAR;
                string tr = cos.trailSku ?? DEFAULT_TRAIL;
                list.players.Add(new S_PlayerList.Player {
                    isSelf = (id == target.SessionID),
                    playerId = id,
                    nickName = r.nick,
                    colorIndex = r.color,
                    posX = r.x,
                    posY = r.y,
                    posZ = r.z,
                    characterSku = ch,
                    trailSku = tr
                });
            }
            return list;
        }

        public void BuyOffer(ClientSession s, C_BuyOffer req) {
            // failReason:
            // 0 OK
            // 1 OfferInvalid(없음/기간외/비노출/가격없음/통화잘못됨)
            // 2 LimitExceeded(계정별 제한 도달)
            // 3 NotEnoughCurrency
            // 4 DuplicateIdempotency(이미 처리됨)
            // 5 ServerError

            try {
                var catalog = Store.Catalog;
                if (catalog == null || !catalog.Offers.TryGetValue(req.offerId, out var offer)) {
                    s.Send(new S_BuyResult { isSuccess = false, failReason = 1 }.Write());
                    Log.Warn($"[BuyOffer] Offer not found: {req.offerId}");
                    return;
                }

                // 기간/노출 체크
                var now = DateTime.UtcNow;
                if ((offer.StartUtc > now) || (offer.EndUtc.HasValue && offer.EndUtc.Value < now) || !offer.Visible) {
                    s.Send(new S_BuyResult { isSuccess = false, failReason = 1 }.Write());
                    Log.Warn($"[BuyOffer] Offer not available: {req.offerId}");
                    return;
                }

                // 가격 선택 (여러 개면 첫 번째를 사용 — 필요시 우선순위 로직 추가)
                if (offer.Prices == null || offer.Prices.Count == 0) {
                    s.Send(new S_BuyResult { isSuccess = false, failReason = 1 }.Write());
                    Log.Warn($"[BuyOffer] Offer has no price: {req.offerId}");
                    return;
                }
                var price = offer.Prices[0];
                var currency = (price.Currency ?? "GOLD").ToUpperInvariant();
                var amount = price.Amount;

                if (currency != "GOLD" && currency != "STAR") {
                    s.Send(new S_BuyResult { isSuccess = false, failReason = 1 }.Write());
                    Log.Warn($"[BuyOffer] Invalid currency: {currency}");
                    return;
                }

                using var db = new AppDBContext();

                // 내 계정 로드 (인벤/장착 포함)
                var account = db.Accounts
                    .Include(a => a.Items)
                    .Include(a => a.Equipped)
                    .FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (account == null) {
                    s.Send(new S_BuyResult { isSuccess = false, failReason = 5 }.Write());
                    Log.Error($"[BuyOffer] Account not found: {s.Account.accountId}");
                    return;
                }

                // 멱등키 중복: 이미 같은 idempotencyKey 주문 있으면 성공 간주
                bool duplicate = db.Set<PurchaseOrder>().Any(p =>
                    p.AccountId == account.Id && p.idempotencyKey == req.idempotencyKey);
                if (duplicate) {
                    s.Send(new S_BuyResult { isSuccess = true, failReason = 0 }.Write());
                    Log.Info($"[BuyOffer] Duplicate idempotency (success): {req.idempotencyKey}");
                    return;
                }

                // 계정별 구매 제한(0=무제한)
                if (offer.LimitPerAccount > 0) {
                    int already = db.Set<PurchaseOrder>().Count(p =>
                        p.AccountId == account.Id && p.offerId == req.offerId && p.status == "Completed");
                    if (already >= offer.LimitPerAccount) {
                        s.Send(new S_BuyResult { isSuccess = false, failReason = 2 }.Write());
                        Log.Warn($"[BuyOffer] Limit exceeded: {req.offerId}, already={already}");
                        return;
                    }
                }

                // 잔액 체크
                if (currency == "GOLD" && account.gold < amount) {
                    s.Send(new S_BuyResult { isSuccess = false, failReason = 3 }.Write());
                    Log.Warn($"[BuyOffer] Not enough GOLD: have={account.gold}, need={amount}");
                    return;
                }
                if (currency == "STAR" && account.star < amount) {
                    s.Send(new S_BuyResult { isSuccess = false, failReason = 3 }.Write());
                    Log.Warn($"[BuyOffer] Not enough STAR: have={account.star}, need={amount}");
                    return;
                }

                // 아이템 참조 검증(카탈로그에 없는 sku가 포함되면 실패)
                foreach (var g in offer.Items) {
                    if (!catalog.Items.ContainsKey(g.Sku)) {
                        s.Send(new S_BuyResult { isSuccess = false, failReason = 1 }.Write());
                        Log.Error($"[BuyOffer] Offer references unknown sku: {g.Sku}");
                        return;
                    }
                }

                // === 트랜잭션 ===
                using var tx = db.Database.BeginTransaction();

                // 통화 차감 + 원장 기록
                if (currency == "GOLD") account.gold -= amount;
                else account.star -= amount;

                var order = new PurchaseOrder {
                    AccountId = account.Id,
                    offerId = req.offerId,
                    priceCurrency = currency,
                    priceAmount = amount,
                    status = "Completed",
                    idempotencyKey = req.idempotencyKey,
                    createdAt = now,
                    completedAt = now
                };
                db.Set<PurchaseOrder>().Add(order);

                var ledger = new CurrencyLedger {
                    AccountId = account.Id,
                    currency = currency,
                    delta = -amount,
                    balanceAfter = (currency == "GOLD") ? account.gold : account.star,
                    reason = "PURCHASE",
                    refType = "ORDER",
                    refId = order.Id.ToString(),
                    createdAt = now
                };
                db.Set<CurrencyLedger>().Add(ledger);

                // 아이템 지급(스택형: 수량 증가 / 비스택: 보유=1로)
                foreach (var g in offer.Items) {
                    // 닉변권은 즉발형: 인벤토리에 쌓지 않는다
                    if (g.Sku == "ETC_CHANGE_NAME")
                        continue;

                    var own = account.Items.FirstOrDefault(i => i.sku == g.Sku);
                    int addQty = Math.Max(1, g.Qty);
                    if (own == null) {
                        own = new AccountItem {
                            AccountId = account.Id,
                            sku = g.Sku,
                            quantity = addQty,
                            createdAt = now
                        };
                        db.Set<AccountItem>().Add(own);
                    } else {
                        own.quantity += addQty;
                    }
                }

                db.SaveChanges();
                tx.Commit();

                // 결과 통지
                s.Send(new S_BuyResult { isSuccess = true, failReason = 0 }.Write());

                // 클라 갱신
                var store = StorePacketBuilder.BuildFor(s);
                s.Send(store.Write());


                // Log.Info($"[BuyOffer] OK: {req.offerId} {currency} {amount} acc={account.accountId}");
                Log.Info($"[BuyOffer Success] {account.nickName} : {req.offerId} : {currency} {amount}");
                if (req.offerId == "ETC_CHANGE_NAME") {
                    account.isNickSet = false;
                    db.SaveChanges();
                    S_BuyChangeName cp = new S_BuyChangeName();
                    s.Send(cp.Write());
                } else if (req.offerId == "ETC_BATTLEPASS_S1") {
                    BuyBattlePass(s);
                }
            } catch (DbUpdateConcurrencyException ex) {
                Log.Error($"[BuyOffer] concurrency: {ex.Message}");
                s.Send(new S_BuyResult { isSuccess = false, failReason = 5 }.Write());
            } catch (Exception ex) {
                Log.Error($"[BuyOffer] error: {ex.Message}");
                s.Send(new S_BuyResult { isSuccess = false, failReason = 5 }.Write());
            }
        }

        public void EquipItem(ClientSession s, C_EquipItem req) {
            try {
                if (s == null || s.Account == null) return;

                // 1) 슬롯 검증 (락커룸: character | trail | dance)
                string slot = (req.slot ?? "").Trim().ToLowerInvariant();
                HashSet<string> ValidSlots = new(StringComparer.OrdinalIgnoreCase) { "character","trail","dance1","dance2","dance3","dance4" };

                if (!ValidSlots.Contains(slot)) {
                    s.Send(new S_EquipResult { isSuccess = false, failReason = 1, isDetach = false }.Write()); // Invalid slot
                    return;
                }
                //if (slot != "character" && slot != "trail" && slot != "dance") {
                //    s.Send(new S_EquipResult { isSuccess = false, failReason = 1 }.Write()); // Invalid slot
                //    return;
                //}

                // 2) SKU/카탈로그 검증 + 카테고리-슬롯 일치 검사
                string sku = (req.sku ?? "").Trim();

                // 해제 처리: sku가 빈 값이면 해당 슬롯 장착 삭제
                if (string.IsNullOrEmpty(sku)) {
                    using var db2 = new AppDBContext();
                    var account2 = db2.Accounts
                        .Include(a => a.Equipped)
                        .FirstOrDefault(a => a.accountId == s.Account.accountId);
                    if (account2 == null) {
                        s.Send(new S_EquipResult { isSuccess = false, failReason = 5, isDetach = false }.Write());
                        return;
                    }

                    var eq2 = account2.Equipped.FirstOrDefault(e => e.slot == slot);
                    if (eq2 != null) {
                        db2.Set<EquippedItem>().Remove(eq2);
                        db2.SaveChanges();
                    }
                    s.Game.EquippedBySlot.Remove(slot);
                    s.Send(new S_EquipResult { isSuccess = true, failReason = 0, isDetach = true }.Write());
                    s.Send(StorePacketBuilder.BuildFor(s).Write());
                    return;
                }

                var catalog = Store.Catalog;
                if (catalog == null || !catalog.Items.TryGetValue(sku, out var itemDef)) {
                    s.Send(new S_EquipResult { isSuccess = false, failReason = 1, isDetach = false }.Write()); // Invalid sku
                    return;
                }
                var cat = (itemDef.Category ?? "misc").ToLowerInvariant();
                bool slotMatches =
                    (slot == "character" && cat == "character") ||
                    (slot == "trail" && cat == "trail") ||
                    (slot.StartsWith("dance") && cat == "dance");
                if (!slotMatches) {
                    s.Send(new S_EquipResult { isSuccess = false, failReason = 1, isDetach = false }.Write()); // Slot/category mismatch
                    return;
                }

                using var db = new AppDBContext();
                // 3) 계정 + 소유/장착 로드
                var account = db.Accounts
                    .Include(a => a.Items)
                    .Include(a => a.Equipped)
                    .FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (account == null) {
                    s.Send(new S_EquipResult { isSuccess = false, failReason = 5, isDetach = false }.Write()); // ServerError
                    return;
                }

                // 4) 소유 확인
                bool owned = account.Items.Any(i => i.sku == sku && i.quantity > 0);
                if (!owned) {
                    s.Send(new S_EquipResult { isSuccess = false, failReason = 2, isDetach = false }.Write()); // Not owned
                    return;
                }

                // 5) Upsert: EquippedItems (AccountId+slot 유니크)
                var eq = account.Equipped.FirstOrDefault(e => e.slot == slot);
                if (eq == null) {
                    eq = new EquippedItem {
                        AccountId = account.Id,
                        slot = slot,
                        sku = sku,
                        updatedAt = DateTime.UtcNow
                    };
                    db.Set<EquippedItem>().Add(eq);
                } else {
                    eq.sku = sku;
                    eq.updatedAt = DateTime.UtcNow;
                }

                db.SaveChanges();
                s.Game.EquippedBySlot[slot] = sku;

                // 6) 성공 응답 + 최신 상태 스냅샷(지갑/인벤/장착/카탈로그)
                s.Send(new S_EquipResult { isSuccess = true, failReason = 0, isDetach = false }.Write());
                var store = StorePacketBuilder.BuildFor(s);
                s.Send(store.Write());
            } catch (Exception ex) {
                Log.Error($"[EquipItem] error: {ex.Message}");
                s.Send(new S_EquipResult { isSuccess = false, failReason = 5, isDetach = false }.Write()); // ServerError
            }
        }

        private (string ch, string tr) LoadEquippedFor(string accountId) {
            using var db = new AppDBContext();
            var acc = db.Accounts
                .Include(a => a.Equipped)
                .FirstOrDefault(a => a.accountId == accountId);

            if (acc == null) return (DEFAULT_CHAR, DEFAULT_TRAIL);

            // slot 이름은 locker와 동일: "character","trail","dance"
            string ch = acc.Equipped.FirstOrDefault(e => e.slot == "character")?.sku ?? DEFAULT_CHAR;
            string tr = acc.Equipped.FirstOrDefault(e => e.slot == "trail")?.sku ?? DEFAULT_TRAIL;
            return (ch, tr);
        }

        public void InviteGroup(ClientSession s, C_InviteGroup pkt) {
            bool madeBecauseRoomNull = false;
            bool InviteAvailable = true;
            int failReason = 0;

            if (s.GroupRoom == null) {
                CreateGroup(s);
                madeBecauseRoomNull = true;
            }

            if (s.GroupRoom != null && s.GroupRoom._sessions.Count >= LevelData.MAX_PER_ROOM) {
                failReason = 1;
                InviteAvailable = false;
            }

            // 이미 초대를 받고 있는 사람인지 체크
            if (InviteAvailable && this == Program.LobbyRoom) {
                long now = TimeUtil.NowMs();

                bool alreadyInvited = _groupInvites.Any(inv =>
                    !inv.consumed &&                // 아직 사용 안 됐고
                    inv.targetNick == pkt.ServentNickName && // 이 사람한테 온 초대이고
                    inv.expireMs > now);            // 아직 안 만료된 초대

                if (alreadyInvited) {
                    InviteAvailable = false;
                    failReason = 6; // 6 = 이미 다른 초대를 받고 있는 중
                }
            }

            bool foundUser = false;
            if (InviteAvailable) {
                foreach (var k in _sessions) {
                    if (k.Account.nickName == pkt.ServentNickName) {
                        if (k.GroupRoom == null) {
                            if (!k.isGaming) {
                                // 게임 안하고 있을때만
                                S_RequestInviteGroup reqPkt = new S_RequestInviteGroup();
                                reqPkt.InviterNickName = pkt.LeaderNickName;
                                k.Send(reqPkt.Write());

                                // 초대 성공: 10초 유효 초대 등록 (로비룸 기준)
                                if (this == Program.LobbyRoom) {
                                    _groupInvites.Add(new GroupInvite {
                                        inviterNick = pkt.LeaderNickName,
                                        targetNick = pkt.ServentNickName,
                                        expireMs = TimeUtil.NowMs() + 10_000, // 10초
                                        consumed = false
                                    });
                                }
                            } else {
                                InviteAvailable = false;
                                failReason = 2;
                            }
                        } else {
                            // 그룹이 있으면
                            InviteAvailable = false;
                            failReason = 3;
                        }
                        foundUser = true;
                        break;
                    }
                }
            }

            if (!foundUser) {
                InviteAvailable = false;
                failReason = 4;
            }

            // 초대 불가능할때
            if (!InviteAvailable) {
                S_InviteGroupResult ps = new S_InviteGroupResult();
                ps.isAvailable = InviteAvailable;
                ps.isAccepted = false;
                ps.failReason = failReason;
                ps.InviterNickName = pkt.LeaderNickName;
                ps.replierNickName = pkt.ServentNickName;
                s.Send(ps.Write());
                if (s.GroupRoom != null && madeBecauseRoomNull) {
                    Program.ChatRooms.Remove(s.GroupRoom);
                    if (s.GroupRoom != null) {
                        s.GroupRoom = null;
                    }
                }
            }

            Log.Info($"[InviteGroup] {s.Account.nickName} invited group to {pkt.ServentNickName} : isAvailable:{InviteAvailable}, failReason:{failReason}");
        }

        public void ReplyInviteGroup(ClientSession s, C_ReplyInviteGroup pkt) {
            
            if (this == Program.LobbyRoom) {
                if (_groupInvites.FirstOrDefault(x => x.inviterNick == pkt.InviterNickName && x.targetNick == pkt.replierNickName) is not GroupInvite inv) {
                    // 못 찾은 경우
                    return;
                }

                long now = TimeUtil.NowMs();

                if (inv == null || now > inv.expireMs) {
                    // 이미 만료된 초대
                    Log.Info($"[ReplyInviteGroup] expired invite {pkt.InviterNickName} -> {pkt.replierNickName}");

                    var timeout = new S_InviteGroupResult {
                        isAvailable = false,
                        isAccepted = false,
                        failReason = 5, // 초대 시간 만료
                        InviterNickName = pkt.InviterNickName,
                        replierNickName = pkt.replierNickName
                    };

                    var inviterSession = _sessions.FirstOrDefault(k => k.Account.nickName == pkt.InviterNickName);
                    if (inviterSession != null)
                        inviterSession.Send(timeout.Write());

                    if (inv != null) {
                        inv.consumed = true;
                        _groupInvites.Remove(inv);
                    }

                    return;
                }

                // 살아 있는 초대라면 소비
                inv.consumed = true;
                _groupInvites.Remove(inv);
            }

            S_InviteGroupResult p = new S_InviteGroupResult();
            p.isAvailable = true;
            if (pkt.isAccept) {
                JoinGroup(s, pkt.InviterNickName);
                p.isAccepted = true;
            } else {
                p.isAccepted = false;
            }
            p.InviterNickName = pkt.InviterNickName;
            p.replierNickName = pkt.replierNickName;
            p.failReason = 0;
            foreach (var k in _sessions) {
                if (k.Account.nickName == pkt.InviterNickName) {
                    k.Send(p.Write());
                    Log.Info($"[ReplyInviteGroup] {s.Account.nickName} accepted to for him inviting {pkt.InviterNickName}'s group");
                    return;
                }
            }
            Log.Error($"[ReplyInviteGroup] error: 초대자의 닉네임을 찾을 수 없습니다.");
        }

        private void CreateGroup(ClientSession s) {
            var groupRoom = new Room(RoomKind.Group);
            Program.ChatRooms.Add(groupRoom);

            groupRoom._sessions.Add(s);
            s.GroupRoom = groupRoom;
            s.GroupRoom.groupLeaderName = s.Account.nickName;

            Log.Info($"[CreateGroup] {s.Account.nickName} created a new group");
        }

        private void JoinGroup(ClientSession s, string inviterName) {
            // 1) 리더 찾고 같은 그룹으로 편입
            var inviter = _sessions.FirstOrDefault(k =>
                string.Equals(k.Account.nickName, inviterName, StringComparison.Ordinal));
            if (inviter == null) return; // 리더 없으면 처리 종료(원하면 예외/로그 등)
            s.GroupRoom = inviter.GroupRoom;
            inviter.GroupRoom._sessions.Add(s);

            var groupM = new List<ClientSession>();
            var group = new S_GroupUpdate();
            group.isDestroy = false;

            using var db = new AppDBContext();

            // === 그룹 멤버들의 accountId(문자열) 수집 → 현재 시즌 rank 일괄 로드 ===
            var accountIds = s.GroupRoom._sessions
                .Select(m => m.Account.accountId)
                .Distinct()
                .ToList();

            // DB의 Account.Id(정수) 매핑
            var accRows = db.Accounts
                .Where(a => accountIds.Contains(a.accountId))
                .Select(a => new { a.Id, a.accountId, a.level })
                .ToList();

            var accIdInts = accRows.Select(a => a.Id).ToList();

            // 현재 시즌 랭크/점수 일괄 로드
            var statRows = db.Set<SeasonStat>()
                .Where(st => st.isCurrent && accIdInts.Contains(st.AccountId))
                .Select(st => new { st.AccountId, st.rank, st.rankScore })
                .ToList();

            // accountId(문자열) -> (rank, score) 딕셔너리
            var rankByAccountId = accRows.ToDictionary(
                a => a.accountId,
                a => {
                    var st = statRows.FirstOrDefault(x => x.AccountId == a.Id);
                    byte r = st != null ? st.rank : (byte)0;
                    int sc = st != null ? st.rankScore : 0;
                    return (rank: r, score: sc);
                });

            var levelByAccountId = accRows.ToDictionary(a => a.accountId, a => a.level);

            foreach (var groupMember in s.GroupRoom._sessions) {
                groupM.Add(groupMember);
                bool isLeader = string.Equals(groupMember.Account.nickName, s.GroupRoom.groupLeaderName, StringComparison.Ordinal);

                var (rk, rks) = rankByAccountId.TryGetValue(groupMember.Account.accountId, out var v)
                        ? v : ((byte)0, 0);

                int lv = levelByAccountId.TryGetValue(groupMember.Account.accountId, out var lv0) ? lv0 : 1;

                if (groupMember.Room == s.Room) {
                    group.players.Add(new S_GroupUpdate.Player {
                        isLobby = true,
                        isLeader = isLeader,
                        playerId = groupMember.SessionID,
                        nickName = groupMember.Account.nickName,
                        rank = rk,
                        level = lv
                    });
                } else {
                    group.players.Add(new S_GroupUpdate.Player {
                        isLobby = false,
                        isLeader = isLeader,
                        playerId = groupMember.SessionID,
                        nickName = groupMember.Account.nickName,
                        rank = rk,
                        level = lv
                    });
                }
            }

            // 3) 같은 그룹에게만 전송
            foreach (var m in groupM) {
                m.Send(group.Write());
                S_BroadcastJoinGroup joinpkt = new S_BroadcastJoinGroup();
                joinpkt.joinnerNickName = s.Account.nickName;
                m.Send(joinpkt.Write());
            }

            Log.Info($"[JoinGroup] {s.Account.nickName} joined to {inviterName}'s Group");
        }

        public void BroadcastGroupUpdate(ClientSession s) {
            var members = s.GroupRoom._sessions.ToList();
            var lobbyMembers = members.Where(m => m.Room == Program.LobbyRoom).ToList();
            var group = new S_GroupUpdate();

            using var db = new AppDBContext();

            var accountIds = members.Select(m => m.Account.accountId).Distinct().ToList();
            
            var accRows = db.Accounts
                .Where(a => accountIds.Contains(a.accountId))
                .Select(a => new { a.Id, a.accountId, a.level })
                .ToList();

            var accIdInts = accRows.Select(a => a.Id).ToList();

            var statRows = db.Set<SeasonStat>()
                .Where(st => st.isCurrent && accIdInts.Contains(st.AccountId))
                .Select(st => new { st.AccountId, st.rank, st.rankScore })
                .ToList();

            var rankByAccountId = accRows.ToDictionary(
                a => a.accountId,
                a => {
                    var st = statRows.FirstOrDefault(x => x.AccountId == a.Id);
                    byte r = st != null ? st.rank : (byte)0;
                    int sc = st != null ? st.rankScore : 0;
                    return (rank: r, score: sc);
                });

            var levelByAccountId = accRows.ToDictionary(a => a.accountId, a => a.level);

            foreach (var m in members) {
                bool isLeader = string.Equals(m.Account.nickName, s.GroupRoom.groupLeaderName, StringComparison.Ordinal);
                var (rk, rks) = rankByAccountId.TryGetValue(m.Account.accountId, out var v)
                        ? v : ((byte)0, 0);

                int lv = levelByAccountId.TryGetValue(m.Account.accountId, out var lv0) ? lv0 : 1;


                if (m.Room == Program.LobbyRoom) {
                    group.players.Add(new S_GroupUpdate.Player {
                        isLobby = true,
                        isLeader = isLeader,
                        playerId = m.SessionID,
                        nickName = m.Account.nickName,
                        rank = rk,
                        level = lv
                    });
                } else {
                    group.players.Add(new S_GroupUpdate.Player {
                        isLobby = false,
                        isLeader = isLeader,
                        playerId = m.SessionID,
                        nickName = m.Account.nickName,
                        rank = rk,
                        level = lv
                    });
                }
            }
            foreach (var m in lobbyMembers) {
                m.Send(group.Write());
            }
        }

        public void GroopintoMatchingRoom(ClientSession s, C_TryFindMatch pkt) {
            // 리더만
            if (string.Equals(s.GroupRoom.groupLeaderName, s.Account.nickName, StringComparison.Ordinal)) {
                foreach (var groopMember in s.GroupRoom._sessions) {
                    groopMember.isGaming = true;
                    Program.LobbyRoom.Push(() => Program.LobbyRoom.DisAccess(groopMember));
                    var mode = (GameMode)pkt.gameMode;
                    switch (mode) {
                        case GameMode.Survive: Program.MatchmakingSurvive.Push(() => Program.MatchmakingSurvive.Access(groopMember)); break;
                        case GameMode.Respawn: Program.MatchmakingRespawn.Push(() => Program.MatchmakingRespawn.Access(groopMember)); break;
                        case GameMode.RankSurvive: Program.MatchmakingRank.Push(() => Program.MatchmakingRank.Access(groopMember)); break;
                    }
                    if (!string.Equals(s.GroupRoom.groupLeaderName, groopMember.Account.nickName, StringComparison.Ordinal)) {
                        groopMember.Send(new S_GroupLeaderFindMatch { }.Write());
                    }
                }
            }
        }

        public void GroopouttoMatchingRoom(ClientSession s) {
            // 리더만
            if (string.Equals(s.GroupRoom.groupLeaderName, s.Account.nickName, StringComparison.Ordinal)) {
                foreach (var groopMember in s.GroupRoom._sessions) {
                    groopMember.isGaming = false;

                    // 변경: 원래방 -> 로비 순서 보장
                    MoveRoom(groopMember, Program.LobbyRoom);

                    if (!string.Equals(s.GroupRoom.groupLeaderName, groopMember.Account.nickName, StringComparison.Ordinal)) {
                        groopMember.Send(new S_GroupLeaderCancelMatch { }.Write());
                    }
                }
            }
        }

        static void MoveRoom(ClientSession s, Room dest) {
            var from = s.Room;
            if (from == null || dest == null || from == dest) return;

            // 순서 보장: 원래 방 큐에서 DisAccess -> dest 큐에서 Access
            from.Push(() => {
                // 여전히 원래 방에 붙어있는지 방어적 확인
                if (s.Room == from)
                    from.DisAccess(s);
                dest.Push(() => dest.Access(s));
            });
        }

        public void ExitGroup(ClientSession s) {
            // 이 메서드는 "로비룸"에서만 호출된다고 가정
            var originalGroup = s.GroupRoom;
            if (originalGroup == null) {
                Log.Warn($"[ExitGroup] SID={s.SessionID} group=null");
                return;
            }

            // 본인 먼저 그룹 해제
            s.GroupRoom = null;
            originalGroup._sessions.Remove(s);

            var members = originalGroup._sessions.ToList();

            S_BroadcastLeaveGroup leavepkt = new S_BroadcastLeaveGroup();
            leavepkt.leaverNickName = s.Account.nickName;

            // 리더 재지정(리더가 나갔으면 첫 멤버를 새 리더로)
            if (string.Equals(originalGroup.groupLeaderName, s.Account.nickName, StringComparison.Ordinal)) {
                leavepkt.isLeader = true;
                if (members.Count > 0) {
                    leavepkt.newLeaderNickName = members[0].Account.nickName;
                    originalGroup.groupLeaderName = members[0].Account.nickName;
                } else {
                    originalGroup.groupLeaderName = string.Empty; // 그룹 공석
                }   
            } else {
                leavepkt.isLeader = false;
                leavepkt.newLeaderNickName = string.Empty;
            }

            // 브로드캐스트 패킷 구성
            var group = new S_GroupUpdate();
            foreach (var m in members) {
                bool isLeader = string.Equals(m.Account.nickName, originalGroup.groupLeaderName, StringComparison.Ordinal);
                if (m.Room == this) {
                    group.players.Add(new S_GroupUpdate.Player {
                        isLobby = true,
                        isLeader = isLeader,
                        playerId = m.SessionID,
                        nickName = m.Account.nickName,
                    });
                } else {
                    group.players.Add(new S_GroupUpdate.Player {
                        isLobby = false,
                        isLeader = isLeader,
                        playerId = m.SessionID,
                        nickName = m.Account.nickName,
                    });
                }
                Log.Info($"[ExitGroup] remain={m.Account.nickName} leader={isLeader}");
            }

            var dest = false;
            if (members.Count <= 1) {
                group.isDestroy = true;
            } else {
                group.isDestroy = false;
            }
            dest = group.isDestroy;

            // 같은 그룹에게만 전송
            foreach (var m in members) {
                m.Send(group.Write());
                m.Send(leavepkt.Write());
            }

            // 남은 멤버가 없으면 그룹 삭제(선택)
            if (dest) {
                foreach (var m in members) {
                    m.GroupRoom = null;
                }
                Program.ChatRooms.Remove(originalGroup);
                Log.Info($"[ExitGroup] group destroyed");
            }
        }

        private static bool IsNickOnlineByNick(string nick) {
            if (string.IsNullOrWhiteSpace(nick))
                return false;

            try {
                // 고정 룸
                if (Program.LoginRoom.Sessions.Any(s => s.Account.nickName == nick)) return true;
                if (Program.LobbyRoom.Sessions.Any(s => s.Account.nickName == nick)) return true;
                if (Program.MatchmakingSurvive.Sessions.Any(s => s.Account.nickName == nick)) return true;
                if (Program.MatchmakingRespawn.Sessions.Any(s => s.Account.nickName == nick)) return true;
                if (Program.MatchmakingRank.Sessions.Any(s => s.Account.nickName == nick)) return true;

                // 동적 게임 룸
                foreach (var r in Program.GameRooms) {
                    if (r.Sessions.Any(s => s.Account.nickName == nick))
                        return true;
                }

                // 그룹/채팅 룸
                foreach (var r in Program.ChatRooms) {
                    if (r.Sessions.Any(s => s.Account.nickName == nick))
                        return true;
                }

                return false;
            } catch {
                // 어떤 예외가 나더라도 온라인 정보 때문에 터지면 안 되니까 방어적으로 false
                return false;
            }
        }

        public void GiveUserInformation(ClientSession s, C_RequestUserInformation pkt) {
            try {
                string targetNick = string.IsNullOrWhiteSpace(pkt.nickName)
                    ? s.Account.nickName
                    : pkt.nickName;

                using var db = new AppDBContext();

                var acc = db.Accounts.FirstOrDefault(a => a.nickName == targetNick);

                var resp = new S_UserInformation();

                // 기본 elem (못 찾았을 때도 이걸 하나 넣어줌)
                var elem = new S_UserInformation.Player {
                    nickName = targetNick,
                    level = 0,
                    totalGames = 0,
                    totalWins = 0,
                    WinPercentage = 0f,
                    avgRank = 0f,
                    avgKill = 0f,
                    tearRank = 0,
                    tearRankScore = 0
                };

                bool ok = (acc != null);

                if (ok) {
                    // 현재 시즌만
                    var cur = db.Set<SeasonStat>()
                                .FirstOrDefault(st => st.AccountId == acc.Id && st.isCurrent);

                    if (cur != null) {
                        // 레벨은 Account 테이블 기준 사용 (안전)
                        elem.level = acc.level;
                        elem.totalGames = cur.totalGames;
                        elem.totalWins = cur.winCount;

                        elem.WinPercentage = (cur.totalGames > 0)
                            ? (float)((double)cur.winCount / cur.totalGames * 100.0)
                            : 0f;

                        elem.avgRank = (cur.totalGames > 0)
                            ? (float)((double)cur.totalRankSum / cur.totalGames)
                            : 0f;

                        elem.avgKill = (cur.totalGames > 0)
                            ? (float)((double)cur.totalKills / cur.totalGames)
                            : 0f;

                        elem.tearRank = (int)cur.rank;      // byte -> int
                        elem.tearRankScore = cur.rankScore;
                    } else {
                        // 시즌 통계가 아직 없으면, 레벨만 채워주고 나머지는 0 유지
                        elem.level = acc.level;
                    }
                }

                // 온라인 여부 (DB에 없어도 닉으로 세션 찾아볼 수 있으니 그냥 체크)
                bool isOnline = IsNickOnlineByNick(targetNick);

                resp.isSuccess = ok;
                resp.failReason = ok ? 0 : 1;   // 1 = 유저 없음
                resp.isOnline = isOnline;

                resp.players.Add(elem);
                s.Send(resp.Write());
            } catch (Exception ex) {
                Log.Error($"[GiveUserInformation] error: {ex.Message}");

                var fallback = new S_UserInformation {
                    isSuccess = false,
                    failReason = 2, // 2 = 서버 에러
                    isOnline = false
                };

                // 최대한 안전하게 기본 한 칸 채워서 보냄
                fallback.players.Add(new S_UserInformation.Player {
                    nickName = string.IsNullOrWhiteSpace(pkt.nickName) ? s.Account.nickName : pkt.nickName,
                    level = 0,
                    totalGames = 0,
                    totalWins = 0,
                    WinPercentage = 0f,
                    avgRank = 0f,
                    avgKill = 0f,
                    tearRank = 0,
                    tearRankScore = 0
                });

                try { s.Send(fallback.Write()); } catch { }
            }
        }


        public void GetRecentGames(ClientSession s, C_RequestRecentGames req) {
            try {
                string targetNick = string.IsNullOrWhiteSpace(req.nickName) ? s.Account.nickName : req.nickName;

                using var db = new AppDBContext();
                var acc = db.Accounts.FirstOrDefault(a => a.nickName == targetNick);
                var resp = new S_RecentGames();

                if (acc != null) {
                    var rows = db.RecentGames
                                 .Where(r => r.AccountId == acc.Id)
                                 .OrderByDescending(r => r.startedAt)
                                 .Take(20)
                                 .ToList();

                    foreach (var r in rows) {
                        int startedAtSec = (int)new DateTimeOffset(DateTime.SpecifyKind(r.startedAt, DateTimeKind.Utc))
                        .ToUnixTimeSeconds();
                        resp.gamess.Add(new S_RecentGames.Games {
                            mode = r.mode,
                            rank = r.rank,
                            kills = r.kills,
                            deaths = r.deaths,
                            startedAtMs = startedAtSec
                        });
                    }
                }

                s.Send(resp.Write());
            } catch (Exception ex) {
                Log.Error($"[GetRecentGames] error: {ex.Message}");
                // 실패 시에도 빈 리스트 전송
                var resp = new S_RecentGames();
                s.Send(resp.Write());
            }
        }


        public void GetLeaderboard(ClientSession s, C_RequestLeaderboard req) {
            try {
                int offset = req.offset;
                int limit = req.limit;

                var (lastSec, page, baseRank) = Server.Infra.LeaderboardService.GetPage(offset, limit);

                var resp = new S_Leaderboard {
                    lastUpdatedSec = lastSec
                };

                int rankNo = baseRank;
                foreach (var r in page) {
                    resp.rowss.Add(new S_Leaderboard.Rows {
                        nickName = r.nickName,
                        tier = r.tier,
                        score = r.score,
                        globalRank = rankNo++,
                        totalGames = r.totalGames,
                        winRate = (float)r.winRate,
                        avgRank = (float)r.avgRank,
                        avgKill = (float)r.avgKill
                    });
                }

                s.Send(resp.Write());
            } catch (Exception ex) {
                Log.Error($"[GetLeaderboard] error: {ex.Message}");
                var resp = new S_Leaderboard { lastUpdatedSec = 0 };
                s.Send(resp.Write());
            }
        }

        public void PlayerUpdate(ClientSession s) {
            try {
                using var db = new AppDBContext();

                // 기본값
                int level = 1;
                int exp = 0;
                int rank = 0;
                int rankScore = 0;

                // 내 계정 로드
                var acc = db.Accounts.FirstOrDefault(a => a.accountId == s.Account.accountId);
                if (acc != null) {
                    level = acc.level;
                    exp = acc.exp;

                    // 현재 시즌 랭크
                    var cur = db.Set<SeasonStat>()
                                .FirstOrDefault(st => st.AccountId == acc.Id && st.isCurrent);
                    if (cur != null) {
                        rank = (int)cur.rank; // byte -> int
                        rankScore = (int)cur.rankScore;
                    }
                }

                var pkt = new S_PlayerUpdate {
                    nickName = s.Account.nickName,
                    level = level,
                    rank = rank,
                    rankScore = rankScore,
                    exp = exp,
                    maxExp = (int)LevelData.MAX_ACCOUNT_EXP
                };
                s.Send(pkt.Write());

                // 여기서 레벨업 알림(PendingLevelUp) 처리
                var lu = s.PendingLevelUp;
                if (lu != null && lu.ToLevel > lu.FromLevel) {
                    s.PendingLevelUp = null; // 한 번 보내고 초기화

                    s.Send(new S_LevelUp {
                        fromLevel = lu.FromLevel,
                        toLevel = lu.ToLevel,
                        rewardGold = lu.RewardGold,
                        rewardStar = lu.RewardStar
                    }.Write());

                    if (acc != null) {
                        BattlePassService.OnAccountLevelUp(db, acc, lu.FromLevel, lu.ToLevel);
                        var pkts = BattlePassPacketBuilder.BuildFor(s);
                        s.Send(pkts.Write());
                    }

                    Log.Info($"[LevelUp] {s.Account.nickName} {lu.FromLevel}->{lu.ToLevel}");
                }


            } catch (Exception ex) {
                Log.Error($"[PlayerUpdate] {ex}");

                // 실패해도 클라가 기다리지 않도록 안전 패킷 전송
                try {
                    var fallback = new S_PlayerUpdate {
                        nickName = s.Account.nickName,
                        level = 1,
                        rank = 0,
                        rankScore = 0,
                        exp = 0,
                        maxExp = (int)LevelData.MAX_ACCOUNT_EXP
                    };
                    s.Send(fallback.Write());
                } catch { /* ignore */ }
            }
        }

        private static void GrantDefaultItemsAndEquip(AppDBContext db, Account account) {
            var catalog = Store.Catalog;
            if (catalog == null) {
                Log.Error("[GrantDefaults] Catalog is null");
                return;
            }

            using var tx = db.Database.BeginTransaction();

            // 1) defaultOwned 아이템 모으기
            var defaults = catalog.Items.Values
                .Where(it => it.DefaultOwned)
                .ToList();

            // 2) 보유 아이템 지급 (이미 있으면 스킵/수량보정)
            foreach (var def in defaults) {
                var own = db.Set<AccountItem>()
                            .FirstOrDefault(i => i.AccountId == account.Id && i.sku == def.Sku);

                if (own == null) {
                    db.Set<AccountItem>().Add(new AccountItem {
                        AccountId = account.Id,
                        sku = def.Sku,
                        quantity = 1,
                        createdAt = DateTime.UtcNow
                    });
                } else if (own.quantity <= 0) {
                    own.quantity = 1; // 혹시 0 이하로 깨진 경우 복구
                }
            }

            // 3) 자동 장착(슬롯 지정된 기본템만)
            var bySlot = defaults
                .Where(d => !string.IsNullOrWhiteSpace(d.EquipSlot))
                .GroupBy(d => d.EquipSlot!)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var kv in bySlot) {
                string slot = (kv.Key ?? "").Trim().ToLowerInvariant();
                if (slot == "dance") slot = "dance1";

                string sku = kv.Value.Sku;

                bool already = db.Set<EquippedItem>()
                                 .Any(e => e.AccountId == account.Id && e.slot == slot);

                if (!already) {
                    db.Set<EquippedItem>().Add(new EquippedItem {
                        AccountId = account.Id,
                        slot = slot,
                        sku = sku,
                        updatedAt = DateTime.UtcNow
                    });
                }
            }

            db.SaveChanges();
            tx.Commit();
        }








        ///////// 스팀 ///////////

        public void SteamLogin(ClientSession session, C_SteamLogin packet) {
            _ = Task.Run(async () =>
            {
                ulong? steamId = null;
                string? err = null;

                try {
                    (steamId, err) = await Server.Infra.SteamAuthService.AuthenticateTicketAsync(packet.ticketHex);
                } catch (Exception ex) {
                    err = ex.Message;
                    Log.Error($"[SteamLogin] Steam verify http error: {ex.Message}");
                }

                this.Push(() =>
                {
                    if (steamId == null) {
                        session.Send(new S_SteamLoginResult {
                            isSuccess = false,
                            failReason = 1,
                            accountId = "",
                            nickName = "",
                            needNickSetup = false,
                            needPolicyAgreement = false
                        }.Write());

                        Log.Warn($"[SteamLogin Failed] {err ?? "verify fail"}");
                        return;
                    }

                    try {
                        using var db = new AppDBContext();
                        long steamLong = unchecked((long)steamId.Value);

                        var account = db.Accounts.FirstOrDefault(a => a.steamId64 == steamLong);

                        bool isNew = false;

                        // ===== 신규 스팀 계정 생성 =====
                        if (account == null) {
                            isNew = true;

                            // 1) 임시 닉네임 생성 (유니크 보장)
                            string baseNick = string.IsNullOrWhiteSpace(packet.personaName)
                                ? $"SteamUser{steamId.Value % 10000}"
                                : packet.personaName.Trim();

                            string tempNick = MakeUniqueNick(db, baseNick, steamId.Value);
                            string accId = MakeUniqueAccountId(db, steamId.Value);

                            account = new Account {
                                accountId = accId,
                                passwordHash = "",      // 스팀 계정은 pw 없음
                                nickName = tempNick,    // 임시 닉
                                steamId64 = steamLong,
                                recentIp = session.Account.IPAddress,
                                level = 1,
                                exp = 0,
                                gold = 0,
                                star = 0,
                                isNickSet = false, // 최초 닉네임 미확정 상태로 저장
                                banStatus = AccountBan.None 
                            };

                            db.Accounts.Add(account);
                            db.SaveChanges();

                            try {
                                GrantDefaultItemsAndEquip(db, account);
                            } catch (Exception ex) {
                                Log.Error($"[SteamLogin] default grants failed: {ex.Message}");
                            }

                            try {
                                StatProfiler.GetOrCreateCurrentSeason(db, account);
                            } catch (Exception ex) {
                                Log.Error($"[SteamLogin] ensure season failed: {ex.Message}");
                            }

                            //Log.Info($"[SteamLogin Register] steam={steamId} accId={accId} nick(temp)={tempNick}");
                            Log.Info($"[SteamLogin Register] {tempNick}");
                        }

                        // 밴 상태 체크 (이상감지/밴확정이면 로그인 거부)
                        if (account.banStatus == AccountBan.Suspicious || account.banStatus == AccountBan.Banned) {
                            // failReason:
                            // 3 = 이상 감지로 로그인 제한 (이상감지 검토중)
                            // 4 = 밴 확정 계정
                            int reason = account.banStatus == AccountBan.Suspicious ? 3 : 4;

                            session.Send(new S_SteamLoginResult {
                                isSuccess = false,
                                failReason = reason,
                                accountId = "",
                                nickName = "",
                                needNickSetup = false,
                                needPolicyAgreement = false
                            }.Write());

                            Log.Warn($"[SteamLogin Blocked] nick={account.nickName}, status={account.banStatus} (reason={reason})");
                            return;
                        }

                        // ===== 로그인 공통 처리 =====
                        account.recentIp = session.Account.IPAddress;
                        db.SaveChanges();

                        session.Account.accountId = account.accountId;
                        session.Account.nickName = account.nickName;

                        bool needNickSetup = (account.isNickSet == false);
                        bool needPolicy = (account.policyVersion < LegalConfig.CURRENT_POLICY_VERSION) || (account.policyAgreedAt == null);

                        session.Send(new S_SteamLoginResult {
                            isSuccess = true,
                            failReason = 0,
                            accountId = account.accountId,
                            nickName = account.nickName,
                            needNickSetup = needNickSetup,
                            needPolicyAgreement = needPolicy
                        }.Write());

                        Log.Info($"[SteamLogin Success] {account.nickName}");

                        Program.LoginRoom.Push(() => Program.LoginRoom.DisAccess(session));
                    } catch (Exception ex) {
                        Log.Error($"[SteamLogin] server error: {ex.Message}");
                        session.Send(new S_SteamLoginResult {
                            isSuccess = false,
                            failReason = 2,
                            accountId = "",
                            nickName = "",
                            needNickSetup = false,
                            needPolicyAgreement = false
                        }.Write());
                    }
                });
            });
        }



        // ---- 유틸: 닉네임/아이디 유니크 보장 ----
        private static string MakeUniqueNick(AppDBContext db, string baseNick, ulong steamId) {
            string nick = baseNick;
            if (!db.Accounts.Any(a => a.nickName == nick))
                return nick;

            // 중복이면 steam 뒤 4자리 붙이기
            string suffix = (steamId % 10000).ToString("D4");
            nick = $"{baseNick}#{suffix}";

            int i = 1;
            while (db.Accounts.Any(a => a.nickName == nick)) {
                nick = $"{baseNick}#{suffix}_{i++}";
            }
            return nick;
        }

        private static string MakeUniqueAccountId(AppDBContext db, ulong steamId) {
            string accId = $"steam_{steamId}";
            if (!db.Accounts.Any(a => a.accountId == accId))
                return accId;

            int i = 1;
            while (db.Accounts.Any(a => a.accountId == $"{accId}_{i}"))
                i++;

            return $"{accId}_{i}";
        }

        bool IsBlockedNick(string nick) {
            if (string.IsNullOrWhiteSpace(nick))
                return true; // 어차피 이런건 막을거니까 true로 둬도 무방

            string lower = nick.ToLowerInvariant();

            foreach (var word in LevelData.BlockedNickWords) {
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                if (lower.Contains(word.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        public void CheckSteamNick(ClientSession session, C_CheckSteamNickName packet) {
            try {
                string wantNick = (packet.nickName ?? "").Trim();

                // 기본 유효성
                if (string.IsNullOrWhiteSpace(wantNick)) {
                    session.Send(new S_CheckSteamNameResult { isSuccess = false }.Write());
                    return;
                }

                // 금지어 필터
                if (IsBlockedNick(wantNick)) {
                    session.Send(new S_CheckSteamNameResult { isSuccess = false }.Write());
                    Log.Info($"[CheckSteamNick] blocked nick={wantNick}");
                    return;
                }

                using var db = new AppDBContext();

                // 내 계정 로드
                var me = db.Accounts.FirstOrDefault(a => a.accountId == session.Account.accountId);
                if (me == null) {
                    session.Send(new S_CheckSteamNameResult { isSuccess = false }.Write());
                    return;
                }

                // 같은 닉을 가진 계정 찾기
                var sameNickAcc = db.Accounts.FirstOrDefault(a => a.nickName == wantNick);

                bool available = true;

                if (sameNickAcc != null) {
                    bool isSelfTemp =
                        (sameNickAcc.Id == me.Id) &&
                        (me.isNickSet == false);

                    if (!isSelfTemp) {
                        available = false;
                    }

                    // if (sameNickAcc.Id != me.Id && sameNickAcc.isNickSet == false)
                    //     available = true;
                }

                session.Send(new S_CheckSteamNameResult { isSuccess = available }.Write());
                Log.Info($"[CheckSteamNick] acc={me.accountId} want={wantNick} ok={available}");
            } catch (Exception ex) {
                Log.Error($"[CheckSteamNick] error: {ex.Message}");
                try {
                    session.Send(new S_CheckSteamNameResult { isSuccess = false }.Write());
                } catch { }
            }
        }

        public void SetSteamNick(ClientSession session, C_SetSteamNickName packet) {
            try {
                string wantNick = (packet.nickName ?? "").Trim();

                if (string.IsNullOrWhiteSpace(wantNick)) {
                    session.Send(new S_SetSteamNameResult { isSuccess = false, nickName = "" }.Write());
                    return;
                }

                // 금지어 필터
                if (IsBlockedNick(wantNick)) {
                    session.Send(new S_SetSteamNameResult { isSuccess = false, nickName = session.Account.nickName }.Write());
                    Log.Info($"[SetSteamNick] blocked nick={wantNick}");
                    return;
                }

                using var db = new AppDBContext();

                var me = db.Accounts.FirstOrDefault(a => a.accountId == session.Account.accountId);
                if (me == null) {
                    session.Send(new S_SetSteamNameResult { isSuccess = false, nickName = "" }.Write());
                    return;
                }

                // 스팀 계정인지 간단 체크(원하면 제거 가능)
                if (me.steamId64 == null) {
                    session.Send(new S_SetSteamNameResult { isSuccess = false, nickName = me.nickName }.Write());
                    return;
                }

                if (me.isNickSet) {
                    // 같은 닉 재확인 요청이면 성공으로 처리해도 되고, 아니면 실패
                    bool same = string.Equals(me.nickName, wantNick, StringComparison.Ordinal);
                    session.Send(new S_SetSteamNameResult {
                        isSuccess = same,
                        nickName = me.nickName
                    }.Write());
                    return;
                }

                // 중복 체크 (내 임시닉 제외)
                var sameNickAcc = db.Accounts.FirstOrDefault(a => a.nickName == wantNick);

                if (sameNickAcc != null) {
                    bool isSelfTemp =
                        (sameNickAcc.Id == me.Id) &&
                        (me.isNickSet == false);

                    if (!isSelfTemp) {
                        session.Send(new S_SetSteamNameResult { isSuccess = false, nickName = me.nickName }.Write());
                        return;
                    }

                }

                // 확정 저장
                string oldNick = me.nickName;

                me.nickName = wantNick;
                me.isNickSet = true;
                me.recentIp = session.Account.IPAddress;

                db.SaveChanges();

                // 런타임 세션 동기화
                session.Account.nickName = wantNick;

                if (session.GroupRoom != null && session.GroupRoom.groupLeaderName == oldNick) {
                    session.GroupRoom.groupLeaderName = wantNick;
                }

                session.Send(new S_SetSteamNameResult {
                    isSuccess = true,
                    nickName = wantNick
                }.Write());

                Log.Info($"[SetSteamNick] acc={me.accountId} {oldNick} -> {wantNick} (fixed)");
            } catch (DbUpdateException ex) {
                // 유니크 인덱스 충돌 등 (레이스)
                Log.Error($"[SetSteamNick] db error: {ex.Message}");
                try {
                    session.Send(new S_SetSteamNameResult { isSuccess = false, nickName = session.Account.nickName }.Write());
                } catch { }
            } catch (Exception ex) {
                Log.Error($"[SetSteamNick] error: {ex.Message}");
                try {
                    session.Send(new S_SetSteamNameResult { isSuccess = false, nickName = session.Account.nickName }.Write());
                } catch { }
            }
        }


        public void OnBuyStars(ClientSession s, C_RequestAddStar pkt) {
            _ = Task.Run(async () => {
                var cfg = Config.Load();

                using var db = new AppDBContext();
                var me = db.Accounts.FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (me == null) {
                    s.Send(new S_BuyStarsResult {
                        isSuccess = false,
                        orderId = 0,
                        transId = 0,
                        packIndex = pkt.packIndex
                    }.Write());
                    Log.Error($"[OnBuyStars] account not found 1");
                    return;
                } else {
                    if (me.steamId64 == null) {
                        s.Send(new S_BuyStarsResult {
                            isSuccess = false,
                            orderId = 0,
                            transId = 0,
                            packIndex = pkt.packIndex
                        }.Write());
                        Log.Error($"[OnBuyStars] account not found 2");
                        return;
                    } else {
                        var steamId = (ulong)me.steamId64; // 네 Account에 이미 있음

                        var r = await SteamMicroTxnService.InitStarsTxnAsync(
                            cfg.Steam.PublisherKey,
                            (uint)cfg.Steam.AppId,
                            steamId,
                            pkt.packIndex
                        );

                        this.Push(() => {
                            if (!r.ok) {
                                // 실패 패킷
                                s.Send(new S_BuyStarsResult { 
                                    isSuccess = false,
                                    orderId = 0,
                                    transId = 0,
                                    packIndex = pkt.packIndex
                                }.Write());
                                Log.Error($"[OnBuyStars] InitStarsTxnAsync Failed: {r.err}");
                                return;
                            }

                            Log.Pay($"[OnBuyStars] {s.Account.nickName} opened pay popup. orderId={r.orderId}, transId={r.transId}, packIndex={pkt.packIndex}");

                            s.Send(new S_BuyStarsResult {
                                isSuccess = true,
                                orderId = (int)r.orderId,
                                transId = unchecked((long)r.transId),
                                packIndex = pkt.packIndex
                            }.Write());
                        });
                    }
                    
                }

                    
            });
        }

        public void ConfirmAddStar(ClientSession s, C_ConfirmAddStar pkt) {
            _ = Task.Run(async () =>
            {
                var cfg = Config.Load();
                using var db = new AppDBContext();
                var me = db.Accounts.FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (me == null || me.steamId64 == null) {
                    this.Push(() => {
                        s.Send(new S_ConfirmAddStarResult {
                            isSuccess = false,
                            packIndex = pkt.packIndex,
                            addedStars = 0,
                            newStarBalance = (int)(me?.star ?? 0)
                        }.Write());
                    });
                    Log.Error($"[ConfirmAddStar] account not found");
                    return;
                }

                var steamId = (ulong)me.steamId64.Value;

                // 스팀에 진짜 승인됐는지 확인
                var qr = await SteamMicroTxnService.QueryTxnAsync(
                    cfg.Steam.PublisherKey,
                    (uint)cfg.Steam.AppId,
                    (ulong)pkt.orderId,
                    unchecked((ulong)pkt.transId)
                );

                this.Push(() =>
                {
                    // 1) Query 실패 or 승인 안됨
                    if (!qr.ok || !qr.isApproved) {
                        Log.Error($"[ConfirmAddStar] QueryTxn fail ok={qr.ok} approved={qr.isApproved} err={qr.error}");
                        s.Send(new S_ConfirmAddStarResult {
                            isSuccess = false,
                            packIndex = pkt.packIndex,
                            addedStars = 0,
                            newStarBalance = (int)me.star
                        }.Write());
                        return;
                    }

                    // 2) DB 재조회 (별도 컨텍스트 추천)
                    using var db2 = new AppDBContext();
                    var acc = db2.Accounts.First(a => a.Id == me.Id);

                    string receiptId = qr.transId.ToString();
                    string idemKey = $"steam:{qr.orderId}";

                    // 3) 이미 처리한 거래인지 체크 (멱등성)
                    bool already = db2.Set<PurchaseOrder>().Any(p =>
                        p.AccountId == acc.Id &&
                        p.receiptProvider == "Steam" &&
                        p.externalReceiptId == receiptId
                    );

                    if (already) {
                        Log.Info($"[ConfirmAddStar] duplicate trans {receiptId}, skip grant");
                        s.Send(new S_ConfirmAddStarResult {
                            isSuccess = true,
                            packIndex = pkt.packIndex,
                            addedStars = 0,
                            newStarBalance = (int)acc.star
                        }.Write());
                        return;
                    }

                    // 4) 몇 스타 줄지 결정
                    int packIndex = (qr.packIndex >= 0) ? qr.packIndex : pkt.packIndex;
                    int stars = SteamMicroTxnService.GetStarsOfPack(packIndex);

                    // 5) 통화(Star) 증가 + 주문/원장 기록
                    acc.star += stars;

                    var order = new PurchaseOrder {
                        AccountId = acc.Id,
                        offerId = $"steam_stars_{packIndex}",
                        priceCurrency = qr.currency,
                        priceAmount = qr.amountCents,
                        status = "Completed",
                        externalReceiptId = receiptId,
                        receiptProvider = "Steam",
                        idempotencyKey = idemKey,
                        createdAt = DateTime.UtcNow,
                        completedAt = DateTime.UtcNow
                    };
                    db2.Set<PurchaseOrder>().Add(order);

                    var ledger = new CurrencyLedger {
                        AccountId = acc.Id,
                        currency = "STAR",
                        delta = stars,
                        balanceAfter = acc.star,
                        reason = "RECHARGE",
                        refType = "STEAM",
                        refId = order.Id.ToString(),
                        createdAt = DateTime.UtcNow
                    };
                    db2.Set<CurrencyLedger>().Add(ledger);

                    db2.SaveChanges();

                    Log.Pay($"[ConfirmAddStar] acc={acc.accountId} +{stars} stars (pack={packIndex})");

                    var store = StorePacketBuilder.BuildFor(s);
                    s.Send(store.Write());

                    // 6) 결과 패킷
                    s.Send(new S_ConfirmAddStarResult {
                        isSuccess = true,
                        packIndex = packIndex,
                        addedStars = stars,
                        newStarBalance = (int)acc.star
                    }.Write());
                });
            });
        }


        public void AgreePolicy(ClientSession s, C_AgreePolicy pkt) {
            try {
                if (string.IsNullOrWhiteSpace(s.Account.accountId)) {
                    s.Send(new S_AgreePolicyResult {
                        isSuccess = false,
                        policyVersion = 0
                    }.Write());
                    return;
                }

                using var db = new AppDBContext();
                var acc = db.Accounts.FirstOrDefault(a => a.accountId == s.Account.accountId);
                if (acc == null) {
                    s.Send(new S_AgreePolicyResult {
                        isSuccess = false,
                        policyVersion = 0
                    }.Write());
                    return;
                }

                int ver = pkt.policyVersion;
                if (ver <= 0) ver = LegalConfig.CURRENT_POLICY_VERSION;

                acc.policyVersion = ver;
                acc.policyAgreedAt = DateTime.UtcNow;
                db.SaveChanges();

                s.Send(new S_AgreePolicyResult {
                    isSuccess = true,
                    policyVersion = ver
                }.Write());

                Log.Info($"[AgreePolicy] {acc.nickName} v={ver} at={acc.policyAgreedAt:O}");
            } catch (Exception ex) {
                Log.Error($"[AgreePolicy] error: {ex.Message}");
                try {
                    s.Send(new S_AgreePolicyResult {
                        isSuccess = false,
                        policyVersion = 0
                    }.Write());
                } catch { }
            }
        }


        public void RequestBattlePass(ClientSession s, C_RequestBattlePass _req) {
            try {
                var pkt = BattlePassPacketBuilder.BuildFor(s);
                s.Send(pkt.Write());
            } catch (Exception ex) {
                Log.Error($"[RequestBattlePass] error: {ex.Message}");
                var fallback = new S_BattlePassInfo {
                    version = 0,
                    bpLevel = 0,
                    hasPremium = false,
                    freeClaimBits = 0,
                    premiumClaimBits = 0
                };
                s.Send(fallback.Write());
            }
        }

        public void ClaimBattlePassReward(ClientSession s, C_ClaimBattlePassReward req) {
            void SendFail(int reason) {
                s.Send(new S_ClaimBattlePassRewardResult {
                    isSuccess = false,
                    failReason = reason,
                    level = req.level,
                    isPremium = req.isPremium
                }.Write());
            }

            try {
                var cfg = BattlePass.Config;
                if (cfg == null) {
                    // 설정 없음
                    SendFail(1);
                    return;
                }

                int level = req.level;
                bool isPremium = req.isPremium;

                // 레벨 범위 체크
                if (level <= 0 || level > cfg.MaxLevel) {
                    SendFail(3);
                    return;
                }

                using var db = new AppDBContext();
                var acc = db.Accounts
                    .Include(a => a.Items)
                    .FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (acc == null) {
                    // 계정 없음
                    SendFail(6);
                    return;
                }

                var state = BattlePassService.GetOrCreateState(db, acc);

                // 레벨 도달 체크
                if (state.level < level) {
                    SendFail(3);
                    return;
                }

                // 프리미엄 필요
                if (isPremium && !state.hasPremium) {
                    SendFail(2);
                    return;
                }

                // 이미 수령했는지
                if (!BattlePassService.CanClaimLevel(state, level, isPremium)) {
                    SendFail(4);
                    return;
                }

                // 실제 보상 정의 있는지
                var reward = BattlePassService.GetReward(level, isPremium);
                if (reward == null) {
                    SendFail(1);
                    return;
                }

                // 지급
                using var tx = db.Database.BeginTransaction();
                BattlePassService.GrantLevelReward(db, acc, state, level, isPremium);
                db.SaveChanges();
                tx.Commit();

                s.Send(new S_ClaimBattlePassRewardResult {
                    isSuccess = true,
                    failReason = 0,
                    level = level,
                    isPremium = isPremium
                }.Write());

                // 갱신된 배패/상점(지갑) 정보 보내주기
                s.Send(BattlePassPacketBuilder.BuildFor(s).Write());
                var store = StorePacketBuilder.BuildFor(s);
                s.Send(store.Write());
            } catch (Exception ex) {
                Log.Error($"[ClaimBattlePassReward] error: {ex.Message}");
                s.Send(new S_ClaimBattlePassRewardResult {
                    isSuccess = false,
                    failReason = 6,
                    level = req.level,
                    isPremium = req.isPremium
                }.Write());
            }
        }


        public void ClaimBattlePassAll(ClientSession s, C_ClaimBattlePassAll req) {
            void SendFail(int reason) {
                s.Send(new S_ClaimBattlePassAllResult {
                    isSuccess = false,
                    failReason = reason,
                    claimedCount = 0,
                    gainedGold = 0,
                    gainedStar = 0
                }.Write());
            }

            try {
                var cfg = BattlePass.Config;
                if (cfg == null) {
                    SendFail(1);
                    return;
                }

                using var db = new AppDBContext();
                var acc = db.Accounts
                    .Include(a => a.Items)
                    .FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (acc == null) {
                    SendFail(6);
                    return;
                }

                var state = BattlePassService.GetOrCreateState(db, acc);

                int maxLevel = Math.Min(state.level, cfg.MaxLevel);
                if (maxLevel <= 0) {
                    // 사실상 수령 가능 없음
                    SendFail(3);
                    return;
                }

                int claimedCount = 0;
                int totalGold = 0;
                int totalStar = 0;

                using var tx = db.Database.BeginTransaction();

                for (int lv = 1; lv <= maxLevel; lv++) {
                    // free
                    if (BattlePassService.CanClaimLevel(state, lv, false)) {
                        var reward = BattlePassService.GetReward(lv, false);
                        if (reward != null) {
                            BattlePassService.GrantLevelReward(db, acc, state, lv, false);
                            claimedCount++;
                            totalGold += reward.Gold;
                            totalStar += reward.Star;
                        }
                    }

                    // premium
                    if (BattlePassService.CanClaimLevel(state, lv, true)) {
                        var reward = BattlePassService.GetReward(lv, true);
                        if (reward != null) {
                            BattlePassService.GrantLevelReward(db, acc, state, lv, true);
                            claimedCount++;
                            totalGold += reward.Gold;
                            totalStar += reward.Star;
                        }
                    }
                }

                if (claimedCount == 0) {
                    tx.Rollback();
                    SendFail(3);
                    return;
                }

                db.SaveChanges();
                tx.Commit();

                s.Send(new S_ClaimBattlePassAllResult {
                    isSuccess = true,
                    failReason = 0,
                    claimedCount = claimedCount,
                    gainedGold = totalGold,
                    gainedStar = totalStar
                }.Write());

                // 최신 상태 갱신
                s.Send(BattlePassPacketBuilder.BuildFor(s).Write());
                var store = StorePacketBuilder.BuildFor(s);
                s.Send(store.Write());
            } catch (Exception ex) {
                Log.Error($"[ClaimBattlePassAll] error: {ex.Message}");
                s.Send(new S_ClaimBattlePassAllResult {
                    isSuccess = false,
                    failReason = 6,
                    claimedCount = 0,
                    gainedGold = 0,
                    gainedStar = 0
                }.Write());
            }
        }



        public void BuyBattlePass(ClientSession s) {
            void SendFail(int reason, int version) {
                s.Send(new S_BuyBattlePassResult {
                    isSuccess = false,
                    failReason = reason,
                    version = version
                }.Write());
            }

            try {
                var cfg = BattlePass.Config;
                if (cfg == null) {
                    // 설정 자체가 없음
                    SendFail(1, 0);
                    return;
                }

                using var db = new AppDBContext();
                var acc = db.Accounts
                    .Include(a => a.BattlePassStates)
                    .FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (acc == null) {
                    SendFail(6, cfg.Version);
                    return;
                }

                var state = BattlePassService.GetOrCreateState(db, acc);

                using var tx = db.Database.BeginTransaction();
                var (ok, reason) = BattlePassService.BuyPremium(db, acc, state);
                if (!ok) {
                    tx.Rollback();
                    SendFail(reason, cfg.Version);
                    return;
                }

                db.SaveChanges();
                tx.Commit();

                s.Send(new S_BuyBattlePassResult {
                    isSuccess = true,
                    failReason = 0,
                    version = cfg.Version
                }.Write());

                // 지갑/배패 상태 갱신
                var store = StorePacketBuilder.BuildFor(s);
                s.Send(store.Write());
                s.Send(BattlePassPacketBuilder.BuildFor(s).Write());
            } catch (Exception ex) {
                Log.Error($"[BuyBattlePass] error: {ex.Message}");
                int version = BattlePass.Config?.Version ?? 0;
                s.Send(new S_BuyBattlePassResult {
                    isSuccess = false,
                    failReason = 6,
                    version = version
                }.Write());
            }
        }




    }
}
