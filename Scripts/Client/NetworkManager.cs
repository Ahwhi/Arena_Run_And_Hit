using DummyClient;
using ServerCore;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class NetworkManager : MonoBehaviour {
    public static NetworkManager Instance { get; private set; }
    ServerSession _session = new ServerSession();
    public bool isConnected = false;
    public bool isInGroup = false;
    public bool isGroupLeader = false;
    public string nickName = "";

    readonly ConcurrentQueue<Action> _mainThread = new();
    const float PING_INTERVAL = 0.5f;
    int _pingSeq = 0;
    readonly Dictionary<int, int> _sentAt = new(); // seq -> Environment.TickCount
    float _ema = -1f;                        // RTT 지수평활 평균
    float _jitter = 0f;                      // RTT 변화량 EMA
    const float ALPHA = 0.1f;
    Coroutine _pingLoop;

    public static bool isTestMode = false;

    void Awake() {
        if (Instance && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() {

        //string ip = "127.0.0.1";

        string ip = "github업로드용이라 가림";  // 메인 서버


        IPAddress ipAddr = IPAddress.Parse(ip);
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
        Connector connector = new Connector();
        connector.Connect(endPoint, () => { return _session; }, 1);
        _pingLoop = StartCoroutine(PingLoop());
    }

    void Update() {
        while (_mainThread.TryDequeue(out var a)) {
            a?.Invoke();
        }
        List<IPacket> list = PacketQueue.Instance.PopAll();
        foreach (IPacket packet in list)
            PacketManager.Instance.HandlePacket(_session, packet);
    }

    public static void PostToMain(Action a) {
        Instance?._mainThread.Enqueue(a);
    }

    private void OnApplicationQuit() {
        if (_session != null && isConnected) {
            _session.Disconnect();
        }
        SteamAPI.Shutdown();
    }

    public static void Send(ArraySegment<byte> sendBuff) {
        Instance._session.Send(sendBuff);
    }

    IEnumerator PingLoop() {
        var wait = new WaitForSeconds(PING_INTERVAL);
        while (true) {
            if (isConnected && _session != null) {
                SendPing();
            }
            yield return wait;
        }
    }

    void SendPing() {
        var pkt = new C_Ping {
            seq = ++_pingSeq,
            clientSentTick = Environment.TickCount
        };
        _sentAt[pkt.seq] = pkt.clientSentTick;
        _session.Send(pkt.Write());
    }

    public static void OnPong(S_Pong pkt) {
        Instance?.HandlePong(pkt);
    }

    void HandlePong(S_Pong pong) {
        int now = Environment.TickCount;
        if (!_sentAt.TryGetValue(pong.seq, out int sentTick))
            return;
        _sentAt.Remove(pong.seq);

        int rttMs = unchecked(now - sentTick);
        if (rttMs < 0) rttMs = 0;

        if (_ema < 0f) _ema = rttMs;
        else _ema = Mathf.Lerp(_ema, rttMs, ALPHA);

        float diff = Mathf.Abs(rttMs - _ema);
        _jitter = Mathf.Lerp(_jitter, diff, ALPHA);

        //Debug.Log($"[PING] rtt={rttMs} ms, avg={_ema:0.0} ms, jitter={_jitter:0.0} ms, srvTick={pong.serverTick}");
    }

    public float AvgRttMs => _ema < 0 ? 0f : _ema;
    public float JitterMs => _jitter;
    
}
