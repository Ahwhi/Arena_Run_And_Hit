using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using Steamworks;
using UnityEngine.UI;
using System;
using System.IO;
using UnityEngine.SceneManagement;
public static class LanguageSwitcher {

    public static IEnumerator SetLocale(string code) {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var target = locales.Find(l => l.Identifier.Code == code);
        if (target != null)
            LocalizationSettings.SelectedLocale = target;
    }

    public static string L(string key) {
        const string TABLE = "GameTexts";

        var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(TABLE, key);
        if (!op.IsDone) op.WaitForCompletion();

        var v = op.Result;
        return string.IsNullOrEmpty(v) ? key : v;
    }

    public static string LF(string key, params object[] args) {
        var fmt = L(key);
        return (args != null && args.Length > 0) ? string.Format(fmt, args) : fmt;
    }
}

public class AuthManager : MonoBehaviour {
    public static AuthManager Instance { get; private set; }
    public bool isTestMode;

    [Header("UI")]
    public TMP_InputField InputField_ID_Login;
    public TMP_InputField InputField_PW_Login;
    public Button Button_Login;
    public Button Button_Open_Register;

    public TMP_InputField InputField_ID_Register;
    public TMP_InputField InputField_PW1_Register;
    public TMP_InputField InputField_PW2_Register;
    public TMP_InputField InputField_NickName;
    public Button Button_Register;
    public Button Button_Close;

    public Toggle Toggle_AutoLogin;

    public GameObject RegisterCanvas;

    public Button Button_BGM;

    public GameObject KoreanWarningPanel;

    public GameObject QuitPanel;

    public GameObject LanPanel;

    public TextMeshProUGUI pingText;

