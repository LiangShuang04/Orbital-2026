using UnityEngine;

public class InsideShip : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.isInsideShip = true;
            Debug.Log("player entered");
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {   
            stats.isInsideShip = false;
            Debug.Log("player exited");
        }
    }
}
