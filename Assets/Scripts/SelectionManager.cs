using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    public GameObject interaction_Info_UI;
    TMP_Text interactionText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interactionText = interaction_Info_UI.GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray,out hit)){
            var selectionTransform = hit.transform;
            if(selectionTransform.GetComponent<InteractableObject>()){

                interactionText.text = selectionTransform.GetComponent<InteractableObject>().getItemName();
                interaction_Info_UI.SetActive(true);
            } else {
                interaction_Info_UI.SetActive(false);
            }
        }
    }
}
