using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPlayer : MonoBehaviour {
    protected Animator anim;
    bool attackLatch;
    bool danceLatch;
    private bool isCanAttack = true;
    int pendingEmoteSlot = 0; // 0=없음

    public static string sku1 = "";
    public static string sku2 = "";
    public static string sku3 = "";
    public static string sku4 = "";

    void Start() {
        anim = GetComponentInChildren<Animator>();
    }

    private void Update() {
        var kb = SettingsApplier.Current?.keys ?? new KeyBindings();

        // 공격
        if (isCanAttack && Input.GetKeyDown(kb.Get(KeyAction.Attack))) {
            attackLatch = true;
            isCanAttack = false;
            StartCoroutine(SetAttackCoolDown());
        }

        if (attackLatch) {
            attackLatch = false;
            anim?.SetTrigger("Attack");
        }

        if (Input.GetKeyDown(kb.Get(KeyAction.Dance1))) {
            pendingEmoteSlot = 1;
            danceLatch = true;
        }
        if (Input.GetKeyDown(kb.Get(KeyAction.Dance2))) {
            pendingEmoteSlot = 2;
            danceLatch = true;
        }
        if (Input.GetKeyDown(kb.Get(KeyAction.Dance3))) {
            pendingEmoteSlot = 3;
            danceLatch = true;
        }
        if (Input.GetKeyDown(kb.Get(KeyAction.Dance4))) {
            pendingEmoteSlot = 4;
            danceLatch = true;
        }

        if (danceLatch) {
            danceLatch = false;

            // 슬롯 번호 → 해당 슬롯에 들어있는 sku 가져오기
            string triggerName = GetDanceTriggerFromSlot(pendingEmoteSlot);
            pendingEmoteSlot = 0;

            // 아무 것도 장착 안 돼있으면 그냥 무시
            if (!string.IsNullOrEmpty(triggerName)) {
                SoundManager.I?.StopAllSfx();
                if (triggerName == "EMOTE_BASIC_01") {
                    SoundManager.I?.Play2D(SfxId.Dance_Basic);
                }
                else if(triggerName == "EMOTE_HIPHOP_01") {
                    SoundManager.I?.Play2D(SfxId.Dance_Hiphop);
                }

                anim?.SetTrigger(triggerName);
            }
        }
    }

    string GetDanceTriggerFromSlot(int slot) {
        switch (slot) {
            case 1: return sku1;
            case 2: return sku2;
            case 3: return sku3;
            case 4: return sku4;
        }
        return null;
    }

    public static void SetDanceSku(int zeroBasedIndex, string sku) {
        // zeroBasedIndex: 0~3 들어온다는 가정
        switch (zeroBasedIndex) {
            case 0: sku1 = sku ?? ""; break;
            case 1: sku2 = sku ?? ""; break;
            case 2: sku3 = sku ?? ""; break;
            case 3: sku4 = sku ?? ""; break;
        }
    }

    IEnumerator SetAttackCoolDown() {
        float time = 0f;
        while (time < LevelData.ATTACK_COOLDOWN) {
            time += Time.deltaTime;
            yield return null;
        }
        isCanAttack = true;
    }
}
