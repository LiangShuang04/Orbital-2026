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
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            healthbar.value = playerStats.currentHealth / playerStats.maxHealth;
            oxygenbar.value = playerStats.currentOxygen / playerStats.maxOxygen;
            saturationbar.value = playerStats.currentSaturation / playerStats.maxSaturation;
            toxicitybar.value = playerStats.currentToxicity / playerStats.maxToxicity;
        }
    }
}

