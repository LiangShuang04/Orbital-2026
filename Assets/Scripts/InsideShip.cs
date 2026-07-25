using UnityEngine;

public class InsideShip : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.isInsideShip = true;
            Debug.Log("player entered");
        }
    }

    void OnTriggerExit(Collider other)
    {
        var stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.isInsideShip = false;
            Debug.Log("player exited");
        }
    }
}
