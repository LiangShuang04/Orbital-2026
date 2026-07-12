using System.Collections.Generic;
using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    public static class CentralCombatVisuals
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            var key = $"{name}_{ColorUtility.ToHtmlStringRGBA(color)}_{metallic:0.00}_{smoothness:0.00}";

            if (Materials.TryGetValue(key, out var existing))
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = key,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            Materials[key] = material;
            return material;
        }

        public static Transform BuildEnemyBody(Transform parent, CentralCombatEnemyConfig config)
        {
            var root = new GameObject("Visual").transform;
            root.SetParent(parent, false);

            var bodyMat = CreateMaterial($"{config.displayName}_Body", config.primaryColor, 0.18f, 0.22f);
            var accentMat = CreateMaterial($"{config.displayName}_Accent", config.accentColor, 0.08f, 0.3f);
            var darkMat = CreateMaterial($"{config.displayName}_Dark", new Color(0.035f, 0.04f, 0.04f, 1f), 0.22f, 0.18f);

            var height = config.bodyHeight;
            var radius = config.bodyRadius;

            AddPrimitive(root, "Torso", PrimitiveType.Capsule, new Vector3(0f, height * 0.52f, 0f), new Vector3(radius * 1.8f, height * 0.46f, radius * 1.25f), bodyMat);
            AddPrimitive(root, "Head", PrimitiveType.Sphere, new Vector3(0f, height * 0.98f, 0f), new Vector3(radius * 1.08f, radius * 0.92f, radius * 1.08f), darkMat);
            AddPrimitive(root, "ChestPlate", PrimitiveType.Cube, new Vector3(0f, height * 0.63f, radius * 0.68f), new Vector3(radius * 1.7f, height * 0.18f, radius * 0.2f), accentMat);
            AddPrimitive(root, "LeftArm", PrimitiveType.Capsule, new Vector3(-radius * 1.25f, height * 0.52f, 0f), new Vector3(radius * 0.45f, height * 0.36f, radius * 0.45f), bodyMat);
            AddPrimitive(root, "RightArm", PrimitiveType.Capsule, new Vector3(radius * 1.25f, height * 0.52f, 0f), new Vector3(radius * 0.45f, height * 0.36f, radius * 0.45f), bodyMat);
            AddPrimitive(root, "LeftLeg", PrimitiveType.Capsule, new Vector3(-radius * 0.45f, height * 0.2f, 0f), new Vector3(radius * 0.5f, height * 0.36f, radius * 0.5f), darkMat);
            AddPrimitive(root, "RightLeg", PrimitiveType.Capsule, new Vector3(radius * 0.45f, height * 0.2f, 0f), new Vector3(radius * 0.5f, height * 0.36f, radius * 0.5f), darkMat);

            if (config.ranged)
            {
                AddPrimitive(root, "Rifle", PrimitiveType.Cube, new Vector3(radius * 0.72f, height * 0.58f, radius * 1.2f), new Vector3(radius * 0.32f, radius * 0.32f, radius * 2.3f), accentMat);
            }

            return root;
        }

        public static Transform BuildProjectileMuzzle(Transform parent, CentralCombatEnemyConfig config)
        {
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(parent, false);
            muzzle.localPosition = new Vector3(config.bodyRadius * 0.72f, config.bodyHeight * 0.64f, config.bodyRadius * 2.25f);
            return muzzle;
        }

        public static GameObject CreateProjectileVisual()
        {
            var shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shot.name = "EnemyProjectile";
            shot.transform.localScale = Vector3.one * 0.18f;
            var collider = shot.GetComponent<Collider>();

            if (collider != null)
                Object.Destroy(collider);

            return shot;
        }

        private static GameObject AddPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = localScale;

            var renderer = obj.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = material;

            return obj;
        }
    }
}
