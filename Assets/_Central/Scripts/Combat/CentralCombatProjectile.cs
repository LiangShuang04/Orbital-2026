using Akila.FPSFramework;
using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralCombatProjectile : MonoBehaviour
    {
        private GameObject source;
        private float damage;
        private float speed;
        private float radius;
        private float remainingLife;
        private Vector3 previousPosition;
        private bool spent;

        public void Configure(GameObject sourceObject, float damageValue, float speedValue, float radiusValue, float lifeTime, Color color)
        {
            source = sourceObject;
            damage = damageValue;
            speed = speedValue;
            radius = radiusValue;
            remainingLife = lifeTime;
            previousPosition = transform.position;

            var renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = CentralCombatVisuals.CreateMaterial("EnemyProjectile", color, 0.05f, 0.35f);
            }
        }

        private void Update()
        {
            if (spent || Time.timeScale <= 0f || FPSFrameworkCore.IsPaused)
                return;

            remainingLife -= Time.deltaTime;

            if (remainingLife <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var nextPosition = transform.position + transform.forward * speed * Time.deltaTime;
            var direction = nextPosition - previousPosition;
            var distance = direction.magnitude;

            if (distance > 0f && Physics.SphereCast(previousPosition, radius, direction.normalized, out var hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Hit(hit);
                return;
            }

            transform.position = nextPosition;
            previousPosition = transform.position;
        }

        private void Hit(RaycastHit hit)
        {
            if (Time.timeScale <= 0f || FPSFrameworkCore.IsPaused)
                return;

            spent = true;

            var damageable = hit.transform.GetComponentInParent<IDamageable>();

            if (damageable != null && damageable.gameObject != source && damageable.Health > 0f)
            {
                damageable.Damage(damage, source);
            }

            Destroy(gameObject);
        }
    }
}
