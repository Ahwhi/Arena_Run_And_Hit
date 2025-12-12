using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }
    public class Bootstrap : MonoBehaviour {
        void Awake() {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            Time.fixedDeltaTime = 1f / 50f;
            Time.maximumDeltaTime = 0.066f;
        }
    }

    [Header("UI")]
    public TMP_InputField InputField_Chat;
    public ScrollRect ScrollRect_Chat;
    public Image Image_Handle;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI pingText;
    public TextMeshProUGUI aliveText;
    public TextMeshProUGUI killText;
    public TextMeshProUGUI watchPlayerNameText;
    public TextMeshProUGUI timeText;
    public GameObject readyPanel;
    public TextMeshProUGUI readyText;
    Image _readyPanelImg;
    public GameObject deathPanel;
    public GameObject watchPanel;

    public Button NonRankExitButtoninDeath;
    public Button RankExitButtoninDeath;
    public Button NonRankExitButtoninWatch;
    public Button RankExitButtoninWatch;

    public GameObject gameOverPanel;
    public GameObject scorePanel;


    public GameObject GameoverScrollView_S;
    public GameObject GameoverScrollView_R;

    public Transform gameOverContent_Survival;
    public Transform gameOverContent_Respawn;

    public GameObject radioHead_R;
    public GameObject radioHead_S;

    public GameObject resultRowPrefab_R;
    public GameObject resultRowPrefab_S;

    public Button GameoverButton_S;
    public Button GameoverButton_R;

    public GameObject timePanel;

    [Header("Data")]
    public int gameMode = -1;
    public static bool isGameStart = false;
    public bool isGameOver = false;
    public static string inputChat;
    public static string mode;
    public static int aliveCount;
    public static int killCount;
    public static int rankNum;
    public static int timeLeftSec = 0;
    public static int countDown = -1;
    bool _fading = false;
    bool _suppressEnterOnce = false;
    public static bool isChated = false;

    [Header("Win Cutscene")]
    [Tooltip("카메라가 우승자한테 붙어있는 시간")]
    public float winFocusTime = 2.0f;
    [Tooltip("우승 포즈 끝나고 결과창 뜨기까지 딜레이")]
    public float resultShowDelay = 1.0f;


    Vector3 _killBaseScale;
    Color _killBaseColor;
    int _lastKillCount = 0;
    int _pendingKillPulses = 0;
    Coroutine _killPulseRoutine = null;


    [Header("Rank Score")]
    public Slider ScoreSlider;
    public TextMeshProUGUI currentRankTear;
    public TextMeshProUGUI nextRankTear;
    public TextMeshProUGUI changeAmount;
    public TextMeshProUGUI yoyak;
    public static int change;
    public static int beforeRank;
    public static int beforeRankScore;
    public static int afterRank;
    public static int afterRankScore;
    public float sliderTime = 1.0f;
    Coroutine _scoreBarRoutine;
    public GameObject SliderFill;


    public LocalizedDynamicText aliveLocalized;
    public LocalizedDynamicText killLocalized;

    public AudioClip bgm;


    static readonly string[] RankMatNames = {
        "0UnrankSparkling",
        "1BronzeSparkling",
        "2SilverSparkling",
        "3GoldSparkling",
        "4PlatinumSparkling",
        "5DiamondSparkling",
        "6GodSparkling"
    };

    public GameObject minimap;

    void Awake() {
        Instance = this;
        readyPanel.SetActive(true);
    }

    void Start() {
        C_EnterGame enterPacket = new C_EnterGame();
        NetworkManager.Send(enterPacket.Write());

        InputField_Chat.onSubmit.AddListener(OnSubmitChat);
        InputField_Chat.onValueChanged.AddListener((text) => { inputChat = text; });
        //readyText.text = "다른 플레이어 대기 중";
        readyText.text = LanguageSwitcher.L("UI/Game5");
        _readyPanelImg = readyPanel ? readyPanel.GetComponent<Image>() : null;
        inputChat = "";
        mode = "";
        aliveCount = 0;
        killCount = 0;
        rankNum = 0;
        countDown = -1;

        _lastKillCount = killCount;

        if (killText != null) {
            _killBaseScale = killText.rectTransform.localScale;
            _killBaseColor = killText.color;
        }
    }

    void Update() {
        if (isChated) {
            isChated = false;
            StartCoroutine(scrollDown());
        }
        
        if (!_suppressEnterOnce && !InputField_Chat.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) {
            InputField_Chat.ActivateInputField();
            InputField_Chat.Select();
            ScrollRect_Chat.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.196f);
            if (Image_Handle != null) {
                Image_Handle.color = new Color(1f, 1f, 1f, 0.196f);
            }
        }

        pingText.text = $"{NetworkManager.Instance.AvgRttMs:0} ms";
        if (NetworkManager.Instance.AvgRttMs <= 40) {
            pingText.color = Color.green;
        } else if (NetworkManager.Instance.AvgRttMs > 40 && NetworkManager.Instance.AvgRttMs <= 80) {
            pingText.color = Color.yellow;
        } else {
            pingText.color = Color.red;
        }

        // 생존자 표시
        aliveLocalized.SetArgs(aliveCount);
        //aliveText.text = $"생존 {aliveCount}";

        //killText.text = $"처치 {killCount}";
        killLocalized.SetArgs(killCount);

        if (killCount > _lastKillCount) {
            int delta = killCount - _lastKillCount;
            _pendingKillPulses += delta;
            if (_killPulseRoutine == null && killText != null) {
                _killPulseRoutine = StartCoroutine(CoRunKillPulses());
            }
        }
        _lastKillCount = killCount;

        if (deathPanel.gameObject.activeSelf) {
            //rankText.text = $"{rankNum}등";
            rankText.text = LanguageSwitcher.LF("UI/Game14", rankNum);
        }

        if (readyPanel.gameObject.activeSelf) {
            if (countDown == -1) {
                return;
            }
            if (countDown > 0) {
                readyText.text = $"{countDown}";
            } else {
                //readyText.text = "게임 시작";
                BgmPlayer.I.Play(bgm);
                readyText.text = LanguageSwitcher.L("UI/Game6");
                if (!_fading) StartCoroutine(FadeOutReadyPanel(0.35f));
            }
        }

    }

    public void OnSettingGameMode(int num) {
        gameMode = num;
        if (num == 0) {
            mode = "서바이벌 모드";
            timePanel.gameObject.SetActive(false);
        } else if (num == 1) {
            mode = "리스폰 모드";
        } else if (num == 2) {
            mode = "경쟁 서바이벌 모드";
            timePanel.gameObject.SetActive(false);
        }
    }

    public void SetMatchTime(int sec) {
        if (gameMode != 1) return;

        int m = Mathf.Max(0, sec) / 60;
        int s = Mathf.Max(0, sec) % 60;
        if (timeText) timeText.text = $"{m:00}:{s:00}";
    }

    public void ShowGameResult(List<(int id, string name, bool isBot, int rank, int kills, int deaths)> results) {
        // 서버에서 바로 호출됨 (S_GameOverHandler)
        StartCoroutine(CoShowGameResult(results));
    }

    IEnumerator CoShowGameResult(List<(int id, string name, bool isBot, int rank, int kills, int deaths)> results) {
        PlayerUIManager.Instance.ClearAllBuffClocks();
        BgmPlayer.I.Stop();
        minimap.SetActive(false);

        if (deathPanel && deathPanel.activeSelf)
            deathPanel.SetActive(false);
        if (watchPanel && watchPanel.activeSelf)
            watchPanel.SetActive(false);

        (int id, string name, bool isBot, int rank, int kills, int deaths) winner = default;
        bool hasWinner = false;
        for (int i = 0; i < results.Count; i++) {
            if (results[i].rank == 1) {
                winner = results[i];
                hasWinner = true;
                break;
            }
        }

        if (hasWinner && PlayerManager.FindAllPlayer.TryGetValue(winner.id, out var winnerPlayer) && winnerPlayer != null) {
            Transform winTr = winnerPlayer.transform;

            if (CameraManager.Instance != null) {
                CameraManager.Instance.FocusToTarget(winTr, 2.0f, 0.25f);
            }

            var anim = winnerPlayer.GetComponentInChildren<Animator>();
            if (anim != null) {
                anim.SetTrigger("Win");
            }
        }

        SoundManager.I?.Play2D(SfxId.GameOver);

        yield return new WaitForSeconds(2.0f);
        yield return new WaitForSeconds(0.8f);

        gameOverPanel.SetActive(true);

        string mtxt = mode;
        if (mtxt == "리스폰 모드") {
            mtxt = LanguageSwitcher.L("MODE_RESPAWN_NAME");
        } else if (mtxt == "서바이벌 모드") {
            mtxt = LanguageSwitcher.L("MODE_SURVIVAL_NAME");
        } else if (mtxt == "경쟁 서바이벌 모드") {
            mtxt = LanguageSwitcher.L("MODE_RANK_NAME");
        }
        modeText.text = mtxt;

        if (mode == "리스폰 모드") {
            GameoverButton_R.gameObject.SetActive(true);
            GameoverScrollView_R.gameObject.SetActive(true);
            radioHead_R.gameObject.SetActive(true);

            if (gameOverContent_Respawn != null) {
                for (int i = gameOverContent_Respawn.childCount - 1; i >= 0; i--) {
                    Destroy(gameOverContent_Respawn.GetChild(i).gameObject);
                }
            }

            foreach (var r in results) {
                var row = Instantiate(resultRowPrefab_R, gameOverContent_Respawn);
                if (r.id == PlayerManager.MyPlayerId) {
                    var img = row.GetComponent<Image>();
                    if (img) img.color = Color.cyan;
                }

                var txts = row.GetComponentsInChildren<TextMeshProUGUI>();
                txts[0].text = r.rank.ToString();
                txts[1].text = r.isBot ? ("BOT_" + r.name) : r.name;
                txts[2].text = r.kills.ToString();
                txts[3].text = r.deaths.ToString();
            }
        } else if (mode == "서바이벌 모드") {
            GameoverButton_R.gameObject.SetActive(true);
            GameoverScrollView_S.gameObject.SetActive(true);
            radioHead_S.gameObject.SetActive(true);

            if (gameOverContent_Survival != null) {
                for (int i = gameOverContent_Survival.childCount - 1; i >= 0; i--) {
                    Destroy(gameOverContent_Survival.GetChild(i).gameObject);
                }
            }

            foreach (var r in results) {
                var row = Instantiate(resultRowPrefab_S, gameOverContent_Survival);
                if (r.id == PlayerManager.MyPlayerId) {
                    var img = row.GetComponent<Image>();
                    if (img) img.color = Color.cyan;
                }

                var txts = row.GetComponentsInChildren<TextMeshProUGUI>();
                txts[0].text = r.rank.ToString();
                txts[1].text = r.isBot ? ("BOT_" + r.name) : r.name;
                txts[2].text = r.kills.ToString();
            }
        } else if (mode == "경쟁 서바이벌 모드") {
            GameoverButton_S.gameObject.SetActive(true);
            GameoverScrollView_S.gameObject.SetActive(true);
            radioHead_S.gameObject.SetActive(true);

            if (gameOverContent_Survival != null) {
                for (int i = gameOverContent_Survival.childCount - 1; i >= 0; i--) {
                    Destroy(gameOverContent_Survival.GetChild(i).gameObject);
                }
            }

            foreach (var r in results) {
                var row = Instantiate(resultRowPrefab_S, gameOverContent_Survival);
                if (r.id == PlayerManager.MyPlayerId) {
                    var img = row.GetComponent<Image>();
                    if (img) img.color = Color.cyan;
                }

                var txts = row.GetComponentsInChildren<TextMeshProUGUI>();
                txts[0].text = r.rank.ToString();
                txts[1].text = r.isBot ? ("BOT_" + r.name) : r.name;
                txts[2].text = r.kills.ToString();
            }
        }

        
    }

    public void OnButtonOpenScorePanel() {
        deathPanel.SetActive(false);
        watchPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        scorePanel.SetActive(true);

        if (change >= 0) {
            //changeAmount.text = $"+{change}점";
            changeAmount.text = LanguageSwitcher.LF("UI/Game15", change);
            if (change != 0) {
                changeAmount.color = Color.cyan;
            }
        } else {
            //changeAmount.text = $"{change}점";
            changeAmount.text = LanguageSwitcher.LF("UI/Game16", change);
            changeAmount.color = Color.red;
        }

        int startValue = 0;
        int endValue = 0;

        if (beforeRank == afterRank) { // 변화없음
            startValue = beforeRankScore;
            endValue = afterRankScore;
            if (beforeRank == 0) {
                currentRankTear.text = LanguageSwitcher.L("Tier1");
                nextRankTear.text = LanguageSwitcher.L("Tier2");
            } else if (beforeRank == 1) {
                currentRankTear.text = LanguageSwitcher.L("Tier2");
                nextRankTear.text = LanguageSwitcher.L("Tier3");
            } else if (beforeRank == 2) {
                currentRankTear.text = LanguageSwitcher.L("Tier3");
                nextRankTear.text = LanguageSwitcher.L("Tier4");
            } else if (beforeRank == 3) {
                currentRankTear.text = LanguageSwitcher.L("Tier4");
                nextRankTear.text = LanguageSwitcher.L("Tier5");
            } else if (beforeRank == 4) {
                currentRankTear.text = LanguageSwitcher.L("Tier5");
                nextRankTear.text = LanguageSwitcher.L("Tier6");
            } else if (beforeRank == 5) {
                currentRankTear.text = LanguageSwitcher.L("Tier6");
                nextRankTear.text = LanguageSwitcher.L("Tier7");
            } else if (beforeRank == 6) {
                currentRankTear.text = LanguageSwitcher.L("Tier7");
                nextRankTear.text = "∞";
            }
        } else if (beforeRank > afterRank) { // 강등
            startValue = 100;
            endValue = afterRankScore;
            if (afterRank == 0) {
                currentRankTear.text = LanguageSwitcher.L("Tier1");
                nextRankTear.text = LanguageSwitcher.L("Tier2");
            } else if (afterRank == 1) {
                currentRankTear.text = LanguageSwitcher.L("Tier2");
                nextRankTear.text = LanguageSwitcher.L("Tier3");
            } else if (afterRank == 2) {
                currentRankTear.text = LanguageSwitcher.L("Tier3");
                nextRankTear.text = LanguageSwitcher.L("Tier4");
            } else if (afterRank == 3) {
                currentRankTear.text = LanguageSwitcher.L("Tier4");
                nextRankTear.text = LanguageSwitcher.L("Tier5");
            } else if (afterRank == 4) {
                currentRankTear.text = LanguageSwitcher.L("Tier5");
                nextRankTear.text = LanguageSwitcher.L("Tier6");
            } else if (afterRank == 5) {
                currentRankTear.text = LanguageSwitcher.L("Tier6");
                nextRankTear.text = LanguageSwitcher.L("Tier7");
            } else if (afterRank == 6) {
                currentRankTear.text = LanguageSwitcher.L("Tier7");
                nextRankTear.text = "∞";
            }
        } else if (beforeRank < afterRank) { // 승급
            startValue = 0;
            endValue = afterRankScore;
            if (afterRank == 0) {
                currentRankTear.text = LanguageSwitcher.L("Tier1");
                nextRankTear.text = LanguageSwitcher.L("Tier2");
            } else if (afterRank == 1) {
                currentRankTear.text = LanguageSwitcher.L("Tier2");
                nextRankTear.text = LanguageSwitcher.L("Tier3");
            } else if (afterRank == 2) {
                currentRankTear.text = LanguageSwitcher.L("Tier3");
                nextRankTear.text = LanguageSwitcher.L("Tier4");
            } else if (afterRank == 3) {
                currentRankTear.text = LanguageSwitcher.L("Tier4");
                nextRankTear.text = LanguageSwitcher.L("Tier5");
            } else if (afterRank == 4) {
                currentRankTear.text = LanguageSwitcher.L("Tier5");
                nextRankTear.text = LanguageSwitcher.L("Tier6");
            } else if (afterRank == 5) {
                currentRankTear.text = LanguageSwitcher.L("Tier6");
                nextRankTear.text = LanguageSwitcher.L("Tier7");
            } else if (afterRank == 6) {
                currentRankTear.text = LanguageSwitcher.L("Tier7");
                nextRankTear.text = "∞";
            }
        }

        //yoyak.text = $"{currentRankTear.text} {afterRankScore}점";
        yoyak.text = LanguageSwitcher.LF("UI/Game18", currentRankTear.text, afterRankScore);
        yoyak.color = Color.yellow;

        if (_scoreBarRoutine != null) StopCoroutine(_scoreBarRoutine);
        _scoreBarRoutine = StartCoroutine(PlayScoreBar(beforeRank, startValue, afterRank, endValue, sliderTime));
    }

    IEnumerator CoTweenSlider(float from, float to, float time) {
        if (!ScoreSlider) {
            yield break;
        }
        
        ScoreSlider.maxValue = 100f;

        float t = 0f;
        ScoreSlider.value = from;

        while (t < time) {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            float e = k * k * (3f - 2f * k);
            ScoreSlider.value = Mathf.Lerp(from, to, e);
            yield return null;
        }
        ScoreSlider.value = to;
    }

    IEnumerator PlayScoreBar(int startRank, int startScore, int endRank, int endScore, float perTier = 0.8f) {
        if (!ScoreSlider) yield break;
        SoundManager.I?.Play2D(SfxId.Adjust);
        ScoreSlider.maxValue = 100f;

        string matName = RankMatNames[startRank];

        Material mat = Resources.Load<Material>($"UI/Material/NameRank/{matName}");
        if (SliderFill.TryGetComponent(out Image img)) {
            img.material = new Material(mat);
        }

        // 같은 티어
        if (startRank == endRank) {
            yield return CoTweenSlider(startScore, endScore, perTier);
            yield break;
        }

        // 승급
        if (startRank < endRank) {
            ScoreSlider.value = startScore;
            for (int r = startRank; r < endRank; r++) {
                yield return CoTweenSlider(ScoreSlider.value, 100f, perTier);
                yield return new WaitForSeconds(0.1f);
                matName = RankMatNames[endRank];

                mat = Resources.Load<Material>($"UI/Material/NameRank/{matName}");
                if (SliderFill.TryGetComponent(out Image imgs)) {
                    imgs.material = new Material(mat);
                }
                ScoreSlider.value = 0f;
            }
            yield return CoTweenSlider(0f, endScore, perTier);
            yield break;
        }

        // 강등
        if (startRank > endRank) {
            ScoreSlider.value = startScore;
            for (int r = startRank; r > endRank; r--) {
                yield return CoTweenSlider(ScoreSlider.value, 0f, perTier);
                yield return new WaitForSeconds(0.1f);
                matName = RankMatNames[endRank];

                mat = Resources.Load<Material>($"UI/Material/NameRank/{matName}");
                if (SliderFill.TryGetComponent(out Image imgs)) {
                    imgs.material = new Material(mat);
                }
                ScoreSlider.value = 100f;
            }
            yield return CoTweenSlider(ScoreSlider.value, endScore, perTier);
        }
    }

    public void OnButtonWatchGame() {
        if (deathPanel.gameObject.activeSelf) {
            deathPanel.SetActive(false);
            watchPanel.SetActive(true);
            if (mode == "경쟁 서바이벌 모드") {
                RankExitButtoninWatch.gameObject.SetActive(true);
            } else {
                NonRankExitButtoninWatch.gameObject.SetActive(true);
            }
        }

        if (CameraManager.Instance != null)
            CameraManager.Instance.EnterSpectate();

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonNextSpectate() {
        if (CameraManager.Instance != null && CameraManager.Instance.IsSpectating)
            CameraManager.Instance.SpectateNext(+1);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }


    public void OnButtonExitGame() {
        beforeRank = 0;
        beforeRankScore = 0;
        afterRank = 0;
        afterRankScore = 0;
        change = 0;

        C_LeaveGame pkt = new C_LeaveGame();
        NetworkManager.Send(pkt.Write());
        
        PlayerManager.Instance.ResetAll();
        BotRuntime.Instance.ResetState();
        isGameStart = false;
        SoundManager.I?.Play2D(SfxId.MouseClick);
        SceneManager.LoadScene("LobbyScene");
    }

    IEnumerator scrollDown() {
        float time = 0f;
        while (time < 0.5f) {
            time += Time.deltaTime;
            ScrollRect_Chat.verticalNormalizedPosition = 0f;
            yield return null;
        }
    }

    private void OnSubmitChat(string text) {
        _suppressEnterOnce = false;
        inputChat = text;
        OnButtonSend();
    }

    public void OnButtonSend() {
        if (!string.IsNullOrEmpty(inputChat)) {
            C_Chat chatPacket = new C_Chat();
            chatPacket.message = inputChat;
            NetworkManager.Send(chatPacket.Write());
            SoundManager.I?.Play2D(SfxId.Chat);
        }
        
        InputField_Chat.text = null;
        inputChat = null;
        InputField_Chat.DeactivateInputField();
        EventSystem.current?.SetSelectedGameObject(null);
        ScrollRect_Chat.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.0f);
        if (Image_Handle != null) {
            Image_Handle.color = new Color(1f, 1f, 1f, 0.0f);
        }
        _suppressEnterOnce = true;
        StartCoroutine(CoResetSuppressEnter());
        
    }

    IEnumerator CoResetSuppressEnter() {
        yield return null;
        while (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            yield return null;
        _suppressEnterOnce = false;
    }

    void EnsureAlpha(float a) {
        if (_readyPanelImg) {
            var c = _readyPanelImg.color; c.a = a; _readyPanelImg.color = c;
        }
        if (readyText) {
            var c = readyText.color; c.a = a; readyText.color = c;
        }
    }

    IEnumerator FadeOutReadyPanel(float duration) {
        _fading = true;

        float t = 0f;
        float startA_img = _readyPanelImg ? _readyPanelImg.color.a : 1f;
        float startA_text = readyText ? readyText.color.a : 1f;

        while (t < duration) {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            if (_readyPanelImg) {
                var c = _readyPanelImg.color; c.a = startA_img * k; _readyPanelImg.color = c;
            }
            if (readyText) {
                var c = readyText.color; c.a = startA_text * k; readyText.color = c;
            }
            yield return null;
        }


        EnsureAlpha(0f);
        readyPanel.SetActive(false);
        _fading = false;
        countDown = -1;
        isGameStart = true;
    }


    IEnumerator CoRunKillPulses() {
        while (_pendingKillPulses > 0) {
            _pendingKillPulses--;
            yield return CoPunchKillText(killText,
                upScale: 2.32f,
                upTime: 0.27f,
                downTime: 0.40f,
                flash: true
            );
            yield return null;
        }
        _killPulseRoutine = null;
    }

    IEnumerator CoPunchKillText(TextMeshProUGUI tmp, float upScale, float upTime, float downTime, bool flash) {
        if (tmp == null) yield break;

        var rt = tmp.rectTransform;

        Color targetColor = _killBaseColor;
        if (flash) {
            targetColor = new Color(
                Mathf.Clamp01(_killBaseColor.r + 0.25f),
                Mathf.Clamp01(_killBaseColor.g + 0.25f),
                Mathf.Clamp01(_killBaseColor.b + 0.25f),
                _killBaseColor.a
            );
        }

        float t = 0f;
        Vector3 from = _killBaseScale;
        Vector3 to = _killBaseScale * upScale;
        Color colorFrom = tmp.color;
        while (t < upTime) {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / upTime);
            float e = 1f - Mathf.Pow(1f - k, 3f);
            rt.localScale = Vector3.LerpUnclamped(from, to, e);
            if (flash) tmp.color = Color.Lerp(colorFrom, targetColor, e);
            yield return null;
        }


        t = 0f;
        from = rt.localScale;
        to = _killBaseScale;
        colorFrom = tmp.color;
        while (t < downTime) {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / downTime);
            float e = k * k * (3f - 2f * k);
            rt.localScale = Vector3.LerpUnclamped(from, to, e);
            if (flash) tmp.color = Color.Lerp(colorFrom, _killBaseColor, e);
            yield return null;
        }

        rt.localScale = _killBaseScale;
        if (flash) tmp.color = _killBaseColor;
    }

    void SetCameraViewMode(CameraViewMode mode) {
        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();

        cur.cameraViewMode = mode;
        SettingsApplier.Current = cur;

        SettingsStorage.Save(cur);

        if (SettingsApplier.I != null) {
            SettingsApplier.I.ApplyCamera(cur);
        } else {
            CameraManager.Instance?.ApplyViewMode(mode);
        }
    }

    public void OnButtonToggleCameraView() {
        if (Input.GetKeyDown(KeyCode.Space) ||
        Input.GetKeyDown(KeyCode.Return) ||
        Input.GetKeyDown(KeyCode.KeypadEnter)) {
            return;
        }

        SoundManager.I?.Play2D(SfxId.MouseClick);

        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        var next = (cur.cameraViewMode == CameraViewMode.Default)
            ? CameraViewMode.TopView
            : CameraViewMode.Default;

        SetCameraViewMode(next);
    }

}
