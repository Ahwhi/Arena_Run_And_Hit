using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager {
    // --- Singleton ---
    public static PlayerManager Instance { get; } = new PlayerManager();

    // --- Runtime State ---
    private MyPlayer _myPlayer;
    public static int MyPlayerId => Instance._myPlayer ? Instance._myPlayer.PlayerId : -1;
    public static MyPlayer MyPlayer => Instance._myPlayer;
    private readonly Dictionary<int, Player> _others = new();      // 사람(나 제외)
    private readonly Dictionary<int, BotAgent> _bots = new();      // 봇

    // === Public Queries ===
    // 기존 호환: "플레이어" 조회 (사람만 필요했던 곳들). 
    // 필요하면 봇까지 포함하려면 FindAllPlayer 사용.
    public static IReadOnlyDictionary<int, Player> FindPlayer => Instance._others;

    // 사람(타인) + 봇 + 나까지 포함한 풀 맵
    public static IReadOnlyDictionary<int, Player> FindAllPlayer {
        get {
            var map = new Dictionary<int, Player>(Instance._others);
            foreach (var kv in Instance._bots) map[kv.Key] = kv.Value;
            if (Instance._myPlayer != null) map[Instance._myPlayer.PlayerId] = Instance._myPlayer;
            return map;
        }
    }

    public static IReadOnlyDictionary<int, Player> FindOthersAndBots {
        get {
            // _others(사람-나 제외) + _bots(봇) = '나'만 뺀 맵
            var map = new Dictionary<int, Player>(Instance._others);
            foreach (var kv in Instance._bots)
                map[kv.Key] = kv.Value;
            return map;
        }
    }

    // 서버가 내려준 플레이어 리스트 (내 캐릭 포함)
    public void Add(S_PlayerList packet) {
        //Object prefab = Resources.Load("Player");

        foreach (var p in packet.players) {
            //GameObject go = Object.Instantiate(prefab) as GameObject;
            string prefabPath = ResolveCharacterPrefabPath(p.characterSku);
            Object prefab = Resources.Load(prefabPath);
            if (!prefab) prefab = Resources.Load("Player"); // 폴백

            GameObject go = Object.Instantiate(prefab) as GameObject;

            // 2) 공통 초기화
            Vector3 spawn = new Vector3(p.posX, p.posY, p.posZ);

            // 3) trail 색 적용
            ApplyTrailCosmetic(go, p.trailSku);

            if (p.isSelf) {
                // 내 캐릭터
                var me = go.AddComponent<MyPlayer>();
                me.PlayerId = p.playerId;
                me.NickName = p.nickName;
                me.transform.position = new Vector3(p.posX, p.posY, p.posZ);
                me.ApplyColorIndex(p.colorIndex);

                _myPlayer = me;
            } else {
                // 타인
                var pl = go.AddComponent<Player>();
                pl.PlayerId = p.playerId;
                pl.NickName = p.nickName;
                pl.transform.position = new Vector3(p.posX, p.posY, p.posZ);
                pl.ApplyColorIndex(p.colorIndex);

                _others[p.playerId] = pl;
            }
        }
    }

    // 서버가 브로드캐스트한 퇴장(사람만)
    public void LeaveGame(S_BroadcastLeaveGame packet) {
        if (_myPlayer != null && _myPlayer.PlayerId == packet.playerId) {
            GameObject.Destroy(_myPlayer.gameObject);
            _myPlayer = null;
            return;
        }

        if (_others.TryGetValue(packet.playerId, out var p)) {
            GameObject.Destroy(p.gameObject);
            _others.Remove(packet.playerId);
        }
    }

    // === Snapshots ===
    // 내 캐릭터의 서버 스냅샷은 MyPlayer 가 리컨실 처리
    public void OnSnapshotFromServer(S_Snapshot pkt) {
        _myPlayer?.OnSnapshotFromServer(pkt);
    }

    // 월드 스냅샷: 타인 + 봇 위치/회전 보간 적용
    public void OnWorldSnapshot(S_WorldSnapshot pkt) {
        foreach (var e in pkt.entitiess) {
            // 내 캐릭은 MyPlayer 리컨 경로에서 처리하므로 스킵
            if (_myPlayer != null && e.playerId == _myPlayer.PlayerId)
                continue;

            // 1) 타인(사람)
            if (_others.TryGetValue(e.playerId, out var player)) {
                player.SetSnapshotTarget(new Vector3(e.posX, e.posY, e.posZ), e.yaw);
                continue;
            }

            // 2) 봇
            if (BotRuntime.Instance != null && BotRuntime.Instance.botIds.Contains(e.playerId)) {
                if (!BotRuntime.Instance.initialPos.ContainsKey(e.playerId)) {
                    BotRuntime.Instance.initialPos[e.playerId] = new Vector3(e.posX, e.posY, e.posZ);
                    BotRuntime.Instance.initialYaw[e.playerId] = e.yaw;
                }
            }

            if (_bots.TryGetValue(e.playerId, out var bot)) {
                bot.SetWorldPose(new Vector3(e.posX, e.posY, e.posZ), e.yaw);
                continue;
            }
        }
    }

    // === Chat ===
    public void Chat(S_BroadcastChat packet) {
        Object prefab = Resources.Load("ChatPrefab");
        Transform parent = GameObject.Find("ChatContent").transform;
        GameObject go = Object.Instantiate(prefab, parent) as GameObject;
        var txt = go.GetComponent<TextMeshProUGUI>();

        txt.text = $"{packet.nickName}: {packet.message}";
        if (_myPlayer != null && packet.nickName == _myPlayer.NickName)
            txt.color = Color.green;

        if (!GameManager.isChated)
            GameManager.isChated = true;
    }

    public void LobbyChat(S_BroadcastLobbyChat packet) {
        Object prefab = Resources.Load("ChatPrefab");
        Transform parent = GameObject.Find("ChatContent").transform;
        GameObject go = Object.Instantiate(prefab, parent) as GameObject;

        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = $"{packet.nickName}: {packet.message}";

        if (packet.type == 1) {
            txt.color = Color.cyan;
            LobbyManager.TagChatGO(go, ChatType.Group);
        } else {
            LobbyManager.TagChatGO(go, ChatType.Normal);
        }
            //if (packet.nickName == NetworkManager.Instance.nickName)
            //    txt.color = Color.green;

            // 버튼 구성(텍스트를 타겟 Graphic으로)
            var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        txt.raycastTarget = true;
        btn.targetGraphic = txt;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => {
            var chatRT = go.GetComponent<RectTransform>();
            LobbyManager.ShowChatPopupNearby(chatRT, packet.nickName); // <-- 닉네임 전달
        });

        if (!LobbyManager.isChated)
            LobbyManager.isChated = true;
    }

    // === Bot Integration (BotCoordinator에서 호출) ===
    // 봇 프리팹 생성은 BotCoordinator에서 하고, 여기엔 '등록'만 맡김
    public static void RegisterBot(int botId, BotAgent agent) {
        var inst = Instance;

        // 중복 등록 방지
        if (inst._bots.TryGetValue(botId, out var exists) && exists != null) {
            if (agent && exists != agent) {
                // 오래된 객체 정리
                GameObject.Destroy(exists.gameObject);
                inst._bots[botId] = agent;
            }
            return;
        }

        inst._bots[botId] = agent;
        agent.PlayerId = botId;              // 일관성 유지(봇도 playerId로 취급)
        agent.NickName = $"BOT_{botId}";
    }

    public static bool TryGet(int id, out Player p) {
        // 통합 조회(사람/봇/나)
        var all = FindAllPlayer;
        return all.TryGetValue(id, out p);
    }

    // (선택) 씬 전환 초기화가 필요하면 호출
    public void ResetAll() {
        var inst = Instance;

        if (inst._myPlayer) {
            GameObject.Destroy(inst._myPlayer.gameObject);
            inst._myPlayer = null;
        }
        foreach (var kv in inst._others) if (kv.Value) GameObject.Destroy(kv.Value.gameObject);
        inst._others.Clear();

        foreach (var kv in inst._bots) if (kv.Value) GameObject.Destroy(kv.Value.gameObject);
        inst._bots.Clear();
    }


    BotAgent SpawnRemoteBot(int botId) {
        // BotCoordinator에 프리팹 레퍼런스가 있다면 그걸 쓰고, 없으면 Resources 경로 사용
        GameObject prefab = BotCoordinator.Instance && BotCoordinator.Instance.botPrefab
            ? BotCoordinator.Instance.botPrefab
            : Resources.Load<GameObject>("Bot");              // ← 프로젝트에 맞게 이름 확인

        if (!prefab) { Debug.LogError("[SpawnRemoteBot] bot prefab missing"); return null; }

        var go = Object.Instantiate(prefab);
        var agent = go.GetComponent<BotAgent>();
        if (!agent) { Debug.LogError("botPrefab에 BotAgent 필요"); Object.Destroy(go); return null; }

        agent.BotId = botId;
        agent.NickName = $"BOT_{botId}";
        agent.SetOwned(false);                                // 원격(비-오너) 기본
        RegisterBot(botId, agent);
        return agent;
    }

    static string ResolveCharacterPrefabPath(string sku) {
        // 폴더 구조 예시: Resources/Characters/CHAR_PENGUIN_01.prefab
        // 프로젝트에 맞게 바꾸면 됨
        if (string.IsNullOrEmpty(sku)) sku = "CHAR_BASIC_01";
        return $"Characters/{sku}";
    }

    static Color ResolveTrailColorBySku(string sku) {
        // 최소 버전: sku 접두어로 색 지정
        // 필요하면 카탈로그에 colorHex를 넣고 거기서 읽게 변경 가능
        if (string.IsNullOrEmpty(sku) || sku == "TRAIL_BASIC_01") return Color.white;
        switch (sku) {
            case "TRAIL_RED_01":
                return Color.red;
            case "TRAIL_YELLOW_01":
                return Color.yellow;
            case "TRAIL_GREEN_01":
                return Color.green;
            case "TRAIL_BLUE_01":
                return Color.blue;
            case "TRAIL_CYAN_01":
                return Color.cyan;
            case "TRAIL_PURPLE_01":
                return Color.magenta;
            case "TRAIL_GRAY_01":
                return Color.gray;
            case "TRAIL_BLACK_01":
                return Color.black;
        }
        //if (sku.StartsWith("TRAIL_RED")) return Color.red;
        // 확장 예: BLUE/GREEN 등
        return Color.white;
    }

    static Material _trailAlphaMat;   // 알파블렌딩용(검정 표시용)
    static Material GetAlphaTrailMat() {
        if (_trailAlphaMat) return _trailAlphaMat;

        // Built-in 기준: Sprites/Default 가 알파 블렌딩이라 가장 간단
        var sh = Shader.Find("Sprites/Default");
        if (!sh) sh = Shader.Find("Particles/Standard Unlit"); // 폴백
        _trailAlphaMat = new Material(sh);
        // 필요 시 큐 조정(투명)
        _trailAlphaMat.renderQueue = 3000;
        return _trailAlphaMat;
    }

    static bool IsNearBlack(Color c) {
        // 아주 어두운 색이면 true
        return Mathf.Max(c.r, c.g, c.b) <= 0.05f;
    }

    public static void ApplyTrailCosmetic(GameObject root, string trailSku) {
        // 1) 트레일 오브젝트 찾기(비활성 포함)
        var tf = root
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => string.Equals(t.name, "AttackTrail", System.StringComparison.OrdinalIgnoreCase));
        if (!tf) return;

        var tr = tf.GetComponent<TrailRenderer>();
        if (!tr) tr = tf.gameObject.AddComponent<TrailRenderer>();

        // 2) 색 결정
        Color c = ResolveTrailColorBySku(trailSku);

        // 3) 검정/암색이면 알파블렌딩 머터리얼 적용 (가산합성 대신)
        if (IsNearBlack(c)) {
            tr.material = GetAlphaTrailMat();          // ← 핵심
                                                       // Sprites/Default는 텍스쳐 곱 색상에 따라 나옴. 텍스쳐 없으면 흰색으로 취급.
                                                       // 필요시: tr.material.color = new Color(0,0,0,1f); // 머터리얼 틴트도 검정으로
        } else {
            // 비검정은 기존 머터리얼 그대로(가산합성도 OK)
            // tr.material = ... // 프로젝트에서 쓰던 걸 유지
        }

        // 4) 그라데이션/알파 페이드(앞=불투명, 뒤=서서히 0)
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        tr.colorGradient = grad;

        // 5) 표시 품질/안정성 옵션(원하면 조정)
        tr.textureMode = LineTextureMode.Stretch;
        tr.alignment = LineAlignment.View;
        tr.numCornerVertices = 2;
        tr.numCapVertices = 2;

        // 6) 기본 상태
        tr.enabled = true;
        tr.emitting = false;
        tf.gameObject.SetActive(false); // 애니에서 켜는 흐름 유지
    }



}
