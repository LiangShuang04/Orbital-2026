using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SelectAvatar : MonoBehaviour
{
    public GameObject[] ListAvatar;
    public GameObject DefaultSelected;
    public Sprite avatarSelected;

    public Image AvatarAccount;

    public void SelectedImage(int id)
    {
        DefaultSelected.GetComponent<EntryAvatar>().SelectedObj.SetActive(false);
        DefaultSelected = ListAvatar[id];
        DefaultSelected.GetComponent<EntryAvatar>().SelectedObj.SetActive(true);
        avatarSelected = UIGlobal.instance.AllAvatar[id];
        UIGlobal.instance.AvatarID = id;
    }

    public void SaveSelect()
    {
        AvatarAccount.sprite = avatarSelected;
    }
}
