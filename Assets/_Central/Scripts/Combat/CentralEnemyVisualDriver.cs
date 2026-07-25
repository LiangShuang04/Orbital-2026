using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralEnemyVisualDriver : MonoBehaviour
    {
        private Animator animator;
        private int idleState;
        private int moveState;
        private int attackState;
        private int deathState;
        private int currentState;
        private float attackLock;
        private float actionEndsAt;
        private Vector3 previousPosition;
        private bool dead;

        public void Configure(Animator value, CentralEnemyVisualCatalog.Entry entry)
        {
            animator = value;
            idleState = FindState(entry.idleState);
            moveState = FindState(entry.moveState);
            attackState = FindState(entry.attackState);
            deathState = FindState(entry.deathState);
            attackLock = Mathf.Max(0.1f, entry.attackLock);
            previousPosition = transform.position;
            dead = false;

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            Play(idleState, 0f);
        }

        public void SetMoving(bool value)
        {
            if (dead || Time.time < actionEndsAt)
                return;

            Play(value ? moveState : idleState, 0.12f);
        }

        public bool PlayAttack()
        {
            if (dead || attackState == 0)
                return false;

            actionEndsAt = Time.time + attackLock;
            Play(attackState, 0.08f);
            return true;
        }

        public bool PlayDeath()
        {
            if (dead)
                return deathState != 0;

            dead = true;

            if (deathState == 0)
                return false;

            Play(deathState, 0.08f);
            return true;
        }

        private void Update()
        {
            var delta = transform.position - previousPosition;
            previousPosition = transform.position;
            SetMoving(delta.sqrMagnitude > 0.000001f);
        }

        private int FindState(string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return 0;

            var shortHash = Animator.StringToHash(stateName);

            if (animator.HasState(0, shortHash))
                return shortHash;

            var fullHash = Animator.StringToHash($"Base Layer.{stateName}");
            return animator.HasState(0, fullHash) ? fullHash : 0;
        }

        private void Play(int stateHash, float fade)
        {
            if (animator == null || stateHash == 0 || stateHash == currentState)
                return;

            currentState = stateHash;
            animator.CrossFadeInFixedTime(stateHash, fade, 0);
        }
    }
}
