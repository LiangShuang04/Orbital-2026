using Akila.FPSFramework;
using UnityEngine;
using UnityEngine.AI;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CentralCombatEnemy))]
    public sealed class CentralCombatEnemyAI : MonoBehaviour
    {
        private enum EnemyState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Reposition,
            Dead
        }

        [SerializeField] private float patrolRadius = 7f;
        [SerializeField] private float loseTargetTime = 4.5f;
        [SerializeField] private float meleeHitRadius = 1.15f;
        [SerializeField] private float projectileSpeed = 28f;
        [SerializeField] private float projectileRadius = 0.13f;
        [SerializeField] private float projectileLife = 4f;

        private CentralCombatEnemy enemy;
        private CentralCombatEnemyConfig config;
        private NavMeshAgent agent;
        private Transform target;
        private Transform muzzle;
        private EnemyState state;
        private Vector3 postPos;
        private Vector3 patrolTarget;
        private float attackTimer;
        private float windupTimer;
        private float loseTimer;
        private float patrolTimer;
        private bool attackResolved;

        public bool HasTarget => target != null;

        private void Awake()
        {
            enemy = GetComponent<CentralCombatEnemy>();
            agent = GetComponent<NavMeshAgent>();
        }

        public void Configure(CentralCombatEnemy value, Transform targetValue, Transform muzzleValue)
        {
            enemy = value;
            config = value.Config;
            target = targetValue;
            muzzle = muzzleValue;
            postPos = transform.position;
            patrolTarget = postPos;
            state = EnemyState.Idle;
            attackTimer = Random.Range(0.2f, 0.8f);
            value.BindAI(this);
        }

        public void SetTarget(Transform value)
        {
            target = value;
        }

        public void MarkDead()
        {
            state = EnemyState.Dead;

            if (agent != null && agent.enabled)
                agent.ResetPath();
        }

        private void Update()
        {
            if (state == EnemyState.Dead || enemy == null || enemy.IsDead)
                return;

            if (target == null)
            {
                target = FindPlayerTarget();

                if (target == null)
                    return;
            }

            attackTimer -= Time.deltaTime;

            switch (state)
            {
                case EnemyState.Idle:
                    TickIdle();
                    break;
                case EnemyState.Patrol:
                    TickPatrol();
                    break;
                case EnemyState.Chase:
                    TickChase();
                    break;
                case EnemyState.Attack:
                    TickAttack();
                    break;
                case EnemyState.Reposition:
                    TickReposition();
                    break;
            }
        }

        private void TickIdle()
        {
            if (CanDetectTarget())
            {
                state = EnemyState.Chase;
                return;
            }

            patrolTimer -= Time.deltaTime;

            if (patrolTimer <= 0f)
            {
                patrolTimer = Random.Range(2.4f, 5.2f);
                PickPatrolDestination();
                state = EnemyState.Patrol;
            }
        }

        private void TickPatrol()
        {
            if (CanDetectTarget())
            {
                state = EnemyState.Chase;
                return;
            }

            MoveTo(patrolTarget);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.35f)
            {
                state = EnemyState.Idle;
            }
        }

        private void TickChase()
        {
            var distance = Vector3.Distance(transform.position, target.position);
            var canSee = CanSeeTarget();

            if (canSee)
            {
                loseTimer = 0f;
            }
            else
            {
                loseTimer += Time.deltaTime;
            }

            if (loseTimer >= loseTargetTime)
            {
                MoveTo(postPos);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.35f)
                {
                    loseTimer = 0f;
                    state = EnemyState.Idle;
                }

                return;
            }

            if (distance <= config.attackRange && canSee && attackTimer <= 0f)
            {
                BeginAttack();
                return;
            }

            if (config.ranged && distance < Mathf.Max(3.8f, config.attackRange * 0.45f))
            {
                BeginReposition();
                return;
            }

            MoveTo(target.position);
        }

        private void TickAttack()
        {
            FaceTarget(10f);

            windupTimer -= Time.deltaTime;

            if (!attackResolved && windupTimer <= 0f)
            {
                ResolveAttack();
            }

            if (windupTimer <= -0.22f)
            {
                attackTimer = config.attackCooldown;
                state = config.ranged ? EnemyState.Reposition : EnemyState.Chase;
            }
        }

        private void TickReposition()
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.35f)
            {
                state = EnemyState.Chase;
                return;
            }

            if (!CanSeeTarget())
            {
                MoveTo(target.position);
            }
        }

        private void BeginAttack()
        {
            if (agent.enabled)
                agent.ResetPath();

            attackResolved = false;
            windupTimer = config.attackWindup;
            state = EnemyState.Attack;
        }

        private void ResolveAttack()
        {
            attackResolved = true;

            if (config.ranged)
            {
                FireProjectile();
                return;
            }

            var origin = transform.position + transform.forward * Mathf.Max(0.6f, config.bodyRadius * 1.9f) + Vector3.up * (config.bodyHeight * 0.48f);
            var hits = Physics.OverlapSphere(origin, meleeHitRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null || damageable.gameObject == gameObject || damageable.Health <= 0f)
                    continue;

                damageable.Damage(config.attackDamage, gameObject);
                return;
            }
        }

        private void FireProjectile()
        {
            var origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * (config.bodyHeight * 0.65f) + transform.forward * 0.9f;
            var aimPoint = GetTargetAimPoint();
            var direction = (aimPoint - origin).normalized;

            if (direction.sqrMagnitude <= 0.01f)
                direction = transform.forward;

            var obj = CentralCombatVisuals.CreateProjectileVisual();
            obj.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, Vector3.up));
            var projectile = obj.AddComponent<CentralCombatProjectile>();
            projectile.Configure(gameObject, config.attackDamage, projectileSpeed, projectileRadius, projectileLife, config.accentColor);
        }

        private void BeginReposition()
        {
            var away = (transform.position - target.position).normalized;

            if (away.sqrMagnitude <= 0.01f)
                away = -transform.forward;

            var side = Vector3.Cross(Vector3.up, away).normalized * (Random.value > 0.5f ? 1f : -1f);
            var desired = transform.position + away * config.repositionDistance + side * Random.Range(2f, 5f);

            if (NavMesh.SamplePosition(desired, out var hit, config.repositionDistance + 3f, NavMesh.AllAreas))
            {
                MoveTo(hit.position);
                state = EnemyState.Reposition;
                return;
            }

            state = EnemyState.Chase;
        }

        private void PickPatrolDestination()
        {
            for (var idx = 0; idx < 8; idx++)
            {
                var random = Random.insideUnitCircle * patrolRadius;
                var candidate = postPos + new Vector3(random.x, 0f, random.y);

                if (NavMesh.SamplePosition(candidate, out var hit, 3.5f, NavMesh.AllAreas))
                {
                    patrolTarget = hit.position;
                    return;
                }
            }

            patrolTarget = postPos;
        }

        private void MoveTo(Vector3 dest)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.SetDestination(dest);
        }

        private bool CanDetectTarget()
        {
            if (target == null || config == null)
                return false;

            var offset = target.position - transform.position;
            var distance = offset.magnitude;

            if (distance > config.detectionRange)
                return false;

            var flatOffset = new Vector3(offset.x, 0f, offset.z);

            if (flatOffset.sqrMagnitude > 0.01f)
            {
                var angle = Vector3.Angle(transform.forward, flatOffset.normalized);

                if (angle > config.sightAngle * 0.5f)
                    return false;
            }

            return CanSeeTarget();
        }

        private bool CanSeeTarget()
        {
            if (target == null)
                return false;

            var origin = transform.position + Vector3.up * Mathf.Max(1.1f, config.bodyHeight * 0.82f);
            var aimPoint = GetTargetAimPoint();
            var direction = aimPoint - origin;
            var distance = direction.magnitude;

            if (distance <= 0.01f)
                return true;

            if (!Physics.Raycast(origin, direction.normalized, out var hit, distance + 0.25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return true;

            return hit.transform == target || hit.transform.IsChildOf(target) || hit.transform.GetComponentInParent<IDamageable>() != null;
        }

        private void FaceTarget(float speed)
        {
            if (target == null)
                return;

            var direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
                return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized, Vector3.up), Time.deltaTime * speed);
        }

        private Vector3 GetTargetAimPoint()
        {
            var damageable = target.GetComponentInParent<IDamageable>();

            if (damageable != null)
                return damageable.transform.position + Vector3.up * 1.35f;

            return target.position + Vector3.up * 1.35f;
        }

        private static Transform FindPlayerTarget()
        {
            var targets = Object.FindObjectsByType<Damageable>(FindObjectsInactive.Exclude);

            foreach (var hp in targets)
            {
                if (hp != null && hp.type == DamagableType.Player && hp.health > 0f)
                    return hp.transform;
            }

            var characters = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);

            foreach (var behaviour in characters)
            {
                if (behaviour is ICharacterController)
                    return behaviour.transform;
            }

            return null;
        }
    }
}