    [Header("Data")]
    private readonly Regex IdRegex = new Regex(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
    private readonly Regex NicknameRegex = new Regex(@"^[°¡-ÆRa-zA-Z0-9]+$", RegexOptions.Compiled);
    private string Input_ID_Text_Login;
    private string Input_PW_Text_Login;
    private string Input_ID_Text_Register;
    private string Input_PW1_Text_Register;
    private string Input_PW2_Text_Register;
    private string Input_NickName_Text;
    private bool isServerStatus;
    private bool isCanUseButton;
    private bool isBGM = true;

    [Header("Audio")]
    public AudioClip bgm;


    public void OnButtonChooseKorean() {
        StartCoroutine(LanguageSwitcher.SetLocale("ko"));

        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        cur.languageIndex = 0;              // 0=ko
        SettingsApplier.Current = cur;      // ¸Þ¸ð¸®µµ °»½Å
        SettingsStorage.Save(cur);          // json ÀúÀå

        LanPanel.SetActive(false);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonChooseEnglish() {
        StartCoroutine(LanguageSwitcher.SetLocale("en"));

        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        cur.languageIndex = 1;              // 1=en
        SettingsApplier.Current = cur;
        SettingsStorage.Save(cur);

        LanPanel.SetActive(false);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    void OpenLanguageSelectPopup() {
        LanPanel.SetActive(true);
    }


    void Start() {
        if (SettingsStorage.WasFirstRunThisSession) {
            OpenLanguageSelectPopup();
        }

        BgmPlayer.I.Play(bgm);
        TryAutoLogin();

        InputField_ID_Login.onValueChanged.AddListener((text) => { Input_ID_Text_Login = text; });
        InputField_PW_Login.onValueChanged.AddListener((text) => { Input_PW_Text_Login = text; });

        InputField_ID_Register.onValueChanged.AddListener((text) => { Input_ID_Text_Register = text; });
        InputField_PW1_Register.onValueChanged.AddListener((text) => { Input_PW1_Text_Register = text; });
        InputField_PW2_Register.onValueChanged.AddListener((text) => { Input_PW2_Text_Register = text; });
        InputField_NickName.onValueChanged.AddListener((text) => { Input_NickName_Text = text; });
        InputField_SteamNickName.onValueChanged.AddListener((text) => { 
            Input_SteamNickName_Text = text;
            Button_SteamNameConfirm.interactable = false;
        });

        InputField_ID_Login.onSubmit.AddListener(OnSubmitLoginID);
        InputField_PW_Login.onSubmit.AddListener(OnSubmitLoginPW);
        InputField_ID_Login.Select();
        InputField_ID_Login.ActivateInputField();

        ApplyIdFilter(InputField_ID_Login);
        ApplyPasswordFilter(InputField_PW_Login);

        ApplyIdFilter(InputField_ID_Register);
        ApplyPasswordFilter(InputField_PW1_Register);
        ApplyPasswordFilter(InputField_PW2_Register);

        RegisterCanvas.SetActive(false);
    }

    void Update() {
        if (NetworkManager.Instance.isConnected != isServerStatus) {
            isServerStatus = NetworkManager.Instance.isConnected;
            OnApplyServerStatus(isServerStatus);
        }

        pingText.text = $"KR Server: {NetworkManager.Instance.AvgRttMs:0} ms";
        if (NetworkManager.Instance.AvgRttMs <= 40) {
            pingText.color = Color.green;
        } else if (NetworkManager.Instance.AvgRttMs > 40 && NetworkManager.Instance.AvgRttMs <= 80) {
            pingText.color = Color.yellow;
        } else {
            pingText.color = Color.red;
        }

        // ÅÇÀ¸·Î ¾ÆÀÌµð, ºñ¹Ð¹øÈ£ ³Ñ¾î°¡±â
        if (Input.GetKeyDown(KeyCode.Tab)) {
            Selectable current = EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>();
            if (current != null) {
                Selectable next = current.FindSelectableOnDown();
                if (next != null)
                    next.Select();
            }
        }
    }

    public void TryAutoLogin() {
        string savedToken = PlayerPrefs.GetString("AccessToken", "");

        // ÅäÅ«ÀÌ ÀÖÀ¸¸é ÀÚµ¿ ·Î±×ÀÎ
        if (!string.IsNullOrEmpty(savedToken)) {
            C_AutoLogin autoLoginPacket = new C_AutoLogin();
            autoLoginPacket.accessToken = savedToken;
            NetworkManager.Send(autoLoginPacket.Write());
        }
    }

    public void OnApplyServerStatus(bool connected) {
        if (!connected) {
            Button_Login.interactable = false;
            Button_Open_Register.interactable = false;
            //UIManager.ShowError("¼­¹ö Á¢¼Ó¿¡ ½ÇÆÐÇÏ¿´½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_CONNECT");
            pingText.text = "Connecting...";
            pingText.color = Color.yellow;
        } else {
            Button_Login.interactable = true;
            Button_Open_Register.interactable = true;
            //UIManager.ShowSuccess("¼­¹ö Á¢¼Ó¿¡ ¼º°øÇÏ¿´½À´Ï´Ù.");
            UIManager.ShowSuccessKey("SUC_CONNECT");
            pingText.text = "Connected";
            pingText.color = Color.green;
        }
    }

    public void OnLoginResult(bool success, int failCode) {
        if (!success) {
            Button_Login.interactable = true;
            Button_Open_Register.interactable = true;
            Button_Register.interactable = true;
        }
        if (failCode == 0) {
            //UIManager.ShowSuccess("·Î±×ÀÎ¿¡ ¼º°øÇÏ¿´½À´Ï´Ù.");
            UIManager.ShowSuccessKey("SUC_LOGIN");
        } else if (failCode == 1) {
            //UIManager.ShowError("¾ø´Â ID ÀÔ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_NOTFOUND_ID");
        } else if (failCode == 2) {
            //UIManager.ShowError("ºñ¹Ð¹øÈ£°¡ Æ²¸³´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_NOTCORRECT_PW");
            InputField_PW_Login.text = "";
            Input_PW_Text_Login = "";
        }
        isCanUseButton = false;
    }

    public void OnAutoLoginResult(bool success, int failCode) {
        if (!success) {
            Button_Login.interactable = true;
            Button_Open_Register.interactable = true;
            Button_Register.interactable = false;
        }
        if (failCode == 0) {
            //UIManager.ShowSuccess("ÀÚµ¿ ·Î±×ÀÎ¿¡ ¼º°øÇÏ¿´½À´Ï´Ù.");
            UIManager.ShowSuccessKey("SUC_AUTOLOGIN");
        } else if (failCode == 1) {
            //UIManager.ShowError("ÀÚµ¿ ·Î±×ÀÎ ÅäÅ«ÀÌ ¾ø½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_NOTFOUND_TOKEN");
        } else if (failCode == 2) {
            //UIManager.ShowError("ÀÚµ¿ ·Î±×ÀÎ ÅäÅ«ÀÌ ¸¸·áµÇ¾ú½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_EXPIRE_TOKEN");
        }
        isCanUseButton = false;
    }

    public void OnRegisterResult(bool success, int failCode) {
        if (failCode == 0) {
            //UIManager.ShowSuccess("È¸¿ø °¡ÀÔ¿¡ ¼º°øÇÏ¿´½À´Ï´Ù.");
            UIManager.ShowSuccessKey("SUC_REGISTER");
        } else if (failCode == 1) {
            //UIManager.ShowError("ÀÌ¹Ì Á¸ÀçÇÏ´Â IDÀÔ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_EXIST_ID");
        } else if (failCode == 2) {
            //UIManager.ShowError("ÀÌ¹Ì Á¸ÀçÇÏ´Â ´Ð³×ÀÓ ÀÔ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_EXIST_NAME");
        }
        Button_Close.interactable = true;
        Button_Register.interactable = true;
        isCanUseButton = false;
    }

    public void OnButtonOpenRegister() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        RegisterCanvas.SetActive(true);
    }

    public void OnButtonClose() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        RegisterCanvas.SetActive(false);
        Input_ID_Text_Login = "";
        Input_PW_Text_Login = "";
        Input_ID_Text_Register = "";
        Input_PW1_Text_Register = "";
        Input_PW2_Text_Register = "";
        Input_NickName_Text = "";
    }

    public void OnButtonRegister() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        if (Input_ID_Text_Register == null || Input_PW1_Text_Register == null || 
            Input_PW2_Text_Register == null || Input_NickName_Text == null) {
            //UIManager.ShowError("ºñ¾îÀÖ´Â Á¤º¸°¡ ÀÖ½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_EMPTY_INFO");
            return;
        }

        if (Input_ID_Text_Register.Length < 6) {
            //UIManager.ShowError("¾ÆÀÌµð´Â 6ÀÚ ÀÌ»óÀÌ¾î¾ß ÇÕ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_ID_LEN");
            return;
        }

        if (Input_PW1_Text_Register.Length < 8) {
            //UIManager.ShowError("ºñ¹Ð¹øÈ£´Â 8ÀÚ ÀÌ»óÀÌ¾î¾ß ÇÕ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_PW_LEN");
            return;
        }

        if (Input_PW1_Text_Register != Input_PW2_Text_Register) {
            //UIManager.ShowError("ºñ¹Ð¹øÈ£°¡ ÀÏÄ¡ÇÏÁö ¾Ê½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_PW_NOTCORRECT");
            return;
        }

        if (Input_NickName_Text.Length < 2 || Input_NickName_Text.Length > 8) {
            //UIManager.ShowError("´Ð³×ÀÓÀº 2±ÛÀÚ ÀÌ»ó, 8±ÛÀÚ ÀÌÇÏ¿©¾ß ÇÕ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_ID_LEN2");
            return;
        }

        // Æ¯¼ö¹®ÀÚ Ã¼Å© (¾ËÆÄºª+¼ýÀÚ¸¸ Çã¿ë)
        if (!IdRegex.IsMatch(Input_ID_Text_Register)) {
            //UIManager.ShowError("¾ÆÀÌµð´Â ¿µ¾î¿Í ¼ýÀÚ¸¸ »ç¿ëÇÒ ¼ö ÀÖ½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_TEXT");
            return;
        }

        if (!NicknameRegex.IsMatch(Input_NickName_Text)) {
            //UIManager.ShowError("´Ð³×ÀÓ¿¡ Æ¯¼ö¹®ÀÚ¸¦ »ç¿ëÇÒ ¼ö ¾ø½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_TEXT2");
            return;
        }

        UIManager.ShowInfo("È¸¿ø °¡ÀÔ ÁßÀÔ´Ï´Ù.");
        isCanUseButton = true;
        Button_Close.interactable = false;
        Button_Register.interactable = false;

        C_Register registerPacket = new C_Register();
        registerPacket.accountId = Input_ID_Text_Register;
        registerPacket.accountPw = Input_PW1_Text_Register;
        registerPacket.nickName = Input_NickName_Text;
        NetworkManager.Send(registerPacket.Write());
    }

