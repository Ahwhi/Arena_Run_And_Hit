using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

public enum PacketID
{
	C_Ping = 1,
	S_Pong = 2,
	S_Population = 3,
	C_SteamLogin = 4,
	S_SteamLoginResult = 5,
	C_CheckSteamNickName = 6,
	C_SetSteamNickName = 7,
	S_CheckSteamNameResult = 8,
	S_SetSteamNameResult = 9,
	C_AgreePolicy = 10,
	S_AgreePolicyResult = 11,
	C_Register = 12,
	S_RegisterResult = 13,
	C_Login = 14,
	S_LoginResult = 15,
	C_AutoLogin = 16,
	S_AutoLoginResult = 17,
	C_Logout = 18,
	S_LogoutResult = 19,
	C_TryFindMatch = 20,
	C_CancelFindMatch = 21,
	S_GroupLeaderFindMatch = 22,
	S_GroupLeaderCancelMatch = 23,
	C_EnterLobby = 24,
	S_MatchFound = 25,
	S_GameSetting = 26,
	C_EnterGame = 27,
	S_GameReady = 28,
	C_LeaveGame = 29,
	S_BroadcastLeaveGame = 30,
	S_PlayerList = 31,
	S_Respawn = 32,
	S_BotInit = 33,
	C_Chat = 34,
	S_BroadcastChat = 35,
	C_LobbyChat = 36,
	S_BroadcastLobbyChat = 37,
	C_Input = 38,
	S_Snapshot = 39,
	S_WorldSnapshot = 40,
	S_BroadcastAttack = 41,
	S_Hit = 42,
	S_Health = 43,
	S_BroadcastEmote = 44,
	C_BotSnapshot = 45,
	S_BotOwnerChanged = 46,
	S_GameStatus = 47,
	S_SupplySpawn = 48,
	S_SupplyGone = 49,
	S_SupplyApplied = 50,
	S_VomitExplode = 51,
	S_MatchTimer = 52,
	S_SetRank = 53,
	S_GameOver = 54,
	C_EquipItem = 55,
	S_EquipResult = 56,
	C_BuyOffer = 57,
	S_BuyResult = 58,
	S_StoreCatalog = 59,
	C_RequestStore = 60,
	S_BuyChangeName = 61,
	C_InviteGroup = 62,
	C_ReplyInviteGroup = 63,
	S_InviteGroupResult = 64,
	S_RequestInviteGroup = 65,
	C_LeaveGroup = 66,
	S_BroadcastJoinGroup = 67,
	S_BroadcastLeaveGroup = 68,
	S_GroupUpdate = 69,
	C_RequestUserInformation = 70,
	S_UserInformation = 71,
	S_RankScoreUpdate = 72,
	C_RequestRecentGames = 73,
	S_RecentGames = 74,
	C_RequestLeaderboard = 75,
	S_Leaderboard = 76,
	S_PlayerUpdate = 77,
	S_LevelUp = 78,
	C_RequestAddStar = 79,
	S_BuyStarsResult = 80,
	C_ConfirmAddStar = 81,
	S_ConfirmAddStarResult = 82,
	C_RequestBattlePass = 83,
	S_BattlePassInfo = 84,
	C_ClaimBattlePassReward = 85,
	S_ClaimBattlePassRewardResult = 86,
	C_ClaimBattlePassAll = 87,
	S_ClaimBattlePassAllResult = 88,
	C_BuyBattlePass = 89,
	S_BuyBattlePassResult = 90,
	
}

public interface IPacket
{
	ushort Protocol { get; }
	void Read(ArraySegment<byte> segment);
	ArraySegment<byte> Write();
}


public class C_Ping : IPacket
{
	public int seq;
	public int clientSentTick;

