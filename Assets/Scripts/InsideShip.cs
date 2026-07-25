using System;
using UnityEngine;

public class InsideShip : MonoBehaviour
{
    public static event Action<PlayerStats, bool> SafetyChanged;

    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.isInsideShip = true;
            SafetyChanged?.Invoke(stats, true);
            Debug.Log("player entered");
        }
    }

    void OnTriggerExit(Collider other)
    {
        var stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.isInsideShip = false;
            SafetyChanged?.Invoke(stats, false);
            Debug.Log("player exited");
        }
    }
}
