using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DontDiePlease.Systems
{
    public sealed class OldIndustryRuntimeRenderGuard : MonoBehaviour
    {
        [SerializeField] private bool runOnAwake = true;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool disableVolumes = true;
        [SerializeField] private bool clearBakedLighting = true;
        [SerializeField] private bool normalizeLights = true;
        [SerializeField] private bool disableReflectionProbes = true;
        [SerializeField] private bool stabilizeCameras = true;
        [SerializeField] private bool clampEmissiveMaterials = true;
        [SerializeField] private float directionalLightIntensity = 1.15f;
        [SerializeField] private float maxPunctualLightIntensity = 3f;
        [SerializeField] private float maxEmissionValue = 0.65f;

        private Scene scene;

        private void Awake()
        {
            scene = gameObject.scene;

            if (runOnAwake)
            {
                Apply();
            }
        }

        private void Start()
        {
            if (runOnStart)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (!scene.IsValid())
            {
                scene = gameObject.scene;
            }

            if (disableVolumes)
            {
                DisableVolumes();
            }

            if (clearBakedLighting)
            {
                ClearLightingData();
            }

            if (normalizeLights)
            {
                NormalizeLights();
            }

            if (disableReflectionProbes)
            {
                DisableReflectionProbes();
            }

            if (stabilizeCameras)
            {
                StabilizeCameras();
            }

            if (clampEmissiveMaterials)
            {
                ClampEmissiveMaterials();
            }
        }

        private void DisableVolumes()
        {
            var objects = scene.GetRootGameObjects();

            foreach (var root in objects)
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);

                foreach (var transform in transforms)
                {
                    var obj = transform.gameObject;
                    var lowerName = obj.name.ToLowerInvariant();

                    if (lowerName.Contains("volume") || lowerName.Contains("sky and fog"))
                    {
                        obj.SetActive(false);
                        continue;
                    }

                    var comps = obj.GetComponents<Component>();

                    foreach (var comp in comps)
                    {
                        if (comp != null && comp.GetType().Name.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            obj.SetActive(false);
                            break;
                        }
                    }
                }
            }
        }

        private void ClearLightingData()
        {
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();
            LightmapSettings.lightProbes = null;
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.25f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.21f, 0.21f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.18f, 0.2f, 0.21f, 1f);
            RenderSettings.fogDensity = 0.008f;
        }

        private void NormalizeLights()
        {
            var lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var light in lights)
            {
                if (light == null || light.gameObject.scene != scene)
                {
                    continue;
                }

                light.useColorTemperature = false;

                if (light.type == LightType.Directional)
                {
                    light.enabled = true;
                    light.intensity = directionalLightIntensity;
                    light.color = new Color(0.78f, 0.86f, 0.9f, 1f);
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.55f;
                    continue;
                }

                light.intensity = Mathf.Clamp(light.intensity, 0f, maxPunctualLightIntensity);
                light.range = Mathf.Clamp(light.range, 3f, 24f);
                light.shadows = LightShadows.None;
            }
        }

        private void DisableReflectionProbes()
        {
            var probes = FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var probe in probes)
            {
                if (probe == null || probe.gameObject.scene != scene)
                {
                    continue;
                }

                probe.enabled = false;
                probe.intensity = 0f;
            }
        }

        private void StabilizeCameras()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var camera in cameras)
            {
                if (camera == null || camera.gameObject.scene != scene)
                {
                    continue;
                }

                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.045f, 0.06f, 0.065f, 1f);

                foreach (var component in camera.GetComponents<Component>())
                {
                    DisableCameraPostProcessing(component);
                }
            }
        }

        private void DisableCameraPostProcessing(Component component)
        {
            if (component == null || component.GetType().Name != "UniversalAdditionalCameraData")
            {
                return;
            }

            var prop = component.GetType().GetProperty("renderPostProcessing");

            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(component, false);
            }
        }

        private void ClampEmissiveMaterials()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene != scene)
                {
                    continue;
                }

                var materials = renderer.materials;

                foreach (var material in materials)
                {
                    ClampEmission(material);
                }
            }
        }

        private void ClampEmission(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (!material.HasProperty("_EmissionColor"))
            {
                material.DisableKeyword("_EMISSION");
                return;
            }

            var emission = material.GetColor("_EmissionColor");
            var max = emission.maxColorComponent;

            if (max <= 0f)
            {
                material.DisableKeyword("_EMISSION");
                return;
            }

            if (max > maxEmissionValue)
            {
                material.SetColor("_EmissionColor", emission * (maxEmissionValue / max));
            }
        }
    }
}
