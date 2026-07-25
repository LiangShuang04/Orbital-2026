using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    public List<ListKeyBindings> listKey = new List<ListKeyBindings>(); //List all keys keyboard
    public Button[] AllMenuButtons;
    public GameObject[] AllPanels;
    public Color SelectedText;
    public Color DeselectedText;
    public TMP_Dropdown QualitySelect;

    bool Rebind = false;
    bool Replace = false;
    public int idCurrentKey;
    public int idReplaceKey;
    KeyCode kcSelect;

    //
    void Start()
    {
        QualitySettings.SetQualityLevel(3, true);//APPLY MEDIUM GRAPHICS QUALITY TO START

        //FOR BUTTON MENU
        //SELECT THE FIRST BUTTON AND DESELECT THE OTHERS BUTTONS
        for (int i = 0; i < AllMenuButtons.Length; i++)
        {
            if(i == 0)
            {
                AllMenuButtons[i].interactable = false;
                AllMenuButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = SelectedText;
            }
            else
            {
                AllMenuButtons[i].interactable = true;
                AllMenuButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = DeselectedText;
            }
        }

        //SHOW THE FIRST PANEL AND HIDE THE OTHERS PANELS
        for (int i = 0; i < AllPanels.Length; i++)
        {
            if (i == 0)
            {
                AllPanels[i].SetActive(true);
            }
            else
            {
                AllPanels[i].SetActive(false);
            }
        }

        for(int i = 0; i < listKey.Count; i++)
        {
            listKey[i].id = i;
            listKey[i].TitleKeyText.text = listKey[i].name;
            listKey[i].BtnKeyText.text = listKey[i].kc.ToString();
        }
    }

    //LAUNCHED AFTER CLICKING THE BUTTON MENU FOR CHANGER IN "KEYBINDING", "GRAPHICS", or "SOUND"
    public void SwitchPanel(GameObject obj)
    {
        for (int i = 0; i < AllMenuButtons.Length; i++)
        {
            if (obj == AllMenuButtons[i].gameObject)
            {
                AllPanels[i].SetActive(true);
                AllMenuButtons[i].interactable = false;
                AllMenuButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = SelectedText;
            }
            else
            {
                AllPanels[i].SetActive(false);
                AllMenuButtons[i].interactable = true;
                AllMenuButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = DeselectedText;
            }
        }
    }

    void Update()
    {
        if (Rebind)
        {
            UIGlobal.instance.Frames[2].SetActive(true);//SHOW THE PANEL FOR PRESSED THE NEW KEY

            if (Input.anyKeyDown)//IF THE ANY KEYS IS PRESSED
            {
                foreach (KeyCode kc in Enum.GetValues( typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kc))
                    {
                        if (kc != KeyCode.Escape)//IF THE KEY PRESSED IS DIFFERENT TO THE "ESCAPE" KEY, SO CONTINUE
                        {
                            foreach (ListKeyBindings keyInList in listKey)//CHECK IF THE PRESSED KEY, EXIST IN THE LIST KEYS, FOR REPLACE
                            {
                                if (kc == keyInList.kc)//IF THE PRESSED KEY EXIST IN THE LIST KEYS
                                {
                                    Replace = true;
                                    idReplaceKey = keyInList.id;
                                    UIGlobal.instance.AllText[18].text = "";
                                    UIGlobal.instance.Frames[2].SetActive(false);//HIDE THE PANEL FOR PRESSED THE NEW KEY
                                    UIGlobal.instance.Frames[3].SetActive(true);//SHOW THE PANEL FOR REPLACE THE EXISTED KEY
                                    UIGlobal.instance.AllText[19].text = keyInList.name + " : " + kc.ToString();
                                    kcSelect = kc;
                                    Rebind = false;
                                }
                            }
                            if (!Replace)
                            {
                                listKey[idCurrentKey].BtnKeyText.text = kc.ToString();
                                listKey[idCurrentKey].kc = kc;
                                Rebind = false;
                            }
                        }
                        else
                        {
                            CancelKeyBinding();
                            Rebind = false;
                        }
                    }
                }
            }
        }
        else
        {
            UIGlobal.instance.AllText[18].text = "";
            UIGlobal.instance.Frames[2].SetActive(false);
        }
    }

    //LAUNCHED AFTER CLICKING A KEY IN THE MENU
    public void StartSetCustomKey(int idKey)
    {
        idCurrentKey = idKey;
        UIGlobal.instance.AllText[18].text = listKey[idCurrentKey].name;
        Rebind = true;
    }

    //LAUNCH AFTER CLICKING A BUTTON FOR CONFIRM THE REPLACED THE NEW KEY
    public void ConfirmReplaceKey()
    {
        UIGlobal.instance.AllText[19].text = "";
        UIGlobal.instance.Frames[3].SetActive(false);
        listKey[idReplaceKey].kc = KeyCode.None;
        listKey[idReplaceKey].BtnKeyText.text = "None";
        listKey[idCurrentKey].kc = kcSelect;
        listKey[idCurrentKey].BtnKeyText.text = listKey[idCurrentKey].kc.ToString();
        Replace = false;
    }

    //LAUNCHED AFTER CLICKING ESCAPE KEY
    void CancelKeyBinding()
    {
        UIGlobal.instance.AllText[18].text = "";
        UIGlobal.instance.AllText[19].text = "";
        UIGlobal.instance.Frames[2].SetActive(false);
        UIGlobal.instance.Frames[3].SetActive(false);
        Replace = false;
    }

    //LAUNCH AFTER CLICKING A BUTTON FOR CANCELED THE REPLACED THE NEW KEY
    public void CancelReplaceKey()
    {
        UIGlobal.instance.AllText[19].text = "";
        UIGlobal.instance.Frames[3].SetActive(false);
        Replace = false;
    }

    //LAUNCH AFTER CHANGING THE QUALITY IN OPTION PANEL
    public void ChangeQuality()
    {
        QualitySettings.SetQualityLevel(QualitySelect.value, true);
    }
}

[Serializable]
public class ListKeyBindings
{
    public int id;
    public string name;
    public TextMeshProUGUI TitleKeyText;
    public TextMeshProUGUI BtnKeyText;
    public Button BtnKey;
    public KeyCode kc;
}
