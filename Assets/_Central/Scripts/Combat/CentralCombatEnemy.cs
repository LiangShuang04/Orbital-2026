using System;
using Akila.FPSFramework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Damageable))]
    [RequireComponent(typeof(Actor))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class CentralCombatEnemy : MonoBehaviour
    {
        [SerializeField] private float despawnDelay = 7f;

        private Damageable damageable;
        private Actor actor;
        private NavMeshAgent agent;
        private CentralCombatEnemyAI ai;
        private Collider[] colliders;
        private bool dead;

        public CentralCombatEnemyConfig Config { get; private set; }
        public bool IsDead => dead;
        public float Health => damageable != null ? damageable.health : 0f;
        public float MaxHealth => damageable != null ? damageable.maxHealth : 0f;
        public event Action<CentralCombatEnemy> Died;

        private void Awake()
        {
            EnsureFrameworkComponents();
        }

        private void OnEnable()
        {
            if (damageable != null)
                damageable.OnDeath.AddListener(Die);
        }

        private void OnDisable()
        {
            if (damageable != null)
                damageable.OnDeath.RemoveListener(Die);
        }

        private void Update()
        {
            if (!dead && damageable != null && damageable.health <= 0f)
                Die();
        }

        public void Configure(CentralCombatEnemyConfig config)
        {
            Config = config;
            gameObject.name = $"Enemy_{config.displayName}";

            EnsureFrameworkComponents();

            damageable.health = config.maxHealth;
            damageable.maxHealth = config.maxHealth;
            damageable.type = DamagableType.NPC;
            damageable.autoHeal = false;
            damageable.destroyOnDeath = false;
            damageable.destroyRoot = false;
            damageable.ragdolls = false;
            damageable.allowRespawn = false;
            damageable.allowDamageableEffects = false;
            damageable.DeadConfirmed = false;
            damageable.isDamagableDisabled = false;

            actor.actorName = config.displayName;
            actor.type = "Enemy";
            actor.teamId = 1;
            actor.respawnable = false;
            actor.playerCardActive = false;
            actor.playerUIEnabled = false;

            agent.speed = config.moveSpeed;
            agent.acceleration = config.acceleration;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = Mathf.Max(0.55f, config.attackRange * 0.72f);

            var capsule = GetComponent<CapsuleCollider>();
            capsule.height = config.bodyHeight;
            capsule.radius = config.bodyRadius;
            capsule.center = new Vector3(0f, config.bodyHeight * 0.5f, 0f);
            capsule.isTrigger = false;

            dead = false;
        }

        public void BindAI(CentralCombatEnemyAI value)
        {
            ai = value;
        }

        public void ReceiveExternalKill(GameObject source)
        {
            if (dead)
                return;

            damageable.DamageSource = source;
            Die();
        }

        private void EnsureFrameworkComponents()
        {
            damageable = GetComponent<Damageable>();
            actor = GetComponent<Actor>();
            agent = GetComponent<NavMeshAgent>();
            ai = GetComponent<CentralCombatEnemyAI>();
            colliders = GetComponentsInChildren<Collider>(true);

            if (damageable.onDeath == null)
                damageable.onDeath = new UnityEvent();
        }

        private void Die()
        {
            if (dead)
                return;

            dead = true;

            if (ai != null)
                ai.MarkDead();

            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
                agent.enabled = false;
            }

            foreach (var col in colliders)
            {
                if (col != null)
                    col.enabled = false;
            }

            var visuals = GetComponent<CentralEnemyVisualDriver>();

            if (visuals == null || !visuals.PlayDeath())
            {
                var visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
                visual.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-78f, 78f));
            }

            Died?.Invoke(this);
            Destroy(gameObject, despawnDelay);
        }
    }
}
