using System;
using UnityEngine;

/// <summary>
/// Tracks an enemy's health and raises an event when it dies. Kept separate from
/// EnemyController (single responsibility): anything that deals damage talks to
/// this, and the controller just listens for OnDied to switch to its Dead state.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    public static event Action<EnemyHealth> AnyEnemyDied;

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
            AnyEnemyDied?.Invoke(this);
        }
    }
}
