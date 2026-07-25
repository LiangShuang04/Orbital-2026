using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    public static event Action<EnemyHealth> AnyEnemyDied;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnDied;

    void Awake()
    {
        CurrentHealth = stats != null ? stats.maxHealth : 50f;
    }

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
