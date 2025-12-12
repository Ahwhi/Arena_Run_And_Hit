using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient {

    class ServerSession : PacketSession {
        public override void OnConnected(EndPoint endPoint) {
            //NetworkManager.Instance.isConnected = true;
            NetworkManager.PostToMain(() => {
                NetworkManager.Instance.isConnected = true;
            });
            //UnityEngine.Debug.Log("OnConnected : " + endPoint);

        }

        public override void OnDisconnected(EndPoint endPoint) {
            //NetworkManager.Instance.isConnected = false;
            NetworkManager.PostToMain(() => {
                NetworkManager.Instance.isConnected = false;
            });

            //UnityEngine.Debug.Log("OnDisconnected : " + endPoint);
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer) {
            PacketManager.Instance.OnRecvPacket(this, buffer, (s, p) => PacketQueue.Instance.Push(p));
        }

        public override void OnSend(int numOfBytes) {
            //Console.WriteLine($"Transferred Bytes: {numOfBytes}");
        }
    }

}