    public void OnButtonLogin() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        if (Input_ID_Text_Login == null || Input_PW_Text_Login == null) {
            //UIManager.ShowError("ºñ¾îÀÖ´Â Á¤º¸°¡ ÀÖ½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_EMPTY_INFO");
            return;
        }

        if (Input_ID_Text_Login.Length < 6) {
            //UIManager.ShowError("¾ÆÀÌµð´Â 6ÀÚ ÀÌ»óÀÌ¾î¾ß ÇÕ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_ID_LEN");
            return;
        }

        if (Input_PW_Text_Login.Length < 8) {
            //UIManager.ShowError("ºñ¹Ð¹øÈ£´Â 8ÀÚ ÀÌ»óÀÌ¾î¾ß ÇÕ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_PW_LEN");
            return;
        }

        // Æ¯¼ö¹®ÀÚ Ã¼Å© (¾ËÆÄºª+¼ýÀÚ¸¸ Çã¿ë)
        if (!IdRegex.IsMatch(Input_ID_Text_Login)) {
            //UIManager.ShowError("¾ÆÀÌµð´Â ¿µ¾î¿Í ¼ýÀÚ¸¸ »ç¿ëÇÒ ¼ö ÀÖ½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_TEXT");
            return;
        }

        //UIManager.ShowInfo("·Î±×ÀÎ ÁßÀÔ´Ï´Ù.");
        UIManager.ShowInfoKey("INF_LOGIN");
        isCanUseButton = true;
        Button_Login.interactable = false;
        Button_Register.interactable = false;

