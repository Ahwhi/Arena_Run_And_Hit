using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour {
    public static SoundManager I { get; private set; }

    [Header("Database")]
    public SfxLibrary sfx;               // ↑ 생성한 SfxLibrary.asset 할당

    [Header("Pool")]
    public int initialPool = 16;
    readonly Queue<AudioSource> _pool = new();

    [Header("Routing Defaults")]
    public AudioMixerGroup defaultSfxGroup;

    void Awake() {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        // 풀 미리 생성
        for (int i = 0; i < initialPool; i++) _pool.Enqueue(CreateSource());
    }

    AudioSource CreateSource() {
        var go = new GameObject("SFX");
        go.transform.parent = transform;
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 1f;                  // 3D
        a.dopplerLevel = 0.2f;
        a.spread = 0f;
        a.loop = false;
        a.enabled = true;
        return a;
    }

    AudioSource Rent() => _pool.Count > 0 ? _pool.Dequeue() : CreateSource();
    void Return(AudioSource a) {
        if (a == null) return;

        a.Stop();
        a.transform.parent = transform;
        a.transform.localPosition = Vector3.zero;
        a.clip = null;
        _pool.Enqueue(a);
    }

    public void Play2D(SfxId id, float volMul = 1f) {
        var ent = sfx?.Get(id); if (ent == null || !ent.clip) return;
        var src = Rent();
        ApplyEntry(src, ent, volMul, is2D: true);
        src.Play();
        StartCoroutine(ReturnWhenDone(src));
    }

    // 3D 원샷 (고정 위치)
    public void Play3D(SfxId id, Vector3 position, float volMul = 1f) {
        var ent = sfx?.Get(id); if (ent == null || !ent.clip) return;
        var src = Rent();
        src.transform.position = position;
        ApplyEntry(src, ent, volMul, is2D: false);
        src.Play();
        StartCoroutine(ReturnWhenDone(src));
    }

    // 3D 원샷 (타겟을 따라다님: 피격/이동체)
    public void Play3DFollow(SfxId id, Transform follow, float volMul = 1f) {
        var ent = sfx?.Get(id); if (ent == null || !ent.clip) return;
        var src = Rent();
        src.transform.parent = follow;     // 따라붙기
        src.transform.localPosition = Vector3.zero;
        ApplyEntry(src, ent, volMul, is2D: false);
        src.Play();
        StartCoroutine(ReturnWhenDone(src));
    }

    System.Collections.IEnumerator ReturnWhenDone(AudioSource a) {
        // 클립 길이 기반 간단 반환
        float t = (a.clip ? a.clip.length / Mathf.Max(0.01f, a.pitch) : 0.5f) + 0.02f;
        yield return new WaitForSeconds(t);
        Return(a);
    }

    void ApplyEntry(AudioSource src, SfxLibrary.Entry e, float volMul, bool is2D) {
        src.clip = e.clip;
        src.volume = Mathf.Clamp01(e.volume * volMul);
        src.pitch = Random.Range(e.pitchMin, e.pitchMax);
        // Mixer 라우팅
        src.outputAudioMixerGroup = e.mixerGroup ? e.mixerGroup : defaultSfxGroup;

        if (is2D) {
            // 2D: 거리/롤오프/도플러 무시
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
        } else {
            // 3D: 엔트리 값 반영
            src.spatialBlend = 1f;
            src.dopplerLevel = 0.2f;
            src.minDistance = e.minDistance;
            src.maxDistance = e.maxDistance;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        src.loop = false;
    }

    public void StopAllSfx() {
        // 코루틴으로 자동 반환 기다리는 거 전부 끊기
        StopAllCoroutines();

        // SoundManager 밑에 붙어있는 풀/재생중 AudioSource들 정리
        foreach (Transform child in transform) {
            var a = child.GetComponent<AudioSource>();
            if (!a) continue;

            a.Stop();
            a.clip = null;

            // 풀에 없으면 넣어주기
            if (!_pool.Contains(a))
                _pool.Enqueue(a);
        }
    }

}
