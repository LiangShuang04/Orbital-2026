using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Globalization;

public class UIGlobal : MonoBehaviour
{
    public static UIGlobal instance;
    public Animator animTitleInput;
    public Animator animInputIndex;
    public Animator animPortraitWelcome;
    public Animator animBackground;
    public Animator animRegister;
    public Animator animSelectAvatar;

    public Color colorText;

    public Sprite[] AllAvatar;
    public TextMeshProUGUI[] AllText;
    public TMP_InputField[] AllInputtext;
    public AudioSource Fxsounds;
    public AudioSource MusicBg;
    public AudioSource Music;
    public AudioClip[] AllFX;
    public AudioClip[] WriteSounds;

    public GameObject[] Frames;
    public Button[] ButtonsMenu;

    public GameObject AvatarObj;
    public GameObject BtnNewAccount;
    public GameObject BtnNext;
    public GameObject BtnValidate;
    public GameObject BtnBack;
    public GameObject BtnCreateAccount;
    public GameObject BtnBackLogin;
    public GameObject PanelRegister;
    public GameObject PanelSelectAvatar;
    public GameObject AccessGranted;
    public GameObject AccessDenied;
    public GameObject AccountCreated;
    public GameObject ConnectionError;
    public GameObject Portrait;
    public GameObject InfoGraphic;
    public GameObject VideoPlayer;

    public Transform parentListAvatar;

    public Image AvatarImageConnexion;
    public Image AvatarImageMenu;
    public SelectAvatar selectAvatar;

    [HideInInspector] public string MyName;
    [HideInInspector] public string MyPass;
    [HideInInspector] public string MyEmail;
    [HideInInspector] public int Moneys;
    [HideInInspector] public int Level;
    [HideInInspector] public int AvatarID;

    [HideInInspector] public bool Registered = false;
    [HideInInspector] public bool ErrorRegistered = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        instance = this;

        StartTextIdentify();

        selectAvatar.ListAvatar = new GameObject[AllAvatar.Length];

