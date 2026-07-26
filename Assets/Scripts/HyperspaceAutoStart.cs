using UnityEngine;

/// <summary>
/// Turns the Vattalus hyperspace effect on automatically, for use as a menu
/// background. The shell renderer is disabled by default and only shows once
/// StartTransitionIn is called, so this does that on scene load
/// Put this on the same object as HyperspaceEffectController
/// </summary>
[RequireComponent(typeof(HyperspaceEffectController))]
public class HyperspaceAutoStart : MonoBehaviour
{
    [Tooltip("Snap straight into hyperspace instead of animating the warp-in")]
    [SerializeField] private bool instant = false;

    void Start()
    {
        GetComponent<HyperspaceEffectController>().StartTransitionIn(instant);
    }
}
