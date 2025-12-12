using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SupplyEffect {
    HealFull = 1,
    RangeUp = 2,
    SpeedUp = 3,
    DamageUp = 4,
    Vomit = 5,
    Invincible = 6,
    Giant = 7,
    Knockback = 8
}

public class SupplyManager : MonoBehaviour {
    public static SupplyManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject supplyPrefab;      // Resources/"SupplyCrate" fallback
    [Header("FX")]
    public GameObject pickupVfx;         // 줍는 순간 반짝

    // ★ 추가: 구토 폭발 VFX 프리팹
    public GameObject vomitExplodeVfx;  // Resources/"VomitExplosion" 로드 fallback

    readonly Dictionary<int, GameObject> drops = new();
    public event Action<int, Vector3> OnSupplySpawned;   // (dropId, worldPos)
    public event Action<int> OnSupplyDespawned;          // (dropId)

    void Awake() {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ==== 서버가 스폰 알릴 때 ====
    public void Spawn(int id, SupplyEffect eff, Vector3 groundPos) {
        if (!supplyPrefab) {
            supplyPrefab = Resources.Load<GameObject>("SupplyCrate");
            if (!supplyPrefab) { Debug.LogError("Supply prefab missing"); return; }
        }
        if (drops.ContainsKey(id)) return;

        var go = Instantiate(supplyPrefab);
        go.name = $"Supply_{id}_{eff}";
        var drop = go.AddComponent<SupplyDropView>();
        drop.PlayFall(fromY: 20f, toPos: groundPos);
        drop.SetIconByEffect(eff);
        drops[id] = go;

        OnSupplySpawned?.Invoke(id, groundPos);
    }

    // ==== 서버가 소멸(픽업/만료) 알릴 때 ====
    public void Despawn(int id, int reason) {
        if (!drops.TryGetValue(id, out var go)) return;

        // reason==0: 픽업, 1: 만료
        if (reason == 0 && pickupVfx)
            Instantiate(pickupVfx, go.transform.position + Vector3.up * 1.5f, Quaternion.identity);

        OnSupplyDespawned?.Invoke(id);

        Destroy(go);
        drops.Remove(id);
    }

    // ==== 효과 적용 알림(S_SupplyApplied) ====
    public void OnApplied(Player p, SupplyEffect eff, int amount, int durationMs) {
        // 토스트(원하면 UI에 붙이기)
        string msg = eff switch {
            SupplyEffect.HealFull => LanguageSwitcher.L("Ingame1"),
            SupplyEffect.RangeUp => LanguageSwitcher.L("Ingame2"),
            SupplyEffect.SpeedUp => LanguageSwitcher.L("Ingame3"),
            SupplyEffect.DamageUp => LanguageSwitcher.L("Ingame4"),
            SupplyEffect.Vomit => LanguageSwitcher.L("Ingame5"),
            SupplyEffect.Invincible => LanguageSwitcher.L("Ingame6"),
            SupplyEffect.Giant => LanguageSwitcher.L("Ingame7"),
            SupplyEffect.Knockback => LanguageSwitcher.L("Ingame8"),
            _ => "Supply!"
        };
        // 예: PopupText.Show($"{(p is MyPlayer ? "You" : p.NickName)}: {msg}", p is MyPlayer ? Color.green : Color.white);


        // ★ 버프 시계 시작 (지속형만)
        if (eff == SupplyEffect.RangeUp || eff == SupplyEffect.SpeedUp ||
            eff == SupplyEffect.DamageUp || eff == SupplyEffect.Vomit ||
            eff == SupplyEffect.Invincible || eff == SupplyEffect.Giant ||
            eff == SupplyEffect.Knockback) {
            PlayerUIManager.Instance?.StartBuffTimer(p, eff, durationMs / 1000f);
        }

        if (eff == SupplyEffect.RangeUp && p != null) {
            var vfx = p.GetComponent<ArmStretchVfx>();
            if (vfx == null) vfx = p.gameObject.AddComponent<ArmStretchVfx>();
            vfx.Play(durationMs / 1000f, LevelData.ARM_STRETCH_LENGTH);
        }

        // 시각 연출은 보조 컴포넌트에 위임
        var vis = p.GetComponent<SupplyVisuals>();
        if (!vis) vis = p.gameObject.AddComponent<SupplyVisuals>();

        float dur = durationMs / 1000f;

        switch (eff) {
            case SupplyEffect.HealFull:
                vis.PlayHealFlash(); // 짧은 반짝
                break;

            case SupplyEffect.RangeUp:
                vis.PlayRangeUp(dur); // 트레일 굵기/색 강조
                break;

            case SupplyEffect.SpeedUp:
                vis.PlaySpeedUp(dur); // 잔상/스피드라인
                break;

            case SupplyEffect.DamageUp:
                vis.PlayDamageUp(dur); // 손/무기 Emission 강화
                break;

            case SupplyEffect.Vomit:
                vis.PlayVomitCountdown(dur); // 머리 위 카운트/오라
                break;

            case SupplyEffect.Invincible:
                vis.PlayInvincible(dur); // 보호막 버블
                break;

            case SupplyEffect.Giant:
                vis.PlayGiant(dur); // 스케일 2배 → 원복
                break;

            case SupplyEffect.Knockback: 
                vis.PlayKnockbackReady(dur); 
                break;
        }

        //if (p is MyPlayer) {
            var col = eff switch {
                SupplyEffect.HealFull => new Color(0.6f, 1f, 0.6f),
                SupplyEffect.RangeUp => new Color(0.6f, 0.8f, 1f),
                SupplyEffect.SpeedUp => new Color(0.8f, 0.9f, 1f),
                SupplyEffect.DamageUp => new Color(1f, 0.6f, 0.6f),
                SupplyEffect.Vomit => new Color(1f, 0.9f, 0.6f),
                SupplyEffect.Invincible => new Color(1f, 1f, 0.6f),
                SupplyEffect.Giant => new Color(0.9f, 0.8f, 1f),
                _ => Color.white
            };
            PlayerUIManager.Instance?.ShowToast(p, msg, col, life: 1.1f, coalesceSame: true);
        //}
    }

    public void SpawnVomitExplosion(Vector3 center, float radius) {
        if (!vomitExplodeVfx) {
            vomitExplodeVfx = Resources.Load<GameObject>("VomitExplosion");
            if (!vomitExplodeVfx) { Debug.LogWarning("[SupplyManager] VomitExplosion prefab missing"); return; }
        }
        var go = Instantiate(vomitExplodeVfx, center, Quaternion.identity);
        var fx = go.GetComponent<VomitExplosionFx>();
        if (fx) fx.Play(radius);
    }

    public IEnumerable<(int id, Vector3 pos)> EnumerateAliveDrops() {
        foreach (var kv in drops) {
            if (kv.Value) yield return (kv.Key, kv.Value.transform.position);
        }
    }

}
