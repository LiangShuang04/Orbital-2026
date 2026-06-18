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
        private const string UndoName = "Apply Toxic Morandi Style";

        private static readonly Color ToxicFogColor = new Color(0.34f, 0.42f, 0.36f, 1f);
        private static readonly Color CoolSunColor = new Color(0.62f, 0.68f, 0.69f, 1f);
        private static readonly Color WarmBeige = new Color(0.63f, 0.57f, 0.49f, 1f);
        private static readonly Color CoolGrey = new Color(0.47f, 0.54f, 0.55f, 1f);

        [MenuItem("Tools/Don't Die Please/Apply Toxic Morandi Style")]
        private static void ApplyToxicMorandiStyle()
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

            Debug.Log($"Applied toxic Morandi style to {materialCount} selected material{(materialCount == 1 ? string.Empty : "s")}.");
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
            RenderSettings.fogColor = ToxicFogColor;
            RenderSettings.fogDensity = 0.026f;
            RenderSettings.ambientLight = new Color(0.29f, 0.34f, 0.33f, 1f);
            RenderSettings.ambientIntensity = 0.72f;

            var sun = RenderSettings.sun != null ? RenderSettings.sun : FindDirectionalLight();

            if (sun == null)
            {
                return;
            }

            Undo.RecordObject(sun, UndoName);
            RenderSettings.sun = sun;
            sun.color = CoolSunColor;
            sun.intensity = 0.72f;
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
                ApplyMorandiAlbedo(material);
                ReduceFloatProperty(material, "_Metallic", 0.22f);
                ReduceFloatProperty(material, "_Smoothness", 0.38f);
                ReduceFloatProperty(material, "_Glossiness", 0.38f);
                ReduceFloatProperty(material, "_SpecularHighlights", 0.5f);
                EditorUtility.SetDirty(material);
            }

            return selectedMaterials.Length;
        }

        private static void ApplyMorandiAlbedo(Material material)
        {
            var sourceColor = ReadAlbedoColor(material);
            var styledColor = CreateMorandiColor(sourceColor);

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

        private static Color CreateMorandiColor(Color sourceColor)
        {
            Color.RGBToHSV(sourceColor, out var hue, out var saturation, out var value);

            var mutedColor = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.32f), Mathf.Lerp(value, 0.64f, 0.35f));
            var anchorColor = IsWarmHue(hue) ? WarmBeige : CoolGrey;
            var finalColor = Color.Lerp(mutedColor, anchorColor, 0.34f);
            finalColor.a = sourceColor.a;
            return finalColor;
        }

        private static bool IsWarmHue(float hue)
        {
            return hue <= 0.18f || hue >= 0.92f;
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
