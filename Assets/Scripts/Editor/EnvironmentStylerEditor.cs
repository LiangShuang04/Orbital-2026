using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontDiePlease.EditorTools
{
    public static class EnvironmentStylerEditor
    {
        private const string UndoName = "Apply Dim Industrial Atmosphere";

        private static readonly Color IndustrialFogColor = new Color(0.18f, 0.2f, 0.21f, 1f);
        private static readonly Color IndustrialSunColor = new Color(0.82f, 0.84f, 0.82f, 1f);
        private static readonly Color IndustrialNeutral = new Color(0.46f, 0.47f, 0.45f, 1f);

        [MenuItem("Tools/Don't Die Please/Apply Dim Industrial Atmosphere")]
        private static void ApplyDimIndustrialAtmosphere()
        {
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            ApplyLightingAndFog();
            var materialCount = StyleSelectedMaterials();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);

            if (materialCount == 0)
            {
                Debug.LogWarning("No project materials were selected. Lighting and fog were still updated.");
                return;
            }

            Debug.Log($"Applied dim industrial atmosphere to {materialCount} selected material{(materialCount == 1 ? string.Empty : "s")}.");
        }

        [MenuItem("Window/Don't Die Please/Apply Dim Industrial Atmosphere")]
        private static void ApplyDimIndustrialAtmosphereFromWindow()
        {
            ApplyDimIndustrialAtmosphere();
        }

        private static void ApplyLightingAndFog()
        {
            var renderSettings = ResolveRenderSettingsObject();

            if (renderSettings != null)
            {
                Undo.RecordObject(renderSettings, UndoName);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = IndustrialFogColor;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.ambientLight = new Color(0.2f, 0.21f, 0.21f, 1f);
            RenderSettings.ambientIntensity = 0.78f;

            var sun = RenderSettings.sun != null ? RenderSettings.sun : FindDirectionalLight();

            if (sun == null)
            {
                return;
            }

            Undo.RecordObject(sun, UndoName);
            RenderSettings.sun = sun;
            sun.color = IndustrialSunColor;
            sun.intensity = 0.82f;
            sun.shadowStrength = 0.55f;
            EditorUtility.SetDirty(sun);
        }

        private static Object ResolveRenderSettingsObject()
        {
            const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var method = typeof(RenderSettings).GetMethod("GetRenderSettings", bindingFlags);
            return method?.Invoke(null, null) as Object;
        }

        private static Light FindDirectionalLight()
        {
            return Object.FindObjectsOfType<Light>()
                .FirstOrDefault(light => light != null && light.type == LightType.Directional);
        }

        private static int StyleSelectedMaterials()
        {
            var selectedMaterials = Selection.objects
                .OfType<Material>()
                .Where(material => material != null)
                .Distinct()
                .ToArray();

            foreach (var material in selectedMaterials)
            {
                Undo.RecordObject(material, UndoName);
                ApplyIndustrialAlbedo(material);
                ReduceFloatProperty(material, "_Metallic", 0.22f);
                ReduceFloatProperty(material, "_Smoothness", 0.38f);
                ReduceFloatProperty(material, "_Glossiness", 0.38f);
                ReduceFloatProperty(material, "_SpecularHighlights", 0.5f);
                EditorUtility.SetDirty(material);
            }

            return selectedMaterials.Length;
        }

        private static void ApplyIndustrialAlbedo(Material material)
        {
            var sourceColor = ReadAlbedoColor(material);
            var styledColor = CreateIndustrialColor(sourceColor);

            SetColorProperty(material, "_BaseColor", styledColor);
            SetColorProperty(material, "_Color", styledColor);
        }

        private static Color ReadAlbedoColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return Color.white;
        }

        private static Color CreateIndustrialColor(Color sourceColor)
        {
            Color.RGBToHSV(sourceColor, out var hue, out var saturation, out var value);

            var adjustedSaturation = Mathf.Clamp01(saturation * 0.68f);
            var adjustedValue = Mathf.Clamp01(Mathf.Lerp(value, 0.5f, 0.22f));
            var industrialColor = Color.HSVToRGB(hue, adjustedSaturation, adjustedValue);
            var finalColor = Color.Lerp(industrialColor, IndustrialNeutral, 0.18f);
            finalColor.a = sourceColor.a;
            return finalColor;
        }

        private static void SetColorProperty(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void ReduceFloatProperty(Material material, string propertyName, float multiplier)
        {
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            var currentValue = material.GetFloat(propertyName);
            material.SetFloat(propertyName, Mathf.Clamp01(currentValue * multiplier));
        }
    }
}
