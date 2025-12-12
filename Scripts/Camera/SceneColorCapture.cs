using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class SceneColorCapture : MonoBehaviour {
    static readonly int _SceneTexID = Shader.PropertyToID("_SceneTex");

    Camera _cam;
    CommandBuffer _cb;
    RenderTexture _rt;

    public CameraEvent when = CameraEvent.AfterImageEffects;
    public RenderTextureFormat format = RenderTextureFormat.ARGB32;

    void OnEnable() {
        _cam = GetComponent<Camera>();
        CreateTargets();
        CreateCB();
    }

    void OnDisable() {
        ReleaseCB();
        ReleaseTargets();
    }

    void OnPreRender() {
        if (_rt == null || _rt.width != Screen.width || _rt.height != Screen.height) {
            ReleaseTargets();
            CreateTargets();
            ReleaseCB();
            CreateCB();
        }
    }

    void CreateTargets() {
        _rt = new RenderTexture(Screen.width, Screen.height, 0, format);
        _rt.name = "_SceneTexRT";
        _rt.Create();
        Shader.SetGlobalTexture(_SceneTexID, _rt);
    }

    void ReleaseTargets() {
        if (_rt != null) {
            Shader.SetGlobalTexture(_SceneTexID, Texture2D.blackTexture);
            _rt.Release();
            DestroyImmediate(_rt);
            _rt = null;
        }
    }

    void CreateCB() {
        _cb = new CommandBuffer { name = "Capture Scene Color to _SceneTex" };
        _cb.Blit(BuiltinRenderTextureType.CurrentActive, _rt);
        _cam.AddCommandBuffer(when, _cb);
    }

    void ReleaseCB() {
        if (_cb != null && _cam != null)
            _cam.RemoveCommandBuffer(when, _cb);
        _cb?.Release();
        _cb = null;
    }
}
