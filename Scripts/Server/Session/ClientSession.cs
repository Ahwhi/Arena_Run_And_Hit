using ServerCore;
using System;
using System.Net;
using Server.Infra;
using System.Text.RegularExpressions;

namespace Server {
    public class LevelUpState {
        public int FromLevel;
        public int ToLevel;
        public int RewardGold;
        public int RewardStar;
    }

    public class SessionAccount {
        public string accountId = "Unknown";
        public string nickName = "Unknown";
        public string IPAddress = "Unknown";
    }

    public class SessionGame {
        public bool isAlive = true;
        public float PosX, PosY, PosZ;
        public float VelX, VelY, VelZ;
        public float Yaw;
        public int LastProcessedSeq;
        public int LastAttackSeq;
        public int NextAttackTick;
        public int MaxHP = 100;
        public int HP = 100;
        public int Kill = 0;
        public int Death = 0;
        public int Rank = 0;
        public byte ColorIndex = 255;

        public readonly Queue<C_Input> InputInbox = new(64);
        public Dictionary<string, string> EquippedBySlot { get; } = new(); // 댄스 캐싱
    }

    class ClientSession : PacketSession {
        public int SessionID { get; set; }
        public Room Room { get; set; }
        public Room GroupRoom { get; set; }
        public SessionAccount Account { get; set; }
        public SessionGame Game { get; set; }
        public bool isGaming { get; set; }
        public LevelUpState PendingLevelUp;

        public long ConnectedMs { get; private set; }
        public long LastRecvMs { get; private set; }
        public bool HandshakeDone { get; set; }   // 최소 1개 정상 패킷을 받았는지

        public override void OnConnected(EndPoint endPoint) {
            if (Account == null || Game == null) {
                Log.Error($"[OnConnected Fail] SID={SessionID}, ep={endPoint}");
                return;
            }

            Account.IPAddress = ((IPEndPoint)endPoint).Address.ToString();
            ConnectedMs = TimeUtil.NowMs();
            LastRecvMs = ConnectedMs;
            HandshakeDone = false;

            // IP 필터 (밑에서 설명하는 블랙리스트)
            if (IpFilter.ShouldReject(Account.IPAddress)) {
                Log.Sys($"[IpFilter] Reject immediately: {Account.IPAddress}");
                Disconnect();    // Socket 끊기
                return;
            }

            Log.CNT($"[OnConnected] {SessionID} : {endPoint}");
            Program.LoginRoom.Push(() => Program.LoginRoom.Access(this));

            // 5초 핸드셰이크 타임아웃
            var self = this;
            JobTimer.Instance.Push(() => {
                long now = TimeUtil.NowMs();
                if (!self.HandshakeDone && (now - self.ConnectedMs) >= 4000) {
                    Log.Sys($"[Timeout] {self.SessionID} : {self.Account.IPAddress}");
                    IpFilter.ReportHandshakeTimeout(self.Account.IPAddress);
                    self.Disconnect();
                }
            }, 4000);
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer) {
            LastRecvMs = TimeUtil.NowMs();

            // 첫 정상 패킷 받았다 = 핸드셰이크 완료로 인정
            if (!HandshakeDone) {
                HandshakeDone = true;
            }

            PacketManager.Instance.OnRecvPacket(this, buffer);
        }

        public override void OnDisconnected(EndPoint endPoint) {
            SessionManager.Instance.Remove(this);

            if (Room != null) {
                var room = Room;
                if (GroupRoom != null) {
                    var Groom = GroupRoom;
                    room.Push(() => room.ExitGroup(this));
                }
                room.Push(() => room.Leave(this));
                Room = null;
            }
            
            Log.CNT($"[OnDisconnected] {endPoint}");
        }

        public override void OnSend(int numOfBytes) { }
    }
}
