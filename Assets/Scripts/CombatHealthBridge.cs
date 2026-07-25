using UnityEngine;
using AkilaDamageable = Akila.FPSFramework.Damageable;

/// <summary>
/// Unifies the two player health systems. Enemy fire hits Akila's Damageable;
/// this watches that health and forwards any drop into PlayerStats.TakeDamage,
/// so combat damage drains the survival health bar shown by SurvivalHUD.
///
/// Akila's Damageable auto-heals, so its value bounces back after a hit — we only
/// forward decreases, which gives a survival-style "no free regen" health bar.
/// Added to the player by the combat bootstrapper.
/// </summary>
public class CombatHealthBridge : MonoBehaviour
{
    private AkilaDamageable damageable;
    private PlayerStats stats;
    private float lastHealth;
    private bool ready;

    private void Update()
    {
        if (!ready)
        {
            // The bootstrapper attaches these over a couple of frames, so keep
            // looking until both are present, then take the current health as the baseline.
            if (damageable == null) damageable = GetComponentInChildren<AkilaDamageable>(true);
            if (stats == null) stats = GetComponent<PlayerStats>() ?? GetComponentInChildren<PlayerStats>();
            if (damageable == null || stats == null) return;

            lastHealth = damageable.health;
            ready = true;
            return;
        }

        float current = damageable.health;
        if (current < lastHealth - 0.01f)
            stats.TakeDamage(lastHealth - current); // forward the combat damage to the survival bar

        lastHealth = current;
    }
}
