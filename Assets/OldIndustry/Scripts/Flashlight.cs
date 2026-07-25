using UnityEngine;
using System.Collections;

public class Flashlight : MonoBehaviour
{
    private Light flashLight;

    void Start()
    {
        flashLight = GetComponent<Light>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (Input.GetKeyUp(KeyCode.F))
            {
                flashLight.enabled = !flashLight.enabled;
            }
            else
            {
            flashLight.enabled = !flashLight.enabled;
            }
        }
    }
}