using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SfxId {
    None = 0,
    CountDown,
    Start,
    Hit,
    FinalHit,
    Kill,
    Pickup,
    MouseClick,
    MatchFound,
    MatchFindStart,
    GameOver,
    Score,
    Chat,
    Invite,
    Adjust,
    Dance_Basic,
    Dance_Hiphop,
    LevelUp,
    Whoosh
}

[CreateAssetMenu(fileName = "SfxLibrary", menuName = "Audio/Sfx Library")]
public class SfxLibrary : ScriptableObject {
    [Serializable]
    public class Entry {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 50.0f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitchMin = 0.98f;
        [Range(0.1f, 3f)] public float pitchMax = 1.02f;
        [Header("3D")]
        public float minDistance = 1.5f;
        public float maxDistance = 20f;
        public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
        [Header("Routing (optional)")]
        public AudioMixerGroup mixerGroup; // SFX 그룹 있으면 지정
    }
    public List<Entry> entries = new();
    Dictionary<SfxId, Entry> _map;

    public Entry Get(SfxId id) {
        if (_map == null) { _map = new(); foreach (var e in entries) _map[e.id] = e; }
        _map.TryGetValue(id, out var x);
        return x;
    }
}
