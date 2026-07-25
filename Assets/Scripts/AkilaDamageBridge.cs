using Akila.FPSFramework;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lets Akila firearms damage the player by routing hits into our own PlayerStats
/// Akila guns look for an IDamageable on whatever they hit, this implements that
/// interface and forwards the damage to the survival health, so there is one
/// health value instead of two and no need for Akila's own Damageable component
/// Put this on the same object as PlayerStats (the player root)
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class AkilaDamageBridge : MonoBehaviour, IDamageable
{
    private PlayerStats stats;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    // --- IDamageable interface (gameObject/transform come from MonoBehaviour) ---
    public bool isDamagableDisabled { get; set; }
    public bool allowDamageableEffects { get; set; }
    public bool DeadConfirmed { get; set; }
    public GameObject DamageSource { get; set; }

    [SerializeField] private UnityEvent onDeath = new UnityEvent();
    public UnityEvent OnDeath => onDeath;

    // Akila reads/writes Health through the interface, mirror it onto PlayerStats
    public float Health
    {
        get => stats != null ? stats.currentHealth : 0f;
        set { if (stats != null) stats.currentHealth = value; }
    }

    // called by Akila firearms/projectiles when they hit the player
    public void Damage(float amount, GameObject damageSource)
    {
        if (isDamagableDisabled || stats == null) return;

        DamageSource = damageSource;
        stats.TakeDamage(amount); // one source of truth, drives the HUD + death screen

        if (stats.IsDead && !DeadConfirmed)
        {
            DeadConfirmed = true;
            onDeath.Invoke();
        }
    }
}
