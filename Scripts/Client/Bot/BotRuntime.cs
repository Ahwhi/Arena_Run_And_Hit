using System.Collections.Generic;
using UnityEngine;

public class BotRuntime : MonoBehaviour {
    public static BotRuntime Instance { get; private set; }

    void Awake() { 
        if (Instance != null) { 
            Destroy(gameObject); 
            return; } 
        Instance = this; 
        DontDestroyOnLoad(gameObject); 
    }

    public int gameMode = -1;
    public int botOwnerId = -1;
    public readonly List<int> botIds = new();
    public readonly Dictionary<int, BotAgent> agents = new();

    public readonly Dictionary<int, Vector3> initialPos = new();
    public readonly Dictionary<int, float> initialYaw = new();
    public readonly Dictionary<int, int> colorById = new();

    public bool AmOwner(int myId) => myId == botOwnerId;

    public void ResetState() {
        foreach (var kv in agents) {
            var a = kv.Value;
            if (a) Destroy(a.gameObject);
        }

        agents.Clear();
        botIds.Clear();
        initialPos.Clear();
        initialYaw.Clear();
        botOwnerId = -1;
        gameMode = -1;
    }
}
