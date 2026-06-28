using System;
using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    [Serializable]
    public sealed class CentralCombatEnemyConfig
    {
        public CentralEnemyArchetype archetype;
        public string displayName;
        public float maxHealth;
        public float moveSpeed;
        public float acceleration;
        public float detectionRange;
        public float sightAngle;
        public float attackRange;
        public float attackDamage;
        public float attackCooldown;
        public float attackWindup;
        public float repositionDistance;
        public float bodyHeight;
        public float bodyRadius;
        public bool ranged;
        public Color primaryColor;
        public Color accentColor;

        public static CentralCombatEnemyConfig Rusher()
        {
            return new CentralCombatEnemyConfig
            {
                archetype = CentralEnemyArchetype.Rusher,
                displayName = "Rusher",
                maxHealth = 45f,
                moveSpeed = 5.8f,
                acceleration = 14f,
                detectionRange = 24f,
                sightAngle = 145f,
                attackRange = 1.75f,
                attackDamage = 14f,
                attackCooldown = 1.15f,
                attackWindup = 0.28f,
                repositionDistance = 4f,
                bodyHeight = 1.75f,
                bodyRadius = 0.34f,
                ranged = false,
                primaryColor = new Color(0.17f, 0.18f, 0.18f, 1f),
                accentColor = new Color(0.65f, 0.58f, 0.38f, 1f)
            };
        }

        public static CentralCombatEnemyConfig Heavy()
        {
            return new CentralCombatEnemyConfig
            {
                archetype = CentralEnemyArchetype.Heavy,
                displayName = "Heavy",
                maxHealth = 160f,
                moveSpeed = 2.55f,
                acceleration = 7f,
                detectionRange = 22f,
                sightAngle = 130f,
                attackRange = 2.2f,
                attackDamage = 34f,
                attackCooldown = 2.1f,
                attackWindup = 0.85f,
                repositionDistance = 3.2f,
                bodyHeight = 2.25f,
                bodyRadius = 0.58f,
                ranged = false,
                primaryColor = new Color(0.13f, 0.13f, 0.13f, 1f),
                accentColor = new Color(0.46f, 0.18f, 0.12f, 1f)
            };
        }

        public static CentralCombatEnemyConfig Shooter()
        {
            return new CentralCombatEnemyConfig
            {
                archetype = CentralEnemyArchetype.Shooter,
                displayName = "Shooter",
                maxHealth = 75f,
                moveSpeed = 3.35f,
                acceleration = 10f,
                detectionRange = 34f,
                sightAngle = 120f,
                attackRange = 18f,
                attackDamage = 13f,
                attackCooldown = 1.55f,
                attackWindup = 0.38f,
                repositionDistance = 8f,
                bodyHeight = 1.85f,
                bodyRadius = 0.38f,
                ranged = true,
                primaryColor = new Color(0.16f, 0.17f, 0.18f, 1f),
                accentColor = new Color(0.25f, 0.41f, 0.56f, 1f)
            };
        }

        public static CentralCombatEnemyConfig Stalker()
        {
            return new CentralCombatEnemyConfig
            {
                archetype = CentralEnemyArchetype.Stalker,
                displayName = "Stalker",
                maxHealth = 55f,
                moveSpeed = 4.4f,
                acceleration = 12f,
                detectionRange = 19f,
                sightAngle = 105f,
                attackRange = 1.55f,
                attackDamage = 22f,
                attackCooldown = 1.35f,
                attackWindup = 0.18f,
                repositionDistance = 6f,
                bodyHeight = 1.65f,
                bodyRadius = 0.3f,
                ranged = false,
                primaryColor = new Color(0.07f, 0.08f, 0.08f, 1f),
                accentColor = new Color(0.42f, 0.36f, 0.54f, 1f)
            };
        }
    }
}
