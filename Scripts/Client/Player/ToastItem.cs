using System.Collections;
using UnityEngine;
using TMPro;

public class ToastItem : MonoBehaviour {
    PlayerUIManager _ui;
    Player _owner;
    string _msg;

    TextMeshProUGUI _tmp;
    CanvasGroup _cg;
    float _life;
    float _age;
    float _lineOffset;

    public RectTransform rect { get; private set; }

    // 같은 메시지 들어오면 합치기(“x2”, “x3” 꼬리표)
    int _stack = 1;

    public void Init(PlayerUIManager ui, Player owner, string msg, Color color, float life) {
        _ui = ui; _owner = owner; _msg = msg; _life = life;
        rect = GetComponent<RectTransform>();
        _tmp = GetComponentInChildren<TextMeshProUGUI>(true);
        _cg = GetComponent<CanvasGroup>(); if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();

        _tmp.text = msg;
        _tmp.color = color;
        _cg.alpha = 0f;

        StartCoroutine(CoLife());
    }

    public void SetLineOffset(float off) {
        _lineOffset = off;
    }

    public bool TryCoalesce(string msg) {
        if (msg != _msg) return false;
        _stack++;
        _age = 0f;                   // 다시 또렷해지게
        _cg.alpha = 1f;
        _tmp.text = $"{_msg}  x{_stack}";
        return true;
    }

    public void FastKill() {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    IEnumerator CoLife() {
        // 짧은 페이드인
        const float FADE_IN = 0.1f;
        const float FADE_OUT = 0.2f;

        while (_age < _life) {
            _age += Time.deltaTime;

            // 위치 계속 추적
            _ui.UpdateToastAnchor(_owner, this, _lineOffset);

            // 알파: 앞쪽은 빠르게 켜지고 뒤쪽은 종료 직전에 꺼짐
            float remain = Mathf.Clamp01((_life - _age) / FADE_OUT);
            float appear = Mathf.Clamp01(_age / FADE_IN);
            _cg.alpha = Mathf.Min(1f, appear) * Mathf.Clamp01(remain + (1f - FADE_OUT)); // 자연스러운 곡선용 대강 혼합

            yield return null;
        }

        // 빠른 페이드아웃(안전)
        float t = 0f;
        while (t < FADE_OUT) {
            t += Time.deltaTime;
            _ui.UpdateToastAnchor(_owner, this, _lineOffset);
            _cg.alpha = 1f - (t / FADE_OUT);
            yield return null;
        }

        _ui.OnToastFinished(_owner, this);
        Destroy(gameObject);
    }
}
