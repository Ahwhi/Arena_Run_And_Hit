using System.Collections.Generic;
using DummyClient;
using ServerCore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;

class PacketHandler {

    public static void S_PongHandler(PacketSession session, IPacket packet) {
        S_Pong pkt = packet as S_Pong;
        ServerSession serverSession = session as ServerSession;
        NetworkManager.OnPong(pkt);
    }

    public static void S_PopulationHandler(PacketSession session, IPacket packet) {
        var pkt = (S_Population)packet;
        LobbyManager.population = pkt.total;
    }

    public static void S_RegisterResultHandler(PacketSession session, IPacket packet) {
        S_RegisterResult pkt = packet as S_RegisterResult;
        ServerSession serverSession = session as ServerSession;
        AuthManager.Instance.OnRegisterResult(pkt.isSuccess, pkt.failReason);
    }

    public static void S_AutoLoginResultHandler(PacketSession session, IPacket packet) {
        S_AutoLoginResult pkt = packet as S_AutoLoginResult;
        ServerSession serverSession = session as ServerSession;
        if (pkt.isSuccess) {
            //UnityEngine.Debug.Log("자동 로그인 성공" + pkt.failReason);
            NetworkManager.Instance.nickName = pkt.nickName;
            SceneManager.LoadScene("LobbyScene");
        } else {
            //UnityEngine.Debug.Log("로그인 실패" + pkt.failReason);
            PlayerPrefs.DeleteKey("AccessToken");
            PlayerPrefs.Save();
            if (pkt.failReason == 1) {
                //UnityEngine.Debug.Log("토큰 없음");
            } else if (pkt.failReason == 2) {
                //UnityEngine.Debug.Log("토큰 만료");
            }
        }
        AuthManager.Instance.OnAutoLoginResult(pkt.isSuccess, pkt.failReason);
    }

    public static void S_LoginResultHandler(PacketSession session, IPacket packet) {
        S_LoginResult pkt = packet as S_LoginResult;
        ServerSession serverSession = session as ServerSession;
        if (pkt.isSuccess) {
            NetworkManager.Instance.nickName = pkt.nickName;
            if (pkt.accessToken != "" && pkt.accessToken != null) {
                PlayerPrefs.SetString("AccessToken", pkt.accessToken);
                PlayerPrefs.Save();
            }
            SceneManager.LoadScene("LobbyScene");
            //SceneManager.LoadScene("GameScene");
        } else {
            //UnityEngine.Debug.Log("로그인 실패" + pkt.failReason);
            if (pkt.failReason == 1) {
                //UnityEngine.Debug.Log("없는 계정");
            } else if (pkt.failReason == 2) {
                //UnityEngine.Debug.Log("비밀번호 불일치");
            }
        }
        AuthManager.Instance.OnLoginResult(pkt.isSuccess, pkt.failReason);
    }

    public static void S_LogoutResultHandler(PacketSession session, IPacket packet) {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        UIManager.ShowSuccess("로그아웃 되었습니다.");
        SceneManager.LoadScene("AuthScene");
    }

    public static void S_MatchFoundHandler(PacketSession session, IPacket packet) {
        var pkt = (S_MatchFound)packet;

        BgmPlayer.I.Stop();
        SoundManager.I?.StopAllSfx();
        SoundManager.I?.Play2D(SfxId.MatchFound);
        //UIManager.ShowSuccess("매칭을 찾았습니다.");
        // BotRuntime 인스턴스 확보
        if (BotRuntime.Instance == null) new GameObject("BotRuntime").AddComponent<BotRuntime>();
        BotRuntime.Instance.gameMode = pkt.gameMode;
        BotRuntime.Instance.botOwnerId = pkt.botOwnerId;
        BotRuntime.Instance.botIds.Clear();
        foreach (var e in pkt.botIdss) BotRuntime.Instance.botIds.Add(e.botId);

        SceneManager.LoadScene("GameScene");
    }