        //ADD ALL AVATARS OF THE LIST, IN GALERY 
        for (int i = 0; i < AllAvatar.Length; i++)
        {
            GameObject ObjAvatar = Instantiate(AvatarObj, parentListAvatar);
            ObjAvatar.transform.SetParent(parentListAvatar);
            ObjAvatar.transform.localScale = Vector3.one;

            ObjAvatar.GetComponent<EntryAvatar>().idAvatar = i;
            ObjAvatar.name = "Avatar" + i.ToString("00");
            ObjAvatar.GetComponent<EntryAvatar>().select = selectAvatar;
            ObjAvatar.GetComponent<EntryAvatar>().AvatarImg.sprite = AllAvatar[i];

            selectAvatar.ListAvatar[i] = ObjAvatar;

            if (i == 0)
            {
                ObjAvatar.GetComponent<EntryAvatar>().SelectedObj.SetActive(true);
                selectAvatar.DefaultSelected = ObjAvatar;
            }
        }
    }

    void StartTextIdentify()
    {
        TextWriter.AddWriter_Static(AllText[0], WriteSounds[0], true, "PLEASE IDENTIFY YOURSELF", .02f, false, true, LaunchAnimInput);
    }

    void LaunchAnimInput()
    {
        animInputIndex.SetTrigger("play");
        BtnNewAccount.SetActive(true);
        BtnNext.SetActive(true);
    }

    public void ShowInputIndex()
    {
        AllInputtext[0].enabled = true;
        EventSystem.current.SetSelectedGameObject(AllInputtext[0].gameObject, null);
        AllInputtext[0].OnPointerClick(new PointerEventData(EventSystem.current));
    }

    public void ShowTextTitleRegister()
    {
        TextWriter.AddWriter_Static(AllText[7], WriteSounds[0], true, "CHOOSE IDENTIFY YOURSELF", .02f, false, true, null);
        TextWriter.AddWriter_Static(AllText[8], WriteSounds[0], true, "ENTER THE PASSWORD", .02f, false, true, null);
        TextWriter.AddWriter_Static(AllText[9], WriteSounds[0], true, "ENTER AGAIN THE PASSWORD", .02f, false, true, null);
        TextWriter.AddWriter_Static(AllText[10], WriteSounds[0], true, "ENTER THE EMAIL", .02f, false, true, null);
        TextWriter.AddWriter_Static(AllText[11], WriteSounds[0], true, "ENTER AGAIN THE EMAIL", .02f, false, true, null);
        TextWriter.AddWriter_Static(AllText[12], WriteSounds[0], true, "REGISTER NEW ACCOUNT", .02f, false, true, null);
    }

    public void ChangeTitleForPassword()
    {
        BtnNewAccount.SetActive(false);
        BtnNext.SetActive(false);
        BtnValidate.SetActive(true);
        BtnBack.SetActive(true);
        AllText[0].text = "";
        AllInputtext[0].gameObject.SetActive(false);
        TextWriter.AddWriter_Static(AllText[0], WriteSounds[0], true, "ENTER PASSWORD", .02f, false, true, ShowPasswordInput);
    }

    public void ShowPasswordInput()
    {
        AllInputtext[1].gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(AllInputtext[1].gameObject, null);
        AllInputtext[1].OnPointerClick(new PointerEventData(EventSystem.current));
    }

    public void CloseInput()
    {
        BtnValidate.SetActive(false);
        BtnBack.SetActive(false);
        animInputIndex.SetTrigger("validate");
        AllInputtext[1].gameObject.SetActive(false);
    }

    public void ShowHelloPortraitAfterConnectedGranted()
    {
        AccessGranted.SetActive(false);
        Portrait.SetActive(true);
        MusicBg.Stop();
        Music.enabled = true;
    }

    public void LaunchGameUIInterface()
    {
        Portrait.SetActive(false);
        InfoGraphic.SetActive(true);
    }

    public void WriteTitleHello()
    {
        TextWriter.AddWriter_Static(AllText[1], WriteSounds[1], false, "HELLO, " + MyName.ToUpper(), .05f, false, true, WriteWelcome);
    }

    public void WriteWelcome()
    {
        TextWriter.AddWriter_Static(AllText[2], WriteSounds[1], false, "WELCOME BACK", .05f, false, true, null);
        StartCoroutine("ClosePortraitWelcome");
    }

    public void WriteWelcomeHUD()
    {
        TextWriter.AddWriter_Static(AllText[3], WriteSounds[1], false, "WELCOME", .05f, false, true, WriteLoginHUD);
    }

    public void WriteLoginHUD()
    {
        TextWriter.AddWriter_Static(AllText[4], WriteSounds[1], false, MyName.ToUpper(), .05f, false, true, null);
        TextWriter.AddWriter_Static(AllText[16], WriteSounds[1], false, "LEVEL "+ Level.ToString("00"), .05f, false, true, null);

        CultureInfo culture;
        culture = CultureInfo.CreateSpecificCulture("en-US");

        TextWriter.AddWriter_Static(AllText[17], WriteSounds[1], false, Moneys.ToString("C", culture), .05f, false, true, null);

    }

    public void WriteBottomTextMenuHUD()
    {
        TextWriter.AddWriter_Static(AllText[5], WriteSounds[1], false, "Here the menu of Your choice", .05f, false, true, null);
    }

    public void ChangeOpacityBackground()
    {
        animBackground.SetTrigger("play");
        Camera.main.gameObject.GetComponent<GlitchEffect>().intensity = 0.15f;
    }

    IEnumerator ClosePortraitWelcome()
    {
        yield return new WaitForSeconds(4f);
        animPortraitWelcome.SetTrigger("Close");
        StopCoroutine("ClosePortraitWelcome");
    }
    public void PlayFXSound(AudioClip sound)
    {
        Fxsounds.PlayOneShot(sound);
    }

    public void HideAlphaInfoGraphic()
    {
        InfoGraphic.GetComponent<CanvasGroup>().alpha = 0.1f;
    }

    public void ShowAlphaInfoGraphic()
    {
        InfoGraphic.GetComponent<CanvasGroup>().alpha = 0.5f;
    }

    public void OpenFrame(int idFrame)
    {
        HideAlphaInfoGraphic();
        Frames[idFrame].SetActive(true);

        for(int i = 0; i < ButtonsMenu.Length; i++)
        {
            ButtonsMenu[i].interactable = false;
        }
    }

    public void BackMainMenu(int idFrame)
    {
        ShowAlphaInfoGraphic();
        Frames[idFrame].SetActive(false);

        for (int i = 0; i < ButtonsMenu.Length; i++)
        {
            ButtonsMenu[i].interactable = true;
        }
    }

    public void ButtonMenuInterract()
    {
        for (int i = 0; i < ButtonsMenu.Length; i++)
        {
            ButtonsMenu[i].interactable = true;
        }
    }

    public void ShowRegisterPanel()
    {
        PanelRegister.SetActive(true);
        ShowTextTitleRegister();
    }

    public void ReloadLoginUI()
    {
        if (Registered && !ErrorRegistered)
        {
            PanelRegister.SetActive(false);
            Registered = false;
            ErrorRegistered = false;
            AccountCreated.SetActive(true);
        }
        else if (Registered && ErrorRegistered)
        {
            PanelRegister.SetActive(false);
            Registered = false;
            ErrorRegistered = false;
        }
        else
        {
            if (PanelRegister.activeSelf)
            {
                PanelRegister.SetActive(false);
                animInputIndex.SetTrigger("reload");
            }

            AccessDenied.SetActive(false);
            AccessGranted.SetActive(false);

            AllInputtext[0].gameObject.SetActive(true);
            AllInputtext[0].text = "";
            AllInputtext[0].enabled = false;
            AllInputtext[0].readOnly = false;
            AllInputtext[0].caretPosition = 0;

            AllInputtext[1].gameObject.SetActive(false);
            AllInputtext[1].text = "";
            AllInputtext[1].enabled = true;
            AllInputtext[1].readOnly = false;

            BtnValidate.SetActive(false);
            BtnBack.SetActive(false);

            animInputIndex.Rebind();
            animTitleInput.Rebind();
            StartTextIdentify();
        }
    }

    public void ShowNewAccount()
    {
        AllInputtext[0].text = "";
        Fxsounds.PlayOneShot(AllFX[0]);
        animInputIndex.SetTrigger("close");
        BtnNewAccount.SetActive(false);
        BtnNext.SetActive(false);
    }

    public void ResetAvatarImageNewAccount()
    {
        selectAvatar.AvatarAccount.sprite = AllAvatar[AvatarID];
        selectAvatar.SelectedImage(AvatarID);
    }

    public void ShowSelectAvatar()
    {
        PanelSelectAvatar.SetActive(true);
        BtnCreateAccount.GetComponent<Button>().interactable = false;
        BtnBackLogin.GetComponent<Button>().interactable = false;
    }

    public void HideSelectAvatar()
    {
        animSelectAvatar.SetTrigger("close");
        BtnCreateAccount.GetComponent<Button>().interactable = true;
        BtnBackLogin.GetComponent<Button>().interactable = true;
    }

    public void LeaveRegisterPanel()
    {
        animRegister.SetTrigger("reload");
        AllInputtext[2].text = "";
        AllInputtext[3].text = "";
        AllInputtext[4].text = "";
        AllInputtext[5].text = "";
        AllInputtext[6].text = "";
        AvatarID = 0;
    }

    public void RegisterNewAccount()
    {
        LoadPHP.instance.StartCoroutine("NewAccount");
    }

    public void ShowHideVideoPlayer()
    {
        if (VideoPlayer.activeSelf)
        {
            VideoPlayer.SetActive(false);
        }
        else
        {
            VideoPlayer.SetActive(true);
        }
    }
}
