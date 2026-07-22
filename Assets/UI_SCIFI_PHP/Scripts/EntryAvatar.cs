using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntryAvatar : MonoBehaviour
{
    public SelectAvatar select;
    public Button btn;
    public GameObject SelectedObj;
    public Image AvatarImg;
    public int idAvatar;

    private void Start()
    {

        btn.onClick.AddListener(delegate () { select.SelectedImage(idAvatar); });

    }
}