    public static void S_GameSettingHandler(PacketSession session, IPacket packet) {
        S_GameSetting pkt = packet as S_GameSetting;
        ServerSession serverSession = session as ServerSession;
        GameManager.Instance.OnSettingGameMode(pkt.gameMode);
    }

    public static void S_BroadcastLeaveGameHandler(PacketSession session, IPacket packet) {
        S_BroadcastLeaveGame pkt = packet as S_BroadcastLeaveGame;
        ServerSession serverSession = session as ServerSession;
        PlayerManager.Instance.LeaveGame(pkt);
    }

    public static void S_PlayerListHandler(PacketSession session, IPacket packet) {
        S_PlayerList pkt = packet as S_PlayerList;
        ServerSession serverSession = session as ServerSession;
        PlayerManager.Instance.Add(pkt);
    }

    public static void S_BroadcastChatHandler(PacketSession session, IPacket packet) {
        S_BroadcastChat pkt = packet as S_BroadcastChat;
        ServerSession serverSession = session as ServerSession;
        if (SceneManager.GetActiveScene().name == "GameScene") {
            PlayerManager.Instance.Chat(pkt);
        } else {
            Debug.LogWarning("게임씬이 아닌데 게임 채팅 도달");
        }
    }

    public static void S_BroadcastLobbyChatHandler(PacketSession session, IPacket packet) {
        S_BroadcastLobbyChat pkt = packet as S_BroadcastLobbyChat;
        ServerSession serverSession = session as ServerSession;
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            PlayerManager.Instance.LobbyChat(pkt);
        } else {
            Debug.LogWarning("로비씬이 아닌데 로비 채팅 도달");
        }
    }

    public static void S_SnapshotHandler(PacketSession session, IPacket packet) {
        S_Snapshot pkt = packet as S_Snapshot;
        ServerSession serverSession = session as ServerSession;
        PlayerManager.Instance?.OnSnapshotFromServer(pkt);
    }

    public static void S_WorldSnapshotHandler(PacketSession session, IPacket packet) {
        S_WorldSnapshot pkt = packet as S_WorldSnapshot;
        PlayerManager.Instance?.OnWorldSnapshot(pkt);
    }

    public static void S_BroadcastAttackHandler(PacketSession session, IPacket packet) {
        S_BroadcastAttack pkt = packet as S_BroadcastAttack;
        if (PlayerManager.FindOthersAndBots.TryGetValue(pkt.playerId, out var player)) {
            player.OnTriggerAttack();
        }
    }

    public static void S_HitHandler(PacketSession session, IPacket packet) {
        S_Hit pkt = packet as S_Hit;

        if (PlayerManager.FindAllPlayer.TryGetValue(pkt.targetId, out var target)) {
            target.OnHitFromServer(pkt.damage, pkt.hpAfter, pkt.killed);
            Object obj = Resources.Load("Particle/HitParticle");
            GameObject go = Object.Instantiate(obj) as GameObject;
            go.transform.position = target.transform.position + new Vector3(0, 3, 0);

            if (pkt.killed) {
                Object objs = Resources.Load("KillLogPrefab");
                Transform pTransform = GameObject.Find("Panel_Log").GetComponent<Transform>();
                GameObject gos = Object.Instantiate(objs, pTransform) as GameObject;
                TextMeshProUGUI[] texts = gos.GetComponentsInChildren<TextMeshProUGUI>();
                PlayerManager.FindAllPlayer.TryGetValue(pkt.attackerId, out var attacker);
                texts[0].text = attacker.NickName;
                if (attacker is MyPlayer) {
                    texts[0].color = Color.green;
                    SoundManager.I?.Play2D(SfxId.Score);
                }
                texts[2].text = target.NickName;
                if (target is MyPlayer) {
                    texts[2].color = Color.green;
                }
                //SoundManager.I?.Play3DFollow(SfxId.FinalHit, target.transform);
                SoundManager.I?.Play3D(SfxId.FinalHit, target.transform.position);
            } else {
                //SoundManager.I?.Play3DFollow(SfxId.Hit, target.transform);
                SoundManager.I?.Play3D(SfxId.Hit, target.transform.position);
            }
        }
    }

    public static void S_HealthHandler(PacketSession session, IPacket packet) {
        S_Health pkt = packet as S_Health;
        if (PlayerManager.FindAllPlayer.TryGetValue(pkt.playerId, out var p)) {
            bool wasDead = (p.HP <= 0);
            p.ApplyServerHealth(pkt.hp, pkt.maxHp);
            //if (wasDead && pkt.hp > 0) {
            //    p.OnRespawned(pkt.hp, pkt.maxHp);
            //}
        }
    }

    public static void S_BroadcastEmoteHandler(PacketSession session, IPacket packet) {
        S_BroadcastEmote pkt = packet as S_BroadcastEmote;
        if (PlayerManager.FindAllPlayer.TryGetValue(pkt.playerId, out var p)) {
            if (!string.IsNullOrEmpty(pkt.emoteSku)) {
                p.OnEmoteBySku(pkt.emoteSku);    // ★ SKU 기반 재생
                if (p is MyPlayer) {
                    //SoundManager.I?.Play2D(SfxId.Dance_Basic);
                }
            }
                
        }
    }

    public static void S_GameStatusHandler(PacketSession session, IPacket packet) {
        S_GameStatus pkt = packet as S_GameStatus;
        GameManager.aliveCount = pkt.aliveCount;
        foreach (var e in pkt.players) {
            // 1) 사람
            if (PlayerManager.FindAllPlayer.TryGetValue(e.playerId, out var target)) {
                target.Kill = e.kills;
                target.Death = e.deaths;
                if (target == PlayerManager.MyPlayer) {
                    GameManager.killCount = target.Kill;
                }
            }
        }

        foreach (var e in pkt.botss) {
            // 2) 봇
            if (PlayerManager.FindAllPlayer.TryGetValue(e.botId, out var target)) {
                target.Kill = e.kills;
                target.Death = e.deaths;
            }
        }
    }

    public static void S_BotOwnerChangedHandler(PacketSession session, IPacket packet) {
        var pkt = (S_BotOwnerChanged)packet;
        var ids = new List<int>();
        foreach (var b in pkt.botIdss) ids.Add(b.botId);

        BotCoordinator.Instance?.OnBotOwnerChanged(pkt.ownerId, ids);
    }

    public static void S_SupplySpawnHandler(PacketSession session, IPacket packet) {
        var pkt = (S_SupplySpawn)packet;
        SupplyManager.Instance.Spawn(pkt.dropId, (SupplyEffect)pkt.effect,
                              new Vector3(pkt.posX, pkt.posY + 0.25f, pkt.posZ));
    }

    public static void S_SupplyGoneHandler(PacketSession session, IPacket packet) {
        var pkt = (S_SupplyGone)packet;
        SupplyManager.Instance.Despawn(pkt.dropId, pkt.reason);
    }

    public static void S_SupplyAppliedHandler(PacketSession session, IPacket packet) {
        var pkt = (S_SupplyApplied)packet;

        if (PlayerManager.TryGet(pkt.playerId, out var p)) {
            SupplyManager.Instance.OnApplied(p, (SupplyEffect)pkt.effect, pkt.amount, pkt.durationMs);
        }

        // ★ 내 캐릭이 SpeedUp이면 로컬 예측도 속도×2로
        if (pkt.playerId == PlayerManager.MyPlayerId) {
            SoundManager.I?.Play2D(SfxId.Pickup);
            if ((SupplyEffect)pkt.effect == SupplyEffect.SpeedUp) {
                PlayerManager.MyPlayer?.ApplyLocalBuff(SupplyEffect.SpeedUp, pkt.durationMs / 1000f);
            }
        }
    }

    public static void S_VomitExplodeHandler(PacketSession session, IPacket packet) {
        var pkt = (S_VomitExplode)packet;
        SupplyManager.Instance?.SpawnVomitExplosion(new Vector3(pkt.posX, pkt.posY, pkt.posZ), pkt.radius);
    }


    public static void S_GameReadyHandler(PacketSession session, IPacket packet) {
        var pkt = (S_GameReady)packet;
        var serverSession = (ServerSession)session;

        int prev = GameManager.countDown;
        int now = pkt.countdownSec;

        // (원하면 이 디버그는 남겨도 됨)
        // Debug.Log($"[S_GameReady] recv countdown={now}, prev={prev}");

        // ★ 1) 카운트다운이 "0 이하 → 1 이상" 으로 바뀌는 순간에만 인트로 요청
        if (now > 0 && prev <= 0 && !GameManager.isGameStart) {
            var cam = CameraManager.Instance;
            if (cam != null) {
                cam.PlayIntroOrbit(now);
            }
        }

        // ★ 2) 기존 카운트다운 사운드/값 처리
        if (prev != now) {
            if (now > 0) {
                SoundManager.I?.Play2D(SfxId.CountDown);
            } else {
                if (prev != -1)
                    SoundManager.I?.Play2D(SfxId.Start);
            }
            GameManager.countDown = now;
        }
    }

    public static void S_SetRankHandler(PacketSession session, IPacket packet) {
        var pkt = (S_SetRank)packet;
        GameManager.Instance?.deathPanel.SetActive(true);
        GameManager.rankNum = pkt.rank;
        if (GameManager.mode == "경쟁 서바이벌 모드") {
            GameManager.Instance.RankExitButtoninDeath.gameObject.SetActive(true);
        } else {
            GameManager.Instance.NonRankExitButtoninDeath.gameObject.SetActive(true);
        }
    }

    public static void S_GameOverHandler(PacketSession session, IPacket packet) {
        var pkt = (S_GameOver)packet;
        GameManager.Instance.isGameOver = true;
        // 결과를 정렬(오름차순: 1위 → N위)
        var results = new List<(int id, string name, bool isBot, int rank, int kills, int deaths)>();
        foreach (var e in pkt.resultss) {
            results.Add((e.playerId, e.nickName, e.isBot, e.rank, e.kills, e.deaths));
            // UI 해제
            PlayerManager.FindAllPlayer.TryGetValue(e.playerId, out var target);
            PlayerUIManager.Instance?.Detach(target);
        }
        results.Sort((a, b) => a.rank.CompareTo(b.rank));

        // UIManager에 넘겨서 게임결과 패널 띄우기
        GameManager.Instance.ShowGameResult(results);
    }

    public static void S_MatchTimerHandler(PacketSession session, IPacket packet) {
        var pkt = (S_MatchTimer)packet;
        GameManager.timeLeftSec = pkt.remainingSec;  // UI 바인딩 값(정수)
        GameManager.Instance?.SetMatchTime(pkt.remainingSec); // 방법 1: 즉시 반영 함수
    }

    public static void S_RespawnHandler(PacketSession session, IPacket packet) {
        var pkt = (S_Respawn)packet;

        if (PlayerManager.FindAllPlayer.TryGetValue(pkt.playerId, out var p)) {
            p.OnRespawned(new Vector3(pkt.posX, pkt.posY, pkt.posZ), pkt.yaw);

            // 내 캐릭이면 예측 버퍼/카메라 등도 정리
            if (p is MyPlayer me) {
                me.OnRespawnedLocal(new Vector3(pkt.posX, pkt.posY, pkt.posZ), pkt.yaw);
            }
        } else {
            // 혹시 엔티티가 아직 씬에 없을 수 있으니(봇 프리팹이 늦게 올라온 경우), 안전망:
            if (BotRuntime.Instance != null && BotRuntime.Instance.botIds.Contains(pkt.playerId)) {
                // 원격 봇 스폰 시도
                // (선택) PlayerManager에 봇 스폰 헬퍼가 있으면 호출
                // var bot = PlayerManager.Instance.SpawnRemoteBot(pkt.playerId);
                // if (bot) bot.OnRespawned(new Vector3(pkt.posX, pkt.posY, pkt.posZ), pkt.yaw);
            }
        }

        // 내 캐릭이면 사망 패널/관전 UI 끄기
        if (pkt.playerId == PlayerManager.MyPlayerId) {
            GameManager.Instance?.deathPanel?.SetActive(false);
        }
    }

    public static void S_BotInitHandler(PacketSession session, IPacket packet) {
        var pkt = (S_BotInit)packet;
        // BotRuntime에 캐시
        foreach (var b in pkt.infos) {
            if (BotRuntime.Instance == null) continue;
            BotRuntime.Instance.initialPos[b.botId] = new Vector3(b.posX, b.posY, b.posZ);
            BotRuntime.Instance.initialYaw[b.botId] = b.yaw;

            // ★ 색상도 저장
            if (!BotRuntime.Instance.colorById.ContainsKey(b.botId))
                BotRuntime.Instance.colorById[b.botId] = b.colorIndex;
            else
                BotRuntime.Instance.colorById[b.botId] = b.colorIndex;
        }
    }

    public static void S_EquipResultHandler(PacketSession session, IPacket packet) {
        var pkt = (S_EquipResult)packet;

        // 실패 시 간단 안내 (원하면 토스트/팝업으로 대체)
        if (!pkt.isSuccess) {
            switch (pkt.failReason) {
                case 1: UIManager.ShowError("Equip 실패: 슬롯이 유효하지 않음"); break;
                case 2: UIManager.ShowError("Equip 실패: 아이템 미보유"); break;
                default: UIManager.ShowError("Equip 실패: 알 수 없는 오류"); break;
            }
        } else {
            if (pkt.isDetach) {
                //UIManager.ShowSuccess("해제가 완료 되었습니다.");
                UIManager.ShowSuccessKey("TOAST7");
            } else {
                //UIManager.ShowSuccess("장착이 완료 되었습니다.");
                UIManager.ShowSuccessKey("TOAST8");
            }
        }
    }

    public static void S_BuyResultHandler(PacketSession session, IPacket packet) {
        var pkt = (S_BuyResult)packet;

        if (!pkt.isSuccess) {
            switch (pkt.failReason) {
                //case 1: UIManager.ShowError("상품이 없거나 구매 불가 상태입니다."); break;   // OfferInvalid
                //case 2: UIManager.ShowError("더이상 구매가 불가능한 상품 입니다."); break;     // LimitExceeded
                //case 3: UIManager.ShowError("보유 재화가 부족합니다."); break;                     // NotEnoughCurrency
                //case 5: UIManager.ShowError("서버에 오류가 있습니다."); break;                     // ServerError
                //case 4: // IdempotentDuplicate (현재 설계상 실패로는 내려오지 않음)
                //default: UIManager.ShowError("알 수 없는 오류로 구매에 실패했습니다."); break;
                case 1: UIManager.ShowErrorKey("TOAST1"); break;   // OfferInvalid
                case 2: UIManager.ShowErrorKey("TOAST2"); break;     // LimitExceeded
                case 3: UIManager.ShowErrorKey("TOAST3"); break;                     // NotEnoughCurrency
                case 5: UIManager.ShowErrorKey("TOAST4"); break;                     // ServerError
                case 4: // IdempotentDuplicate (현재 설계상 실패로는 내려오지 않음)
                default: UIManager.ShowErrorKey("TOAST5"); break;
            }
        } else {
            //UIManager.ShowSuccess("구매가 완료 되었습니다.");
            UIManager.ShowSuccessKey("TOAST6");
        }
    }

    public static void S_StoreCatalogHandler(PacketSession session, IPacket packet) {
        var pkt = (S_StoreCatalog)packet;

        bool needRebuildShop = (ShopCache.Version != pkt.version);
        ShopCache.Version = pkt.version;

        // 1) Wallet
        ShopCache.Gold = pkt.gold;
        ShopCache.Star = pkt.star;

        // 2) Items meta
        ShopCache.Items.Clear();
        foreach (var im in pkt.itemss) {
            ShopCache.Items[im.sku] = new ShopCache.ItemMeta {
                name = im.name,
                category = im.category,
                imageKey = im.imageKey
            };
        }

        // 3) Ownership / Equipped
        ShopCache.Owned.Clear();
        foreach (var inv in pkt.inventorys) ShopCache.Owned.Add(inv.sku);

        ShopCache.EquippedBySlot.Clear();
        foreach (var eq in pkt.equippeds) ShopCache.EquippedBySlot[eq.slot] = eq.sku;

        // 4) Offers (샵)
        ShopCache.Offers.Clear();
        foreach (var o in pkt.offerss) {
            var vo = new ShopCache.StoreOfferVo {
                offerId = o.offerId,
                displayName = o.displayName,
                imageKey = o.imageKey,
                category = o.category,
                visible = o.visible
            };
            foreach (var p in o.pricess) vo.prices.Add((p.currency, p.amount));
            ShopCache.Offers.Add(vo);
        }
        ShopUI.Instance?.RebuildFromCache();

        // 5) 락커룸 즉시 리프레시
        UnityEngine.Object.FindAnyObjectByType<LockerRoomUI>()?.RefreshFromCache();

        // 6) 로비 캐릭터 프리뷰 갱신
        var lobby = UnityEngine.Object.FindAnyObjectByType<LobbyManager>();
        lobby?.SpawnEquippedCharacterPreview();

        var bp = UnityEngine.Object.FindAnyObjectByType<BattlePassPanel>();
        if (bp != null && bp.isActiveAndEnabled) {
            bp.RequestBattlePass();
        }
    }

    public static void S_RequestInviteGroupHandler(PacketSession session, IPacket packet) {
        var pkt = (S_RequestInviteGroup)packet;
        LobbyManager.ShowInvitePanel(pkt.InviterNickName);
    }

    public static void S_GroupUpdateHandler(PacketSession session, IPacket packet) {
        var pkt = (S_GroupUpdate)packet;
        if (!pkt.isDestroy) {
            NetworkManager.Instance.isInGroup = true;
        } else {
            NetworkManager.Instance.isInGroup = false;
        }
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            LobbyManager.ShowGroupPanel(pkt);
        }
    }

    public static void S_InviteGroupResultHandler(PacketSession session, IPacket packet) {
        var pkt = (S_InviteGroupResult)packet;
        LobbyManager.ShowInviteResult(pkt);
    }

    public static void S_BroadcastJoinGroupHandler(PacketSession session, IPacket packet) {
        var pkt = (S_BroadcastJoinGroup)packet;
        LobbyManager.ShowJoinGroupMessage(pkt.joinnerNickName);
    }

    public static void S_BroadcastLeaveGroupHandler(PacketSession session, IPacket packet) {
        var pkt = (S_BroadcastLeaveGroup)packet;
        LobbyManager.ShowLeaveGroupMessage(pkt);
    }

    public static void S_GroupLeaderFindMatchHandler(PacketSession session, IPacket packet) {
        var pkt = (S_GroupLeaderFindMatch)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            LobbyManager.G_OnButtonFindMatch();
        }
    }

    public static void S_GroupLeaderCancelMatchHandler(PacketSession session, IPacket packet) {
        var pkt = (S_GroupLeaderCancelMatch)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            LobbyManager.G_OnButtonFindMatch();
        }
    }

    public static void S_UserInformationHandler(PacketSession session, IPacket packet) {
        var pkt = (S_UserInformation)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            LobbyManager.OnShowUserInformation(pkt);
        }
    }

    public static void S_RankScoreUpdateHandler(PacketSession session, IPacket packet) {
        var pkt = (S_RankScoreUpdate)packet;
        if (SceneManager.GetActiveScene().name == "GameScene") {
            GameManager.change = pkt.change;
            GameManager.beforeRank = pkt.beforeRank;
            GameManager.afterRank = pkt.afterRank;
            GameManager.beforeRankScore = pkt.beforeRankScore;
            GameManager.afterRankScore = pkt.afterRankScore;
        }
    }

    public static void S_RecentGamesHandler(PacketSession session, IPacket packet) {
        var pkt = (S_RecentGames)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            LobbyManager.OnShowUserHistory(pkt);
        }
    }

    public static void S_LeaderboardHandler(PacketSession session, IPacket packet) {
        var pkt = (S_Leaderboard)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            LobbyManager.OnShowLeaderboard(pkt);
        }
    }

    public static void S_PlayerUpdateHandler(PacketSession session, IPacket packet) {
        var pkt = (S_PlayerUpdate)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene") {
            LobbyManager.OnUpdatePlayerData(pkt);
        }
    }

    public static void S_LevelUpHandler(PacketSession session, IPacket packet) {
        var p = (S_LevelUp)packet;
        SoundManager.I?.Play2D(SfxId.LevelUp);
        UIManager.ShowLevelUp(p.fromLevel, p.toLevel, p.rewardGold, p.rewardStar);
    }


    // 스팀
    public static void S_SteamLoginResultHandler(PacketSession session, IPacket packet) {
        var p = (S_SteamLoginResult)packet;
        AuthManager.OnAvailableLogin(p);
    }

    public static void S_CheckSteamNameResultHandler(PacketSession session, IPacket packet) {
        var p = (S_CheckSteamNameResult)packet;
        AuthManager.OnCheckSteamNameResult(p);
    }

    public static void S_SetSteamNameResultHandler(PacketSession session, IPacket packet) {
        var p = (S_SetSteamNameResult)packet;
        AuthManager.OnSetSteamNameResult(p);
    }

    public static void S_BuyStarsResultHandler(PacketSession session, IPacket packet) {
        var p = (S_BuyStarsResult)packet;
        SteamPayManager.Instance.OnBuyStarsResult(p);
        //Debug.Log($"{p.isSuccess}, {p.transId}, {p.orderId}, {p.packIndex}");
    }

    public static void S_ConfirmAddStarResultHandler(PacketSession session, IPacket packet) {
        var p = (S_ConfirmAddStarResult)packet;
        if (p.isSuccess) {
            UIManager.ShowSuccessKey("PAY_SUCCESS");
        } else {
            UIManager.ShowErrorKey("PAY_FAIL");
        }
        //Debug.Log($"{p.isSuccess}, {p.addedStars}, {p.newStarBalance}, {p.packIndex}");
    }

    public static void S_AgreePolicyResultHandler(PacketSession session, IPacket packet) {
        var p = (S_AgreePolicyResult)packet;
        AuthManager.OnAvailableLogin_2(p);
    }

    public static void S_BuyChangeNameHandler(PacketSession session, IPacket packet) {
        var p = (S_BuyChangeName)packet;
        SceneManager.LoadScene("AuthScene");
    }

    public static void S_BattlePassInfoHandler(PacketSession session, IPacket packet) {
        var pkt = (S_BattlePassInfo)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene")
            BattlePassPanel.OnBattlePassInfo(pkt);
    }

    public static void S_ClaimBattlePassRewardResultHandler(PacketSession session, IPacket packet) {
        var pkt = (S_ClaimBattlePassRewardResult)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene")
            BattlePassPanel.OnClaimResult(pkt);
    }

    public static void S_ClaimBattlePassAllResultHandler(PacketSession session, IPacket packet) {
        var pkt = (S_ClaimBattlePassAllResult)packet;
        if (SceneManager.GetActiveScene().name == "LobbyScene")
            BattlePassPanel.OnClaimAllResult(pkt);
    }

    public static void S_BuyBattlePassResultHandler(PacketSession session, IPacket packet) {
        var pkt = (S_BuyBattlePassResult)packet;

    }

}