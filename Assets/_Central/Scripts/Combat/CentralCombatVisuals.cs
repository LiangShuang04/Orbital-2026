using System.Collections.Generic;
using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    public static class CentralCombatVisuals
    {
        private const string VisualCatalogPath = "Combat/CentralEnemyVisualCatalog";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<Material, Material> UrpMaterials = new Dictionary<Material, Material>();
        private static CentralEnemyVisualCatalog visualCatalog;

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
            if (TryGetVisual(config.archetype, out var entry))
                return BuildPrefabBody(parent, entry, config.bodyHeight, "Visual");

            return BuildFallbackBody(parent, config);
        }

        public static Transform ReplaceEnemyBody(Transform parent, CentralEnemyArchetype archetype, float bodyHeight)
        {
            if (!TryGetVisual(archetype, out var entry))
                return null;

            var renderers = parent.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            return BuildPrefabBody(parent, entry, bodyHeight, "RobotVisual");
        }

        private static Transform BuildFallbackBody(Transform parent, CentralCombatEnemyConfig config)
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

        private static Transform BuildPrefabBody(
            Transform parent,
            CentralEnemyVisualCatalog.Entry entry,
            float bodyHeight,
            string rootName)
        {
            var root = new GameObject(rootName).transform;
            root.SetParent(parent, false);

            var prefab = Object.Instantiate(entry.prefab, root, false);
            prefab.name = entry.prefab.name;
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;

            foreach (var col in prefab.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            ApplyUrpMaterials(prefab);
            FitToHeight(prefab.transform, root, bodyHeight);

            var animator = prefab.GetComponentInChildren<Animator>(true);
            var driver = parent.GetComponent<CentralEnemyVisualDriver>();

            if (driver == null)
                driver = parent.gameObject.AddComponent<CentralEnemyVisualDriver>();

            driver.Configure(animator, entry);
            return root;
        }

        private static void ApplyUrpMaterials(GameObject prefab)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                var sourceMaterials = renderer.sharedMaterials;
                var changed = false;

                for (var idx = 0; idx < sourceMaterials.Length; idx++)
                {
                    var compatible = GetUrpMaterial(sourceMaterials[idx]);

                    if (compatible == sourceMaterials[idx])
                        continue;

                    sourceMaterials[idx] = compatible;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = sourceMaterials;
            }
        }

        private static Material GetUrpMaterial(Material source)
        {
            if (source == null || source.shader == null ||
                source.shader.name.StartsWith("Universal Render Pipeline/"))
            {
                return source;
            }

            if (UrpMaterials.TryGetValue(source, out var existing))
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                return source;

            var mainProperty = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            var colorProperty = source.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            var mainTexture = source.HasProperty(mainProperty) ? source.GetTexture(mainProperty) : source.mainTexture;
            var mainScale = source.HasProperty(mainProperty) ? source.GetTextureScale(mainProperty) : source.mainTextureScale;
            var mainOffset = source.HasProperty(mainProperty) ? source.GetTextureOffset(mainProperty) : source.mainTextureOffset;
            var color = source.HasProperty(colorProperty) ? source.GetColor(colorProperty) : source.color;
            var material = new Material(shader)
            {
                name = $"{source.name}_URP",
                enableInstancing = source.enableInstancing,
                renderQueue = 2000
            };

            material.SetTexture("_BaseMap", mainTexture);
            material.mainTexture = mainTexture;
            material.SetTextureScale("_BaseMap", mainScale);
            material.SetTextureOffset("_BaseMap", mainOffset);
            material.SetColor("_BaseColor", color);
            CopyTexture(source, material, "_BumpMap", "_BumpMap");
            CopyTexture(source, material, "_MetallicGlossMap", "_MetallicGlossMap");
            CopyTexture(source, material, "_OcclusionMap", "_OcclusionMap");
            CopyTexture(source, material, "_EmissionMap", "_EmissionMap");
            CopyFloat(source, material, "_BumpScale", "_BumpScale");
            CopyFloat(source, material, "_Metallic", "_Metallic");
            CopyFloat(source, material, "_Glossiness", "_Smoothness");
            CopyFloat(source, material, "_Smoothness", "_Smoothness");
            CopyFloat(source, material, "_OcclusionStrength", "_OcclusionStrength");
            CopyColor(source, material, "_EmissionColor", "_EmissionColor");

            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");

            if (material.GetTexture("_BumpMap") != null)
                material.EnableKeyword("_NORMALMAP");

            if (material.GetTexture("_MetallicGlossMap") != null)
                material.EnableKeyword("_METALLICSPECGLOSSMAP");

            if (material.GetTexture("_OcclusionMap") != null)
                material.EnableKeyword("_OCCLUSIONMAP");

            if (material.GetTexture("_EmissionMap") != null ||
                material.GetColor("_EmissionColor").maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            ConfigureSurface(source, material);
            UrpMaterials[source] = material;
            return material;
        }

        private static void ConfigureSurface(Material source, Material material)
        {
            var mode = source.HasProperty("_Mode") ? Mathf.RoundToInt(source.GetFloat("_Mode")) : 0;

            if (mode == 1)
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = 2450;
                return;
            }

            if (mode < 2)
                return;

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
        }

        private static void CopyTexture(Material source, Material target, string sourceName, string targetName)
        {
            if (source.HasProperty(sourceName) && target.HasProperty(targetName))
                target.SetTexture(targetName, source.GetTexture(sourceName));
        }

        private static void CopyFloat(Material source, Material target, string sourceName, string targetName)
        {
            if (source.HasProperty(sourceName) && target.HasProperty(targetName))
                target.SetFloat(targetName, source.GetFloat(sourceName));
        }

        private static void CopyColor(Material source, Material target, string sourceName, string targetName)
        {
            if (source.HasProperty(sourceName) && target.HasProperty(targetName))
                target.SetColor(targetName, source.GetColor(sourceName));
        }

        private static bool TryGetVisual(
            CentralEnemyArchetype archetype,
            out CentralEnemyVisualCatalog.Entry entry)
        {
            if (visualCatalog == null)
                visualCatalog = Resources.Load<CentralEnemyVisualCatalog>(VisualCatalogPath);

            if (visualCatalog != null)
                return visualCatalog.TryGet(archetype, out entry);

            entry = null;
            return false;
        }

        private static void FitToHeight(Transform visual, Transform space, float bodyHeight)
        {
            if (!TryGetBounds(visual, space, out var bounds) || bounds.size.y <= 0.001f)
                return;

            var scale = Mathf.Max(0.01f, bodyHeight) / bounds.size.y;
            visual.localScale *= scale;

            if (!TryGetBounds(visual, space, out bounds))
                return;

            visual.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static bool TryGetBounds(Transform visual, Transform space, out Bounds bounds)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            var initialized = false;
            bounds = default;

            foreach (var renderer in renderers)
            {
                var worldBounds = renderer.bounds;

                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var worldPoint = worldBounds.center + Vector3.Scale(
                                worldBounds.extents,
                                new Vector3(x, y, z));
                            var localPoint = space.InverseTransformPoint(worldPoint);

                            if (!initialized)
                            {
                                bounds = new Bounds(localPoint, Vector3.zero);
                                initialized = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localPoint);
                            }
                        }
                    }
                }
            }

            return initialized;
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
