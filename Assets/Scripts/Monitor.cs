using UnityEngine;

public class Monitor : MonoBehaviour
{
    [Range(0, 15)]
    public int screenImage;

    public bool on;
    public Transform screen;

    Renderer rend;

    void Start()
    {
        rend = screen.GetComponent<Renderer>();
    }

    void Update()
    {
        if (on)
        {
            rend.material.SetColor("_EmissionColor", new Color(1.517f, 1.517f, 1.517f, 1f));
            rend.material.SetTextureOffset("_MainTex", new Vector2(0.25f * (screenImage % 4), 0.25f * (screenImage / 4)));
        }
        else
        {
            rend.material.SetColor("_EmissionColor", new Color(0.3f, 0.3f, 0.3f, 0.3f));
            rend.material.SetTextureOffset("_MainTex", new Vector2(0.25f * (8 % 4), 0.25f * (8 / 4)));
        }
    }
}
