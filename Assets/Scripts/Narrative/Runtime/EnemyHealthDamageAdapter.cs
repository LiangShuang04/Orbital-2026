using Akila.FPSFramework;
using UnityEngine;
using UnityEngine.Events;

namespace DontDiePlease.Narrative.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyHealthDamageAdapter : MonoBehaviour, IDamageable
    {
        private readonly UnityEvent death = new UnityEvent();
        private EnemyHealth enemyHealth;

        GameObject IDamageable.gameObject => gameObject;
        Transform IDamageable.transform => transform;

        public bool isDamagableDisabled { get; set; }
        public bool allowDamageableEffects { get; set; }
        public bool DeadConfirmed { get; set; }
        public GameObject DamageSource { get; set; }
        public UnityEvent OnDeath => death;

        public float Health
        {
            get => enemyHealth != null ? enemyHealth.CurrentHealth : 0f;
            set
            {
                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    return;
                }

                var damage = enemyHealth.CurrentHealth - value;

                if (damage > 0f)
                {
                    enemyHealth.TakeDamage(damage);
                }
            }
        }

        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        private void OnEnable()
        {
            enemyHealth.OnDied += HandleDeath;
        }

        private void OnDisable()
        {
            enemyHealth.OnDied -= HandleDeath;
        }

        public void Damage(float amount, GameObject damageSource)
        {
            if (isDamagableDisabled || enemyHealth.IsDead || amount <= 0f)
            {
                return;
            }

            DamageSource = damageSource;
            enemyHealth.TakeDamage(amount);
        }

        private void HandleDeath()
        {
            DeadConfirmed = true;
            death.Invoke();
        }
    }
}
