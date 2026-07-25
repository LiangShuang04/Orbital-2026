using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using TMPro;

public class Register : MonoBehaviour
{
    public bool LoginValid = false;
    public bool PasswordValid = false;
    public bool EmailValid = false;
    public Color colorTextBtnCreate;

    void Start()
    {
        Color alphaText = colorTextBtnCreate;
        alphaText.a = 0.4f;
    }

    //CHECK IF THE CHARACTERS IN THE INPUTFIELD LOGIN, IS GREATER THAN OR EQUAL TO 4
    public void CheckLogin()
    {
        if (UIGlobal.instance.AllInputtext[2].text.Length >= 4)
        {
            LoadPHP.instance.StartCoroutine("VerifLogin");
        }
    }

    //CHECK IF THE PASSWORD IS CORRECTLY ENTERED IN TWO INPUTFIELDS
    public void CheckPassword()
    {
        if (!string.IsNullOrEmpty(UIGlobal.instance.AllInputtext[3].text) && !string.IsNullOrEmpty(UIGlobal.instance.AllInputtext[4].text))
        {
            if (UIGlobal.instance.AllInputtext[3].text != UIGlobal.instance.AllInputtext[4].text)
            {
                UIGlobal.instance.AllText[14].color = Color.red;
                UIGlobal.instance.AllText[14].text = "Passwords not the same!";
                PasswordValid = false;
            }
            else
            {
                UIGlobal.instance.AllText[14].color = Color.green;
                UIGlobal.instance.AllText[14].text = "Valid passwords!";
                UIGlobal.instance.MyPass = UIGlobal.instance.AllInputtext[3].text;
                PasswordValid = true;
            }
        }
        else
        {
            UIGlobal.instance.AllText[14].text = "";
            PasswordValid = false;
        }
    }
    
    //CHECK IF THE EMAIL IS CORRECTLY ENTERED
    public const string MatchEmailPattern =
            @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
            + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
              + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
            + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

    public static bool IsEmail(string email)
    {
        if (email != null) return Regex.IsMatch(email, MatchEmailPattern);
        else return false;
    }

    //CHECK IF THE EMAIL IS CORRECTLY ENTERED IN TWO INPUTFIELDS
    public void CheckEmail()
    {
        if (!string.IsNullOrEmpty(UIGlobal.instance.AllInputtext[5].text) && !string.IsNullOrEmpty(UIGlobal.instance.AllInputtext[6].text))
        {
            if (IsEmail(UIGlobal.instance.AllInputtext[5].text) && IsEmail(UIGlobal.instance.AllInputtext[6].text))
            {

                if (UIGlobal.instance.AllInputtext[5].text != UIGlobal.instance.AllInputtext[6].text)
                {
                    UIGlobal.instance.AllText[15].color = Color.red;
                    UIGlobal.instance.AllText[15].text = "Emails not the same!";
                    EmailValid = false;
                }
                else
                {
                    UIGlobal.instance.AllText[15].color = Color.green;
                    UIGlobal.instance.AllText[15].text = "Valid emails!";
                    UIGlobal.instance.MyEmail = UIGlobal.instance.AllInputtext[5].text;
                    EmailValid = true;
                }
            }
            else
            {
                UIGlobal.instance.AllText[15].color = Color.red;
                UIGlobal.instance.AllText[15].text = "Emails not valid!";
                EmailValid = false;
            }
        }
        else
        {
            UIGlobal.instance.AllText[15].text = "";
            EmailValid = false;
        }
    }

    //CHECK IF ALL INPUTFIELDS IS NOT EMPTY
    public void CheckValidInputCreateAccount()
    {
        if(LoginValid && PasswordValid && EmailValid)
        {
            UIGlobal.instance.BtnCreateAccount.GetComponent<Button>().interactable = true;
            Color alphaText = colorTextBtnCreate;
            alphaText.a = 1f;
            UIGlobal.instance.BtnCreateAccount.GetComponentInChildren<TextMeshProUGUI>().color = alphaText;
        }
        else
        {
            UIGlobal.instance.BtnCreateAccount.GetComponent<Button>().interactable = false;
            Color alphaText = colorTextBtnCreate;
            alphaText.a = 0.4f;
            UIGlobal.instance.BtnCreateAccount.GetComponentInChildren<TextMeshProUGUI>().color = alphaText;
        }
    }
}
