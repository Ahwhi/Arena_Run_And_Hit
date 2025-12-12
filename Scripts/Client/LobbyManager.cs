using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#region 정의
enum Tab {
    Play,
    LockerRoom,
    Ranking,
    History,
    Shop
}

enum Tab_LockerRoom {
    Character,
    Trail,
    Dance
}

enum Tab_Shop {
    Character,
    Trail,
    Dance,
    Etc
}

enum Tab_Setting {
    Video,
    Sound,
    Key,
    Social,
    Account
}

enum GameMode {
    SURVIVAL,
    RESPAWN,
    RANKSURVIVAL
}

enum Tab_Chat {
    Normal,
    Group
}

public enum ChatType { Normal, Group, System }
#endregion

public class LobbyManager : MonoBehaviour {
    [Header("Canvas")]
    public GameObject Canvas_Play;
    public GameObject Canvas_LockerRoom;
    public GameObject Canvas_Ranking;
    public GameObject Canvas_History;
    public GameObject Canvas_Shop;
    public GameObject Canvas_Popup;
    public GameObject Canvas_PopupUser;

    [Header("Panel")]
    public GameObject Panel_MatchMaking;
    public GameObject Panel_Invite;
    public GameObject Panel_Group;
    public GameObject Panel_Pay;

    public GameObject Panel_Pay1;
    public GameObject Panel_Pay2;
    public GameObject Panel_Pay3;
    public GameObject Panel_Pay4;
    public GameObject Panel_Pay5;

    //LockerRoom Panel
    public GameObject Panel_LockerRoom_Character;
    public GameObject Panel_LockerRoom_Trail;
    public GameObject Panel_LockerRoom_Dance;
    public GameObject Panel_LockerRoom_DanceSlot;
    public GameObject Panel_LockerRoom_Help;

    //Shop Panel
    public GameObject Panel_Shop_Character;
    public GameObject Panel_Shop_Trail;
    public GameObject Panel_Shop_Dance;
    public GameObject Panel_Shop_Etc;

    //Popup Panel
    public GameObject Panel_Popup;
    public GameObject Panel_Setting;
    public GameObject Panel_Setting_Video;
    public GameObject Panel_Setting_Sound;
    public GameObject Panel_Setting_Key;
    public GameObject Panel_Setting_Social;
    public GameObject Panel_Setting_Account;

    public GameObject Panel_UserInformation;
    public GameObject Panel_FindUser;
    public TMP_InputField InputField_FindUser;
    public static string inputFindUser;

    [Header("Chat")]
    public TMP_InputField InputField_Chat;
    public ScrollRect ScrollRect_Chat;
    public Image Image_Handle;
    public GameObject Chat_Filter_Panel;
    bool _suppressEnterOnce = false;
    public static bool isChated = false;
    public static string inputChat;
    public TextMeshProUGUI tabChat;

    public Toggle Toggle_ShowNormalChat;
    public Toggle Toggle_ShowGroupChat;
    public Toggle Toggle_ShowSystemChat;

    public GameObject Panel_Information;
    public Slider Slider_Information;

    public GameObject Panel_Battlepass;
    public TextMeshProUGUI pingText;

    class ChatItem : MonoBehaviour {
        public ChatType type = ChatType.Normal;
    }

    [SerializeField] Transform _chatContent;

    void Awake() {
        if (_chatContent == null) {
            var go = GameObject.Find("ChatContent");
            if (go) _chatContent = go.transform;
        }

        //var pkt = new C_RequestBattlePass();
        //NetworkManager.Send(pkt.Write());
        //Panel_Battlepass.SetActive(true);
    }

    const string PopupRootName = "ChatPopupPrefab";
    static GameObject _currentPopup;
    static string _currentPopupNick;

    static string _currentInviterNick;


    [Header("Settings UI")]
    public SettingsPanelUI settingsPanel;

    [Header("Button")]
    //Play
    public Button Button_SelectLeft;
    public Button Button_SelectRight;
    public Button Button_FindMatch;

    //TabButton
    public Button Button_Tab_Play;
    public Button Button_Tab_LockerRoom;
    public Button Button_Tab_Ranking;
    public Button Button_Tab_History;
    public Button Button_Tab_Shop;

    //TabButton - LockerRoom
    public Button Button_Tab_LockerRoom_Character;
    public Button Button_Tab_LockerRoom_Trail;
    public Button Button_Tab_LockerRoom_Dance;

    //TabButton - Shop
    public Button Button_Tab_Shop_Character;
    public Button Button_Tab_Shop_Trail;
    public Button Button_Tab_Shop_Dance;
    public Button Button_Tab_Shop_Etc;

    //Popup
    public Button Button_Setting;
    public Button Button_LogOut;
    public Button Button_Cancel;

    //TabButton - Setting
    public Button Button_Setting_Video;
    public Button Button_Setting_Sound;
    public Button Button_Setting_Key;
    public Button Button_Setting_Social;
    public Button Button_Setting_Account;

    [Header("Text")]
    public TextMeshProUGUI TMP_SelectedMode;
    public TextMeshProUGUI TMP_ModeInformation1;
    public TextMeshProUGUI TMP_ModeInformation2;
    public TextMeshProUGUI TMP_ModeInformation3;
    public TextMeshProUGUI TMP_MatchButton;
    public TextMeshProUGUI TMP_Population;
    public TextMeshProUGUI TMP_MatchMaking;

    [Header("Loading")]
    public GameObject spinnerObj;

    [Header("Data")]
    private Tab selectedTab = Tab.Play;
    private Tab_LockerRoom selectedTab_LockerRoom = Tab_LockerRoom.Character;
    private Tab_Shop selectedTab_Shop = Tab_Shop.Character;
    private Tab_Setting selectedTab_Setting = Tab_Setting.Video;
    static GameMode selectedMode = GameMode.SURVIVAL;
    private Tab_Chat selectedChatMode = Tab_Chat.Normal;
    private bool isButtonClicked = false;
    static bool isFindingMatch = false;
    static float findingTime = 0;
    public static int population = 0;

    [Header("Preview (Character)")]
    public Transform PreviewAnchor;
    private GameObject _previewGO;
    private string _previewSku = null;
    private string _previewTrailSku = null;

    [Header("Audio")]
    public AudioClip bgm;

    [Header("MatchMaking FX")]
    [SerializeField] float mmTopMargin = 64f;
    [SerializeField] float mmShowDuration = 0.24f;
    [SerializeField] float mmHideDuration = 0.20f;
    [SerializeField] bool mmUseUnscaledTime = true;
    [SerializeField] float mmHorizontalOffset = 0f;

    RectTransform _mmRect;
    CanvasGroup _mmGroup;
    Coroutine _mmAnimCo;

    [Header("Invite FX")]
    [SerializeField] float inviteRightMargin = 32f;
    [SerializeField] float inviteShowDuration = 0.28f;
    [SerializeField] float inviteHideDuration = 0.22f;
    [SerializeField] bool inviteUseUnscaledTime = true;

    RectTransform _invRect;
    CanvasGroup _invGroup;
    Coroutine _invAnimCo;

    [Header("Invite UI")]
    public Slider Slider_Invite;
    public float inviteExpireSeconds = 10f;

    Coroutine _inviteTimerCo;

    [Header("Group UI")]
    public GameObject GroupListPrefab;
    public static bool isCanFindGroupMatch = false;


    public GameObject RecordPrefab;
    public GameObject leaderboardPrefab;
    public TextMeshProUGUI leaderboardUpdateTime;

    TextMeshProUGUI _lockerRoomHelpText;
    public GameObject RankTimePanel;



    // 로컬 언어
    //public LocalizedDynamicText t1Localized;
    //public LocalizedDynamicText t2Localized;
    //public LocalizedDynamicText t3Localized;
    //public LocalizedDynamicText t4Localized;


    static readonly string[] RankMatNames = {
        "0UnrankSparkling",
        "1BronzeSparkling",
        "2SilverSparkling",
        "3GoldSparkling",
        "4PlatinumSparkling",
        "5DiamondSparkling",
        "6GodSparkling"
    };


    void Start() {
        SetupMatchPanel();
        Panel_MatchMaking.SetActive(false);
        TMP_MatchMaking.text = "";
        findingTime = 0;
        isFindingMatch = false;
        selectedMode = GameMode.SURVIVAL;
        Button_SelectLeft.interactable = false;

        InputField_Chat.onSubmit.AddListener(OnSubmitChat);
        InputField_Chat.onValueChanged.AddListener((text) => { inputChat = text; });
        Chat_Filter_Panel.gameObject.SetActive(false);

        SetupInvitePanel();     
        Panel_Invite.SetActive(false);
        Panel_Group.SetActive(false);

        C_EnterLobby pkt = new C_EnterLobby();
        NetworkManager.Send(pkt.Write());

        BgmPlayer.I.Play(bgm);

        Toggle_ShowNormalChat.onValueChanged.AddListener(_ => ApplyChatFilter());
        Toggle_ShowGroupChat.onValueChanged.AddListener(_ => ApplyChatFilter());
        Toggle_ShowSystemChat.onValueChanged.AddListener(_ => ApplyChatFilter());
        ApplyChatFilter();

        // Tab으로 포커스 이동 방지
        var nav = InputField_Chat.navigation;
        nav.mode = Navigation.Mode.None;
        InputField_Chat.navigation = nav;

        RefreshLockerRoomHelp();

        InputField_FindUser.onValueChanged.AddListener((texts) => { inputFindUser = texts; });

        // Tab 문자가 텍스트에 들어가는 것 차단
        InputField_Chat.onValidateInput += (string text, int charIndex, char addedChar) =>
            (addedChar == '\t') ? '\0' : addedChar;
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
            Chat_Filter_Panel.gameObject.SetActive(true);
            if (Image_Handle != null) {
                Image_Handle.color = new Color(1f, 1f, 1f, 0.196f);
            }
        }

        if (InputField_Chat.isFocused && Input.GetKeyDown(KeyCode.Tab)) {
            OnButtonChatTab();

            EventSystem.current?.SetSelectedGameObject(InputField_Chat.gameObject);
            InputField_Chat.ActivateInputField();
            // 캐럿을 맨 끝으로
            int len = InputField_Chat.text?.Length ?? 0;
            InputField_Chat.caretPosition = len;
            InputField_Chat.stringPosition = len;
        }

