using System.Collections.Generic;
using UnityEngine;

public class ControlAnimateLights : MonoBehaviour
{
    public List<ButtonLamp> controls;
    public float interval = 1f;
    public float startTime = 1f;

    private int result;

    void Start()
    {
        InvokeRepeating("Run", startTime, interval);
    }

    void Run()
    {
        foreach (var item in controls)
        {
            result = Random.Range(0, 2);
            item.on = intToBool(result);
        }
    }

    public static bool intToBool(int Number)
    {
        return Number != 0;
    }
}
