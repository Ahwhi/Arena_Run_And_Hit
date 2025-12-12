using Server;
using Server.Infra;
using ServerCore;

class PacketHandler {
    static Room Ensure(ClientSession cs) => cs?.Room;

    public static void C_PingHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        var ping = (C_Ping)packet; room.Push(() => room.Ping(cs, ping));
    }

    public static void C_RegisterHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.Register(cs, (C_Register)packet));
    }

    public static void C_LoginHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.Login(cs, (C_Login)packet));
    }

    public static void C_AutoLoginHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.AutoLogin(cs, (C_AutoLogin)packet));
    }

    public static void C_LogoutHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.Logout(cs, (C_Logout)packet));
    }

    public static void C_EnterLobbyHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        cs.isGaming = false;
        Program.LobbyRoom.Push(() => Program.LobbyRoom.Access(cs));
        cs.Room.Push(() => {
            var store = StorePacketBuilder.BuildFor(cs);
            cs.Send(store.Write());
        });

        cs.Room.Push(() => cs.Room.PlayerUpdate(cs));

        if (cs.GroupRoom != null) {
            cs.Room.Push(() => cs.Room.BroadcastGroupUpdate(cs));
        }
    }

    public static void C_RequestStoreHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        
    }

    public static void C_TryFindMatchHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var req = (C_TryFindMatch)packet;
        if (cs.GroupRoom == null) {
            cs.isGaming = true;
            Program.LobbyRoom.Push(() => Program.LobbyRoom.DisAccess(cs));
            var mode = (GameMode)req.gameMode;
            switch (mode) {
                case GameMode.Survive: Program.MatchmakingSurvive.Push(() => Program.MatchmakingSurvive.Access(cs)); break;
                case GameMode.Respawn: Program.MatchmakingRespawn.Push(() => Program.MatchmakingRespawn.Access(cs)); break;
                case GameMode.RankSurvive: Program.MatchmakingRank.Push(() => Program.MatchmakingRank.Access(cs)); break;
            }
            cs.Room.Push(() => cs.Room.intoMatchingRoom(cs, req));
        } else {
            // 그룹일경우 
            cs.Room.Push(() => cs.Room.GroopintoMatchingRoom(cs, req));
        }
        
    }

    public static void C_CancelFindMatchHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        if (cs.GroupRoom == null) {
            cs.isGaming = false;
            cs.Room.Push(() => cs.Room.DisAccess(cs));
            Program.LobbyRoom.Push(() => Program.LobbyRoom.Access(cs));
            cs.Room.Push(() => cs.Room.cancelMatchingRoom(cs));
        } else {
            // 그룹일경우
            cs.Room.Push(() => cs.Room.GroopouttoMatchingRoom(cs));
        }
    }

    public static void C_EnterGameHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var room = cs.Room;
        cs.isGaming = true;
        room.Push(() => room.Enter(cs, (C_EnterGame)packet));
        var set = new S_GameSetting { gameMode = (int)room.Mode };
        session.Send(set.Write());

        if (cs.GroupRoom != null) {
            cs.Room.Push(() => cs.Room.BroadcastGroupUpdate(cs));
        }
    }

    public static void C_LeaveGameHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        cs.isGaming = false;
        cs.Room.Push(() => cs.Room.Leave(cs));
    }

    public static void C_ChatHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        cs.Room.Push(() => cs.Room.Chat(cs, (C_Chat)packet));
    }

    public static void C_LobbyChatHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        cs.Room.Push(() => cs.Room.LobbyChat(cs, (C_LobbyChat)packet));
    }

    public static void C_InputHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        cs.Room.Push(() => cs.Room.Input(cs, (C_Input)packet));
    }

    public static void C_BotSnapshotHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var snap = (C_BotSnapshot)packet;
        if (cs.Room == null) return;
        cs.Room.Push(() => cs.Room.OnBotSnapshot(cs, snap));
    }

    public static void C_EquipItemHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        cs.Room.Push(() => cs.Room.EquipItem(cs, (C_EquipItem)packet));
    }

    public static void C_BuyOfferHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var req = (C_BuyOffer)packet;
        cs.Room.Push(() => cs.Room.BuyOffer(cs, req));
    }

    public static void C_LeaveGroupHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        cs.Room.Push(() => cs.Room.ExitGroup(cs));
    }

    public static void C_InviteGroupHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var req = (C_InviteGroup)packet;
        cs.Room.Push(() => cs.Room.InviteGroup(cs, req));
    }

    public static void C_ReplyInviteGroupHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var p = (C_ReplyInviteGroup)packet;
        cs.Room.Push(() => cs.Room.ReplyInviteGroup(cs, p));
    }

    public static void C_RequestUserInformationHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var p = (C_RequestUserInformation)packet;
        cs.Room.Push(() => cs.Room.GiveUserInformation(cs, p));
    }

    public static void C_RequestRecentGamesHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var p = (C_RequestRecentGames)packet;
        cs.Room.Push(() => cs.Room.GetRecentGames(cs, p));
    }

    public static void C_RequestLeaderboardHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; if (cs.Room == null) return;
        var p = (C_RequestLeaderboard)packet;
        cs.Room.Push(() => cs.Room.GetLeaderboard(cs, p));
    }

    public static void C_SteamLoginHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.SteamLogin(cs, (C_SteamLogin)packet));
    }

    public static void C_CheckSteamNickNameHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.CheckSteamNick(cs, (C_CheckSteamNickName)packet));
    }

    public static void C_SetSteamNickNameHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.SetSteamNick(cs, (C_SetSteamNickName)packet));
    }

    public static void C_RequestAddStarHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.OnBuyStars(cs, (C_RequestAddStar)packet));
    }

    public static void C_ConfirmAddStarHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.ConfirmAddStar(cs, (C_ConfirmAddStar)packet));
    }

    public static void C_AgreePolicyHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.AgreePolicy(cs, (C_AgreePolicy)packet));
    }

    public static void C_RequestBattlePassHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.RequestBattlePass(cs, (C_RequestBattlePass)packet));
    }

    public static void C_ClaimBattlePassRewardHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.ClaimBattlePassReward(cs, (C_ClaimBattlePassReward)packet));
    }

    public static void C_ClaimBattlePassAllHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.ClaimBattlePassAll(cs, (C_ClaimBattlePassAll)packet));
    }

    public static void C_BuyBattlePassHandler(PacketSession session, IPacket packet) {
        var cs = (ClientSession)session; var room = Ensure(cs); if (room == null) return;
        room.Push(() => room.BuyBattlePass(cs));
    }
}
