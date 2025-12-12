using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MouseCursorController : MonoBehaviour {
    // ====== 자동 부트스트랩 (씬에 안 올려도 됨) ======
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap() {
        if (Instance) return;
        var go = new GameObject("[Global] MouseCursorController");
        go.hideFlags = HideFlags.DontSave;
        go.AddComponent<MouseCursorController>();
    }

    // ====== 싱글턴 & 퍼시스턴트 ======
    public static MouseCursorController Instance { get; private set; }

    [Header("Cursor Textures (Resources path)")]
    public string pointerTexPath = "Image/RawImage/Cursor/Pointer";   // Resources/Cursor/Pointer.png
    public string clickTexPath = "Image/RawImage/Cursor/Click";     // Resources/Cursor/Click.png

    [Header("Hotspot (pixels from top-left)")]
    public Vector2 hotspot = new Vector2(4, 4);

    [Header("Ripple Sprite (Resources path)")]
    public string rippleSpritePath = "Image/RawImage/Cursor/CursorRipple"; // Resources/Image/RawImage/CursorRipple.png

    [Header("Ripple Options")]
    public float rippleDuration = 0.35f;
    public float rippleStartSize = 24f;
    public float rippleEndSize = 120f;
    public float rippleStartAlpha = 0.35f;
    public float rippleEndAlpha = 0f;
    public AnimationCurve rippleEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Texture2D _texPointer, _texClick;
    Sprite _rippleSprite;
    Canvas _uiCanvas;
    RectTransform _canvasRT;

    void Awake() {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAssets();
        EnsureCanvas();
        ApplyCursor(_texPointer);

        // 씬 전환 시 캔버스/커서 재설정
        SceneManager.activeSceneChanged += (_, __) => {
            EnsureCanvas();
            ApplyCursor(_texPointer);
        };
    }

    void OnApplicationFocus(bool hasFocus) {
        if (hasFocus) ApplyCursor(_texPointer); // 포커스 복귀 시 재적용
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            if (_texClick) ApplyCursor(_texClick);
            SpawnRippleAtMouse();
        }
        if (Input.GetMouseButtonUp(0)) {
            if (_texPointer) ApplyCursor(_texPointer);
        }
    }

    void LoadAssets() {
        _texPointer = Resources.Load<Texture2D>(pointerTexPath);
        _texClick = Resources.Load<Texture2D>(clickTexPath);
        _rippleSprite = Resources.Load<Sprite>(rippleSpritePath);
    }

    void EnsureCanvas() {
        // Screen Space Overlay 캔버스가 있으면 사용, 없으면 생성
        _uiCanvas = null;
        var canvases = FindObjectsByType<Canvas>(
        FindObjectsInactive.Exclude,    // 비활성 오브젝트는 무시
        FindObjectsSortMode.None);

        foreach (var c in canvases) {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) {
                _uiCanvas = c;
                break;
            }
        }

        if (!_uiCanvas) {
            var go = new GameObject("UICanvas");
            _uiCanvas = go.AddComponent<Canvas>();
            _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
        _canvasRT = _uiCanvas.transform as RectTransform;
    }

    void ApplyCursor(Texture2D tex) {
        if (!tex) return;
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void SpawnRippleAtMouse() {
        if (!_rippleSprite || !_uiCanvas) return;

        var go = new GameObject("CursorRipple");
        go.transform.SetParent(_canvasRT, false);
        var img = go.AddComponent<Image>();
        img.sprite = _rippleSprite;
        img.raycastTarget = false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, Input.mousePosition, null, out Vector2 localPos);

        var rt = img.rectTransform;
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(rippleStartSize, rippleStartSize);

        StartCoroutine(RunRipple(img));
    }

    IEnumerator RunRipple(Image img) {
        if (!img) yield break;

        var rt = img.rectTransform;
        if (!rt) yield break;

        float t = 0f;
        var baseCol = img.color;

        while (t < rippleDuration) {
            // 씬 전환 등으로 오브젝트가 파괴되면 바로 종료
            if (!img || !rt)
                yield break;

            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / rippleDuration);
            float e = rippleEase.Evaluate(n);

            float size = Mathf.Lerp(rippleStartSize, rippleEndSize, e);
            rt.sizeDelta = new Vector2(size, size);

            float a = Mathf.Lerp(rippleStartAlpha, rippleEndAlpha, e);
            img.color = new Color(baseCol.r, baseCol.g, baseCol.b, a);

            yield return null;
        }

        if (img) Destroy(img.gameObject);
    }
}