        if (isFindingMatch) {
            //Button_FindMatch.GetComponentInChildren<TextMeshProUGUI>().text = "매칭 취소";
            Button_FindMatch.GetComponentInChildren<TextMeshProUGUI>().text = LanguageSwitcher.L("MATCH_B_CANCEL");
        } else {
            //Button_FindMatch.GetComponentInChildren<TextMeshProUGUI>().text = "매칭 찾기";
            Button_FindMatch.GetComponentInChildren<TextMeshProUGUI>().text = LanguageSwitcher.L("MATCH_B_FIND");
        }
        if (isButtonClicked) {
            isButtonClicked = false;
            Button_SelectLeft.interactable = true;
            Button_SelectRight.interactable = true;
            Button_FindMatch.interactable = true;
            RankTimePanel.SetActive(false);
            if (selectedMode == GameMode.SURVIVAL) {
                Button_SelectLeft.interactable = false;
            } else if (selectedMode == GameMode.RANKSURVIVAL) {
                RankTimePanel.SetActive(true);
                Button_SelectRight.interactable = false;
                if (NetworkManager.Instance.isInGroup) {
                    Button_FindMatch.interactable = false;
                }
            }
        }
        if (selectedMode == GameMode.SURVIVAL) {
            TMP_SelectedMode.text = LanguageSwitcher.L("MODE_SURVIVAL_NAME");
            TMP_ModeInformation1.text = LanguageSwitcher.L("T1_SURVIVAL");
            TMP_ModeInformation1.color = Color.yellow;
            TMP_ModeInformation2.text = LanguageSwitcher.L("T2_SURVIVAL");
            TMP_ModeInformation2.color = Color.green;
            TMP_ModeInformation3.text = LanguageSwitcher.L("T3_SURVIVAL");
            TMP_ModeInformation3.color = Color.white;
        } else if (selectedMode == GameMode.RESPAWN) {
            TMP_SelectedMode.text = LanguageSwitcher.L("MODE_RESPAWN_NAME");
            TMP_ModeInformation1.text = LanguageSwitcher.L("T1_RESPAWN");
            TMP_ModeInformation1.color = Color.yellow;
            TMP_ModeInformation2.text = LanguageSwitcher.L("T2_RESPAWN");
            TMP_ModeInformation2.color = Color.green;
            TMP_ModeInformation3.text = LanguageSwitcher.L("T3_RESPAWN");
        } else if (selectedMode == GameMode.RANKSURVIVAL) {
            TMP_SelectedMode.text = LanguageSwitcher.L("MODE_RANK_NAME");
            TMP_ModeInformation1.text = LanguageSwitcher.L("T1_RANK");
            TMP_ModeInformation1.color = Color.yellow;
            TMP_ModeInformation2.text = LanguageSwitcher.L("T2_RANK");
            TMP_ModeInformation2.color = Color.red;
            TMP_ModeInformation3.text = LanguageSwitcher.L("T3_RANK");
            TMP_ModeInformation3.color = Color.white;
        }

        //TMP_Population.text = $"동시 접속자 수: {population}";
        //TMP_Population.text = LanguageSwitcher.LF("UI/Population", population);

        pingText.text = $"KR Server: {NetworkManager.Instance.AvgRttMs:0} ms";
        if (NetworkManager.Instance.AvgRttMs <= 40) {
            pingText.color = Color.green;
        } else if (NetworkManager.Instance.AvgRttMs > 40 && NetworkManager.Instance.AvgRttMs <= 80) {
            pingText.color = Color.yellow;
        } else {
            pingText.color = Color.red;
        }

        // ESC 로 닫기
        if (_currentPopup != null && Input.GetKeyDown(KeyCode.Escape)) {
            CloseChatPopup();
        }

