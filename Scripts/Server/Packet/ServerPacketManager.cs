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
		_makeFunc.Add((ushort)PacketID.C_Ping, MakePacket<C_Ping>);
		_handler.Add((ushort)PacketID.C_Ping, PacketHandler.C_PingHandler);
		_makeFunc.Add((ushort)PacketID.C_SteamLogin, MakePacket<C_SteamLogin>);
		_handler.Add((ushort)PacketID.C_SteamLogin, PacketHandler.C_SteamLoginHandler);
		_makeFunc.Add((ushort)PacketID.C_CheckSteamNickName, MakePacket<C_CheckSteamNickName>);
		_handler.Add((ushort)PacketID.C_CheckSteamNickName, PacketHandler.C_CheckSteamNickNameHandler);
		_makeFunc.Add((ushort)PacketID.C_SetSteamNickName, MakePacket<C_SetSteamNickName>);
		_handler.Add((ushort)PacketID.C_SetSteamNickName, PacketHandler.C_SetSteamNickNameHandler);
		_makeFunc.Add((ushort)PacketID.C_AgreePolicy, MakePacket<C_AgreePolicy>);
		_handler.Add((ushort)PacketID.C_AgreePolicy, PacketHandler.C_AgreePolicyHandler);
		_makeFunc.Add((ushort)PacketID.C_Register, MakePacket<C_Register>);
		_handler.Add((ushort)PacketID.C_Register, PacketHandler.C_RegisterHandler);
		_makeFunc.Add((ushort)PacketID.C_Login, MakePacket<C_Login>);
		_handler.Add((ushort)PacketID.C_Login, PacketHandler.C_LoginHandler);
		_makeFunc.Add((ushort)PacketID.C_AutoLogin, MakePacket<C_AutoLogin>);
		_handler.Add((ushort)PacketID.C_AutoLogin, PacketHandler.C_AutoLoginHandler);
		_makeFunc.Add((ushort)PacketID.C_Logout, MakePacket<C_Logout>);
		_handler.Add((ushort)PacketID.C_Logout, PacketHandler.C_LogoutHandler);
		_makeFunc.Add((ushort)PacketID.C_TryFindMatch, MakePacket<C_TryFindMatch>);
		_handler.Add((ushort)PacketID.C_TryFindMatch, PacketHandler.C_TryFindMatchHandler);
		_makeFunc.Add((ushort)PacketID.C_CancelFindMatch, MakePacket<C_CancelFindMatch>);
		_handler.Add((ushort)PacketID.C_CancelFindMatch, PacketHandler.C_CancelFindMatchHandler);
		_makeFunc.Add((ushort)PacketID.C_EnterLobby, MakePacket<C_EnterLobby>);
		_handler.Add((ushort)PacketID.C_EnterLobby, PacketHandler.C_EnterLobbyHandler);
		_makeFunc.Add((ushort)PacketID.C_EnterGame, MakePacket<C_EnterGame>);
		_handler.Add((ushort)PacketID.C_EnterGame, PacketHandler.C_EnterGameHandler);
		_makeFunc.Add((ushort)PacketID.C_LeaveGame, MakePacket<C_LeaveGame>);
		_handler.Add((ushort)PacketID.C_LeaveGame, PacketHandler.C_LeaveGameHandler);
		_makeFunc.Add((ushort)PacketID.C_Chat, MakePacket<C_Chat>);
		_handler.Add((ushort)PacketID.C_Chat, PacketHandler.C_ChatHandler);
		_makeFunc.Add((ushort)PacketID.C_LobbyChat, MakePacket<C_LobbyChat>);
		_handler.Add((ushort)PacketID.C_LobbyChat, PacketHandler.C_LobbyChatHandler);
		_makeFunc.Add((ushort)PacketID.C_Input, MakePacket<C_Input>);
		_handler.Add((ushort)PacketID.C_Input, PacketHandler.C_InputHandler);
		_makeFunc.Add((ushort)PacketID.C_BotSnapshot, MakePacket<C_BotSnapshot>);
		_handler.Add((ushort)PacketID.C_BotSnapshot, PacketHandler.C_BotSnapshotHandler);
		_makeFunc.Add((ushort)PacketID.C_EquipItem, MakePacket<C_EquipItem>);
		_handler.Add((ushort)PacketID.C_EquipItem, PacketHandler.C_EquipItemHandler);
		_makeFunc.Add((ushort)PacketID.C_BuyOffer, MakePacket<C_BuyOffer>);
		_handler.Add((ushort)PacketID.C_BuyOffer, PacketHandler.C_BuyOfferHandler);
		_makeFunc.Add((ushort)PacketID.C_RequestStore, MakePacket<C_RequestStore>);
		_handler.Add((ushort)PacketID.C_RequestStore, PacketHandler.C_RequestStoreHandler);
		_makeFunc.Add((ushort)PacketID.C_InviteGroup, MakePacket<C_InviteGroup>);
		_handler.Add((ushort)PacketID.C_InviteGroup, PacketHandler.C_InviteGroupHandler);
		_makeFunc.Add((ushort)PacketID.C_ReplyInviteGroup, MakePacket<C_ReplyInviteGroup>);
		_handler.Add((ushort)PacketID.C_ReplyInviteGroup, PacketHandler.C_ReplyInviteGroupHandler);
		_makeFunc.Add((ushort)PacketID.C_LeaveGroup, MakePacket<C_LeaveGroup>);
		_handler.Add((ushort)PacketID.C_LeaveGroup, PacketHandler.C_LeaveGroupHandler);
		_makeFunc.Add((ushort)PacketID.C_RequestUserInformation, MakePacket<C_RequestUserInformation>);
		_handler.Add((ushort)PacketID.C_RequestUserInformation, PacketHandler.C_RequestUserInformationHandler);
		_makeFunc.Add((ushort)PacketID.C_RequestRecentGames, MakePacket<C_RequestRecentGames>);
		_handler.Add((ushort)PacketID.C_RequestRecentGames, PacketHandler.C_RequestRecentGamesHandler);
		_makeFunc.Add((ushort)PacketID.C_RequestLeaderboard, MakePacket<C_RequestLeaderboard>);
		_handler.Add((ushort)PacketID.C_RequestLeaderboard, PacketHandler.C_RequestLeaderboardHandler);
		_makeFunc.Add((ushort)PacketID.C_RequestAddStar, MakePacket<C_RequestAddStar>);
		_handler.Add((ushort)PacketID.C_RequestAddStar, PacketHandler.C_RequestAddStarHandler);
		_makeFunc.Add((ushort)PacketID.C_ConfirmAddStar, MakePacket<C_ConfirmAddStar>);
		_handler.Add((ushort)PacketID.C_ConfirmAddStar, PacketHandler.C_ConfirmAddStarHandler);
		_makeFunc.Add((ushort)PacketID.C_RequestBattlePass, MakePacket<C_RequestBattlePass>);
		_handler.Add((ushort)PacketID.C_RequestBattlePass, PacketHandler.C_RequestBattlePassHandler);
		_makeFunc.Add((ushort)PacketID.C_ClaimBattlePassReward, MakePacket<C_ClaimBattlePassReward>);
		_handler.Add((ushort)PacketID.C_ClaimBattlePassReward, PacketHandler.C_ClaimBattlePassRewardHandler);
		_makeFunc.Add((ushort)PacketID.C_ClaimBattlePassAll, MakePacket<C_ClaimBattlePassAll>);
		_handler.Add((ushort)PacketID.C_ClaimBattlePassAll, PacketHandler.C_ClaimBattlePassAllHandler);
		_makeFunc.Add((ushort)PacketID.C_BuyBattlePass, MakePacket<C_BuyBattlePass>);
		_handler.Add((ushort)PacketID.C_BuyBattlePass, PacketHandler.C_BuyBattlePassHandler);

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