using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUDDisplay : MonoBehaviour
    {
        [Header("Stat Bars")]
        [SerializeField] private Slider healthbar;
        [SerializeField] private Slider oxygenbar;
        [SerializeField] private Slider saturationbar;
        [SerializeField] private Slider toxicitybar;

        [Header("Player Reference")]
        [SerializeField] private PlayerStats playerStats;

        void Update()
        {
            if (playerStats == null) return;

            healthbar.value = playerStats.currentHealth / playerStats.maxHealth;
            oxygenbar.value = playerStats.currentOxygen / playerStats.maxOxygen;
            saturationbar.value = playerStats.currentSaturation / playerStats.maxSaturation;
            toxicitybar.value = playerStats.currentToxicity / playerStats.maxToxicity;
        }
    }
}