        // 마우스/터치로 팝업 바깥 클릭 시 닫기
        if (_currentPopup != null) {
            if (Input.GetMouseButtonDown(0)) {
                TryClosePopupByPointer(Input.mousePosition);
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
                TryClosePopupByPointer(Input.GetTouch(0).position);
            }
        }
    }

    public void OnButtonPopup() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Canvas_Popup.SetActive(true);
        Panel_Popup.SetActive(true);
        if (Panel_Setting.gameObject.activeSelf) {
            Panel_Setting.SetActive(false);
        }
    }

    public void OnButtonSetting() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        if (Panel_Popup.activeSelf) {
            Panel_Popup.SetActive(false);
        }
        Panel_Setting.SetActive(true);
    }

    public void OnButtonLogOut() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Application.Quit();
        //C_Logout pkt = new C_Logout();
        //NetworkManager.Send(pkt.Write());
    }

    public void OnButtonCancel() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        if (Canvas_Popup.gameObject.activeSelf) Canvas_Popup.SetActive(false);
    }

    public void OnButton_SettingReset() {
        settingsPanel?.ClickReset();
        //UIManager.ShowSuccess("설정이 초기화 되었습니다.");
        UIManager.ShowSuccessKey("UI/Set18");
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButton_SettingSave() {
        settingsPanel?.ClickSave();
        //UIManager.ShowSuccess("설정이 저장 되었습니다.");
        UIManager.ShowSuccessKey("UI/Set19");
        SoundManager.I?.Play2D(SfxId.MouseClick);
        RefreshLockerRoomHelp();
    }

    public void OnButton_SettingCancel() {
        settingsPanel?.ClickCancel();
        if (Canvas_Popup.gameObject.activeSelf) Canvas_Popup.SetActive(false);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Setting_Video() {
        if (selectedTab_Setting == Tab_Setting.Video) return;

        selectedTab_Setting = Tab_Setting.Video;

        Panel_Setting_Video.SetActive(true);
        Panel_Setting_Sound.SetActive(false);
        Panel_Setting_Key.SetActive(false);
        Panel_Setting_Social.SetActive(false);
        Panel_Setting_Account.SetActive(false);

        Button_Setting_Video.GetComponent<TabButtonFX>().SetActive(true);
        Button_Setting_Sound.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Key.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Social.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Account.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Setting_Sound() {
        if (selectedTab_Setting == Tab_Setting.Sound) return;

        selectedTab_Setting = Tab_Setting.Sound;

        Panel_Setting_Video.SetActive(false);
        Panel_Setting_Sound.SetActive(true);
        Panel_Setting_Key.SetActive(false);
        Panel_Setting_Social.SetActive(false);
        Panel_Setting_Account.SetActive(false);

        Button_Setting_Video.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Sound.GetComponent<TabButtonFX>().SetActive(true);
        Button_Setting_Key.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Social.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Account.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Setting_Key() {
        if (selectedTab_Setting == Tab_Setting.Key) return;

        selectedTab_Setting = Tab_Setting.Key;

        Panel_Setting_Video.SetActive(false);
        Panel_Setting_Sound.SetActive(false);
        Panel_Setting_Key.SetActive(true);
        Panel_Setting_Social.SetActive(false);
        Panel_Setting_Account.SetActive(false);

        Button_Setting_Video.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Sound.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Key.GetComponent<TabButtonFX>().SetActive(true);
        Button_Setting_Social.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Account.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Setting_Social() {
        if (selectedTab_Setting == Tab_Setting.Social) return;

        selectedTab_Setting = Tab_Setting.Social;

        Panel_Setting_Video.SetActive(false);
        Panel_Setting_Sound.SetActive(false);
        Panel_Setting_Key.SetActive(false);
        Panel_Setting_Social.SetActive(true);
        Panel_Setting_Account.SetActive(false);

        Button_Setting_Video.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Sound.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Key.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Social.GetComponent<TabButtonFX>().SetActive(true);
        Button_Setting_Account.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Setting_Account() {
        if (selectedTab_Setting == Tab_Setting.Account) return;

        selectedTab_Setting = Tab_Setting.Account;

        Panel_Setting_Video.SetActive(false);
        Panel_Setting_Sound.SetActive(false);
        Panel_Setting_Key.SetActive(false);
        Panel_Setting_Social.SetActive(false);
        Panel_Setting_Account.SetActive(true);

        Button_Setting_Video.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Sound.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Key.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Social.GetComponent<TabButtonFX>().SetActive(false);
        Button_Setting_Account.GetComponent<TabButtonFX>().SetActive(true);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }


    public void OnSelectTab_Play() {
        if (selectedTab == Tab.Play) return;

        selectedTab = Tab.Play;
        if (!Canvas_Play.activeSelf) Canvas_Play.SetActive(true);
        if (Canvas_LockerRoom.activeSelf) Canvas_LockerRoom.SetActive(false);
        if (Canvas_Ranking.activeSelf) Canvas_Ranking.SetActive(false);
        if (Canvas_History.activeSelf) Canvas_History.SetActive(false);
        if (Canvas_Shop.activeSelf) Canvas_Shop.SetActive(false);

        Button_Tab_Play.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_LockerRoom.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Ranking.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_History.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_LockerRoom() {
        if (selectedTab == Tab.LockerRoom) return;

        selectedTab = Tab.LockerRoom;
        if (Canvas_Play.activeSelf) Canvas_Play.SetActive(false);
        if (!Canvas_LockerRoom.activeSelf) Canvas_LockerRoom.SetActive(true);
        if (Canvas_Ranking.activeSelf) Canvas_Ranking.SetActive(false);
        if (Canvas_History.activeSelf) Canvas_History.SetActive(false);
        if (Canvas_Shop.activeSelf) Canvas_Shop.SetActive(false);

        Button_Tab_Play.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_Ranking.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_History.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
        RefreshLockerRoomHelp();
    }

    public void OnSelectTab_Ranking() {
        if (selectedTab == Tab.Ranking) return;

        selectedTab = Tab.Ranking;
        if (Canvas_Play.activeSelf) Canvas_Play.SetActive(false);
        if (Canvas_LockerRoom.activeSelf) Canvas_LockerRoom.SetActive(false);
        if (!Canvas_Ranking.activeSelf) Canvas_Ranking.SetActive(true);
        if (Canvas_History.activeSelf) Canvas_History.SetActive(false);
        if (Canvas_Shop.activeSelf) Canvas_Shop.SetActive(false);

        Button_Tab_Play.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Ranking.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_History.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);

        C_RequestLeaderboard pkt = new C_RequestLeaderboard();
        pkt.offset = 0;
        pkt.limit = 20;
        NetworkManager.Send(pkt.Write());
    }

    public void OnSelectTab_History() {
        if (selectedTab == Tab.History) return;

        selectedTab = Tab.History;
        if (Canvas_Play.activeSelf) Canvas_Play.SetActive(false);
        if (Canvas_LockerRoom.activeSelf) Canvas_LockerRoom.SetActive(false);
        if (Canvas_Ranking.activeSelf) Canvas_Ranking.SetActive(false);
        if (!Canvas_History.activeSelf) Canvas_History.SetActive(true);
        if (Canvas_Shop.activeSelf) Canvas_Shop.SetActive(false);

        Button_Tab_Play.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Ranking.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_History.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_Shop.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);

        C_RequestRecentGames pkt = new C_RequestRecentGames();
        pkt.nickName = NetworkManager.Instance.nickName;
        NetworkManager.Send(pkt.Write());
    }

    public void OnSelectTab_Shop() {
        if (selectedTab == Tab.Shop) return;

        selectedTab = Tab.Shop;
        if (Canvas_Play.activeSelf) Canvas_Play.SetActive(false);
        if (Canvas_LockerRoom.activeSelf) Canvas_LockerRoom.SetActive(false);
        if (Canvas_Ranking.activeSelf) Canvas_Ranking.SetActive(false);
        if (Canvas_History.activeSelf) Canvas_History.SetActive(false);
        if (!Canvas_Shop.activeSelf) Canvas_Shop.SetActive(true);

        Button_Tab_Play.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Ranking.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_History.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop.GetComponent<TabButtonFX>().SetActive(true);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_LockerRoom_Character() {
        if (selectedTab_LockerRoom == Tab_LockerRoom.Character) return;

        selectedTab_LockerRoom = Tab_LockerRoom.Character;
        Panel_LockerRoom_Character.SetActive(true);
        Panel_LockerRoom_Trail.SetActive(false);
        Panel_LockerRoom_Dance.SetActive(false);
        Panel_LockerRoom_DanceSlot.SetActive(false);

        Button_Tab_LockerRoom_Character.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_LockerRoom_Trail.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom_Dance.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
        RefreshLockerRoomHelp();
    }

    public void OnSelectTab_LockerRoom_Trail() {
        if (selectedTab_LockerRoom == Tab_LockerRoom.Trail) return;

        selectedTab_LockerRoom = Tab_LockerRoom.Trail;
        Panel_LockerRoom_Character.SetActive(false);
        Panel_LockerRoom_Trail.SetActive(true);
        Panel_LockerRoom_Dance.SetActive(false);
        Panel_LockerRoom_DanceSlot.SetActive(false);

        Button_Tab_LockerRoom_Character.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom_Trail.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_LockerRoom_Dance.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
        RefreshLockerRoomHelp();
    }

    public void OnSelectTab_LockerRoom_Dance() {
        if (selectedTab_LockerRoom == Tab_LockerRoom.Dance) return;

        selectedTab_LockerRoom = Tab_LockerRoom.Dance;
        Panel_LockerRoom_Character.SetActive(false);
        Panel_LockerRoom_Trail.SetActive(false);
        Panel_LockerRoom_Dance.SetActive(true);
        Panel_LockerRoom_DanceSlot.SetActive(true);

        Button_Tab_LockerRoom_Character.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom_Trail.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_LockerRoom_Dance.GetComponent<TabButtonFX>().SetActive(true);

        SoundManager.I?.Play2D(SfxId.MouseClick);
        RefreshLockerRoomHelp();
    }

    public void OnSelectTab_Shop_Character() {
        if (selectedTab_Shop == Tab_Shop.Character) return;

        selectedTab_Shop = Tab_Shop.Character;
        Panel_Shop_Character.SetActive(true);
        Panel_Shop_Trail.SetActive(false);
        Panel_Shop_Dance.SetActive(false);
        Panel_Shop_Etc.SetActive(false);

        Button_Tab_Shop_Character.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_Shop_Trail.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Dance.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Etc.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Shop_Trail() {
        if (selectedTab_Shop == Tab_Shop.Trail) return;

        selectedTab_Shop = Tab_Shop.Trail;
        Panel_Shop_Character.SetActive(false);
        Panel_Shop_Trail.SetActive(true);
        Panel_Shop_Dance.SetActive(false);
        Panel_Shop_Etc.SetActive(false);

        Button_Tab_Shop_Character.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Trail.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_Shop_Dance.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Etc.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Shop_Dance() {
        if (selectedTab_Shop == Tab_Shop.Dance) return;

        selectedTab_Shop = Tab_Shop.Dance;
        Panel_Shop_Character.SetActive(false);
        Panel_Shop_Trail.SetActive(false);
        Panel_Shop_Dance.SetActive(true);
        Panel_Shop_Etc.SetActive(false);

        Button_Tab_Shop_Character.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Trail.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Dance.GetComponent<TabButtonFX>().SetActive(true);
        Button_Tab_Shop_Etc.GetComponent<TabButtonFX>().SetActive(false);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnSelectTab_Shop_Etc() {
        if (selectedTab_Shop == Tab_Shop.Etc) return;

        selectedTab_Shop = Tab_Shop.Etc;
        Panel_Shop_Character.SetActive(false);
        Panel_Shop_Trail.SetActive(false);
        Panel_Shop_Dance.SetActive(false);
        Panel_Shop_Etc.SetActive(true);

        Button_Tab_Shop_Character.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Trail.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Dance.GetComponent<TabButtonFX>().SetActive(false);
        Button_Tab_Shop_Etc.GetComponent<TabButtonFX>().SetActive(true);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonTest() {
        string testOfferId = "SHOP_HAT_001_GOLD";
        // string testOfferId = "BUNDLE_STARTER";

        var pkt = new C_BuyOffer {
            offerId = testOfferId,
            idempotencyKey = Guid.NewGuid().ToString("N") // 매번 유니크
        };

        NetworkManager.Send(pkt.Write());
        Debug.Log($"[TEST] Sent C_BuyOffer  offerId={pkt.offerId}, key={pkt.idempotencyKey}");
    }

    public void OnButtonSelectLeft() {
        if (selectedMode > GameMode.SURVIVAL) {
            selectedMode--;
        }
        isButtonClicked = true;
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonSelectRight() {
        if (selectedMode < GameMode.RANKSURVIVAL) {
            selectedMode++;
        }
        isButtonClicked = true;
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonFindMatch() {
        if (Input.GetKeyDown(KeyCode.Space) ||
        Input.GetKeyDown(KeyCode.Return) ||
        Input.GetKeyDown(KeyCode.KeypadEnter)) {
            return;
        }

        if (!isFindingMatch) {
            if (GameMode.SURVIVAL <= selectedMode && selectedMode <= GameMode.RANKSURVIVAL) {
                C_TryFindMatch pkt = new C_TryFindMatch();
                pkt.gameMode = (int)selectedMode;
                NetworkManager.Send(pkt.Write());
                isFindingMatch = true;
                StartCoroutine(addFindingTime());
            }
        } else {
            C_CancelFindMatch pkt = new C_CancelFindMatch();
            NetworkManager.Send(pkt.Write());
            StartCoroutine(cancelFindingMatch());
        }
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    IEnumerator addFindingTime() {
        findingTime = 0f;
        PlayMM(true); // 상단에서 "내려오기"
        //Panel_MatchMaking.SetActive(true);
        Button_SelectLeft.interactable = false;
        Button_SelectRight.interactable = false;

        SoundManager.I?.Play2D(SfxId.MatchFindStart);


        var spinner = spinnerObj.GetComponent<UISpinner>();
        spinner.Play();
        while (isFindingMatch) {
            findingTime += Time.deltaTime;
            int total = Mathf.FloorToInt(findingTime);
            int mm = total / 60;
            int ss = total % 60;

            //TMP_MatchMaking.text = $"게임을 찾는 중\n{mm:00}:{ss:00}";
            TMP_MatchMaking.text = LanguageSwitcher.LF("MATCH_FINDING", mm, ss);
            yield return null;
        }
    }

    IEnumerator cancelFindingMatch() {
        isFindingMatch = false;
        //TMP_MatchMaking.text = $"매칭 찾기 취소";
        TMP_MatchMaking.text = LanguageSwitcher.L("MATCH_CANCEL");
        var spinner = spinnerObj.GetComponent<UISpinner>();
        spinner.Stop();
        float t = 0f;
        isButtonClicked = true;
        while (t < 3f || isFindingMatch) {
            t += Time.deltaTime;
            yield return null;
        }
        PlayMM(false); // 위로 "올라가며" 사라지기
        //Panel_MatchMaking.SetActive(false);
    }

    public void SpawnEquippedCharacterPreview() {
        string sku = null;
        string trail_sku = null;
        if (!ShopCache.EquippedBySlot.TryGetValue("character", out sku) || string.IsNullOrEmpty(sku))
            sku = "CHAR_BASIC_01"; // 안전 기본값

        if (!ShopCache.EquippedBySlot.TryGetValue("trail", out trail_sku) || string.IsNullOrEmpty(trail_sku))
            trail_sku = "TRAIL_BASIC_01"; // 안전 기본값

        if (_previewSku == sku &&  _previewTrailSku == trail_sku && _previewGO != null) return; // 바뀐게 없으면 무시
        _previewTrailSku = trail_sku;
        _previewSku = sku;

        if (_previewGO) Destroy(_previewGO);

        var prefab = Resources.Load<GameObject>(CharacterPrefabPathFor(sku));
        if (prefab == null) {
            Debug.LogWarning($"[LobbyPreview] prefab missing for {sku}, fallback to basic.");
            prefab = Resources.Load<GameObject>(CharacterPrefabPathFor("CHAR_BASIC_01"));
            if (prefab == null) return; // 기본도 없으면 패스
        }

        _previewGO = Instantiate(prefab, PreviewAnchor ? PreviewAnchor : transform);
        //_previewGO.transform.localPosition = Vector3.zero;
        _previewGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        //_previewGO.transform.localScale = Vector3.one;
        var lp = _previewGO.GetComponent<LobbyPlayer>();
        if (!lp) lp = _previewGO.AddComponent<LobbyPlayer>();

        PlayerManager.ApplyTrailCosmetic(_previewGO, trail_sku);
    }

    private string CharacterPrefabPathFor(string sku) {
        switch (sku) {
            case "CHAR_CASUAL_01": return "Characters/CHAR_CASUAL_01";
            case "CHAR_PENGUIN_01": return "Characters/CHAR_PENGUIN_01";
            case "CHAR_RABBIT_01": return "Characters/CHAR_RABBIT_01";
            case "CHAR_BEAR_01": return "Characters/CHAR_BEAR_01";
            case "CHAR_BOXMAN_01": return "Characters/CHAR_BOXMAN_01";
            case "CHAR_ROBOT_01": return "Characters/CHAR_ROBOT_01";
            case "CHAR_HUE_01": return "Characters/CHAR_HUE_01";
            case "CHAR_MARIE_01": return "Characters/CHAR_MARIE_01";
            case "CHAR_ALEX_01": return "Characters/CHAR_ALEX_01";
            case "CHAR_JACKIE_01": return "Characters/CHAR_JACKIE_01";
            case "CHAR_KIMI_01": return "Characters/CHAR_KIMI_01";
            case "CHAR_SORA_01": return "Characters/CHAR_SORA_01";
            case "CHAR_MONROE_01": return "Characters/CHAR_MONROE_01";
            case "CHAR_JUDE_01": return "Characters/CHAR_JUDE_01";
            case "CHAR_VOVOVO_01": return "Characters/CHAR_VOVOVO_01";
            case "CHAR_BASIC_01":
            default: return "Characters/CHAR_BASIC_01";
        }
    }


    void SetupMatchPanel() {
        if (!Panel_MatchMaking) return;

        _mmRect = Panel_MatchMaking.GetComponent<RectTransform>();
        if (!_mmRect) _mmRect = Panel_MatchMaking.AddComponent<RectTransform>();

        _mmGroup = Panel_MatchMaking.GetComponent<CanvasGroup>();
        if (!_mmGroup) _mmGroup = Panel_MatchMaking.AddComponent<CanvasGroup>();

        // 상단 중앙 앵커/피벗
        _mmRect.anchorMin = new Vector2(0.5f, 1f);
        _mmRect.anchorMax = new Vector2(0.5f, 1f);
        _mmRect.pivot = new Vector2(0.5f, 1f);

        // 시작은 화면 밖 위쪽
        _mmRect.anchoredPosition = new Vector2(mmHorizontalOffset, OffscreenTopY());
        _mmRect.localScale = Vector3.one;
        _mmGroup.alpha = 0f;
    }

    float OffscreenTopY() {
        var parent = _mmRect ? _mmRect.parent as RectTransform : null;
        float h = parent ? parent.rect.height : Screen.height;
        return h + 200f; // 안전 여유치
    }

    float VisibleTopY() {
        return -mmTopMargin; // 상단에서 아래로 여백
    }

    IEnumerator CoSlide(bool show) {
        if (!_mmRect || !_mmGroup) yield break;
        if (show) Panel_MatchMaking.SetActive(true);

        // 시작/목표값
        float y0 = show ? OffscreenTopY() : VisibleTopY();
        float y1 = show ? VisibleTopY() : OffscreenTopY();
        float a0 = show ? 0f : 1f;
        float a1 = show ? 1f : 0f;
        float dur = show ? mmShowDuration : mmHideDuration;

        float t = 0f;
        _mmRect.anchoredPosition = new Vector2(mmHorizontalOffset, y0);
        _mmGroup.alpha = a0;

        if (dur <= 0f) {
            _mmRect.anchoredPosition = new Vector2(mmHorizontalOffset, y1);
            _mmGroup.alpha = a1;
        } else {
            while (t < dur) {
                t += mmUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                // 약간의 부드러운 이징
                float e = show ? EaseOutCubic(k) : EaseInCubic(k);
                float y = Mathf.LerpUnclamped(y0, y1, e);
                float a = Mathf.LerpUnclamped(a0, a1, e);
                _mmRect.anchoredPosition = new Vector2(mmHorizontalOffset, y);
                _mmGroup.alpha = a;
                yield return null;
            }
            _mmRect.anchoredPosition = new Vector2(mmHorizontalOffset, y1);
            _mmGroup.alpha = a1;
        }

        if (!show) Panel_MatchMaking.SetActive(false);
        _mmAnimCo = null;
    }

    void PlayMM(bool show) {
        if (_mmAnimCo != null) StopCoroutine(_mmAnimCo);
        _mmAnimCo = StartCoroutine(CoSlide(show));
    }

    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    static float EaseInCubic(float x) => x * x * x;

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
            if (selectedChatMode == Tab_Chat.Group && !NetworkManager.Instance.isInGroup) {
                UnityEngine.Object prefab = Resources.Load("ChatPrefab");
                Transform parent = GameObject.Find("ChatContent").transform;
                GameObject go = UnityEngine.Object.Instantiate(prefab, parent) as GameObject;

                var txt = go.GetComponent<TextMeshProUGUI>();
                txt.color = Color.yellow;
                //txt.text = $"참여한 그룹이 없습니다.";
                txt.text = LanguageSwitcher.L("UI/Group19");
                TagChatGO(go, ChatType.System);

            } else {
                SoundManager.I?.Play2D(SfxId.Chat);
                C_LobbyChat chatPacket = new C_LobbyChat();
                chatPacket.message = inputChat;
                chatPacket.type = (int)selectedChatMode;
                NetworkManager.Send(chatPacket.Write());
            }
        }
        
        InputField_Chat.text = null;
        inputChat = null;
        InputField_Chat.DeactivateInputField();
        EventSystem.current?.SetSelectedGameObject(null);
        ScrollRect_Chat.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.0f);
        Chat_Filter_Panel.gameObject.SetActive(false);
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

    public static void ShowChatPopupNearby(RectTransform chatRT, string nickName) {
        var panelGO = GameObject.Find("Panel_Chat");
        if (panelGO == null) { Debug.LogWarning("Panel_Chat 없음"); return; }
        var panel = panelGO.GetComponent<RectTransform>();

        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogWarning("Canvas 없음"); return; }
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        DestroyExistingPopupsUnder(panel);
        if (_currentPopup != null) { Destroy(_currentPopup); _currentPopup = null; }

        var popupPrefab = Resources.Load<GameObject>(PopupRootName);
        if (popupPrefab == null) { Debug.LogWarning("Resources/ChatPopupPrefab 없음"); return; }
        var popup = Instantiate(popupPrefab, panel);
        popup.name = PopupRootName; // (Clone) 제거해서 깔끔하게
        _currentPopup = popup;

        WirePopupButtons(popup, nickName);

        var popupRT = popup.GetComponent<RectTransform>();
        popupRT.anchorMin = popupRT.anchorMax = new Vector2(0f, 1f);
        popupRT.pivot = new Vector2(0f, 1f);

        Vector3[] corners = new Vector3[4];
        chatRT.GetWorldCorners(corners); // 0:좌하,1:좌상,2:우상,3:우하
        Vector3 worldAnchor = corners[3];
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, worldAnchor);
        //screenPt += new Vector2(1f, -1f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screenPt, cam, out var localPt);
        popupRT.anchoredPosition = localPt;

        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRT);
        //ClampToPanel(popupRT, panel, 0.1f);

        ApplyNickNameToPopup(popup, nickName);
    }

    // Panel_Chat 하위에 있는 기존 팝업들(이름 기준) 전부 제거
    public static void DestroyExistingPopupsUnder(RectTransform panel) {
        // 이름이 ChatPopupPrefab 또는 ChatPopupPrefab(Clone) 인 루트 오브젝트만 정리
        for (int i = panel.childCount - 1; i >= 0; --i) {
            var child = panel.GetChild(i).gameObject;
            if (child.name == PopupRootName || child.name == PopupRootName + "(Clone)") {
                Destroy(child);
            }
        }
    }

    public static void ApplyNickNameToPopup(GameObject popup, string nick) {
        // 깊숙이 있어도 찾도록 안전 탐색
        var tmps = popup.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps) {
            if (t.gameObject.name == "nickName") {
                t.text = nick ?? string.Empty;
                return;
            }
        }
        // 못 찾았으면 로그
        Debug.LogWarning("ChatPopupPrefab 내부에 이름이 'nickName' 인 TextMeshProUGUI 를 찾지 못했습니다.");
    }

    // 패널 경계 내로 위치 고정
    public static void ClampToPanel(RectTransform child, RectTransform panel, float padding) {
        float panelW = panel.rect.width;
        float panelH = panel.rect.height;
        var size = child.rect.size;

        var pos = child.anchoredPosition; // 좌상단 기준
        float minX = padding;
        float maxX = panelW - size.x - padding;
        float maxY = -padding;
        float minY = -(panelH - size.y - padding);

        pos.x = Mathf.Clamp(pos.x, minX, Mathf.Max(minX, maxX));
        pos.y = Mathf.Clamp(pos.y, Mathf.Min(maxY, 0f), Mathf.Max(minY, -padding));
        child.anchoredPosition = pos;
    }

    public static void CloseChatPopup() {
        if (_currentPopup != null) {
            Destroy(_currentPopup);
            _currentPopup = null;
        }
    }

    static void TryClosePopupByPointer(Vector2 screenPos) {
        if (_currentPopup == null) return;

        var popupRT = _currentPopup.GetComponent<RectTransform>();
        var canvas = popupRT.GetComponentInParent<Canvas>();
        var cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(popupRT, screenPos, cam);
        if (!inside) {
            CloseChatPopup();
        }
    }

    static void WirePopupButtons(GameObject popup, string nickName) {
        _currentPopupNick = nickName;

        Button b0 = DirectChildButton(popup, 1);
        //Button b1 = DirectChildButton(popup, 2);
        Button b2 = DirectChildButton(popup, 2);

        var lm = FindAnyObjectByType<LobbyManager>();

        if (b0 != null) {
            b0.onClick.RemoveAllListeners();
            b0.onClick.AddListener(() => lm?.OnButtonShowInformation());
        }
        //if (b1 != null) {
        //    b1.onClick.RemoveAllListeners();
        //    b1.onClick.AddListener(() => lm?.OnButtonRequestFriend());
        //}
        if (b2 != null) {
            b2.onClick.RemoveAllListeners();
            b2.onClick.AddListener(() => lm?.OnButtonInviteGroup());
        }
    }

    static Button DirectChildButton(GameObject root, int childIndex) {
        if (root == null) return null;
        var t = root.transform;
        if (childIndex < 0 || childIndex >= t.childCount) return null;
        return t.GetChild(childIndex).GetComponent<Button>();
    }

    public void OnButtonShowInformation() {
        CloseChatPopup();
        SoundManager.I?.Play2D(SfxId.MouseClick);
        C_RequestUserInformation pkt = new C_RequestUserInformation();
        pkt.nickName = _currentPopupNick;
        NetworkManager.Send(pkt.Write());
    }

    public void OnButtonMyInformation() {
        _currentPopupNick = NetworkManager.Instance.nickName;
        OnButtonShowInformation();
    }

    public static void OnShowUserInformation(S_UserInformation pkt) {
        var lm = FindAnyObjectByType<LobbyManager>();

        if (!pkt.isSuccess) {
            if (lm != null && lm.Canvas_PopupUser != null)
                lm.Canvas_PopupUser.SetActive(false);

            switch (pkt.failReason) {
                case 1:
                    UIManager.ShowErrorKey("CANNOTFIND");
                    break;
                case 2:
                default:
                    UIManager.ShowErrorKey("CANNOTFIND");
                    Debug.LogWarning("[UserInfo] 유저 정보 조회 중 오류가 발생했습니다.");
                    break;
            }

            return;
        }


        if (lm != null && lm.Canvas_PopupUser != null)
            lm.Canvas_PopupUser.SetActive(true);

        if (pkt.players.Count <= 0) {
            UIManager.ShowErrorKey("CANNOTFIND");
            Debug.LogWarning("[UserInfo] players 리스트가 비어 있습니다.");
            return;
        }

        // 초대 때문에 넣어둠
        inputFindUser = pkt.players[0].nickName;
        lm.Panel_FindUser.SetActive(false);

        TextMeshProUGUI u_name = GameObject.Find("u_name").GetComponent<TextMeshProUGUI>();
        u_name.text = pkt.players[0].nickName;

        TextMeshProUGUI u_level = GameObject.Find("u_level").GetComponent<TextMeshProUGUI>();
        u_level.text = $"Lv.{pkt.players[0].level}";

        TextMeshProUGUI u_tear = GameObject.Find("u_tear").GetComponent<TextMeshProUGUI>();
        string ranktxt = "오류";
        if (pkt.players[0].tearRank == 0) {
            //ranktxt = "언랭크";
            ranktxt = LanguageSwitcher.L("Tier1");
            u_name.color = Color.white;
        } else if (pkt.players[0].tearRank == 1) {
            //ranktxt = "브론즈";
            ranktxt = LanguageSwitcher.L("Tier2");
            u_name.color = Color.white;
        } else if (pkt.players[0].tearRank == 2) {
            //ranktxt = "실버";
            ranktxt = LanguageSwitcher.L("Tier3");
            u_name.color = Color.black;
        } else if (pkt.players[0].tearRank == 3) {
            //ranktxt = "골드";
            ranktxt = LanguageSwitcher.L("Tier4");
            u_name.color = Color.black;
        } else if (pkt.players[0].tearRank == 4) {
            //ranktxt = "플래티넘";
            ranktxt = LanguageSwitcher.L("Tier5");
            u_name.color = Color.black;
        } else if (pkt.players[0].tearRank == 5) {
            //ranktxt = "다이아몬드";
            ranktxt = LanguageSwitcher.L("Tier6");
            u_name.color = Color.black;
        } else if (pkt.players[0].tearRank == 6) {
            //ranktxt = "신";
            ranktxt = LanguageSwitcher.L("Tier7");
            u_name.color = Color.black;
        }

        
        //u_tear.text = $"티어: {ranktxt} ({pkt.players[0].tearRankScore}점)";
        u_tear.text = LanguageSwitcher.LF("Card1", ranktxt, pkt.players[0].tearRankScore);

        TextMeshProUGUI u_games = GameObject.Find("u_games").GetComponent<TextMeshProUGUI>();
        //u_games.text = $"경쟁 경기: {pkt.players[0].totalGames}";
        u_games.text = LanguageSwitcher.LF("Card2", pkt.players[0].totalGames);

        TextMeshProUGUI u_wins = GameObject.Find("u_wins").GetComponent<TextMeshProUGUI>();
        //u_wins.text = $"경쟁 승리: {pkt.players[0].totalWins} ({pkt.players[0].WinPercentage.ToString("F1")}%)";
        u_wins.text = LanguageSwitcher.LF("Card3", pkt.players[0].totalWins, pkt.players[0].WinPercentage.ToString("F1"));

        TextMeshProUGUI u_avgRate = GameObject.Find("u_avgRate").GetComponent<TextMeshProUGUI>();
        //u_avgRate.text = $"경쟁 평균 순위: {pkt.players[0].avgRank.ToString("F1")}";
        u_avgRate.text = LanguageSwitcher.LF("Card4", pkt.players[0].avgRank.ToString("F1"));

        TextMeshProUGUI u_avgKill = GameObject.Find("u_avgKill").GetComponent<TextMeshProUGUI>();
        //u_avgKill.text = $"경쟁 평균 처치: {pkt.players[0].avgKill.ToString("F1")}";
        u_avgKill.text = LanguageSwitcher.LF("Card5", pkt.players[0].avgKill.ToString("F1"));

        var onlineObj = GameObject.Find("u_online");
        if (onlineObj != null) {
            var u_online = onlineObj.GetComponent<TextMeshProUGUI>();
            if (u_online != null) {
                // 다국어 키는 알아서 맞추면 됨
                string statusText = pkt.isOnline
                    ? LanguageSwitcher.L("UserOnline")
                    : LanguageSwitcher.L("UserOffline");

                u_online.text = statusText;
                u_online.color = pkt.isOnline
                    ? new Color(0.3f, 1f, 0.3f)
                    : new Color(0.7f, 0.7f, 0.7f);

                Button invitebObj = GameObject.Find("groupinvitebutton").GetComponent<Button>();
                if (pkt.isOnline) {
                    if (invitebObj != null) {
                        invitebObj.interactable = true;
                    }
                } else {
                    if (invitebObj != null) {
                        invitebObj.interactable = false;
                    }
                }
                if (pkt.players[0].nickName == NetworkManager.Instance.nickName) {
                    if (invitebObj != null) {
                        invitebObj.interactable = false;
                    }
                }
            }
        }

        GameObject Panel_uName = GameObject.Find("Panel_uName");

        int rank = Mathf.Clamp(pkt.players[0].tearRank, 0, 6);
        string matName = RankMatNames[rank];

        Material mat = Resources.Load<Material>($"UI/Material/NameRank/{matName}");
        if (mat == null) {
            Debug.LogWarning($"[PanelInfoMaterialSetter] Resources에서 '{matName}' 머터리얼을 찾지 못함");
            return;
        }

        if (Panel_uName.TryGetComponent(out Image img)) {
            img.material = new Material(mat);
            return;
        }
        if (Panel_uName.TryGetComponent(out RawImage raw)) {
            raw.material = new Material(mat);
            return;
        }
        if (Panel_uName.TryGetComponent(out Renderer rend)) {
            rend.material = mat;
            return;
        }

        
    }

    public void OnButtonCloseUserInfor() {
        Canvas_PopupUser.SetActive(false);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonRequestFriend() {
        CloseChatPopup();
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonInviteGroup() {
        CloseChatPopup();
        C_InviteGroup pkt = new C_InviteGroup();
        pkt.LeaderNickName = NetworkManager.Instance.nickName;
        pkt.ServentNickName = _currentPopupNick;
        NetworkManager.Send(pkt.Write());
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonOKInvite() {
        // TODO: 수락 로직
        C_ReplyInviteGroup pkt = new C_ReplyInviteGroup();
        pkt.isAccept = true;
        pkt.InviterNickName = _currentInviterNick;
        pkt.replierNickName = NetworkManager.Instance.nickName;
        NetworkManager.Send(pkt.Write());
        PlayInvite(false);
        StopInviteTimer();
        _currentInviterNick = "";
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonRejectInvite() {
        // TODO: 거절 로직
        C_ReplyInviteGroup pkt = new C_ReplyInviteGroup();
        pkt.isAccept = false;
        pkt.InviterNickName = _currentInviterNick;
        pkt.replierNickName = NetworkManager.Instance.nickName;
        NetworkManager.Send(pkt.Write());
        PlayInvite(false);
        StopInviteTimer();
        _currentInviterNick = "";
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public static void ShowInvitePanel(string inviterName) {
        var lm = FindAnyObjectByType<LobbyManager>();
        if (!lm || !lm.Panel_Invite) return;

        var txt = lm.Panel_Invite.GetComponentInChildren<TextMeshProUGUI>();
        //if (txt) txt.text = $"{inviterName}님이\n그룹에 초대 했습니다.";
        if (txt) txt.text = LanguageSwitcher.LF("UI/Group4", inviterName);
        _currentInviterNick = inviterName;

        if (lm._invRect == null || lm._invGroup == null) lm.SetupInvitePanel();
        lm._invRect.anchoredPosition = new Vector2(lm.OffscreenRightX(), 0f);
        lm._invGroup.alpha = 0f;
        lm.PlayInvite(true);

        lm.StartInviteTimer();

        SoundManager.I?.Play2D(SfxId.Invite);
    }

    void SetupInvitePanel() {
        if (!Panel_Invite) return;

        _invRect = Panel_Invite.GetComponent<RectTransform>();
        if (!_invRect) _invRect = Panel_Invite.AddComponent<RectTransform>();

        _invGroup = Panel_Invite.GetComponent<CanvasGroup>();
        if (!_invGroup) _invGroup = Panel_Invite.AddComponent<CanvasGroup>();

        _invRect.anchorMin = new Vector2(1f, 0.5f);
        _invRect.anchorMax = new Vector2(1f, 0.5f);
        _invRect.pivot = new Vector2(1f, 0.5f);

        _invRect.anchoredPosition = new Vector2(OffscreenRightX(), 0f);
        _invGroup.alpha = 0f;
    }

    float OffscreenRightX() {
        float w = _invRect ? _invRect.rect.width : 600f;
        return w + 200f; // 안전 여유치
    }

    float VisibleRightX() {
        return -inviteRightMargin;
    }

    IEnumerator CoSlideInvite(bool show) {
        if (!_invRect || !_invGroup) yield break;
        if (show) Panel_Invite.SetActive(true);

        float x0 = show ? OffscreenRightX() : VisibleRightX();
        float x1 = show ? VisibleRightX() : OffscreenRightX();
        float a0 = show ? 0f : 1f;
        float a1 = show ? 1f : 0f;
        float dur = show ? inviteShowDuration : inviteHideDuration;

        float t = 0f;
        _invRect.anchoredPosition = new Vector2(x0, 0f);
        _invGroup.alpha = a0;

        if (dur <= 0f) {
            _invRect.anchoredPosition = new Vector2(x1, 0f);
            _invGroup.alpha = a1;
        } else {
            while (t < dur) {
                t += inviteUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = show ? EaseOutCubic(k) : EaseInCubic(k);
                float x = Mathf.LerpUnclamped(x0, x1, e);
                float a = Mathf.LerpUnclamped(a0, a1, e);
                _invRect.anchoredPosition = new Vector2(x, 0f);
                _invGroup.alpha = a;
                yield return null;
            }
            _invRect.anchoredPosition = new Vector2(x1, 0f);
            _invGroup.alpha = a1;
        }

        if (!show) Panel_Invite.SetActive(false);
        _invAnimCo = null;
    }

    void PlayInvite(bool show) {
        if (_invAnimCo != null) StopCoroutine(_invAnimCo);
        _invAnimCo = StartCoroutine(CoSlideInvite(show));
    }

    public static void ShowInviteResult(S_InviteGroupResult pkt) {
        UnityEngine.Object prefab = Resources.Load("ChatPrefab");
        Transform parent = GameObject.Find("ChatContent").transform;
        GameObject go = UnityEngine.Object.Instantiate(prefab, parent) as GameObject;

        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.color = Color.yellow;

        if (!pkt.isAvailable) {
            if (pkt.failReason == 1) {
                //txt.text = $"그룹의 인원이 최대 상태 입니다.";
                txt.text = LanguageSwitcher.L("UI/Group5");
            } else if (pkt.failReason == 2) {
                //txt.text = $"{pkt.replierNickName}님은 게임 중입니다.";
                txt.text = LanguageSwitcher.LF("UI/Group6", pkt.replierNickName);
            } else if (pkt.failReason == 3) {
                //txt.text = $"{pkt.replierNickName}님은 이미 그룹이 있습니다.";
                txt.text = LanguageSwitcher.LF("UI/Group7", pkt.replierNickName);
            } else if (pkt.failReason == 4) {
                //txt.text = $"{pkt.replierNickName}님을 찾을 수 없습니다.";
                txt.text = LanguageSwitcher.LF("UI/Group8", pkt.replierNickName);
            } else if (pkt.failReason == 5) {
                // 초대 시간 만료
                txt.text = LanguageSwitcher.LF("UI/GroupInviteTimeout", pkt.replierNickName);
            } else if (pkt.failReason == 6) {
                // 이미 초대를 받고 있음
                txt.text = LanguageSwitcher.LF("UI/GroupInvitePending", pkt.replierNickName);
            } else {
                //txt.text = $"{pkt.replierNickName}님은 초대 받을 수 없는 상태입니다.";
                txt.text = LanguageSwitcher.LF("UI/Group9", pkt.replierNickName);
            }
        } else {
            //txt.text = $"{pkt.replierNickName}님이 그룹 초대를 ";
            txt.text = LanguageSwitcher.LF("UI/Group10", pkt.replierNickName);
            if (pkt.isAccepted) {
                //txt.text += "수락 했습니다.";
                txt.text += LanguageSwitcher.L("UI/Group11");
            } else {
                //txt.text += "거절 했습니다.";
                txt.text += LanguageSwitcher.L("UI/Group12");
            }
        }
        TagChatGO(go, ChatType.System);

    }

    public static void ShowGroupPanel(S_GroupUpdate pkt) {
        var lm = FindAnyObjectByType<LobbyManager>();
        
        if (!lm || !lm.Panel_Group) { Debug.LogWarning("LobbyManager/Panel_Group 없음"); return; }
        lm.Panel_Group.SetActive(true);

        Transform root = lm.Panel_Group.transform;

        for (int i = root.childCount - 1; i >= 0; --i) {
            var child = root.GetChild(i).gameObject;
            if (child.name.StartsWith("GroupItem_", StringComparison.Ordinal)) {
                Destroy(child);
            }
        }

        if (lm.GroupListPrefab == null) {
            lm.GroupListPrefab = Resources.Load<GameObject>("GroupListPrefab");
            if (lm.GroupListPrefab == null) {
                Debug.LogWarning("GroupListPrefab 미할당이며 Resources/GroupListPrefab 도 없음");
                return;
            }
        }

        Sprite captainSprite = Resources.Load<Sprite>("Image/RawImage/Captain");
        if (captainSprite == null) {
            Debug.LogWarning("Resources/Image/RawImage/Captain 스프라이트를 찾을 수 없음");
        }

        string myNick = NetworkManager.Instance?.nickName;
        int memberCount = 0;
        int lobbymemberCount = 0;
        foreach (var p in pkt.players) {
            memberCount++;
            var go = UnityEngine.Object.Instantiate(lm.GroupListPrefab, root);
            go.name = $"GroupItem_{p.nickName}";

            var tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) {
                if (t.gameObject.name == "level") {
                    t.text = $"Lv.{p.level}" ?? "";
                } else if (t.gameObject.name == "nickName") {
                    t.text = p.nickName ?? "";
                    if (!string.IsNullOrEmpty(myNick) && p.nickName == myNick) {
                        t.color = Color.green;
                    }
                }
            }

            var lobby = go.GetComponentsInChildren<Image>(true);
            foreach (var l in lobby) {
                if (l.gameObject.name == "AvailablePanel") {
                    if (p.isLobby) {
                        l.GetComponent<Image>().color = Color.green;
                        lobbymemberCount++;
                    } else {
                        l.GetComponent<Image>().color = Color.red;
                    }
                    break;
                }
            }

            var imgs = go.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs) {
                if (img.gameObject.name == "Panel_Leader") {
                    var c = img.color;
                    if (p.isLeader) {
                        if (captainSprite != null) {
                            img.sprite = captainSprite;  // 이미지 교체
                            img.preserveAspect = true;   // 왜곡 방지(선택)
                        }
                        img.color = new Color(c.r, c.g, c.b, 1f); // 보이게
                        img.enabled = true;

                        if (p.nickName == myNick) {
                            NetworkManager.Instance.isGroupLeader = true;
                        }

                    } else {
                        img.color = new Color(c.r, c.g, c.b, 0f); // 숨김
                        img.enabled = true; // 알파 0으로만 숨김 요청이라 enabled는 유지
                    }
                    break;
                }
            }

            ApplyTierMaterialToGroupItem(go, p.rank);
        }

        //lm.Panel_Group.GetComponentsInChildren<TextMeshProUGUI>()[1].text = $"그룹 ({memberCount}/10)";
        lm.Panel_Group.GetComponentsInChildren<TextMeshProUGUI>()[1].text = LanguageSwitcher.LF("UI/Group20", memberCount);

        //TODO
        if (memberCount > lobbymemberCount) {
            isCanFindGroupMatch = false;
        } else {
            isCanFindGroupMatch = true;
        }

        if (NetworkManager.Instance.isGroupLeader && isCanFindGroupMatch) {
            lm.Button_FindMatch.interactable = true;
        } else {
            lm.Button_FindMatch.interactable = false;
        }

        if (NetworkManager.Instance.isInGroup && !NetworkManager.Instance.isGroupLeader) {
            lm.Button_SelectLeft.interactable = false;
            lm.Button_SelectRight.interactable = false;
        }
        

        if (pkt.isDestroy) {
            lm.Panel_Group.SetActive(false);
            UnityEngine.Object prefab = Resources.Load("ChatPrefab");
            Transform parent = GameObject.Find("ChatContent").transform;
            GameObject go = UnityEngine.Object.Instantiate(prefab, parent) as GameObject;

            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.color = Color.yellow;
            //txt.text = $"그룹이 해체 되었습니다.";
            txt.text = LanguageSwitcher.L("UI/Group13");
            TagChatGO(go, ChatType.System);

            NetworkManager.Instance.isGroupLeader = false;
            NetworkManager.Instance.isInGroup = false;
            isCanFindGroupMatch = true;
            lm.Button_FindMatch.interactable = true;
            lm.isButtonClicked = true;
        }
        
    }

    public static void ShowJoinGroupMessage(string joinnerName) {
        UnityEngine.Object prefab = Resources.Load("ChatPrefab");
        Transform parent = GameObject.Find("ChatContent").transform;
        GameObject go = UnityEngine.Object.Instantiate(prefab, parent) as GameObject;

        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.color = Color.yellow;
        //txt.text = $"{joinnerName}님이 그룹에 참여했습니다.";
        txt.text = LanguageSwitcher.LF("UI/Group14", joinnerName);
        TagChatGO(go, ChatType.System);
    }

    public static void ShowLeaveGroupMessage(S_BroadcastLeaveGroup pkt) {
        UnityEngine.Object prefab = Resources.Load("ChatPrefab");
        Transform parent = GameObject.Find("ChatContent").transform;
        GameObject go = UnityEngine.Object.Instantiate(prefab, parent) as GameObject;

        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.color = Color.yellow;
        //txt.text = $"{pkt.leaverNickName}님이 그룹에서 나갔습니다.";
        txt.text = LanguageSwitcher.LF("UI/Group21", pkt.leaverNickName);
        if (pkt.isLeader) {
            //txt.text += $" {pkt.newLeaderNickName}님이 새로운 그룹장이 되었습니다.";
            txt.text += LanguageSwitcher.LF("UI/Group15", pkt.newLeaderNickName);
        }
        TagChatGO(go, ChatType.System);

    }

    public void OnButtonLeaveGroup() {
        C_LeaveGroup pkt = new C_LeaveGroup();
        NetworkManager.Send(pkt.Write());
        var lm = FindAnyObjectByType<LobbyManager>();
        lm.Panel_Group.SetActive(false);

        UnityEngine.Object prefab = Resources.Load("ChatPrefab");
        Transform parent = GameObject.Find("ChatContent").transform;
        GameObject go = UnityEngine.Object.Instantiate(prefab, parent) as GameObject;

        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.color = Color.yellow;
        //txt.text = $"그룹에서 나갔습니다.";
        txt.text = LanguageSwitcher.L("UI/Group16");

        NetworkManager.Instance.isGroupLeader = false;
        NetworkManager.Instance.isInGroup = false;
        lm.Button_FindMatch.interactable = true;
        isCanFindGroupMatch = true;
        isButtonClicked = true;

        TagChatGO(go, ChatType.System);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonChatTab() {
        if (selectedChatMode < Tab_Chat.Group) {
            selectedChatMode++;
        } else {
            selectedChatMode = Tab_Chat.Normal;
        }

        if (selectedChatMode == Tab_Chat.Normal) {
            //tabChat.text = "전체";
            tabChat.text = LanguageSwitcher.L("UI/Group17");
            tabChat.color = Color.white;
        } else if (selectedChatMode == Tab_Chat.Group) {
            //tabChat.text = "그룹";
            tabChat.text = LanguageSwitcher.L("UI/Group18");
            tabChat.color = Color.cyan;
        }

    }

    public void ApplyChatFilter() {
        if (_chatContent == null) return;

        bool showNormal = Toggle_ShowNormalChat != null && Toggle_ShowNormalChat.isOn;
        bool showGroup = Toggle_ShowGroupChat != null && Toggle_ShowGroupChat.isOn;
        bool showSystem = Toggle_ShowSystemChat != null && Toggle_ShowSystemChat.isOn;

        for (int i = 0; i < _chatContent.childCount; i++) {
            var child = _chatContent.GetChild(i).gameObject;

            ChatType type = DetectType(child);
            bool visible = type switch {
                ChatType.Normal => showNormal,
                ChatType.Group => showGroup,
                ChatType.System => showSystem,
                _ => true
            };
            if (child.activeSelf != visible) child.SetActive(visible);
        }
    }

    ChatType DetectType(GameObject go) {
        var tag = go.GetComponent<ChatItem>();
        if (tag != null) return tag.type;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) {
            var c = tmp.color;
            if (Approximately(c, Color.cyan)) return ChatType.Group;
            if (Approximately(c, Color.yellow)) return ChatType.System;
            if (Approximately(c, Color.white) || c.a > 0f) return ChatType.Normal;
        }
        return ChatType.Normal;
    }

    static bool Approximately(Color a, Color b, float eps = 0.08f) {
        return Mathf.Abs(a.r - b.r) < eps &&
               Mathf.Abs(a.g - b.g) < eps &&
               Mathf.Abs(a.b - b.b) < eps;
    }

    public static void TagChatGO(GameObject go, ChatType type) {
        if (!go) return;
        var item = go.GetComponent<ChatItem>();
        if (!item) item = go.AddComponent<ChatItem>();
        item.type = type;

        // 현재 토글 상태에 맞춰 즉시 표시/비표시 반영
        var lm = FindAnyObjectByType<LobbyManager>();
        if (lm) lm.ApplyChatFilter();
    }

    public static void G_OnButtonFindMatch() {
        var lm = FindAnyObjectByType<LobbyManager>();
        if (!isFindingMatch) {
            isFindingMatch = true;
            lm.StartCoroutine(G_addFindingTime());
        } else {
            lm.StartCoroutine(G_cancelFindingMatch());
        }
    }

    public static IEnumerator G_addFindingTime() {
        var lm = FindAnyObjectByType<LobbyManager>();
        findingTime = 0f;
        lm.PlayMM(true); // 상단에서 "내려오기"
        //Panel_MatchMaking.SetActive(true);
        lm.Button_SelectLeft.interactable = false;
        lm.Button_SelectRight.interactable = false;

        SoundManager.I?.Play2D(SfxId.MatchFindStart);


        var spinner = lm.spinnerObj.GetComponent<UISpinner>();
        spinner.Play();
        while (isFindingMatch) {
            findingTime += Time.deltaTime;
            int total = Mathf.FloorToInt(findingTime);
            int mm = total / 60;
            int ss = total % 60;

            //lm.TMP_MatchMaking.text = $"게임을 찾는 중\n{mm:00}:{ss:00}";
            lm.TMP_MatchMaking.text = LanguageSwitcher.LF("MATCH_FINDING", mm, ss);
            yield return null;
        }
    }

    public static IEnumerator G_cancelFindingMatch() {
        var lm = FindAnyObjectByType<LobbyManager>();
        isFindingMatch = false;
        //lm.TMP_MatchMaking.text = $"매칭 찾기 취소";
        lm.TMP_MatchMaking.text = LanguageSwitcher.L("MATCH_CANCEL");
        var spinner = lm.spinnerObj.GetComponent<UISpinner>();
        spinner.Stop();
        float t = 0f;
        if (NetworkManager.Instance.isInGroup && !NetworkManager.Instance.isGroupLeader) {
            lm.Button_SelectLeft.interactable = false;
            lm.Button_SelectRight.interactable = false;
        }
        while (t < 3f || isFindingMatch) {
            t += Time.deltaTime;
            yield return null;
        }
        lm.PlayMM(false);
    }

    public static void OnShowUserHistory(S_RecentGames pkt) {
        var lm = FindAnyObjectByType<LobbyManager>();
        if (lm == null) { Debug.LogWarning("LobbyManager 없음"); return; }
        if (lm.RecordPrefab == null) {
            Debug.LogWarning("RecordPrefab 미할당");
            return;
        }

        Color winColor = new Color(0, 0.35f, 0.55f, 1f);
        Color loseColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        // RecordPanel 찾기
        var recordPanelGO = GameObject.Find("RecordPanel");
        if (recordPanelGO == null) {
            Debug.LogWarning("RecordPanel 오브젝트를 찾을 수 없음");
            return;
        }
        var recordPanel = recordPanelGO.transform;

        // 기존 항목 정리(RecordItem_ 접두사만 제거)
        for (int i = recordPanel.childCount - 1; i >= 0; --i) {
            var child = recordPanel.GetChild(i).gameObject;
            if (child.name.StartsWith("RecordItem_", StringComparison.Ordinal)) {
                UnityEngine.Object.Destroy(child);
            }
        }

        // 유틸: epoch 값이 초/밀리초 섞여와도 처리
        static DateTime ToLocalTimeFromEpochInt(int startedAtMsMaybeSec) {
            long v = startedAtMsMaybeSec;
            bool isMs = v >= 10_000_000_000L;
            long ms = isMs ? v : v * 1000L;
            try {
                return DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime().DateTime;
            } catch {
                return DateTime.Now; // 방어
            }
        }

        // 모드 문자열 매핑
        static string ModeToString(int mode) {
            return mode switch {
                (int)GameMode.SURVIVAL => LanguageSwitcher.L("UI/Record1"),
                (int)GameMode.RESPAWN => LanguageSwitcher.L("UI/Record2"),
                (int)GameMode.RANKSURVIVAL => LanguageSwitcher.L("UI/Record3"),
                _ => $"모드{mode}"
            };
        }

        int idx = 0;
        foreach (var g in pkt.gamess) {
            idx++;
            var go = UnityEngine.Object.Instantiate(lm.RecordPrefab, recordPanel);
            go.name = $"RecordItem_{idx}";

            var tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length < 5) {
                Debug.LogWarning("RecordPrefab 안에 TMP가 5개 미만입니다. (모드/순위/킬/데스/시간 순)");
            }

            TextMeshProUGUI tMode = tmps.Length > 0 ? tmps[0] : null;
            TextMeshProUGUI tRank = tmps.Length > 1 ? tmps[1] : null;
            TextMeshProUGUI tKill = tmps.Length > 2 ? tmps[2] : null;
            TextMeshProUGUI tDeath = tmps.Length > 3 ? tmps[3] : null;
            TextMeshProUGUI tTime = tmps.Length > 4 ? tmps[4] : null;

            string modeStr = ModeToString(g.mode);
            tMode?.SetText(modeStr);
            tRank?.SetText($"{g.rank}");
            tKill?.SetText($"{g.kills}");
            tDeath?.SetText($"{g.deaths}");

            var localTime = ToLocalTimeFromEpochInt(g.startedAtMs);
            tTime?.SetText(localTime.ToString("yyyy-MM-dd HH:mm"));

            if (tMode != null) tMode.color = Color.white;
            if (tRank != null) tRank.color = Color.white;
            if (tKill != null) tKill.color = Color.white;
            if (tDeath != null) tDeath.color = Color.white;
            if (tTime != null) tTime.color = Color.white;

            if (go.TryGetComponent<Image>(out var img)) {
                if (1 <= g.rank && g.rank <= 5) img.color = winColor;
                else if (6 <= g.rank && g.rank <= 10) img.color = loseColor;
                else img.color = Color.white;
            } else if (go.TryGetComponent<RawImage>(out var rimg)) {
                if (1 <= g.rank && g.rank <= 5) rimg.color = winColor;
                else if (6 <= g.rank && g.rank <= 10) rimg.color = loseColor;
                else rimg.color = Color.white;
            }

            if (g.rank == 1 && tRank != null) tRank.color = Color.red;

            if (g.kills > 5 && tKill != null) tKill.color = Color.red;

            if (g.mode == (int)GameMode.RANKSURVIVAL && tMode != null) tMode.color = Color.yellow;
        }
    }

    public static void OnShowLeaderboard(S_Leaderboard pkt) {
        var lm = FindAnyObjectByType<LobbyManager>();
        if (lm == null) { Debug.LogWarning("LobbyManager 없음"); return; }
        if (lm.leaderboardPrefab == null) { Debug.LogWarning("leaderboardPrefab 미할당"); return; }

        var localTime = ToLocalTimeFromEpochInt(pkt.lastUpdatedSec);
        lm.leaderboardUpdateTime.SetText(localTime.ToString($"yyyy-MM-dd HH:mm"));

        static DateTime ToLocalTimeFromEpochInt(int startedAtMsMaybeSec) {
            long v = startedAtMsMaybeSec;
            bool isMs = v >= 10_000_000_000L;
            long ms = isMs ? v : v * 1000L;
            try {
                return DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime().DateTime;
            } catch {
                return DateTime.Now; // 방어
            }
        }

        // 1) Content 찾기
        var contentGO = GameObject.Find("LeaderboardContent");
        if (contentGO == null) { Debug.LogWarning("LeaderboardContent 오브젝트를 찾을 수 없음"); return; }
        var content = contentGO.transform;

        // 2) 기존 항목 정리(접두사 기준)
        for (int i = content.childCount - 1; i >= 0; --i) {
            var child = content.GetChild(i).gameObject;
            if (child.name.StartsWith("LeaderboardItem_", StringComparison.Ordinal)) {
                UnityEngine.Object.Destroy(child);
            }
        }

        // 3) 행 생성
        int place = 0;
        foreach (var r in pkt.rowss) {
            place++;

            var go = UnityEngine.Object.Instantiate(lm.leaderboardPrefab, content);
            go.name = $"LeaderboardItem_{place}_{r.nickName}";

            // --- 텍스트 채우기 ---
            // 우선 이름으로 찾아보고, 없으면 인덱스 순(총 8개)을 백업 경로로 사용
            var tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);

            TextMeshProUGUI tRank = FindTmp(go, "rank") ?? GetTmp(tmps, 0);
            TextMeshProUGUI tNick = FindTmp(go, "nick") ?? GetTmp(tmps, 1);
            TextMeshProUGUI tTier = FindTmp(go, "tier") ?? GetTmp(tmps, 2);
            TextMeshProUGUI tScore = FindTmp(go, "score") ?? GetTmp(tmps, 3);
            TextMeshProUGUI tGames = FindTmp(go, "games") ?? GetTmp(tmps, 4);
            TextMeshProUGUI tWinRate = FindTmp(go, "winrate") ?? GetTmp(tmps, 5);
            TextMeshProUGUI tAvgRank = FindTmp(go, "avgrank") ?? GetTmp(tmps, 6);
            TextMeshProUGUI tAvgKill = FindTmp(go, "avgkill") ?? GetTmp(tmps, 7);

            tRank?.SetText(place.ToString());
            tNick?.SetText(r.nickName ?? "");
            tTier?.SetText(TierToKorean(r.tier));
            tScore?.SetText(r.score.ToString());
            tGames?.SetText(r.totalGames.ToString());
            tWinRate?.SetText($"{r.winRate:F1}%");
            tAvgRank?.SetText($"{r.avgRank:F1}");
            tAvgKill?.SetText($"{r.avgKill:F1}");

            // --- 티어 머터리얼 적용 ---
            ApplyTierMaterialToRoot(go, r.tier);
        }

        // ---- helpers ----
        static TextMeshProUGUI FindTmp(GameObject root, string key) {
            // 이름에 key가 포함된 TMP를 느슨하게 탐색 (rank/nick/tier/score/games/winrate/avgrank/avgkill 등)
            var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            key = key.ToLowerInvariant();
            foreach (var t in tmps) {
                var n = t.gameObject.name.ToLowerInvariant();
                if (n.Contains(key)) return t;
            }
            return null;
        }
        static TextMeshProUGUI GetTmp(TextMeshProUGUI[] arr, int idx) {
            return (arr != null && arr.Length > idx) ? arr[idx] : null;
        }
        static string TierToKorean(int tier) {
            return tier switch {
                0 => LanguageSwitcher.L("Tier1"),
                1 => LanguageSwitcher.L("Tier2"),
                2 => LanguageSwitcher.L("Tier3"),
                3 => LanguageSwitcher.L("Tier4"),
                4 => LanguageSwitcher.L("Tier5"),
                5 => LanguageSwitcher.L("Tier6"),
                6 => LanguageSwitcher.L("Tier7"),
                _ => $"티어{tier}"
            };
        }
        static void ApplyTierMaterialToRoot(GameObject go, int tier) {
            tier = Mathf.Clamp(tier, 0, 6);
            string[] names = LobbyManager.RankMatNames; // ["0UnrankSparkling", ..., "6GodSparkling"]
            string matName = names[tier];

            // PanelInfo에서 쓰던 경로 그대로 사용
            var mat = Resources.Load<Material>($"UI/Material/NameRank/{matName}");
            if (mat == null) { Debug.LogWarning($"랭킹 머터리얼 로드 실패: {matName}"); return; }

            // UI(Image/RawImage) 우선 적용, 없으면 Renderer
            if (go.TryGetComponent<Image>(out var img)) {
                img.material = new Material(mat);   // 공유 재질 오염 방지
                return;
            }
            if (go.TryGetComponent<RawImage>(out var raw)) {
                raw.material = new Material(mat);
                return;
            }
            if (go.TryGetComponent<Renderer>(out var rend)) {
                rend.material = mat;
            }
        }
    }

    static void ApplyTierMaterialToGroupItem(GameObject itemRoot, int tier) {
        tier = Mathf.Clamp(tier, 0, 6);
        string matName = RankMatNames[tier];

        // 1) 머터리얼 로드
        var baseMat = Resources.Load<Material>($"UI/Material/NameRank/{matName}");
        if (baseMat == null) {
            Debug.LogWarning($"[GroupItemMaterial] '{matName}' 머터리얼을 찾지 못했습니다.");
            return;
        }

        // 2) 적용 대상을 찾는 우선순위:
        //    (1) 이름에 'Rank'/'Badge'/'Name'이 포함된 Image/RawImage
        //    (2) 루트에 붙은 Image/RawImage
        //    (3) (없다면) 첫 번째 Renderer
        Image targetImg = null;
        RawImage targetRaw = null;

        // 자식 Image들 중 이름 키워드 우선 탐색
        var images = itemRoot.GetComponentsInChildren<Image>(true);
        foreach (var img in images) {
            string n = img.gameObject.name.ToLowerInvariant();
            if (n.Contains("rank") || n.Contains("badge") || n.Contains("name")) {
                targetImg = img; break;
            }
        }
        if (targetImg == null) {
            // 루트에 Image 있으면 사용
            itemRoot.TryGetComponent(out targetImg);
        }
        if (targetImg == null) {
            // RawImage도 같은 방식으로 탐색
            var raws = itemRoot.GetComponentsInChildren<RawImage>(true);
            foreach (var r in raws) {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("rank") || n.Contains("badge") || n.Contains("name")) {
                    targetRaw = r; break;
                }
            }
            if (targetRaw == null) {
                itemRoot.TryGetComponent(out targetRaw);
            }
        }

        // 3) 실제 적용
        if (targetImg != null) {
            // 공유 머터리얼 오염 방지
            targetImg.material = new Material(baseMat);
            return;
        }
        if (targetRaw != null) {
            targetRaw.material = new Material(baseMat);
            return;
        }
        if (itemRoot.TryGetComponent<Renderer>(out var rend)) {
            // 3D용은 공유해도 되면 baseMat, 아니면 new Material(baseMat)
            rend.material = baseMat;
        }
    }

    public static void OnUpdatePlayerData(S_PlayerUpdate pkt) {
        var lm = FindAnyObjectByType<LobbyManager>();
        TextMeshProUGUI[] txt = lm.Panel_Information.GetComponentsInChildren<TextMeshProUGUI>();
        txt[0].text = pkt.nickName;
        txt[1].text = $"Lv.{pkt.level}";
        txt[2].text = $"{TierToKorean(pkt.rank)} {pkt.rankScore}p";

        if (pkt.rank >= 2) {
            txt[0].color = Color.black;
            txt[1].color = Color.black;
            txt[2].color = Color.black;
        }

        lm.Slider_Information.maxValue = pkt.maxExp;
        lm.Slider_Information.value = pkt.exp;
        ApplyTierMaterialToGroupItem(lm.Panel_Information, pkt.rank);
        static string TierToKorean(int tier) {
            return tier switch {
                0 => LanguageSwitcher.L("Tier1"),
                1 => LanguageSwitcher.L("Tier2"),
                2 => LanguageSwitcher.L("Tier3"),
                3 => LanguageSwitcher.L("Tier4"),
                4 => LanguageSwitcher.L("Tier5"),
                5 => LanguageSwitcher.L("Tier6"),
                6 => LanguageSwitcher.L("Tier7"),
                _ => $"티어{tier}"
            };
        }

    }

    void RefreshLockerRoomHelp() {
        if (!Panel_LockerRoom_Help) return;

        // 1) 설정에서 도움말 끈 경우 → 패널 비활성
        if (!SettingsApplier.IsHelpMessageEnabled) {
            if (Panel_LockerRoom_Help.activeSelf)
                Panel_LockerRoom_Help.SetActive(false);
            return;
        }

        // 2) LockerRoom 캔버스가 활성화되어 있을 때만 보이도록
        bool show = Canvas_LockerRoom != null && Canvas_LockerRoom.activeSelf;
        Panel_LockerRoom_Help.SetActive(show);
        if (!show) return;

        // 3) TMP 컴포넌트 한 번만 찾아서 캐시
        if (_lockerRoomHelpText == null) {
            _lockerRoomHelpText = Panel_LockerRoom_Help.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_lockerRoomHelpText == null) return;
        }

        // 4) 현재 공격 키 가져오기
        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        if (cur.keys == null) cur.keys = new KeyBindings();
        KeyCode attackKey = cur.keys.attack;
        string attackKeyStr = ToDisplayKey(attackKey);

        // 5) Dance 패널이 켜져 있으면 댄스 문구, 아니면 공격 문구
        bool isDancePanelActive = Panel_LockerRoom_Dance != null && Panel_LockerRoom_Dance.activeSelf;
        
        if (isDancePanelActive) {
            //_lockerRoomHelpText.text = "키를 눌러 댄스를 출 수 있습니다";
            _lockerRoomHelpText.text = LanguageSwitcher.L("INFO_DANCE");
        } else {
            //_lockerRoomHelpText.text = $"{attackKeyStr} 키를 눌러 공격해 볼 수 있습니다";
            string msg = string.Format(L("INFO_ATK"), attackKeyStr);
            _lockerRoomHelpText.text = msg;
        }
    }

    static string L(string key) {
        const string TABLE = "GameTexts";

        var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(TABLE, key);
        if (!op.IsDone) op.WaitForCompletion();

        var v = op.Result;
        return string.IsNullOrEmpty(v) ? key : v;
    }

    // KeyCode → 화면에 보여줄 문자열
    string ToDisplayKey(KeyCode key) {
        // 숫자 키 예쁘게
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) {
            int num = key - KeyCode.Alpha0;
            return num.ToString();
        }

        // 마우스
        if (key == KeyCode.Mouse0) return "Mouse0";
        if (key == KeyCode.Mouse1) return "Mouse1";
        if (key == KeyCode.Mouse2) return "Mouse2";

        // 그 외에는 ToString() 그대로
        return key.ToString();
    }

    public void OnButtonBuyStar() {
        //UIManager.ShowError("Playtest 버전에서는 불가");
        Panel_Pay.SetActive(true);
        Panel_Pay1.GetComponentInChildren<Button>().interactable = true;
        Panel_Pay2.GetComponentInChildren<Button>().interactable = true;
        Panel_Pay3.GetComponentInChildren<Button>().interactable = true;
        Panel_Pay4.GetComponentInChildren<Button>().interactable = true;
        Panel_Pay5.GetComponentInChildren<Button>().interactable = true;
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonClosePay() {
        Panel_Pay.SetActive(false);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnButtonPay1() {
        C_RequestAddStar pkt = new C_RequestAddStar();
        pkt.packIndex = 0;
        NetworkManager.Send(pkt.Write());
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Panel_Pay1.GetComponentInChildren<Button>().interactable = false;
        Panel_Pay.SetActive(false);
    }

    public void OnButtonPay2() {
        C_RequestAddStar pkt = new C_RequestAddStar();
        pkt.packIndex = 1;
        NetworkManager.Send(pkt.Write());
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Panel_Pay2.GetComponentInChildren<Button>().interactable = false;
        Panel_Pay.SetActive(false);
    }

    public void OnButtonPay3() {
        C_RequestAddStar pkt = new C_RequestAddStar();
        pkt.packIndex = 2;
        NetworkManager.Send(pkt.Write());
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Panel_Pay3.GetComponentInChildren<Button>().interactable = false;
        Panel_Pay.SetActive(false);
    }

    public void OnButtonPay4() {
        C_RequestAddStar pkt = new C_RequestAddStar();
        pkt.packIndex = 3;
        NetworkManager.Send(pkt.Write());
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Panel_Pay4.GetComponentInChildren<Button>().interactable = false;
        Panel_Pay.SetActive(false);
    }

    public void OnButtonPay5() {
        C_RequestAddStar pkt = new C_RequestAddStar();
        pkt.packIndex = 4;
        NetworkManager.Send(pkt.Write());
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Panel_Pay5.GetComponentInChildren<Button>().interactable = false;
        Panel_Pay.SetActive(false);
    }

    public void OnButtonFindUser() {
        CloseChatPopup();
        Canvas_PopupUser.SetActive(false);
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Panel_FindUser.SetActive(true);
        inputFindUser = null;
        InputField_FindUser.text = null;
    }

    public void OnButtonSearchUser() {
        if (inputFindUser != null) {
            Canvas_PopupUser.SetActive(false);
            SoundManager.I?.Play2D(SfxId.MouseClick);
            C_RequestUserInformation pkt = new C_RequestUserInformation();
            pkt.nickName = inputFindUser;
            NetworkManager.Send(pkt.Write());
        }
        inputFindUser = null;
        InputField_FindUser.text = null;
    }

    public void OnCloseFindUser() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        Panel_FindUser.SetActive(false);
        inputFindUser = null;
        InputField_FindUser.text = null;
    }

    public void OnButtonInviteGroupBySearch() {
        CloseChatPopup();
        Panel_FindUser.SetActive(false);
        Canvas_PopupUser.SetActive(false);
        C_InviteGroup pkt = new C_InviteGroup();
        pkt.LeaderNickName = NetworkManager.Instance.nickName;
        pkt.ServentNickName = inputFindUser;
        NetworkManager.Send(pkt.Write());
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    // === 그룹 초대 타이머 ===
    void StartInviteTimer() {
        if (Slider_Invite == null) return;

        if (_inviteTimerCo != null)
            StopCoroutine(_inviteTimerCo);

        Slider_Invite.minValue = 0f;
        Slider_Invite.maxValue = 1f;
        Slider_Invite.value = 1f;
        Slider_Invite.gameObject.SetActive(true);

        _inviteTimerCo = StartCoroutine(CoInviteTimer());
    }

    IEnumerator CoInviteTimer() {
        float t = 0f;
        while (t < inviteExpireSeconds) {
            t += Time.unscaledDeltaTime;              // 초대는 UI니까 unscaled 추천
            float k = Mathf.Clamp01(t / inviteExpireSeconds);
            Slider_Invite.value = 1f - k;            // ★ 풀게이지 -> 0 으로
            yield return null;
        }

        // 시간 끝: 패널 자동 닫기
        StopInviteTimer();     // 슬라이더 정리
        PlayInvite(false);     // 오른쪽으로 빠지게
        _currentInviterNick = "";
    }

    void StopInviteTimer() {
        if (_inviteTimerCo != null) {
            StopCoroutine(_inviteTimerCo);
            _inviteTimerCo = null;
        }

        if (Slider_Invite != null) {
            Slider_Invite.value = 0f;
            Slider_Invite.gameObject.SetActive(false);
        }
    }

}
