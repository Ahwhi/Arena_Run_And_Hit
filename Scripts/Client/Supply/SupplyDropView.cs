using System.Collections;
using UnityEngine;

public class SupplyDropView : MonoBehaviour {
    public void PlayFall(float fromY, Vector3 toPos) {
        StartCoroutine(CoFall(fromY, toPos));
    }

    IEnumerator CoFall(float fromY, Vector3 toPos) {
        Vector3 start = new Vector3(toPos.x, fromY, toPos.z);
        Vector3 end = toPos;
        transform.position = start;

        float t = 0f, dur = 0.8f;
        while (t < dur) {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / dur);
            // ease-out
            float y = Mathf.Lerp(start.y, end.y, 1f - (1f - n) * (1f - n));
            transform.position = new Vector3(end.x, y, end.z);
            yield return null;
        }
        transform.position = end;
    }

    public void SetIconByEffect(SupplyEffect eff) {
        

        // 기획 7종 전용 컬러 태그(원하면 아이콘/머티 교체로 변경)
        Color c = eff switch {
            SupplyEffect.HealFull => new Color(0.60f, 1.00f, 0.60f),
            SupplyEffect.RangeUp => new Color(0.95f, 0.65f, 0.20f),
            SupplyEffect.SpeedUp => new Color(0.25f, 0.60f, 1.00f),
            SupplyEffect.DamageUp => new Color(1.00f, 0.00f, 0.00f),
            SupplyEffect.Vomit => new Color(0.40f, 0.95f, 0.30f),
            SupplyEffect.Invincible => new Color(0.95f, 0.95f, 0.35f),
            SupplyEffect.Giant => new Color(0.75f, 0.55f, 0.95f),
            SupplyEffect.Knockback => new Color(0.40f, 0.80f, 1.00f),
            _ => Color.white
        };

        var rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) {
            if (!r) continue;
            // (중요) 머티리얼 키워드 켜기  permaterial (sharedMaterial 기준)
            var mat = r.sharedMaterial;
            if (mat) {
                // 표준/URP 공통
                if (mat.HasProperty("_EmissionColor"))
                    mat.EnableKeyword("_EMISSION");
                // HDRP 호환 (프로젝트가 HDRP인 경우)
                if (mat.HasProperty("_EmissiveColor"))
                    mat.EnableKeyword("_EMISSION"); // HDRP는 키워드 세트가 다르지만 대개 같이 켜짐
            }

            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            // 색상(셰이더별 호환)
            if (mat && mat.HasProperty("_BaseColor")) mpb.SetColor("_BaseColor", c); // URP Lit
            if (mat && mat.HasProperty("_Color")) mpb.SetColor("_Color", c);     // Standard/기타

            // 에미션(HDR)
            // 빛나 보이게 하려면 카메라 HDR  Bloom 필요
            var emitHDR = c * 3.0f; // 강도는 취향대로(씬에 Bloom 세기 따라 조절)
            if (mat && mat.HasProperty("_EmissionColor")) mpb.SetColor("_EmissionColor", emitHDR);
            if (mat && mat.HasProperty("_EmissiveColor")) mpb.SetColor("_EmissiveColor", emitHDR);   // HDRP
            if (mat && mat.HasProperty("_EmissiveColorLDR")) mpb.SetColor("_EmissiveColorLDR", emitHDR);

            r.SetPropertyBlock(mpb);
        }
    }
}