        C_Login loginPacket = new C_Login();
        loginPacket.accountId = Input_ID_Text_Login;
        loginPacket.accountPw = Input_PW_Text_Login;
        loginPacket.isAutoLogin = Toggle_AutoLogin.isOn;
        NetworkManager.Send(loginPacket.Write());
    }

    private void OnSubmitLoginID(string text) {
        if (!isServerStatus || isCanUseButton) {
            return;
        }

        if (!string.IsNullOrEmpty(text)) {
            Input_ID_Text_Login = text;
            OnButtonLogin();

            // ´Ù½Ã ÀÔ·ÂÇÒ ¼ö ÀÖµµ·Ï Æ÷Ä¿½º À¯Áö
            InputField_ID_Login.ActivateInputField();
        }
    }

    private void OnSubmitLoginPW(string text) {
        if (!isServerStatus || isCanUseButton) {
            return;
        }

        if (!string.IsNullOrEmpty(text)) {
            Input_PW_Text_Login = text;
            OnButtonLogin();

            // ´Ù½Ã ÀÔ·ÂÇÒ ¼ö ÀÖµµ·Ï Æ÷Ä¿½º À¯Áö
            InputField_PW_Login.ActivateInputField();
        }
    }

    public void OnButtonBGM() {
        if(!isBGM) {
            isBGM = true;
            Button_BGM.GetComponent<Image>().sprite = Resources.Load<Sprite>($"Image/RawImage/soundon");
            BgmPlayer.I.Play(bgm);
        } else {
            isBGM = false;
            Button_BGM.GetComponent<Image>().sprite = Resources.Load<Sprite>($"Image/RawImage/soundoff");
            BgmPlayer.I.Stop();
        }
    }

    private static bool IsAsciiAlnum(char c)
        => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    private static bool IsAsciiPrintableNoSpace(char c)
        => (c >= '!' && c <= '~'); // 0x21 ~ 0x7E

    private void ApplyIdFilter(TMP_InputField f) {
        if (!f) return;
        f.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        f.onValidateInput = (string text, int charIndex, char added) => IsAsciiAlnum(added) ? added : '\0';
        f.onValueChanged.AddListener(current => {
            string cleaned = Regex.Replace(current, "[^A-Za-z0-9]", "");
            if (cleaned != current) {
                int caret = f.caretPosition;
                f.SetTextWithoutNotify(cleaned);
                f.caretPosition = Mathf.Clamp(caret - (current.Length - cleaned.Length), 0, cleaned.Length);
            }
        });
    }

    private void ApplyPasswordFilter(TMP_InputField f) {
        if (!f) return;
        f.characterValidation = TMP_InputField.CharacterValidation.None; // Á÷Á¢ °ËÁõ
        f.onValidateInput = (string text, int charIndex, char added) => IsAsciiPrintableNoSpace(added) ? added : '\0';
        f.onValueChanged.AddListener(current => {
            string cleaned = Regex.Replace(current, @"[^\x21-\x7E]", "");
            if (cleaned != current) {
                int caret = f.caretPosition;
                f.SetTextWithoutNotify(cleaned);
                f.caretPosition = Mathf.Clamp(caret - (current.Length - cleaned.Length), 0, cleaned.Length);
            }
        });
    }

    public void OnButtonQuit() {
        QuitPanel.SetActive(true);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonQuitYes() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Application.Quit();
    }

    public void OnButtonQuitNo() {
        QuitPanel.SetActive(false);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }










    //////////////////////// ½ºÆÀ //////////////////////

    [Header("Steam")]
    public Button Button_SteamLogin;
    public Button Button_SteamNameCheck;
    public Button Button_SteamNameConfirm;
    public Button Button_SteamRetry;
    public TextMeshProUGUI SteamAuthStatus;
    public TMP_InputField InputField_SteamNickName;
    public GameObject Panel_SteamNick;
    public GameObject Panel_SteamError;
    public GameObject SteamPayManager;
    private string Input_SteamNickName_Text;
    private HAuthTicket _ticket = HAuthTicket.Invalid;
    private Callback<GetTicketForWebApiResponse_t> _cbTicket;

    public GameObject panelLogin;
    public GameObject buttonOpenRegister;
    public GameObject panelWarning;
    public GameObject panelSteamLogin;
    public GameObject steamManager;
    public GameObject steamPayManager;

    public GameObject Panel_Agreement;
    public Toggle Toggle_AgreementAll;
    public Button Button_Agree;
    private const int POLICY_VERSION = 1;
    public TextMeshProUGUI Legal1;
    public TextMeshProUGUI Legal2;

    void Awake() {
        Instance = this;
        if (isTestMode) {
            panelLogin.SetActive(true);
            buttonOpenRegister.SetActive(true);
            panelWarning.SetActive(true);
            panelSteamLogin.SetActive(false);
            steamManager.SetActive(false);
            steamPayManager.SetActive(false);
            NetworkManager.isTestMode = true;
            return;
        }
        steamManager.SetActive(true);
        steamPayManager.SetActive(true);

        Button_SteamLogin.interactable = false;
        Panel_SteamError.SetActive(false);
        //Debug.Log("CWD = " + Directory.GetCurrentDirectory());
        //Debug.Log("dataPath = " + Application.dataPath);
        if (SteamManager.Initialized) {
            SteamAuthStatus.text = LanguageSwitcher.LF("UI/Auth19", SteamFriends.GetPersonaName());
            //Debug.Log("AppId = " + SteamUtils.GetAppID().m_AppId);
            _cbTicket = Callback<GetTicketForWebApiResponse_t>.Create(OnGetTicket);
            string identity = "MyGameLogin";
            _ticket = SteamUser.GetAuthTicketForWebApi(identity);
        
            if (_ticket == HAuthTicket.Invalid) {
                //Debug.LogError("GetAuthTicketForWebApi returned Invalid ticket handle");
            } else {
                //Debug.Log("Ticket requested OK, waiting callback...");
            }
        
            SteamPayManager.SetActive(true);
        } else {
            Panel_SteamError.SetActive(true);
        }
    }

    public void OnButtonSteamRetry() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        SceneManager.LoadScene("AuthScene");
    }

    public void OnButtonSteamLogin() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        SceneManager.LoadScene("LobbyScene");
    }

    private void OnGetTicket(GetTicketForWebApiResponse_t cb) {
        if (cb.m_eResult != EResult.k_EResultOK) {
            Debug.LogError($"GetTicketForWebApiResponse failed: {cb.m_eResult}");
            return;
        }

        byte[] ticketData = new byte[cb.m_cubTicket];
        Array.Copy(cb.m_rgubTicket, ticketData, cb.m_cubTicket);

        string ticketHex = BitConverter.ToString(ticketData).Replace("-", "").ToLowerInvariant();
        ulong localSteamId = SteamUser.GetSteamID().m_SteamID;

        //Debug.Log($"1: {ticketHex}\n2: {localSteamId}");
        SendTicketToServer(ticketHex, localSteamId);
    }

    void SendTicketToServer(string ticketHex, ulong localSteamId) {
        C_SteamLogin pkt = new C_SteamLogin();
        pkt.ticketHex = ticketHex;
        pkt.personaName = SteamFriends.GetPersonaName();
        NetworkManager.Send(pkt.Write());
    }

    public static void OnAvailableLogin(S_SteamLoginResult p) {
        var am = FindAnyObjectByType<AuthManager>();

        if (p.isSuccess == true) {
            if (p.needNickSetup) {
                am.Panel_SteamNick.SetActive(true);
                am.InputField_SteamNickName.Select();
                am.InputField_SteamNickName.ActivateInputField();
                am.Button_SteamNameCheck.interactable = true;
                am.Button_SteamNameConfirm.interactable = false;
                am.Button_SteamLogin.interactable = false;
            } else {
                am.Button_SteamLogin.interactable = true;
                am.Panel_SteamError.SetActive(false);
                am.SteamAuthStatus.text = LanguageSwitcher.LF("UI/Auth18", p.nickName);
                NetworkManager.Instance.nickName = p.nickName;
            }

            if (p.needPolicyAgreement) {
                am.Panel_Agreement.SetActive(true);
                am.Legal1.text = LanguageSwitcher.L("POLICY3");
                am.Legal2.text = LanguageSwitcher.L("POLICY4");
            }

        } else {
            am.Button_SteamLogin.interactable = false;
            if (p.failReason == 1) {
                UIManager.ShowErrorKey("ERR_F1"); //½ºÆÀÀÎÁõ½ÇÆÐ
                am.SteamAuthStatus.text = LanguageSwitcher.L("ERR_F1");
            } else if (p.failReason == 2) {
                UIManager.ShowErrorKey("ERR_F2"); //¼­¹ö¿¡·¯
                am.SteamAuthStatus.text = LanguageSwitcher.L("ERR_F2");
            } else if (p.failReason == 3) {
                UIManager.ShowErrorKey("ERR_F3"); //[ÀÓ½Ã ¹ê] °èÁ¤ Á¶»ç ÁßÀÔ´Ï´Ù.
                am.SteamAuthStatus.text = LanguageSwitcher.L("ERR_F3");
            } else if (p.failReason == 4) {
                UIManager.ShowErrorKey("ERR_F4"); //[¹ê] Â÷´ÜµÈ °èÁ¤ÀÔ´Ï´Ù.
                am.SteamAuthStatus.text = LanguageSwitcher.L("ERR_F4");
            } else {
                UIManager.ShowErrorKey("ERR_F5"); //¾Ë¼ö¾ø´Â¿À·ù
                am.SteamAuthStatus.text = LanguageSwitcher.L("ERR_F5");
            }
        }
    }

    public static void OnAvailableLogin_2(S_AgreePolicyResult p) {
        var am = FindAnyObjectByType<AuthManager>();
        am.Panel_Agreement.SetActive(false);
       
    }

    public void OnButtonCheckSteamName() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        if (Input_SteamNickName_Text == null) {
            //UIManager.ShowError("ºñ¾îÀÖ´Â Á¤º¸°¡ ÀÖ½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_EMPTY_INFO");
            return;
        }

        if (Input_SteamNickName_Text.Length < 2 || Input_SteamNickName_Text.Length > 8) {
            //UIManager.ShowError("´Ð³×ÀÓÀº 2±ÛÀÚ ÀÌ»ó, 8±ÛÀÚ ÀÌÇÏ¿©¾ß ÇÕ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_ID_LEN2");
            return;
        }

        if (!NicknameRegex.IsMatch(Input_SteamNickName_Text)) {
            //UIManager.ShowError("´Ð³×ÀÓ¿¡ Æ¯¼ö¹®ÀÚ¸¦ »ç¿ëÇÒ ¼ö ¾ø½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_TEXT2");
            return;
        }

        //UIManager.ShowInfo("È¸¿ø °¡ÀÔ ÁßÀÔ´Ï´Ù.");

        C_CheckSteamNickName pkt = new C_CheckSteamNickName();
        pkt.nickName = Input_SteamNickName_Text;
        NetworkManager.Send(pkt.Write());
        Button_SteamNameCheck.interactable = false;
        Button_SteamNameConfirm.interactable = false;
    }

    public void OnButtonSetSteamName() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        if (Input_SteamNickName_Text == null) {
            //UIManager.ShowError("ºñ¾îÀÖ´Â Á¤º¸°¡ ÀÖ½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_EMPTY_INFO");
            return;
        }

        if (Input_SteamNickName_Text.Length < 2 || Input_SteamNickName_Text.Length > 8) {
            //UIManager.ShowError("´Ð³×ÀÓÀº 2±ÛÀÚ ÀÌ»ó, 8±ÛÀÚ ÀÌÇÏ¿©¾ß ÇÕ´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_ID_LEN2");
            return;
        }

        if (!NicknameRegex.IsMatch(Input_SteamNickName_Text)) {
            //UIManager.ShowError("´Ð³×ÀÓ¿¡ Æ¯¼ö¹®ÀÚ¸¦ »ç¿ëÇÒ ¼ö ¾ø½À´Ï´Ù.");
            UIManager.ShowErrorKey("ERR_TEXT2");
            return;
        }

        C_SetSteamNickName pkt = new C_SetSteamNickName();
        pkt.nickName = Input_SteamNickName_Text;
        NetworkManager.Send(pkt.Write());
        Button_SteamNameCheck.interactable = false;
        Button_SteamNameConfirm.interactable = false;
    }


    public static void OnCheckSteamNameResult(S_CheckSteamNameResult pkt) {
        var am = FindAnyObjectByType<AuthManager>();
        if (pkt.isSuccess) {
            UIManager.ShowSuccessKey("SUC_OK_NAME");
            am.Button_SteamNameConfirm.interactable = true;
        } else {
            UIManager.ShowErrorKey("ERR_EXIST_NAME");
            am.Button_SteamNameConfirm.interactable = false;
        }
        am.Button_SteamNameCheck.interactable = true;
    }

    public static void OnSetSteamNameResult(S_SetSteamNameResult pkt) {
        var am = FindAnyObjectByType<AuthManager>();
        NetworkManager.Instance.nickName = pkt.nickName;
        if (pkt.isSuccess) {
            if (pkt.nickName != "") {
                am.SteamAuthStatus.text = LanguageSwitcher.LF("UI/Auth18", pkt.nickName);
                am.Button_SteamNameCheck.interactable = false;
                am.Button_SteamNameConfirm.interactable = false;
                am.Panel_SteamNick.SetActive(false);
                am.Button_SteamLogin.interactable = true;
            }
        } else {
            UIManager.ShowErrorKey("ERR_EXIST_NAME");
            am.Button_SteamNameCheck.interactable = true;
            am.Button_SteamNameConfirm.interactable = false;
        }
    }

    public void OnToggleChange() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        if (!Toggle_AgreementAll || !Toggle_AgreementAll.isOn) {
            Button_Agree.interactable = false;
        } else {
            Button_Agree.interactable = true;
        }
    }

    public void OnClickAgreementStart() {
        SoundManager.I?.Play2D(SfxId.MouseClick);

        if (!Toggle_AgreementAll || !Toggle_AgreementAll.isOn) {
            UIManager.ShowErrorKey("ERR_NEED_POLICY");
            return;
        }

        var pkt = new C_AgreePolicy();
        pkt.policyVersion = POLICY_VERSION;
        NetworkManager.Send(pkt.Write());
        Button_Agree.interactable = false;
        Toggle_AgreementAll.interactable = false;
    }



}
