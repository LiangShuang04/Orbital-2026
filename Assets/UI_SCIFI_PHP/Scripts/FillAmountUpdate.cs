using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FillAmountUpdate : MonoBehaviour
{
    public Image img;
    public TextMeshProUGUI textAmount;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(img.fillAmount < 1)
        {
            textAmount.text = (img.fillAmount * 100f).ToString("0")+"%";
        }
    }
}
