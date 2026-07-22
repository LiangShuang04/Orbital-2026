using System;
using UnityEngine;

/// <summary>
/// Tracks enemy health, weapons call TakeDamage and the controller listens for OnDied
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    /// <summary>Raised once when health reaches zero</summary>
    public event Action OnDied;

    void Awake()
    {
        CurrentHealth = stats != null ? stats.maxHealth : 50f;
    }

    /// <summary>Apply damage. Safe to call repeatedly; only dies once</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            OnDied?.Invoke();
        }
    }
}