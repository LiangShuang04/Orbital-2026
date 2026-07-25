using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class LoadPHP : MonoBehaviour
{
    public static LoadPHP instance;
    public Register register;
    char[] splitchar = { '|' };
    string login_variables_url = "YOUR URL TO login.php";
    string load_variables_url = " YOUR URL TO loadStats.php";
    string load_login_url = " YOUR URL TO VerifLogin.php";
    string register_url = " YOUR URL TO register.php";

    bool sendCurrent;

    void Awake()
    {
        instance = this;
    }

    public void phpLogin()//SEND TO PHP IN STARTCOROUTINE FOR LOGIN
    {
        if (!sendCurrent)
        {
            UIGlobal.instance.AllText[0].text = "";
            StartCoroutine("Login");
        }
    }

    IEnumerator Login()//SEND AND LOAD TO PHP FOR LOGIN
    {
        sendCurrent = true;
        WWWForm form = new WWWForm();
        form.AddField("playerName", UIGlobal.instance.MyName);
        form.AddField("password", UIGlobal.instance.MyPass);
        form.AddField("key", "9f28f10d96d7227ba213d613a38fb5be");
        var download = UnityWebRequest.Post(login_variables_url, form);

        yield return download.SendWebRequest();
        if (download.isNetworkError || download.isHttpError)
        {

        }
        else
        {
            if (download.downloadHandler.text == "Error login" || download.downloadHandler.text == "Bad login")
            {
                UIGlobal.instance.AccessDenied.SetActive(true);
                UIGlobal.instance.AccessGranted.SetActive(false);
                sendCurrent = false;
                StopCoroutine("Login");
            }
            else if (download.downloadHandler.text == "Login success")
            {
                UIGlobal.instance.AccessGranted.SetActive(true);
                UIGlobal.instance.AccessDenied.SetActive(false);
                StopCoroutine("Login");
                StartCoroutine("LoadStats");
            }
        }
    }

    IEnumerator LoadStats()//LOAD TO PHP THE NAME, PASSWORD, MONEYS, ETC...IN MYSQL WITH YOUR LOGIN
    {
        WWWForm form = new WWWForm();
        form.AddField("playerName", UIGlobal.instance.MyName);
        form.AddField("password", UIGlobal.instance.MyPass);
        form.AddField("key", "9f28f10d96d7227ba213d613a38fb5be");
        var download = UnityWebRequest.Post(load_variables_url, form);

        yield return download.SendWebRequest();
        if (download.isNetworkError || download.isHttpError)
        {

        }
        else
        {
            if (download.downloadHandler.text == "Error login" || download.downloadHandler.text == "Bad login")
            {
                UIGlobal.instance.AccessDenied.SetActive(true);
                UIGlobal.instance.AccessGranted.SetActive(false);
                sendCurrent = false;
                StopCoroutine("LoadStats");
            }
            else
            {
                string[] variables = download.downloadHandler.text.Split(splitchar);
                UIGlobal.instance.Level = int.Parse(variables[0]);
                UIGlobal.instance.MyEmail = variables[1];
                UIGlobal.instance.Moneys = int.Parse(variables[2]);
                UIGlobal.instance.AvatarID = int.Parse(variables[3]);
                UIGlobal.instance.AvatarImageConnexion.sprite = UIGlobal.instance.AllAvatar[UIGlobal.instance.AvatarID];
                UIGlobal.instance.AvatarImageMenu.sprite = UIGlobal.instance.AllAvatar[UIGlobal.instance.AvatarID];
                sendCurrent = false;
                StopCoroutine("LoadStats");
            }
        }
    }

    IEnumerator VerifLogin()//CHECK IN PHP IF LOGIN IS ALREADY USED WHEN CREATE NEW ACCOUNT
    {
        sendCurrent = true;
        WWWForm form = new WWWForm();
        form.AddField("playerName", UIGlobal.instance.AllInputtext[2].text);
        form.AddField("key", "9f28f10d96d7227ba213d613a38fb5be");
        var download = UnityWebRequest.Post(load_login_url, form);

        yield return download.SendWebRequest();
        if (download.isNetworkError || download.isHttpError)
        {

        }
        else
        {
            if (download.downloadHandler.text == "Error login" || download.downloadHandler.text == "Bad login")
            {
                if (!string.IsNullOrEmpty(UIGlobal.instance.AllInputtext[2].text))
                {
                    UIGlobal.instance.AllText[13].color = Color.red;
                    UIGlobal.instance.AllText[13].text = "Already used!";
                    register.LoginValid = false;
                }
                else
                {
                    UIGlobal.instance.AllText[13].text = "";
                    register.LoginValid = false;
                }
                sendCurrent = false;
                StopCoroutine("VerifLogin");
            }
            else if (download.downloadHandler.text == "Login success")
            {
                if (!string.IsNullOrEmpty(UIGlobal.instance.AllInputtext[2].text))
                {
                    UIGlobal.instance.AllText[13].color = Color.green;
                    UIGlobal.instance.AllText[13].text = "Available!";
                    UIGlobal.instance.MyName = UIGlobal.instance.AllInputtext[2].text;
                    register.LoginValid = true;
                }
                else
                {
                    UIGlobal.instance.AllText[13].text = "";
                    register.LoginValid = false;
                }
                sendCurrent = false;
                StopCoroutine("VerifLogin");
            }
        }
    }

    IEnumerator NewAccount()//SEND TO PHP FOR CREATE NEW ACCOUNT IN MYSQL
    {
        sendCurrent = true;
        WWWForm form = new WWWForm();
        form.AddField("playerName", UIGlobal.instance.MyName);
        form.AddField("password", UIGlobal.instance.MyPass);
        form.AddField("email", UIGlobal.instance.MyEmail);
        form.AddField("idavatar", UIGlobal.instance.AvatarID);
        form.AddField("key", "9f28f10d96d7227ba213d613a38fb5be");
        var download = UnityWebRequest.Post(register_url, form);

        yield return download.SendWebRequest();
        if (download.isNetworkError || download.isHttpError)
        {

        }
        else
        {
            if (download.downloadHandler.text == "Error")
            {
                UIGlobal.instance.Registered = true;
                UIGlobal.instance.ErrorRegistered = true;
                UIGlobal.instance.LeaveRegisterPanel();

                sendCurrent = false;
                StopCoroutine("NewAccount");
            }
            else if (download.downloadHandler.text == "Ok")
            {
                UIGlobal.instance.Registered = true;
                UIGlobal.instance.ErrorRegistered = false;
                UIGlobal.instance.LeaveRegisterPanel();

                sendCurrent = false;
                StopCoroutine("NewAccount");
            }
        }
    }
}
