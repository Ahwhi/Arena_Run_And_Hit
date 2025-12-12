using UnityEngine;
using UnityEngine.Audio;

public class BgmPlayer : MonoBehaviour {
    public static BgmPlayer I { get; private set; }
    public AudioMixerGroup bgmGroup;   // ← BGM 그룹 드래그
    AudioSource _src;

    void Awake() {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = true;
        _src.spatialBlend = 0f; // 2D
        _src.outputAudioMixerGroup = bgmGroup;
    }

    public void Play(AudioClip clip) { _src.clip = clip; _src.Play(); }
    public void Stop() { _src.Stop(); }
}