	public ushort Protocol { get { return (ushort)PacketID.C_Ping; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.seq = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.clientSentTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Ping), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.seq), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.clientSentTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Pong : IPacket
{
	public int seq;
	public int serverTick;

	public ushort Protocol { get { return (ushort)PacketID.S_Pong; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.seq = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Pong), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.seq), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Population : IPacket
{
	public int total;

	public ushort Protocol { get { return (ushort)PacketID.S_Population; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.total = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Population), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.total), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SteamLogin : IPacket
{
	public string ticketHex;
	public string personaName;

	public ushort Protocol { get { return (ushort)PacketID.C_SteamLogin; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort ticketHexLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.ticketHex = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, ticketHexLen);
		count += ticketHexLen;
		ushort personaNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.personaName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, personaNameLen);
		count += personaNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SteamLogin), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort ticketHexLen = (ushort)Encoding.Unicode.GetBytes(this.ticketHex, 0, this.ticketHex.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(ticketHexLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += ticketHexLen;
		ushort personaNameLen = (ushort)Encoding.Unicode.GetBytes(this.personaName, 0, this.personaName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(personaNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += personaNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SteamLoginResult : IPacket
{
	public bool isSuccess;
	public string accountId;
	public string nickName;
	public int failReason;
	public bool needNickSetup;
	public bool needPolicyAgreement;

	public ushort Protocol { get { return (ushort)PacketID.S_SteamLoginResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort accountIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accountId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accountIdLen);
		count += accountIdLen;
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.needNickSetup = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.needPolicyAgreement = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SteamLoginResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		ushort accountIdLen = (ushort)Encoding.Unicode.GetBytes(this.accountId, 0, this.accountId.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accountIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accountIdLen;
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.needNickSetup), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.needPolicyAgreement), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CheckSteamNickName : IPacket
{
	public string nickName;

	public ushort Protocol { get { return (ushort)PacketID.C_CheckSteamNickName; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CheckSteamNickName), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_SetSteamNickName : IPacket
{
	public string nickName;

	public ushort Protocol { get { return (ushort)PacketID.C_SetSteamNickName; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_SetSteamNickName), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_CheckSteamNameResult : IPacket
{
	public bool isSuccess;

	public ushort Protocol { get { return (ushort)PacketID.S_CheckSteamNameResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_CheckSteamNameResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SetSteamNameResult : IPacket
{
	public bool isSuccess;
	public string nickName;

	public ushort Protocol { get { return (ushort)PacketID.S_SetSteamNameResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SetSteamNameResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_AgreePolicy : IPacket
{
	public int policyVersion;

	public ushort Protocol { get { return (ushort)PacketID.C_AgreePolicy; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.policyVersion = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_AgreePolicy), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.policyVersion), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_AgreePolicyResult : IPacket
{
	public bool isSuccess;
	public int policyVersion;

	public ushort Protocol { get { return (ushort)PacketID.S_AgreePolicyResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.policyVersion = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_AgreePolicyResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.policyVersion), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Register : IPacket
{
	public string accountId;
	public string accountPw;
	public string nickName;

	public ushort Protocol { get { return (ushort)PacketID.C_Register; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort accountIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accountId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accountIdLen);
		count += accountIdLen;
		ushort accountPwLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accountPw = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accountPwLen);
		count += accountPwLen;
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Register), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort accountIdLen = (ushort)Encoding.Unicode.GetBytes(this.accountId, 0, this.accountId.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accountIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accountIdLen;
		ushort accountPwLen = (ushort)Encoding.Unicode.GetBytes(this.accountPw, 0, this.accountPw.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accountPwLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accountPwLen;
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_RegisterResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_RegisterResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_RegisterResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Login : IPacket
{
	public string accountId;
	public string accountPw;
	public bool isAutoLogin;

	public ushort Protocol { get { return (ushort)PacketID.C_Login; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort accountIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accountId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accountIdLen);
		count += accountIdLen;
		ushort accountPwLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accountPw = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accountPwLen);
		count += accountPwLen;
		this.isAutoLogin = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Login), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort accountIdLen = (ushort)Encoding.Unicode.GetBytes(this.accountId, 0, this.accountId.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accountIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accountIdLen;
		ushort accountPwLen = (ushort)Encoding.Unicode.GetBytes(this.accountPw, 0, this.accountPw.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accountPwLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accountPwLen;
		Array.Copy(BitConverter.GetBytes(this.isAutoLogin), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_LoginResult : IPacket
{
	public bool isSuccess;
	public string accountId;
	public string nickName;
	public int failReason;
	public string accessToken;

	public ushort Protocol { get { return (ushort)PacketID.S_LoginResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort accountIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accountId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accountIdLen);
		count += accountIdLen;
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort accessTokenLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accessToken = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accessTokenLen);
		count += accessTokenLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_LoginResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		ushort accountIdLen = (ushort)Encoding.Unicode.GetBytes(this.accountId, 0, this.accountId.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accountIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accountIdLen;
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		ushort accessTokenLen = (ushort)Encoding.Unicode.GetBytes(this.accessToken, 0, this.accessToken.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accessTokenLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accessTokenLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_AutoLogin : IPacket
{
	public string accessToken;

	public ushort Protocol { get { return (ushort)PacketID.C_AutoLogin; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort accessTokenLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accessToken = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accessTokenLen);
		count += accessTokenLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_AutoLogin), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort accessTokenLen = (ushort)Encoding.Unicode.GetBytes(this.accessToken, 0, this.accessToken.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accessTokenLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accessTokenLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_AutoLoginResult : IPacket
{
	public bool isSuccess;
	public string accountId;
	public string nickName;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_AutoLoginResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort accountIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.accountId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, accountIdLen);
		count += accountIdLen;
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_AutoLoginResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		ushort accountIdLen = (ushort)Encoding.Unicode.GetBytes(this.accountId, 0, this.accountId.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(accountIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += accountIdLen;
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Logout : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_Logout; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Logout), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_LogoutResult : IPacket
{
	public bool isSuccess;

	public ushort Protocol { get { return (ushort)PacketID.S_LogoutResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_LogoutResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_TryFindMatch : IPacket
{
	public int gameMode;

	public ushort Protocol { get { return (ushort)PacketID.C_TryFindMatch; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.gameMode = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_TryFindMatch), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.gameMode), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_CancelFindMatch : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_CancelFindMatch; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_CancelFindMatch), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GroupLeaderFindMatch : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.S_GroupLeaderFindMatch; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GroupLeaderFindMatch), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GroupLeaderCancelMatch : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.S_GroupLeaderCancelMatch; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GroupLeaderCancelMatch), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EnterLobby : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_EnterLobby; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EnterLobby), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_MatchFound : IPacket
{
	public int gameMode;
	public int playerCount;
	public int botCount;
	public int botOwnerId;
	public class BotIds
	{
		public int botId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.botId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.botId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<BotIds> botIdss = new List<BotIds>();

	public ushort Protocol { get { return (ushort)PacketID.S_MatchFound; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.gameMode = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.playerCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.botCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.botOwnerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.botIdss.Clear();
		ushort botIdsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < botIdsLen; i++)
		{
			BotIds botIds = new BotIds();
			botIds.Read(segment, ref count);
			botIdss.Add(botIds);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_MatchFound), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.gameMode), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.playerCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.botCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.botOwnerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.botIdss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (BotIds botIds in this.botIdss)
			botIds.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GameSetting : IPacket
{
	public int gameMode;

	public ushort Protocol { get { return (ushort)PacketID.S_GameSetting; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.gameMode = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GameSetting), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.gameMode), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EnterGame : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_EnterGame; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EnterGame), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GameReady : IPacket
{
	public int startServerTick;
	public int countdownSec;

	public ushort Protocol { get { return (ushort)PacketID.S_GameReady; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.startServerTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.countdownSec = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GameReady), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.startServerTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.countdownSec), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_LeaveGame : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_LeaveGame; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_LeaveGame), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BroadcastLeaveGame : IPacket
{
	public int playerId;

	public ushort Protocol { get { return (ushort)PacketID.S_BroadcastLeaveGame; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastLeaveGame), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_PlayerList : IPacket
{
	public class Player
	{
		public bool isSelf;
		public int playerId;
		public string nickName;
		public int colorIndex;
		public float posX;
		public float posY;
		public float posZ;
		public string characterSku;
		public string trailSku;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.isSelf = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
			count += nickNameLen;
			this.colorIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			ushort characterSkuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.characterSku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, characterSkuLen);
			count += characterSkuLen;
			ushort trailSkuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.trailSku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, trailSkuLen);
			count += trailSkuLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.isSelf), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nickNameLen;
			Array.Copy(BitConverter.GetBytes(this.colorIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			ushort characterSkuLen = (ushort)Encoding.Unicode.GetBytes(this.characterSku, 0, this.characterSku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(characterSkuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += characterSkuLen;
			ushort trailSkuLen = (ushort)Encoding.Unicode.GetBytes(this.trailSku, 0, this.trailSku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(trailSkuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += trailSkuLen;
			return success;
		}	
	}
	public List<Player> players = new List<Player>();

	public ushort Protocol { get { return (ushort)PacketID.S_PlayerList; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.players.Clear();
		ushort playerLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < playerLen; i++)
		{
			Player player = new Player();
			player.Read(segment, ref count);
			players.Add(player);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_PlayerList), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.players.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Player player in this.players)
			player.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Respawn : IPacket
{
	public int playerId;
	public float posX;
	public float posY;
	public float posZ;
	public float yaw;

	public ushort Protocol { get { return (ushort)PacketID.S_Respawn; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.yaw = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Respawn), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.yaw), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BotInit : IPacket
{
	public class Info
	{
		public int botId;
		public int colorIndex;
		public float posX;
		public float posY;
		public float posZ;
		public float yaw;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.botId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.colorIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.yaw = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.botId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.colorIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.yaw), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			return success;
		}	
	}
	public List<Info> infos = new List<Info>();

	public ushort Protocol { get { return (ushort)PacketID.S_BotInit; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.infos.Clear();
		ushort infoLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < infoLen; i++)
		{
			Info info = new Info();
			info.Read(segment, ref count);
			infos.Add(info);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BotInit), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.infos.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Info info in this.infos)
			info.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Chat : IPacket
{
	public string message;

	public ushort Protocol { get { return (ushort)PacketID.C_Chat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort messageLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.message = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, messageLen);
		count += messageLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Chat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort messageLen = (ushort)Encoding.Unicode.GetBytes(this.message, 0, this.message.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(messageLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += messageLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BroadcastChat : IPacket
{
	public int playerId;
	public string nickName;
	public string message;

	public ushort Protocol { get { return (ushort)PacketID.S_BroadcastChat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
		ushort messageLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.message = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, messageLen);
		count += messageLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastChat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;
		ushort messageLen = (ushort)Encoding.Unicode.GetBytes(this.message, 0, this.message.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(messageLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += messageLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_LobbyChat : IPacket
{
	public int type;
	public string message;

	public ushort Protocol { get { return (ushort)PacketID.C_LobbyChat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.type = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort messageLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.message = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, messageLen);
		count += messageLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_LobbyChat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.type), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		ushort messageLen = (ushort)Encoding.Unicode.GetBytes(this.message, 0, this.message.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(messageLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += messageLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BroadcastLobbyChat : IPacket
{
	public int type;
	public int playerId;
	public string nickName;
	public string message;

	public ushort Protocol { get { return (ushort)PacketID.S_BroadcastLobbyChat; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.type = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
		ushort messageLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.message = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, messageLen);
		count += messageLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastLobbyChat), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.type), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;
		ushort messageLen = (ushort)Encoding.Unicode.GetBytes(this.message, 0, this.message.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(messageLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += messageLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_Input : IPacket
{
	public int seq;
	public float moveX;
	public float moveZ;
	public float yaw;
	public byte action;
	public byte emoteSlot;

	public ushort Protocol { get { return (ushort)PacketID.C_Input; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.seq = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.moveX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.moveZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.yaw = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.action = (byte)segment.Array[segment.Offset + count];
		count += sizeof(byte);
		this.emoteSlot = (byte)segment.Array[segment.Offset + count];
		count += sizeof(byte);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_Input), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.seq), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.moveX), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.moveZ), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.yaw), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		segment.Array[segment.Offset + count] = (byte)this.action;
		count += sizeof(byte);
		segment.Array[segment.Offset + count] = (byte)this.emoteSlot;
		count += sizeof(byte);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Snapshot : IPacket
{
	public int serverTick;
	public float posX;
	public float posY;
	public float posZ;
	public float yaw;
	public int lastProcessedInputSeq;

	public ushort Protocol { get { return (ushort)PacketID.S_Snapshot; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.yaw = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.lastProcessedInputSeq = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Snapshot), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.yaw), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.lastProcessedInputSeq), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_WorldSnapshot : IPacket
{
	public int serverTick;
	public class Entities
	{
		public int playerId;
		public float posX;
		public float posY;
		public float posZ;
		public float yaw;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.yaw = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.yaw), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			return success;
		}	
	}
	public List<Entities> entitiess = new List<Entities>();

	public ushort Protocol { get { return (ushort)PacketID.S_WorldSnapshot; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.entitiess.Clear();
		ushort entitiesLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < entitiesLen; i++)
		{
			Entities entities = new Entities();
			entities.Read(segment, ref count);
			entitiess.Add(entities);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_WorldSnapshot), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.entitiess.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Entities entities in this.entitiess)
			entities.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BroadcastAttack : IPacket
{
	public int serverTick;
	public int playerId;

	public ushort Protocol { get { return (ushort)PacketID.S_BroadcastAttack; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastAttack), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Hit : IPacket
{
	public int serverTick;
	public int attackerId;
	public int targetId;
	public int damage;
	public int hpAfter;
	public bool killed;

	public ushort Protocol { get { return (ushort)PacketID.S_Hit; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.attackerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.targetId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.damage = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hpAfter = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.killed = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Hit), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.attackerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.targetId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.damage), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hpAfter), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.killed), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Health : IPacket
{
	public int playerId;
	public int hp;
	public int maxHp;

	public ushort Protocol { get { return (ushort)PacketID.S_Health; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxHp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Health), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxHp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BroadcastEmote : IPacket
{
	public int serverTick;
	public int playerId;
	public int emoteId;
	public string emoteSku;

	public ushort Protocol { get { return (ushort)PacketID.S_BroadcastEmote; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.emoteId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		ushort emoteSkuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.emoteSku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, emoteSkuLen);
		count += emoteSkuLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastEmote), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.emoteId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		ushort emoteSkuLen = (ushort)Encoding.Unicode.GetBytes(this.emoteSku, 0, this.emoteSku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(emoteSkuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += emoteSkuLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_BotSnapshot : IPacket
{
	public class Bots
	{
		public int botId;
		public float posX;
		public float posY;
		public float posZ;
		public float yaw;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.botId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.yaw = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.botId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.yaw), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			return success;
		}	
	}
	public List<Bots> botss = new List<Bots>();

	public ushort Protocol { get { return (ushort)PacketID.C_BotSnapshot; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.botss.Clear();
		ushort botsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < botsLen; i++)
		{
			Bots bots = new Bots();
			bots.Read(segment, ref count);
			botss.Add(bots);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_BotSnapshot), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.botss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Bots bots in this.botss)
			bots.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BotOwnerChanged : IPacket
{
	public int ownerId;
	public class BotIds
	{
		public int botId;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.botId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.botId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<BotIds> botIdss = new List<BotIds>();

	public ushort Protocol { get { return (ushort)PacketID.S_BotOwnerChanged; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.ownerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.botIdss.Clear();
		ushort botIdsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < botIdsLen; i++)
		{
			BotIds botIds = new BotIds();
			botIds.Read(segment, ref count);
			botIdss.Add(botIds);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BotOwnerChanged), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.ownerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.botIdss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (BotIds botIds in this.botIdss)
			botIds.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GameStatus : IPacket
{
	public int aliveCount;
	public class Bots
	{
		public int botId;
		public int kills;
		public int deaths;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.botId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kills = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.deaths = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.botId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kills), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.deaths), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Bots> botss = new List<Bots>();
	public class Player
	{
		public int playerId;
		public int kills;
		public int deaths;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kills = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.deaths = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kills), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.deaths), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Player> players = new List<Player>();

	public ushort Protocol { get { return (ushort)PacketID.S_GameStatus; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.aliveCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.botss.Clear();
		ushort botsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < botsLen; i++)
		{
			Bots bots = new Bots();
			bots.Read(segment, ref count);
			botss.Add(bots);
		}
		this.players.Clear();
		ushort playerLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < playerLen; i++)
		{
			Player player = new Player();
			player.Read(segment, ref count);
			players.Add(player);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GameStatus), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.aliveCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.botss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Bots bots in this.botss)
			bots.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.players.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Player player in this.players)
			player.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SupplySpawn : IPacket
{
	public int dropId;
	public int effect;
	public float posX;
	public float posY;
	public float posZ;

	public ushort Protocol { get { return (ushort)PacketID.S_SupplySpawn; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.dropId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.effect = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SupplySpawn), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.dropId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.effect), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SupplyGone : IPacket
{
	public int dropId;
	public int reason;

	public ushort Protocol { get { return (ushort)PacketID.S_SupplyGone; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.dropId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.reason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SupplyGone), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.dropId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.reason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SupplyApplied : IPacket
{
	public int serverTick;
	public int playerId;
	public int effect;
	public int amount;
	public int durationMs;

	public ushort Protocol { get { return (ushort)PacketID.S_SupplyApplied; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.effect = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.amount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.durationMs = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SupplyApplied), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.effect), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.amount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.durationMs), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_VomitExplode : IPacket
{
	public int serverTick;
	public int ownerId;
	public float posX;
	public float posY;
	public float posZ;
	public float radius;

	public ushort Protocol { get { return (ushort)PacketID.S_VomitExplode; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.serverTick = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.ownerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.posX = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posY = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.posZ = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
		this.radius = BitConverter.ToSingle(segment.Array, segment.Offset + count);
		count += sizeof(float);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_VomitExplode), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.serverTick), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.ownerId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.posX), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posY), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.posZ), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);
		Array.Copy(BitConverter.GetBytes(this.radius), 0, segment.Array, segment.Offset + count, sizeof(float));
		count += sizeof(float);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_MatchTimer : IPacket
{
	public int remainingSec;

	public ushort Protocol { get { return (ushort)PacketID.S_MatchTimer; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.remainingSec = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_MatchTimer), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.remainingSec), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_SetRank : IPacket
{
	public int rank;

	public ushort Protocol { get { return (ushort)PacketID.S_SetRank; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_SetRank), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GameOver : IPacket
{
	public int totalPlayers;
	public class Results
	{
		public int playerId;
		public string nickName;
		public bool isBot;
		public int rank;
		public int kills;
		public int deaths;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
			count += nickNameLen;
			this.isBot = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kills = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.deaths = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nickNameLen;
			Array.Copy(BitConverter.GetBytes(this.isBot), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kills), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.deaths), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Results> resultss = new List<Results>();

	public ushort Protocol { get { return (ushort)PacketID.S_GameOver; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.totalPlayers = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.resultss.Clear();
		ushort resultsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < resultsLen; i++)
		{
			Results results = new Results();
			results.Read(segment, ref count);
			resultss.Add(results);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GameOver), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.totalPlayers), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.resultss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Results results in this.resultss)
			results.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_EquipItem : IPacket
{
	public string slot;
	public string sku;

	public ushort Protocol { get { return (ushort)PacketID.C_EquipItem; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.slot = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, slotLen);
		count += slotLen;
		ushort skuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.sku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, skuLen);
		count += skuLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_EquipItem), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort slotLen = (ushort)Encoding.Unicode.GetBytes(this.slot, 0, this.slot.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(slotLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += slotLen;
		ushort skuLen = (ushort)Encoding.Unicode.GetBytes(this.sku, 0, this.sku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(skuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += skuLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_EquipResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public bool isDetach;

	public ushort Protocol { get { return (ushort)PacketID.S_EquipResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isDetach = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_EquipResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isDetach), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_BuyOffer : IPacket
{
	public string offerId;
	public string idempotencyKey;

	public ushort Protocol { get { return (ushort)PacketID.C_BuyOffer; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
		count += offerIdLen;
		ushort idempotencyKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.idempotencyKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, idempotencyKeyLen);
		count += idempotencyKeyLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_BuyOffer), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(this.offerId, 0, this.offerId.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += offerIdLen;
		ushort idempotencyKeyLen = (ushort)Encoding.Unicode.GetBytes(this.idempotencyKey, 0, this.idempotencyKey.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(idempotencyKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += idempotencyKeyLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BuyResult : IPacket
{
	public bool isSuccess;
	public int failReason;

	public ushort Protocol { get { return (ushort)PacketID.S_BuyResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BuyResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_StoreCatalog : IPacket
{
	public int version;
	public int gold;
	public int star;
	public class Items
	{
		public string sku;
		public string name;
		public string category;
		public string imageKey;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort skuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.sku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, skuLen);
			count += skuLen;
			ushort nameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.name = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nameLen);
			count += nameLen;
			ushort categoryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.category = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, categoryLen);
			count += categoryLen;
			ushort imageKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.imageKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, imageKeyLen);
			count += imageKeyLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			ushort skuLen = (ushort)Encoding.Unicode.GetBytes(this.sku, 0, this.sku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(skuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += skuLen;
			ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nameLen;
			ushort categoryLen = (ushort)Encoding.Unicode.GetBytes(this.category, 0, this.category.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(categoryLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += categoryLen;
			ushort imageKeyLen = (ushort)Encoding.Unicode.GetBytes(this.imageKey, 0, this.imageKey.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(imageKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += imageKeyLen;
			return success;
		}	
	}
	public List<Items> itemss = new List<Items>();
	public class Offers
	{
		public string offerId;
		public string displayName;
		public string imageKey;
		public string category;
		public bool visible;
		public class Prices
		{
			public string currency;
			public int amount;
		
			public void Read(ArraySegment<byte> segment, ref ushort count)
			{
				ushort currencyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
				count += sizeof(ushort);
				this.currency = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, currencyLen);
				count += currencyLen;
				this.amount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
				count += sizeof(int);
			}
		
			public bool Write(ArraySegment<byte> segment, ref ushort count)
			{
				bool success = true;
				ushort currencyLen = (ushort)Encoding.Unicode.GetBytes(this.currency, 0, this.currency.Length, segment.Array, segment.Offset + count + sizeof(ushort));
				Array.Copy(BitConverter.GetBytes(currencyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
				count += sizeof(ushort);
				count += currencyLen;
				Array.Copy(BitConverter.GetBytes(this.amount), 0, segment.Array, segment.Offset + count, sizeof(int));
				count += sizeof(int);
				return success;
			}	
		}
		public List<Prices> pricess = new List<Prices>();
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort offerIdLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.offerId = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, offerIdLen);
			count += offerIdLen;
			ushort displayNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.displayName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, displayNameLen);
			count += displayNameLen;
			ushort imageKeyLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.imageKey = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, imageKeyLen);
			count += imageKeyLen;
			ushort categoryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.category = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, categoryLen);
			count += categoryLen;
			this.visible = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.pricess.Clear();
			ushort pricesLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			for (int i = 0; i < pricesLen; i++)
			{
				Prices prices = new Prices();
				prices.Read(segment, ref count);
				pricess.Add(prices);
			}
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			ushort offerIdLen = (ushort)Encoding.Unicode.GetBytes(this.offerId, 0, this.offerId.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(offerIdLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += offerIdLen;
			ushort displayNameLen = (ushort)Encoding.Unicode.GetBytes(this.displayName, 0, this.displayName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(displayNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += displayNameLen;
			ushort imageKeyLen = (ushort)Encoding.Unicode.GetBytes(this.imageKey, 0, this.imageKey.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(imageKeyLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += imageKeyLen;
			ushort categoryLen = (ushort)Encoding.Unicode.GetBytes(this.category, 0, this.category.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(categoryLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += categoryLen;
			Array.Copy(BitConverter.GetBytes(this.visible), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes((ushort)this.pricess.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			foreach (Prices prices in this.pricess)
				prices.Write(segment, ref count);
			return success;
		}	
	}
	public List<Offers> offerss = new List<Offers>();
	public class Inventory
	{
		public string sku;
		public int qty;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort skuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.sku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, skuLen);
			count += skuLen;
			this.qty = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			ushort skuLen = (ushort)Encoding.Unicode.GetBytes(this.sku, 0, this.sku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(skuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += skuLen;
			Array.Copy(BitConverter.GetBytes(this.qty), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Inventory> inventorys = new List<Inventory>();
	public class Equipped
	{
		public string slot;
		public string sku;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort slotLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.slot = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, slotLen);
			count += slotLen;
			ushort skuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.sku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, skuLen);
			count += skuLen;
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			ushort slotLen = (ushort)Encoding.Unicode.GetBytes(this.slot, 0, this.slot.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(slotLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += slotLen;
			ushort skuLen = (ushort)Encoding.Unicode.GetBytes(this.sku, 0, this.sku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(skuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += skuLen;
			return success;
		}	
	}
	public List<Equipped> equippeds = new List<Equipped>();

	public ushort Protocol { get { return (ushort)PacketID.S_StoreCatalog; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.version = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.star = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.itemss.Clear();
		ushort itemsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < itemsLen; i++)
		{
			Items items = new Items();
			items.Read(segment, ref count);
			itemss.Add(items);
		}
		this.offerss.Clear();
		ushort offersLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < offersLen; i++)
		{
			Offers offers = new Offers();
			offers.Read(segment, ref count);
			offerss.Add(offers);
		}
		this.inventorys.Clear();
		ushort inventoryLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < inventoryLen; i++)
		{
			Inventory inventory = new Inventory();
			inventory.Read(segment, ref count);
			inventorys.Add(inventory);
		}
		this.equippeds.Clear();
		ushort equippedLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < equippedLen; i++)
		{
			Equipped equipped = new Equipped();
			equipped.Read(segment, ref count);
			equippeds.Add(equipped);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_StoreCatalog), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.version), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.star), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.itemss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Items items in this.itemss)
			items.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.offerss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Offers offers in this.offerss)
			offers.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.inventorys.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Inventory inventory in this.inventorys)
			inventory.Write(segment, ref count);
		Array.Copy(BitConverter.GetBytes((ushort)this.equippeds.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Equipped equipped in this.equippeds)
			equipped.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestStore : IPacket
{
	public int knownVersion;

	public ushort Protocol { get { return (ushort)PacketID.C_RequestStore; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.knownVersion = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestStore), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.knownVersion), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BuyChangeName : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.S_BuyChangeName; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BuyChangeName), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_InviteGroup : IPacket
{
	public string LeaderNickName;
	public string ServentNickName;

	public ushort Protocol { get { return (ushort)PacketID.C_InviteGroup; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort LeaderNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.LeaderNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, LeaderNickNameLen);
		count += LeaderNickNameLen;
		ushort ServentNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.ServentNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, ServentNickNameLen);
		count += ServentNickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_InviteGroup), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort LeaderNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.LeaderNickName, 0, this.LeaderNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(LeaderNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += LeaderNickNameLen;
		ushort ServentNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.ServentNickName, 0, this.ServentNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(ServentNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += ServentNickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ReplyInviteGroup : IPacket
{
	public bool isAccept;
	public string InviterNickName;
	public string replierNickName;

	public ushort Protocol { get { return (ushort)PacketID.C_ReplyInviteGroup; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isAccept = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort InviterNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.InviterNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, InviterNickNameLen);
		count += InviterNickNameLen;
		ushort replierNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.replierNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, replierNickNameLen);
		count += replierNickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ReplyInviteGroup), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isAccept), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		ushort InviterNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.InviterNickName, 0, this.InviterNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(InviterNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += InviterNickNameLen;
		ushort replierNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.replierNickName, 0, this.replierNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(replierNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += replierNickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_InviteGroupResult : IPacket
{
	public bool isAvailable;
	public int failReason;
	public bool isAccepted;
	public string InviterNickName;
	public string replierNickName;

	public ushort Protocol { get { return (ushort)PacketID.S_InviteGroupResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isAvailable = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isAccepted = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort InviterNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.InviterNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, InviterNickNameLen);
		count += InviterNickNameLen;
		ushort replierNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.replierNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, replierNickNameLen);
		count += replierNickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_InviteGroupResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isAvailable), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isAccepted), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		ushort InviterNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.InviterNickName, 0, this.InviterNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(InviterNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += InviterNickNameLen;
		ushort replierNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.replierNickName, 0, this.replierNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(replierNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += replierNickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_RequestInviteGroup : IPacket
{
	public string InviterNickName;

	public ushort Protocol { get { return (ushort)PacketID.S_RequestInviteGroup; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort InviterNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.InviterNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, InviterNickNameLen);
		count += InviterNickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_RequestInviteGroup), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort InviterNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.InviterNickName, 0, this.InviterNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(InviterNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += InviterNickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_LeaveGroup : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_LeaveGroup; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_LeaveGroup), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BroadcastJoinGroup : IPacket
{
	public string joinnerNickName;

	public ushort Protocol { get { return (ushort)PacketID.S_BroadcastJoinGroup; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort joinnerNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.joinnerNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, joinnerNickNameLen);
		count += joinnerNickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastJoinGroup), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort joinnerNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.joinnerNickName, 0, this.joinnerNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(joinnerNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += joinnerNickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BroadcastLeaveGroup : IPacket
{
	public string leaverNickName;
	public bool isLeader;
	public string newLeaderNickName;

	public ushort Protocol { get { return (ushort)PacketID.S_BroadcastLeaveGroup; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort leaverNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.leaverNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, leaverNickNameLen);
		count += leaverNickNameLen;
		this.isLeader = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		ushort newLeaderNickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.newLeaderNickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, newLeaderNickNameLen);
		count += newLeaderNickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BroadcastLeaveGroup), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort leaverNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.leaverNickName, 0, this.leaverNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(leaverNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += leaverNickNameLen;
		Array.Copy(BitConverter.GetBytes(this.isLeader), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		ushort newLeaderNickNameLen = (ushort)Encoding.Unicode.GetBytes(this.newLeaderNickName, 0, this.newLeaderNickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(newLeaderNickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += newLeaderNickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_GroupUpdate : IPacket
{
	public bool isDestroy;
	public class Player
	{
		public bool isLeader;
		public bool isLobby;
		public int playerId;
		public string nickName;
		public int rank;
		public int level;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.isLeader = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.isLobby = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
			count += sizeof(bool);
			this.playerId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
			count += nickNameLen;
			this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.isLeader), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.isLobby), 0, segment.Array, segment.Offset + count, sizeof(bool));
			count += sizeof(bool);
			Array.Copy(BitConverter.GetBytes(this.playerId), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nickNameLen;
			Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Player> players = new List<Player>();

	public ushort Protocol { get { return (ushort)PacketID.S_GroupUpdate; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isDestroy = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.players.Clear();
		ushort playerLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < playerLen; i++)
		{
			Player player = new Player();
			player.Read(segment, ref count);
			players.Add(player);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_GroupUpdate), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isDestroy), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.players.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Player player in this.players)
			player.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestUserInformation : IPacket
{
	public string nickName;

	public ushort Protocol { get { return (ushort)PacketID.C_RequestUserInformation; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestUserInformation), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_UserInformation : IPacket
{
	public bool isSuccess;
	public int failReason;
	public bool isOnline;
	public class Player
	{
		public string nickName;
		public int level;
		public int totalGames;
		public int totalWins;
		public float WinPercentage;
		public float avgRank;
		public float avgKill;
		public int tearRank;
		public int tearRankScore;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
			count += nickNameLen;
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.totalGames = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.totalWins = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.WinPercentage = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.avgRank = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.avgKill = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.tearRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.tearRankScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nickNameLen;
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.totalGames), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.totalWins), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.WinPercentage), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.avgRank), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.avgKill), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.tearRank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.tearRankScore), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Player> players = new List<Player>();

	public ushort Protocol { get { return (ushort)PacketID.S_UserInformation; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isOnline = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.players.Clear();
		ushort playerLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < playerLen; i++)
		{
			Player player = new Player();
			player.Read(segment, ref count);
			players.Add(player);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_UserInformation), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isOnline), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes((ushort)this.players.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Player player in this.players)
			player.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_RankScoreUpdate : IPacket
{
	public int change;
	public int beforeRank;
	public int beforeRankScore;
	public int afterRank;
	public int afterRankScore;

	public ushort Protocol { get { return (ushort)PacketID.S_RankScoreUpdate; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.change = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.beforeRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.beforeRankScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.afterRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.afterRankScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_RankScoreUpdate), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.change), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.beforeRank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.beforeRankScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.afterRank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.afterRankScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestRecentGames : IPacket
{
	public string nickName;

	public ushort Protocol { get { return (ushort)PacketID.C_RequestRecentGames; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestRecentGames), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_RecentGames : IPacket
{
	public class Games
	{
		public int mode;
		public int rank;
		public int kills;
		public int deaths;
		public int startedAtMs;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.mode = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.kills = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.deaths = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.startedAtMs = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.mode), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.kills), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.deaths), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.startedAtMs), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			return success;
		}	
	}
	public List<Games> gamess = new List<Games>();

	public ushort Protocol { get { return (ushort)PacketID.S_RecentGames; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.gamess.Clear();
		ushort gamesLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < gamesLen; i++)
		{
			Games games = new Games();
			games.Read(segment, ref count);
			gamess.Add(games);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_RecentGames), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)this.gamess.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Games games in this.gamess)
			games.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestLeaderboard : IPacket
{
	public int offset;
	public int limit;

	public ushort Protocol { get { return (ushort)PacketID.C_RequestLeaderboard; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.offset = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.limit = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestLeaderboard), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.offset), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.limit), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_Leaderboard : IPacket
{
	public int lastUpdatedSec;
	public class Rows
	{
		public string nickName;
		public int tier;
		public int score;
		public int globalRank;
		public int totalGames;
		public float winRate;
		public float avgRank;
		public float avgKill;
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
			count += nickNameLen;
			this.tier = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.score = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.globalRank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.totalGames = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.winRate = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.avgRank = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
			this.avgKill = BitConverter.ToSingle(segment.Array, segment.Offset + count);
			count += sizeof(float);
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
			Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			count += nickNameLen;
			Array.Copy(BitConverter.GetBytes(this.tier), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.score), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.globalRank), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.totalGames), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.winRate), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.avgRank), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			Array.Copy(BitConverter.GetBytes(this.avgKill), 0, segment.Array, segment.Offset + count, sizeof(float));
			count += sizeof(float);
			return success;
		}	
	}
	public List<Rows> rowss = new List<Rows>();

	public ushort Protocol { get { return (ushort)PacketID.S_Leaderboard; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.lastUpdatedSec = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rowss.Clear();
		ushort rowsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < rowsLen; i++)
		{
			Rows rows = new Rows();
			rows.Read(segment, ref count);
			rowss.Add(rows);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_Leaderboard), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.lastUpdatedSec), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes((ushort)this.rowss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Rows rows in this.rowss)
			rows.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_PlayerUpdate : IPacket
{
	public string nickName;
	public int level;
	public int exp;
	public int maxExp;
	public int rank;
	public int rankScore;

	public ushort Protocol { get { return (ushort)PacketID.S_PlayerUpdate; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		ushort nickNameLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		this.nickName = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, nickNameLen);
		count += nickNameLen;
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.exp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.maxExp = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rank = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rankScore = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_PlayerUpdate), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		ushort nickNameLen = (ushort)Encoding.Unicode.GetBytes(this.nickName, 0, this.nickName.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		Array.Copy(BitConverter.GetBytes(nickNameLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		count += nickNameLen;
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.exp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.maxExp), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rank), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rankScore), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_LevelUp : IPacket
{
	public int fromLevel;
	public int toLevel;
	public int rewardGold;
	public int rewardStar;

	public ushort Protocol { get { return (ushort)PacketID.S_LevelUp; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.fromLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.toLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rewardGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.rewardStar = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_LevelUp), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.fromLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.toLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rewardGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.rewardStar), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestAddStar : IPacket
{
	public int packIndex;

	public ushort Protocol { get { return (ushort)PacketID.C_RequestAddStar; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.packIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestAddStar), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.packIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BuyStarsResult : IPacket
{
	public bool isSuccess;
	public int packIndex;
	public int orderId;
	public long transId;

	public ushort Protocol { get { return (ushort)PacketID.S_BuyStarsResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.packIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.orderId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.transId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BuyStarsResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.packIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.orderId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.transId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ConfirmAddStar : IPacket
{
	public int packIndex;
	public int orderId;
	public long transId;

	public ushort Protocol { get { return (ushort)PacketID.C_ConfirmAddStar; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.packIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.orderId = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.transId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ConfirmAddStar), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.packIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.orderId), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.transId), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ConfirmAddStarResult : IPacket
{
	public bool isSuccess;
	public int packIndex;
	public int addedStars;
	public int newStarBalance;

	public ushort Protocol { get { return (ushort)PacketID.S_ConfirmAddStarResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.packIndex = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.addedStars = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.newStarBalance = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ConfirmAddStarResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.packIndex), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.addedStars), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.newStarBalance), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_RequestBattlePass : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_RequestBattlePass; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_RequestBattlePass), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BattlePassInfo : IPacket
{
	public int version;
	public int bpLevel;
	public bool hasPremium;
	public long freeClaimBits;
	public long premiumClaimBits;
	public class Levels
	{
		public int level;
		public int freeGold;
		public int freeStar;
		public class FreeItemSkus
		{
			public string sku;
		
			public void Read(ArraySegment<byte> segment, ref ushort count)
			{
				ushort skuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
				count += sizeof(ushort);
				this.sku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, skuLen);
				count += skuLen;
			}
		
			public bool Write(ArraySegment<byte> segment, ref ushort count)
			{
				bool success = true;
				ushort skuLen = (ushort)Encoding.Unicode.GetBytes(this.sku, 0, this.sku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
				Array.Copy(BitConverter.GetBytes(skuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
				count += sizeof(ushort);
				count += skuLen;
				return success;
			}	
		}
		public List<FreeItemSkus> freeItemSkuss = new List<FreeItemSkus>();
		public int premiumGold;
		public int premiumStar;
		public class PremiumItemSkus
		{
			public string sku;
		
			public void Read(ArraySegment<byte> segment, ref ushort count)
			{
				ushort skuLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
				count += sizeof(ushort);
				this.sku = Encoding.Unicode.GetString(segment.Array, segment.Offset + count, skuLen);
				count += skuLen;
			}
		
			public bool Write(ArraySegment<byte> segment, ref ushort count)
			{
				bool success = true;
				ushort skuLen = (ushort)Encoding.Unicode.GetBytes(this.sku, 0, this.sku.Length, segment.Array, segment.Offset + count + sizeof(ushort));
				Array.Copy(BitConverter.GetBytes(skuLen), 0, segment.Array, segment.Offset + count, sizeof(ushort));
				count += sizeof(ushort);
				count += skuLen;
				return success;
			}	
		}
		public List<PremiumItemSkus> premiumItemSkuss = new List<PremiumItemSkus>();
	
		public void Read(ArraySegment<byte> segment, ref ushort count)
		{
			this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.freeGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.freeStar = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.freeItemSkuss.Clear();
			ushort freeItemSkusLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			for (int i = 0; i < freeItemSkusLen; i++)
			{
				FreeItemSkus freeItemSkus = new FreeItemSkus();
				freeItemSkus.Read(segment, ref count);
				freeItemSkuss.Add(freeItemSkus);
			}
			this.premiumGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.premiumStar = BitConverter.ToInt32(segment.Array, segment.Offset + count);
			count += sizeof(int);
			this.premiumItemSkuss.Clear();
			ushort premiumItemSkusLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
			count += sizeof(ushort);
			for (int i = 0; i < premiumItemSkusLen; i++)
			{
				PremiumItemSkus premiumItemSkus = new PremiumItemSkus();
				premiumItemSkus.Read(segment, ref count);
				premiumItemSkuss.Add(premiumItemSkus);
			}
		}
	
		public bool Write(ArraySegment<byte> segment, ref ushort count)
		{
			bool success = true;
			Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.freeGold), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.freeStar), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes((ushort)this.freeItemSkuss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			foreach (FreeItemSkus freeItemSkus in this.freeItemSkuss)
				freeItemSkus.Write(segment, ref count);
			Array.Copy(BitConverter.GetBytes(this.premiumGold), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes(this.premiumStar), 0, segment.Array, segment.Offset + count, sizeof(int));
			count += sizeof(int);
			Array.Copy(BitConverter.GetBytes((ushort)this.premiumItemSkuss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
			count += sizeof(ushort);
			foreach (PremiumItemSkus premiumItemSkus in this.premiumItemSkuss)
				premiumItemSkus.Write(segment, ref count);
			return success;
		}	
	}
	public List<Levels> levelss = new List<Levels>();

	public ushort Protocol { get { return (ushort)PacketID.S_BattlePassInfo; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.version = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.bpLevel = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.hasPremium = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.freeClaimBits = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.premiumClaimBits = BitConverter.ToInt64(segment.Array, segment.Offset + count);
		count += sizeof(long);
		this.levelss.Clear();
		ushort levelsLen = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
		count += sizeof(ushort);
		for (int i = 0; i < levelsLen; i++)
		{
			Levels levels = new Levels();
			levels.Read(segment, ref count);
			levelss.Add(levels);
		}
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BattlePassInfo), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.version), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.bpLevel), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.hasPremium), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.freeClaimBits), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes(this.premiumClaimBits), 0, segment.Array, segment.Offset + count, sizeof(long));
		count += sizeof(long);
		Array.Copy(BitConverter.GetBytes((ushort)this.levelss.Count), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		foreach (Levels levels in this.levelss)
			levels.Write(segment, ref count);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimBattlePassReward : IPacket
{
	public int level;
	public bool isPremium;

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimBattlePassReward; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isPremium = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimBattlePassReward), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isPremium), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimBattlePassRewardResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int level;
	public bool isPremium;

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimBattlePassRewardResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.level = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.isPremium = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimBattlePassRewardResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.level), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.isPremium), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_ClaimBattlePassAll : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_ClaimBattlePassAll; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_ClaimBattlePassAll), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_ClaimBattlePassAllResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int claimedCount;
	public int gainedGold;
	public int gainedStar;

	public ushort Protocol { get { return (ushort)PacketID.S_ClaimBattlePassAllResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.claimedCount = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedGold = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.gainedStar = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_ClaimBattlePassAllResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.claimedCount), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedGold), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.gainedStar), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class C_BuyBattlePass : IPacket
{
	

	public ushort Protocol { get { return (ushort)PacketID.C_BuyBattlePass; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.C_BuyBattlePass), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

public class S_BuyBattlePassResult : IPacket
{
	public bool isSuccess;
	public int failReason;
	public int version;

	public ushort Protocol { get { return (ushort)PacketID.S_BuyBattlePassResult; } }

	public void Read(ArraySegment<byte> segment)
	{
		ushort count = 0;
		count += sizeof(ushort);
		count += sizeof(ushort);
		this.isSuccess = BitConverter.ToBoolean(segment.Array, segment.Offset + count);
		count += sizeof(bool);
		this.failReason = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
		this.version = BitConverter.ToInt32(segment.Array, segment.Offset + count);
		count += sizeof(int);
	}

	public ArraySegment<byte> Write()
	{
		ArraySegment<byte> segment = SendBufferHelper.Open(65535);
		ushort count = 0;

		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes((ushort)PacketID.S_BuyBattlePassResult), 0, segment.Array, segment.Offset + count, sizeof(ushort));
		count += sizeof(ushort);
		Array.Copy(BitConverter.GetBytes(this.isSuccess), 0, segment.Array, segment.Offset + count, sizeof(bool));
		count += sizeof(bool);
		Array.Copy(BitConverter.GetBytes(this.failReason), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);
		Array.Copy(BitConverter.GetBytes(this.version), 0, segment.Array, segment.Offset + count, sizeof(int));
		count += sizeof(int);

		Array.Copy(BitConverter.GetBytes(count), 0, segment.Array, segment.Offset, sizeof(ushort));

		return SendBufferHelper.Close(count);
	}
}

