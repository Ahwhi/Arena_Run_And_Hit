using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotCoordinator : MonoBehaviour {
    [Header("Prefab / Settings")]
    public GameObject botPrefab;
    public float sendHz = 25f;

    public static BotCoordinator Instance { get; private set; }

    float _sendInterval;
    Coroutine _sendLoop;

    int MyId => PlayerManager.MyPlayerId;

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _sendInterval = 1f / Mathf.Max(1f, sendHz);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
        if (_sendLoop != null) { StopCoroutine(_sendLoop); _sendLoop = null; }
    }

    void Start() {
        StartCoroutine(InitWhenReady());
    }

    IEnumerator InitWhenReady() {
        while (BotRuntime.Instance == null || PlayerManager.MyPlayer == null)
            yield return null;
        yield return null;

        if (BotRuntime.Instance.AmOwner(MyId)) {
            while (!HasInitialsForAllBots())
                yield return null;
        }
        
        SpawnBotsAtOrigin();
        ApplyOwnership();
        StartSendLoopIfOwner();

        if (BotRuntime.Instance.AmOwner(MyId))
            SendBotSnapshot();
    }

    bool HasInitialsForAllBots() {
        if (BotRuntime.Instance == null) return false;
        var ids = BotRuntime.Instance.botIds;
        var pos = BotRuntime.Instance.initialPos;
        for (int i = 0; i < ids.Count; i++)
            if (!pos.ContainsKey(ids[i])) return false;
        return true;
    }

    public void OnBotOwnerChanged(int newOwnerId, List<int> botIds) {
        if (BotRuntime.Instance == null) return;

        BotRuntime.Instance.botOwnerId = newOwnerId;
        BotRuntime.Instance.botIds.Clear();
        if (botIds != null && botIds.Count > 0)
            BotRuntime.Instance.botIds.AddRange(botIds);

        ApplyOwnership();
        StartSendLoopIfOwner();

        if (BotRuntime.Instance.AmOwner(MyId))
            SendBotSnapshot();
    }

    void SpawnBotsAtOrigin() {
        if (BotRuntime.Instance == null || botPrefab == null) return;

        foreach (var id in BotRuntime.Instance.botIds) {
            if (BotRuntime.Instance.agents.ContainsKey(id)) continue;

            var go = Instantiate(botPrefab);
            if (BotRuntime.Instance.initialPos.TryGetValue(id, out var pos)) {
                float yaw = 0f;
                BotRuntime.Instance.initialYaw.TryGetValue(id, out yaw);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            } else {
                Debug.LogError("봇 좌표 오류");
                //go.transform.position = Vector3.zero;
            }

            var agent = go.GetComponent<BotAgent>();
            if (!agent) { Debug.LogError("botPrefab에 BotAgent 필요"); Destroy(go); continue; }

            agent.BotId = id;
            agent.NickName = $"BOT_{id}";

            BotRuntime.Instance.agents[id] = agent;
            PlayerManager.RegisterBot(id, agent);

            if (BotRuntime.Instance.colorById.TryGetValue(id, out int col))
                agent.ApplyColorIndex(col);
            else
                agent.ApplyColorIndex(9); // 안전장치
        }
    }

    void ApplyOwnership() {
        if (BotRuntime.Instance == null) return;
        bool amOwner = BotRuntime.Instance.AmOwner(MyId);
        foreach (var kv in BotRuntime.Instance.agents)
            kv.Value.SetOwned(amOwner);
    }

    void StartSendLoopIfOwner() {
        if (BotRuntime.Instance == null) return;
        bool amOwner = BotRuntime.Instance.AmOwner(MyId);

        if (_sendLoop != null) { StopCoroutine(_sendLoop); _sendLoop = null; }
        if (amOwner) _sendLoop = StartCoroutine(SendSnapshotsLoop());
    }

    IEnumerator SendSnapshotsLoop() {
        yield return new WaitForSeconds(Random.Range(0f, _sendInterval));

        while (true) {
            SendBotSnapshot();
            float j = Random.Range(-_sendInterval * 0.25f, _sendInterval * 0.25f);
            yield return new WaitForSeconds(Mathf.Max(0.01f, _sendInterval + j));
        }
    }

    void SendBotSnapshot() {
        if (BotRuntime.Instance == null) return;
        if (!BotRuntime.Instance.AmOwner(MyId)) return;

        var pkt = new C_BotSnapshot();
        foreach (var agent in BotRuntime.Instance.agents.Values) {
            agent.Collect(out int id, out Vector3 pos, out float yaw);
            var item = new C_BotSnapshot.Bots {
                botId = id,
                posX = pos.x,
                posY = pos.y,
                posZ = pos.z,
                yaw = yaw
            };
            pkt.botss.Add(item);
        }

        NetworkManager.Send(pkt.Write());
    }
}
