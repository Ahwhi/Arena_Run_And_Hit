using Steamworks;
using UnityEngine;
using System.Collections.Generic;

public class SteamPayManager : MonoBehaviour {
    public static SteamPayManager Instance;

    Callback<MicroTxnAuthorizationResponse_t> _microTxnCallback;

    class PendingTxn {
        public int packIndex;
        public ulong transId;
    }

    Dictionary<ulong, PendingTxn> _pending = new(); // key = orderId

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _microTxnCallback = Callback<MicroTxnAuthorizationResponse_t>
            .Create(OnMicroTxnAuth);
    }

    void Update() {
        SteamAPI.RunCallbacks();
    }

    // 서버에서 S_BuyStarsResult 받았을 때 호출
    public void OnBuyStarsResult(S_BuyStarsResult pkt) {
        if (!pkt.isSuccess)
            return;

        var p = new PendingTxn {
            packIndex = pkt.packIndex,
            transId = (ulong)pkt.transId
        };
        _pending[(ulong)pkt.orderId] = p;
    }

    void OnMicroTxnAuth(MicroTxnAuthorizationResponse_t cb) {
        ulong orderId = cb.m_ulOrderID;
        bool authorized = cb.m_bAuthorized != 0;

        if (!authorized) {
            _pending.Remove(orderId); // 취소이므로 버리기
            Debug.Log("취소임");
            return;
        }

        if (!_pending.TryGetValue(orderId, out var pending)) {
            Debug.Log("내가연거래가아님");
            return; // 내가 연 거래가 아니면 무시
        }

        _pending.Remove(orderId);

        var req = new C_ConfirmAddStar {
            orderId = (int)orderId,
            transId = unchecked((long)pending.transId),
            packIndex = pending.packIndex
        };
        NetworkManager.Send(req.Write());
    }
}
