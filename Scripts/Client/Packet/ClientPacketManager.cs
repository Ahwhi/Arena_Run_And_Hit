using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
	Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
		
	public void Register()
	{
		_makeFunc.Add((ushort)PacketID.S_Pong, MakePacket<S_Pong>);
		_handler.Add((ushort)PacketID.S_Pong, PacketHandler.S_PongHandler);
		_makeFunc.Add((ushort)PacketID.S_Population, MakePacket<S_Population>);
		_handler.Add((ushort)PacketID.S_Population, PacketHandler.S_PopulationHandler);
		_makeFunc.Add((ushort)PacketID.S_SteamLoginResult, MakePacket<S_SteamLoginResult>);
		_handler.Add((ushort)PacketID.S_SteamLoginResult, PacketHandler.S_SteamLoginResultHandler);
		_makeFunc.Add((ushort)PacketID.S_CheckSteamNameResult, MakePacket<S_CheckSteamNameResult>);
		_handler.Add((ushort)PacketID.S_CheckSteamNameResult, PacketHandler.S_CheckSteamNameResultHandler);
		_makeFunc.Add((ushort)PacketID.S_SetSteamNameResult, MakePacket<S_SetSteamNameResult>);
		_handler.Add((ushort)PacketID.S_SetSteamNameResult, PacketHandler.S_SetSteamNameResultHandler);
		_makeFunc.Add((ushort)PacketID.S_AgreePolicyResult, MakePacket<S_AgreePolicyResult>);
		_handler.Add((ushort)PacketID.S_AgreePolicyResult, PacketHandler.S_AgreePolicyResultHandler);
		_makeFunc.Add((ushort)PacketID.S_RegisterResult, MakePacket<S_RegisterResult>);
		_handler.Add((ushort)PacketID.S_RegisterResult, PacketHandler.S_RegisterResultHandler);
		_makeFunc.Add((ushort)PacketID.S_LoginResult, MakePacket<S_LoginResult>);
		_handler.Add((ushort)PacketID.S_LoginResult, PacketHandler.S_LoginResultHandler);
		_makeFunc.Add((ushort)PacketID.S_AutoLoginResult, MakePacket<S_AutoLoginResult>);
		_handler.Add((ushort)PacketID.S_AutoLoginResult, PacketHandler.S_AutoLoginResultHandler);
		_makeFunc.Add((ushort)PacketID.S_LogoutResult, MakePacket<S_LogoutResult>);
		_handler.Add((ushort)PacketID.S_LogoutResult, PacketHandler.S_LogoutResultHandler);
		_makeFunc.Add((ushort)PacketID.S_GroupLeaderFindMatch, MakePacket<S_GroupLeaderFindMatch>);
		_handler.Add((ushort)PacketID.S_GroupLeaderFindMatch, PacketHandler.S_GroupLeaderFindMatchHandler);
		_makeFunc.Add((ushort)PacketID.S_GroupLeaderCancelMatch, MakePacket<S_GroupLeaderCancelMatch>);
		_handler.Add((ushort)PacketID.S_GroupLeaderCancelMatch, PacketHandler.S_GroupLeaderCancelMatchHandler);
		_makeFunc.Add((ushort)PacketID.S_MatchFound, MakePacket<S_MatchFound>);
		_handler.Add((ushort)PacketID.S_MatchFound, PacketHandler.S_MatchFoundHandler);
		_makeFunc.Add((ushort)PacketID.S_GameSetting, MakePacket<S_GameSetting>);
		_handler.Add((ushort)PacketID.S_GameSetting, PacketHandler.S_GameSettingHandler);
		_makeFunc.Add((ushort)PacketID.S_GameReady, MakePacket<S_GameReady>);
		_handler.Add((ushort)PacketID.S_GameReady, PacketHandler.S_GameReadyHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastLeaveGame, MakePacket<S_BroadcastLeaveGame>);
		_handler.Add((ushort)PacketID.S_BroadcastLeaveGame, PacketHandler.S_BroadcastLeaveGameHandler);
		_makeFunc.Add((ushort)PacketID.S_PlayerList, MakePacket<S_PlayerList>);
		_handler.Add((ushort)PacketID.S_PlayerList, PacketHandler.S_PlayerListHandler);
		_makeFunc.Add((ushort)PacketID.S_Respawn, MakePacket<S_Respawn>);
		_handler.Add((ushort)PacketID.S_Respawn, PacketHandler.S_RespawnHandler);
		_makeFunc.Add((ushort)PacketID.S_BotInit, MakePacket<S_BotInit>);
		_handler.Add((ushort)PacketID.S_BotInit, PacketHandler.S_BotInitHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastChat, MakePacket<S_BroadcastChat>);
		_handler.Add((ushort)PacketID.S_BroadcastChat, PacketHandler.S_BroadcastChatHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastLobbyChat, MakePacket<S_BroadcastLobbyChat>);
		_handler.Add((ushort)PacketID.S_BroadcastLobbyChat, PacketHandler.S_BroadcastLobbyChatHandler);
		_makeFunc.Add((ushort)PacketID.S_Snapshot, MakePacket<S_Snapshot>);
		_handler.Add((ushort)PacketID.S_Snapshot, PacketHandler.S_SnapshotHandler);
		_makeFunc.Add((ushort)PacketID.S_WorldSnapshot, MakePacket<S_WorldSnapshot>);
		_handler.Add((ushort)PacketID.S_WorldSnapshot, PacketHandler.S_WorldSnapshotHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastAttack, MakePacket<S_BroadcastAttack>);
		_handler.Add((ushort)PacketID.S_BroadcastAttack, PacketHandler.S_BroadcastAttackHandler);
		_makeFunc.Add((ushort)PacketID.S_Hit, MakePacket<S_Hit>);
		_handler.Add((ushort)PacketID.S_Hit, PacketHandler.S_HitHandler);
		_makeFunc.Add((ushort)PacketID.S_Health, MakePacket<S_Health>);
		_handler.Add((ushort)PacketID.S_Health, PacketHandler.S_HealthHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastEmote, MakePacket<S_BroadcastEmote>);
		_handler.Add((ushort)PacketID.S_BroadcastEmote, PacketHandler.S_BroadcastEmoteHandler);
		_makeFunc.Add((ushort)PacketID.S_BotOwnerChanged, MakePacket<S_BotOwnerChanged>);
		_handler.Add((ushort)PacketID.S_BotOwnerChanged, PacketHandler.S_BotOwnerChangedHandler);
		_makeFunc.Add((ushort)PacketID.S_GameStatus, MakePacket<S_GameStatus>);
		_handler.Add((ushort)PacketID.S_GameStatus, PacketHandler.S_GameStatusHandler);
		_makeFunc.Add((ushort)PacketID.S_SupplySpawn, MakePacket<S_SupplySpawn>);
		_handler.Add((ushort)PacketID.S_SupplySpawn, PacketHandler.S_SupplySpawnHandler);
		_makeFunc.Add((ushort)PacketID.S_SupplyGone, MakePacket<S_SupplyGone>);
		_handler.Add((ushort)PacketID.S_SupplyGone, PacketHandler.S_SupplyGoneHandler);
		_makeFunc.Add((ushort)PacketID.S_SupplyApplied, MakePacket<S_SupplyApplied>);
		_handler.Add((ushort)PacketID.S_SupplyApplied, PacketHandler.S_SupplyAppliedHandler);
		_makeFunc.Add((ushort)PacketID.S_VomitExplode, MakePacket<S_VomitExplode>);
		_handler.Add((ushort)PacketID.S_VomitExplode, PacketHandler.S_VomitExplodeHandler);
		_makeFunc.Add((ushort)PacketID.S_MatchTimer, MakePacket<S_MatchTimer>);
		_handler.Add((ushort)PacketID.S_MatchTimer, PacketHandler.S_MatchTimerHandler);
		_makeFunc.Add((ushort)PacketID.S_SetRank, MakePacket<S_SetRank>);
		_handler.Add((ushort)PacketID.S_SetRank, PacketHandler.S_SetRankHandler);
		_makeFunc.Add((ushort)PacketID.S_GameOver, MakePacket<S_GameOver>);
		_handler.Add((ushort)PacketID.S_GameOver, PacketHandler.S_GameOverHandler);
		_makeFunc.Add((ushort)PacketID.S_EquipResult, MakePacket<S_EquipResult>);
		_handler.Add((ushort)PacketID.S_EquipResult, PacketHandler.S_EquipResultHandler);
		_makeFunc.Add((ushort)PacketID.S_BuyResult, MakePacket<S_BuyResult>);
		_handler.Add((ushort)PacketID.S_BuyResult, PacketHandler.S_BuyResultHandler);
		_makeFunc.Add((ushort)PacketID.S_StoreCatalog, MakePacket<S_StoreCatalog>);
		_handler.Add((ushort)PacketID.S_StoreCatalog, PacketHandler.S_StoreCatalogHandler);
		_makeFunc.Add((ushort)PacketID.S_BuyChangeName, MakePacket<S_BuyChangeName>);
		_handler.Add((ushort)PacketID.S_BuyChangeName, PacketHandler.S_BuyChangeNameHandler);
		_makeFunc.Add((ushort)PacketID.S_InviteGroupResult, MakePacket<S_InviteGroupResult>);
		_handler.Add((ushort)PacketID.S_InviteGroupResult, PacketHandler.S_InviteGroupResultHandler);
		_makeFunc.Add((ushort)PacketID.S_RequestInviteGroup, MakePacket<S_RequestInviteGroup>);
		_handler.Add((ushort)PacketID.S_RequestInviteGroup, PacketHandler.S_RequestInviteGroupHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastJoinGroup, MakePacket<S_BroadcastJoinGroup>);
		_handler.Add((ushort)PacketID.S_BroadcastJoinGroup, PacketHandler.S_BroadcastJoinGroupHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastLeaveGroup, MakePacket<S_BroadcastLeaveGroup>);
		_handler.Add((ushort)PacketID.S_BroadcastLeaveGroup, PacketHandler.S_BroadcastLeaveGroupHandler);
		_makeFunc.Add((ushort)PacketID.S_GroupUpdate, MakePacket<S_GroupUpdate>);
		_handler.Add((ushort)PacketID.S_GroupUpdate, PacketHandler.S_GroupUpdateHandler);
		_makeFunc.Add((ushort)PacketID.S_UserInformation, MakePacket<S_UserInformation>);
		_handler.Add((ushort)PacketID.S_UserInformation, PacketHandler.S_UserInformationHandler);
		_makeFunc.Add((ushort)PacketID.S_RankScoreUpdate, MakePacket<S_RankScoreUpdate>);
		_handler.Add((ushort)PacketID.S_RankScoreUpdate, PacketHandler.S_RankScoreUpdateHandler);
		_makeFunc.Add((ushort)PacketID.S_RecentGames, MakePacket<S_RecentGames>);
		_handler.Add((ushort)PacketID.S_RecentGames, PacketHandler.S_RecentGamesHandler);
		_makeFunc.Add((ushort)PacketID.S_Leaderboard, MakePacket<S_Leaderboard>);
		_handler.Add((ushort)PacketID.S_Leaderboard, PacketHandler.S_LeaderboardHandler);
		_makeFunc.Add((ushort)PacketID.S_PlayerUpdate, MakePacket<S_PlayerUpdate>);
		_handler.Add((ushort)PacketID.S_PlayerUpdate, PacketHandler.S_PlayerUpdateHandler);
		_makeFunc.Add((ushort)PacketID.S_LevelUp, MakePacket<S_LevelUp>);
		_handler.Add((ushort)PacketID.S_LevelUp, PacketHandler.S_LevelUpHandler);
		_makeFunc.Add((ushort)PacketID.S_BuyStarsResult, MakePacket<S_BuyStarsResult>);
		_handler.Add((ushort)PacketID.S_BuyStarsResult, PacketHandler.S_BuyStarsResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ConfirmAddStarResult, MakePacket<S_ConfirmAddStarResult>);
		_handler.Add((ushort)PacketID.S_ConfirmAddStarResult, PacketHandler.S_ConfirmAddStarResultHandler);
		_makeFunc.Add((ushort)PacketID.S_BattlePassInfo, MakePacket<S_BattlePassInfo>);
		_handler.Add((ushort)PacketID.S_BattlePassInfo, PacketHandler.S_BattlePassInfoHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimBattlePassRewardResult, MakePacket<S_ClaimBattlePassRewardResult>);
		_handler.Add((ushort)PacketID.S_ClaimBattlePassRewardResult, PacketHandler.S_ClaimBattlePassRewardResultHandler);
		_makeFunc.Add((ushort)PacketID.S_ClaimBattlePassAllResult, MakePacket<S_ClaimBattlePassAllResult>);
		_handler.Add((ushort)PacketID.S_ClaimBattlePassAllResult, PacketHandler.S_ClaimBattlePassAllResultHandler);
		_makeFunc.Add((ushort)PacketID.S_BuyBattlePassResult, MakePacket<S_BuyBattlePassResult>);
		_handler.Add((ushort)PacketID.S_BuyBattlePassResult, PacketHandler.S_BuyBattlePassResultHandler);

	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
		if (_makeFunc.TryGetValue(id, out func))
		{
			IPacket packet = func.Invoke(session, buffer);
			if (onRecvCallback != null)
				onRecvCallback.Invoke(session, packet);
			else
				HandlePacket(session, packet);
		}
	}

	T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
	{
		T pkt = new T();
		pkt.Read(buffer);
		return pkt;
	}

	public void HandlePacket(PacketSession session, IPacket packet)
	{
		Action<PacketSession, IPacket> action = null;
		if (_handler.TryGetValue(packet.Protocol, out action))
			action.Invoke(session, packet);
	}
}