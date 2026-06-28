using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DontDiePlease.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class URPMaterialFixer : EditorWindow
{
    private const string OldIndustryFolder = "Assets/OldIndustry";
    private const string GeneratedGroundFolder = "Assets/Generated/OldIndustry";
    private const string GroundMaterialPath = GeneratedGroundFolder + "/YggdrasilToxicGround_URP.mat";
    private const string TerrainMaterialPath = GeneratedGroundFolder + "/YggdrasilToxicTerrain_URP.mat";
    private const string TerrainLayerPath = GeneratedGroundFolder + "/YggdrasilToxicTerrainLayer.terrainlayer";
    private const string IndoorConcreteMaterialPath = GeneratedGroundFolder + "/YggdrasilIndoorConcrete_URP.mat";
    private const string GroundMeshPath = GeneratedGroundFolder + "/YggdrasilToxicGroundMesh.asset";
    private const string SourceTerrainLayerPath = OldIndustryFolder + "/Models/Terrains/Layers/MudTracks01.terrainlayer";
    private const string SourceConcreteMaterialPath = OldIndustryFolder + "/Models/Architecture/Blocks/Textures/Materials/BlockConcrete01.mat";
    private const string ToxicGroundName = "Yggdrasil Toxic Ground";
    private static readonly string[] OutdoorTerrainLayerPaths =
    {
        OldIndustryFolder + "/Models/Terrains/Layers/MudTracks01.terrainlayer",
        OldIndustryFolder + "/Models/Terrains/Layers/Ground04.terrainlayer",
        OldIndustryFolder + "/Models/Terrains/Layers/Ground05.terrainlayer",
        OldIndustryFolder + "/Models/Terrains/Layers/Ground03.terrainlayer"
    };

    [MenuItem("Tools/Fix Old Industry Materials")]
    public static void FixMaterials()
    {
        FixMaterialsForUrp();
    }

    [MenuItem("Tools/Old Industry/Fix Materials For URP")]
    public static void FixMaterialsForUrp()
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (litShader == null)
        {
            EditorUtility.DisplayDialog("URP shader missing", "Universal Render Pipeline/Lit was not found. Check that URP is installed and active.", "OK");
            return;
        }

        var materialPaths = GetOldIndustryMaterialPaths();

        if (materialPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("No materials found", $"No materials were found under {OldIndustryFolder}.", "OK");
            return;
        }

        var fixedCount = 0;
        var decalCount = 0;
        var transparentCount = 0;
        var missingBaseMapCount = 0;

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (var path in materialPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat == null)
                    continue;

                var data = MaterialData.Read(mat, path);
                mat.shader = data.IsDecal && unlitShader != null ? unlitShader : litShader;

                ApplyMaterialData(mat, data);

                if (data.IsDecal)
                    decalCount++;

                if (data.IsTransparent)
                    transparentCount++;

                if (data.BaseMap == null)
                    missingBaseMapCount++;

                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Old Industry URP material pass finished. Fixed {fixedCount} materials, {decalCount} decals, {transparentCount} transparent materials, {missingBaseMapCount} without base textures.");
    }

    [MenuItem("Tools/Old Industry/Fix Current Scene Rendering")]
    public static void FixCurrentSceneRendering()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Exit Play Mode first", "Stop Play Mode before running the Old Industry scene rendering fix.", "OK");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("No active scene", "Open demoMainScene or another Old Industry scene before running the scene rendering fix.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Fix Old Industry URP Scene");

        var allObjects = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .Distinct()
            .ToArray();

        var disabledVolumeCount = 0;

        foreach (var obj in allObjects)
        {
            if (!ShouldDisableVolumeObject(obj))
                continue;

            Undo.RecordObject(obj, "Disable HDRP volume");
            obj.SetActive(false);
            EditorUtility.SetDirty(obj);
            disabledVolumeCount++;
        }

        var missingScriptCount = RemoveMissingScripts(scene);
        var sceneMaterialCount = FixCurrentSceneMaterials(scene);
        var groundCreated = EnsureToxicGround(scene);
        var terrainCount = FixSceneTerrains(scene);
        var concreteFloorCount = FixSceneIndoorConcreteMaterials(scene);
        var rendererCount = FixSceneRenderers(scene);
        var reflectionProbeCount = FixReflectionProbes(scene);
        var lightCount = FixSceneLights(scene);
        var cameraCount = FixSceneCameras(scene);
        var guardCreated = EnsureRuntimeGuard(scene);

        ClearBakedLighting();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.18f, 0.2f, 0.21f, 1f);
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.2f, 0.21f, 0.21f, 1f);
        RenderSettings.reflectionIntensity = 0.35f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneView.RepaintAll();

        Debug.Log($"Old Industry scene rendering pass finished for {scene.name}. Disabled {disabledVolumeCount} volume objects, removed {missingScriptCount} missing scripts, fixed {sceneMaterialCount} scene materials, created {groundCreated} toxic ground layers, fixed {terrainCount} terrains, fixed {concreteFloorCount} concrete floor renderers, fixed {rendererCount} renderers, disabled {reflectionProbeCount} reflection probes, normalized {lightCount} lights, fixed {cameraCount} cameras, created {guardCreated} runtime guards, cleared baked lighting.");
    }

    [MenuItem("Tools/Old Industry/Fix Materials And Current Scene")]
    public static void FixMaterialsAndCurrentScene()
    {
        FixMaterialsForUrp();
        FixCurrentSceneRendering();
    }

    [MenuItem("Tools/Old Industry/Fix Current Scene Terrain")]
    public static void FixCurrentSceneTerrain()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Exit Play Mode first", "Stop Play Mode before running the Old Industry terrain fix.", "OK");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("No active scene", "Open demoMainScene before fixing the terrain.", "OK");
            return;
        }

        var terrainCount = FixSceneTerrains(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneView.RepaintAll();
        Debug.Log($"Old Industry terrain pass finished for {scene.name}. Fixed {terrainCount} terrains.");
    }

    [MenuItem("Tools/Old Industry/Apply Ground And Concrete Materials")]
    public static void ApplyGroundAndConcreteMaterials()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Exit Play Mode first", "Stop Play Mode before applying Old Industry ground materials.", "OK");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("No active scene", "Open demoMainScene before applying Old Industry ground materials.", "OK");
            return;
        }

        var groundCreated = EnsureToxicGround(scene);
        var terrainCount = FixSceneTerrains(scene);
        var concreteFloorCount = FixSceneIndoorConcreteMaterials(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneView.RepaintAll();
        Debug.Log($"Old Industry ground material pass finished for {scene.name}. Created {groundCreated} outdoor ground meshes, fixed {terrainCount} terrain objects, applied concrete to {concreteFloorCount} floor renderers.");
    }

    private static void ApplyMaterialData(Material mat, MaterialData data)
    {
        var supportsEmission = mat.HasProperty("_EmissionColor") && !mat.shader.name.Contains("/Unlit");

        SetTexture(mat, "_BaseMap", data.BaseMap);
        SetTexture(mat, "_MainTex", data.BaseMap);
        SetColor(mat, "_BaseColor", data.BaseColor);
        SetColor(mat, "_Color", data.BaseColor);

        if (data.NormalMap != null)
        {
            SetTexture(mat, "_BumpMap", data.NormalMap);
            SetFloat(mat, "_BumpScale", data.NormalScale);
            mat.EnableKeyword("_NORMALMAP");
        }
        else
        {
            mat.DisableKeyword("_NORMALMAP");
        }

        if (data.MaskMap != null)
        {
            SetTexture(mat, "_MetallicGlossMap", data.MaskMap);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        else
        {
            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        }

        if (supportsEmission && (data.EmissionMap != null || data.EmissionColor.maxColorComponent > 0.02f))
        {
            SetTexture(mat, "_EmissionMap", data.EmissionMap);
            SetColor(mat, "_EmissionColor", data.EmissionColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        SetFloat(mat, "_Metallic", data.Metallic);
        SetFloat(mat, "_Smoothness", data.Smoothness);
        SetFloat(mat, "_AlphaClip", data.AlphaClip ? 1f : 0f);
        SetFloat(mat, "_Cutoff", data.AlphaCutoff);

        if (data.IsTransparent)
            ConfigureTransparent(mat);
        else
            ConfigureOpaque(mat);

        if (supportsEmission)
            MaterialEditor.FixupEmissiveFlag(mat);
    }

    private static string[] GetOldIndustryMaterialPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { OldIndustryFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (!string.IsNullOrWhiteSpace(path))
            {
                paths.Add(NormalizeAssetPath(path));
            }
        }

        var fullRoot = Path.Combine(Directory.GetCurrentDirectory(), OldIndustryFolder);

        if (Directory.Exists(fullRoot))
        {
            foreach (var path in Directory.GetFiles(fullRoot, "*.mat", SearchOption.AllDirectories))
            {
                paths.Add(NormalizeAssetPath(path));
            }
        }

        return paths.Where(path => path.StartsWith(OldIndustryFolder, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private static string NormalizeAssetPath(string path)
    {
        var normalized = path.Replace("\\", "/");
        var projectRoot = Directory.GetCurrentDirectory().Replace("\\", "/");

        if (normalized.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Substring(projectRoot.Length + 1);
        }

        return normalized;
    }

    private static void ConfigureOpaque(Material mat)
    {
        SetFloat(mat, "_Surface", 0f);
        SetFloat(mat, "_Blend", 0f);
        SetFloat(mat, "_SrcBlend", (float)BlendMode.One);
        SetFloat(mat, "_DstBlend", (float)BlendMode.Zero);
        SetFloat(mat, "_ZWrite", 1f);
        SetFloat(mat, "_QueueOffset", 0f);
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.renderQueue = -1;
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
    }

    private static void ConfigureTransparent(Material mat)
    {
        SetFloat(mat, "_Surface", 1f);
        SetFloat(mat, "_Blend", 0f);
        SetFloat(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(mat, "_ZWrite", 0f);
        SetFloat(mat, "_QueueOffset", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    private static int FixSceneLights(Scene scene)
    {
        var lights = Resources.FindObjectsOfTypeAll<Light>()
            .Where(light => light != null && light.gameObject.scene == scene)
            .ToArray();

        foreach (var light in lights)
        {
            Undo.RecordObject(light, "Normalize Old Industry light");
            light.useColorTemperature = false;

            if (light.type == LightType.Directional)
            {
                light.intensity = 1.15f;
                light.color = new Color(0.78f, 0.86f, 0.9f, 1f);
                light.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.55f;
            }
            else
            {
                light.intensity = Mathf.Clamp(light.intensity, 0.2f, 3f);
                light.range = Mathf.Clamp(light.range, 4f, 28f);
                light.color = Color.Lerp(light.color, new Color(0.82f, 0.93f, 0.88f, 1f), 0.25f);
                light.shadows = LightShadows.None;
            }

            EditorUtility.SetDirty(light);
        }

        if (lights.Any(light => light.type == LightType.Directional))
            return lights.Length;

        var lightObj = new GameObject("Directional Light");
        Undo.RegisterCreatedObjectUndo(lightObj, "Create directional light");
        SceneManager.MoveGameObjectToScene(lightObj, scene);
        var newLight = lightObj.AddComponent<Light>();
        newLight.type = LightType.Directional;
        newLight.intensity = 1.15f;
        newLight.color = new Color(0.78f, 0.86f, 0.9f, 1f);
        newLight.shadows = LightShadows.Soft;
        newLight.shadowStrength = 0.55f;
        lightObj.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
        EditorUtility.SetDirty(newLight);

        return lights.Length + 1;
    }

    private static int RemoveMissingScripts(Scene scene)
    {
        var total = 0;
        var objects = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .Distinct()
            .ToArray();

        foreach (var obj in objects)
        {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);

            if (count <= 0)
                continue;

            Undo.RegisterCompleteObjectUndo(obj, "Remove Old Industry missing scripts");
            total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            EditorUtility.SetDirty(obj);
        }

        return total;
    }

    private static int FixCurrentSceneMaterials(Scene scene)
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (litShader == null)
            return 0;

        var fixedCount = 0;
        var seenMaterials = new HashSet<int>();
        var renderers = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene)
            .ToArray();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null || !seenMaterials.Add(mat.GetInstanceID()) || !ShouldFixSceneMaterial(mat))
                    continue;

                Undo.RecordObject(mat, "Fix Old Industry scene material");

                var path = NormalizeAssetPath(AssetDatabase.GetAssetPath(mat));
                var data = MaterialData.Read(mat, path);
                mat.shader = data.IsDecal && unlitShader != null ? unlitShader : litShader;
                ApplyMaterialData(mat, data);
                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        return fixedCount;
    }

    private static bool ShouldFixSceneMaterial(Material mat)
    {
        var shaderName = mat.shader != null ? mat.shader.name : string.Empty;
        var path = NormalizeAssetPath(AssetDatabase.GetAssetPath(mat));

        if (shaderName.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.StartsWith(OldIndustryFolder, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.IsNullOrWhiteSpace(shaderName)
            || shaderName.IndexOf("HDRP", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("High Definition", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int FixSceneRenderers(Scene scene)
    {
        var renderers = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene)
            .ToArray();

        foreach (var renderer in renderers)
        {
            Undo.RecordObject(renderer, "Fix Old Industry renderer GI");
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.receiveShadows = true;
            SetRendererReceiveGiToLightProbes(renderer);

            var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
            flags &= ~StaticEditorFlags.ContributeGI;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(renderer.gameObject);
        }

        return renderers.Length;
    }

    private static int FixSceneIndoorConcreteMaterials(Scene scene)
    {
        var concreteMaterial = EnsureConcreteMaterial();
        var changedCount = 0;
        var renderers = Resources.FindObjectsOfTypeAll<MeshRenderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && ShouldUseIndoorConcrete(renderer))
            .ToArray();

        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;

            if (materials.Length == 0 || materials.All(mat => mat == concreteMaterial))
                continue;

            Undo.RecordObject(renderer, "Apply Old Industry concrete floor material");

            for (var idx = 0; idx < materials.Length; idx++)
                materials[idx] = concreteMaterial;

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
            changedCount++;
        }

        AssetDatabase.SaveAssets();
        return changedCount;
    }

    private static Material EnsureConcreteMaterial()
    {
        EnsureGeneratedFolder();

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = AssetDatabase.LoadAssetAtPath<Material>(IndoorConcreteMaterialPath);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, IndoorConcreteMaterialPath);
        }

        Undo.RecordObject(material, "Update Old Industry concrete material");

        if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceConcreteMaterialPath);

        if (sourceMaterial != null)
        {
            var data = MaterialData.Read(sourceMaterial, SourceConcreteMaterialPath);
            ApplyMaterialData(material, data);
        }
        else
        {
            SetTexture(material, "_BaseMap", LoadTexture("Assets/OldIndustry/Models/Architecture/Blocks/Textures/BlockConcrete01_a.png"));
            SetTexture(material, "_MainTex", LoadTexture("Assets/OldIndustry/Models/Architecture/Blocks/Textures/BlockConcrete01_a.png"));
            SetTexture(material, "_BumpMap", LoadTexture("Assets/OldIndustry/Models/Architecture/Blocks/Textures/BlockConcrete01_n.png"));
            SetTexture(material, "_MetallicGlossMap", LoadTexture("Assets/OldIndustry/Models/Architecture/Blocks/Textures/BlockConcrete01_MaskMap.png"));
        }

        SetTextureScale(material, "_BaseMap", new Vector2(4f, 4f));
        SetTextureScale(material, "_MainTex", new Vector2(4f, 4f));
        SetTextureScale(material, "_BumpMap", new Vector2(4f, 4f));
        SetTextureScale(material, "_MetallicGlossMap", new Vector2(4f, 4f));
        SetColor(material, "_BaseColor", new Color(0.68f, 0.67f, 0.62f, 1f));
        SetColor(material, "_Color", new Color(0.68f, 0.67f, 0.62f, 1f));
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_Smoothness", 0.16f);
        SetFloat(material, "_BumpScale", 0.9f);
        ConfigureOpaque(material);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static bool ShouldUseIndoorConcrete(Renderer renderer)
    {
        var lowerPath = GetHierarchyPath(renderer.transform).ToLowerInvariant();

        if (lowerPath.Contains(ToxicGroundName.ToLowerInvariant()) || lowerPath.Contains("terrain"))
            return false;

        if (lowerPath.Contains("floor") || lowerPath.Contains("track02_concrete") || lowerPath.Contains("walkway") || lowerPath.Contains("platform"))
            return true;

        if (lowerPath.Contains("wall") || lowerPath.Contains("roof") || lowerPath.Contains("window") || lowerPath.Contains("glass") || lowerPath.Contains("pipe") || lowerPath.Contains("rail"))
            return false;

        var size = renderer.bounds.size;
        var mostlyFlat = size.y <= 0.65f && Mathf.Max(size.x, size.z) >= 2f;
        return mostlyFlat && (lowerPath.Contains("building") || lowerPath.Contains("block") || lowerPath.Contains("concrete"));
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var names = new List<string>();
        var current = transform;

        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static int EnsureToxicGround(Scene scene)
    {
        EnsureGeneratedFolder();

        var material = EnsureGroundMaterial();
        var mesh = EnsureGroundMesh();
        var existing = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == ToxicGroundName);

        var created = existing == null;
        var obj = created ? new GameObject(ToxicGroundName) : existing.gameObject;

        if (created)
        {
            Undo.RegisterCreatedObjectUndo(obj, "Create Yggdrasil toxic ground");
            SceneManager.MoveGameObjectToScene(obj, scene);
        }
        else
        {
            Undo.RecordObject(obj, "Update Yggdrasil toxic ground");
        }

        obj.transform.position = new Vector3(0f, -0.55f, 0f);
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var meshFilter = obj.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            meshFilter = obj.AddComponent<MeshFilter>();
        }

        var meshRenderer = obj.GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            meshRenderer = obj.AddComponent<MeshRenderer>();
        }

        var meshCollider = obj.GetComponent<MeshCollider>();

        if (meshCollider == null)
        {
            meshCollider = obj.AddComponent<MeshCollider>();
        }

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        meshCollider.sharedMesh = mesh;

        var flags = GameObjectUtility.GetStaticEditorFlags(obj);
        flags &= ~StaticEditorFlags.ContributeGI;
        GameObjectUtility.SetStaticEditorFlags(obj, flags);

        EditorUtility.SetDirty(obj);
        EditorUtility.SetDirty(meshFilter);
        EditorUtility.SetDirty(meshRenderer);
        EditorUtility.SetDirty(meshCollider);

        return created ? 1 : 0;
    }

    private static int FixSceneTerrains(Scene scene)
    {
        EnsureGeneratedFolder();

        var terrainMaterial = EnsureTerrainMaterial();
        var terrainLayers = EnsureOutdoorTerrainLayers();
        var terrains = Resources.FindObjectsOfTypeAll<Terrain>()
            .Where(terrain => terrain != null && terrain.gameObject.scene == scene)
            .ToArray();

        foreach (var terrain in terrains)
        {
            Undo.RecordObject(terrain, "Fix Old Industry terrain");
            terrain.materialTemplate = terrainMaterial;
            terrain.reflectionProbeUsage = ReflectionProbeUsage.Off;
            terrain.shadowCastingMode = ShadowCastingMode.On;
            terrain.drawInstanced = false;
            terrain.heightmapPixelError = Mathf.Max(2f, terrain.heightmapPixelError);
            terrain.basemapDistance = Mathf.Max(800f, terrain.basemapDistance);
            SetTerrainReceiveGiToLightProbes(terrain);

            var terrainData = terrain.terrainData;

            if (terrainData != null)
            {
                Undo.RecordObject(terrainData, "Fix Old Industry terrain layers");
                terrainData.terrainLayers = terrainLayers;
                PaintOutdoorTerrain(terrainData, terrainLayers.Length);
                EditorUtility.SetDirty(terrainData);
            }

            var flags = GameObjectUtility.GetStaticEditorFlags(terrain.gameObject);
            flags &= ~StaticEditorFlags.ContributeGI;
            GameObjectUtility.SetStaticEditorFlags(terrain.gameObject, flags);

            EditorUtility.SetDirty(terrain);
            EditorUtility.SetDirty(terrain.gameObject);
        }

        AssetDatabase.SaveAssets();
        return terrains.Length;
    }

    private static Material EnsureTerrainMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
        var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, TerrainMaterialPath);
        }

        Undo.RecordObject(material, "Update toxic terrain material");

        if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        SetTexture(material, "_BaseMap", LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_a.png"));
        SetTexture(material, "_MainTex", LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_a.png"));
        SetTexture(material, "_BumpMap", LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_n.png"));
        SetTexture(material, "_NormalMap", LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_n.png"));
        SetTextureScale(material, "_BaseMap", new Vector2(18f, 18f));
        SetTextureScale(material, "_MainTex", new Vector2(18f, 18f));
        SetColor(material, "_BaseColor", new Color(0.48f, 0.43f, 0.34f, 1f));
        SetColor(material, "_Color", new Color(0.48f, 0.43f, 0.34f, 1f));
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_Smoothness", 0.05f);
        SetFloat(material, "_BumpScale", 1.15f);
        SetFloat(material, "_NormalScale", 1.15f);
        ConfigureOpaque(material);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static TerrainLayer[] EnsureOutdoorTerrainLayers()
    {
        var terrainLayers = OutdoorTerrainLayerPaths
            .Select(path => AssetDatabase.LoadAssetAtPath<TerrainLayer>(path))
            .Where(layer => layer != null)
            .ToArray();

        if (terrainLayers.Length > 0)
            return terrainLayers;

        return new[] { EnsureTerrainLayer() };
    }

    private static TerrainLayer EnsureTerrainLayer()
    {
        var terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerPath);
        var sourceLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SourceTerrainLayerPath);

        if (terrainLayer == null)
        {
            terrainLayer = new TerrainLayer();
            AssetDatabase.CreateAsset(terrainLayer, TerrainLayerPath);
        }

        Undo.RecordObject(terrainLayer, "Update toxic terrain layer");

        terrainLayer.diffuseTexture = sourceLayer != null ? sourceLayer.diffuseTexture : LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_a.png");
        terrainLayer.normalMapTexture = sourceLayer != null ? sourceLayer.normalMapTexture : LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_n.png");
        terrainLayer.maskMapTexture = sourceLayer != null ? sourceLayer.maskMapTexture : null;
        terrainLayer.tileSize = sourceLayer != null ? sourceLayer.tileSize : new Vector2(11f, 11f);
        terrainLayer.tileOffset = Vector2.zero;
        terrainLayer.specular = new Color(0.06f, 0.07f, 0.06f, 0f);
        terrainLayer.metallic = 0f;
        terrainLayer.smoothness = 0.08f;
        terrainLayer.normalScale = sourceLayer != null ? Mathf.Clamp(sourceLayer.normalScale, 0.5f, 1.5f) : 1.15f;

        EditorUtility.SetDirty(terrainLayer);
        AssetDatabase.SaveAssets();
        return terrainLayer;
    }

    private static void PaintOutdoorTerrain(TerrainData terrainData, int layerCount)
    {
        if (terrainData == null || layerCount <= 1)
            return;

        var width = terrainData.alphamapWidth;
        var height = terrainData.alphamapHeight;

        if (width <= 0 || height <= 0)
            return;

        var alpha = new float[width, height, layerCount];
        var weights = new float[layerCount];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Array.Clear(weights, 0, weights.Length);

                var px = x / (float)Mathf.Max(1, width - 1);
                var py = y / (float)Mathf.Max(1, height - 1);
                var mudNoise = Mathf.PerlinNoise(px * 9.7f + 12.4f, py * 9.7f + 4.8f);
                var soilNoise = Mathf.PerlinNoise(px * 17.5f + 3.2f, py * 17.5f + 19.1f);
                var dryNoise = Mathf.PerlinNoise(px * 5.1f + 41.6f, py * 5.1f + 8.3f);
                var total = 0f;

                weights[0] = Mathf.Lerp(0.48f, 0.78f, mudNoise);

                if (layerCount > 1)
                    weights[1] = Mathf.Lerp(0.12f, 0.34f, soilNoise);

                if (layerCount > 2)
                    weights[2] = Mathf.Lerp(0.04f, 0.18f, dryNoise);

                if (layerCount > 3)
                    weights[3] = Mathf.Lerp(0.02f, 0.1f, 1f - mudNoise);

                for (var layer = 0; layer < layerCount; layer++)
                    total += weights[layer];

                if (total <= 0f)
                    total = 1f;

                for (var layer = 0; layer < layerCount; layer++)
                    alpha[x, y, layer] = weights[layer] / total;
            }
        }

        terrainData.SetAlphamaps(0, 0, alpha);
    }

    private static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(GeneratedGroundFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "OldIndustry");
        }
    }

    private static Material EnsureGroundMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);

        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, GroundMaterialPath);
        }

        Undo.RecordObject(material, "Update toxic ground material");

        if (material.shader == null || !material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase))
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader != null)
            {
                material.shader = shader;
            }
        }

        SetTexture(material, "_BaseMap", LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_a.png"));
        SetTexture(material, "_MainTex", LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_a.png"));
        SetTexture(material, "_BumpMap", LoadTexture("Assets/OldIndustry/Textures/Grounds/Ground04_n.png"));
        SetTextureScale(material, "_BaseMap", new Vector2(24f, 24f));
        SetTextureScale(material, "_MainTex", new Vector2(24f, 24f));
        SetTextureScale(material, "_BumpMap", new Vector2(24f, 24f));
        SetColor(material, "_BaseColor", new Color(0.5f, 0.44f, 0.34f, 1f));
        SetColor(material, "_Color", new Color(0.5f, 0.44f, 0.34f, 1f));
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_Smoothness", 0.05f);
        SetFloat(material, "_BumpScale", 1.1f);
        ConfigureOpaque(material);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static Mesh EnsureGroundMesh()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GroundMeshPath);

        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "YggdrasilToxicGroundMesh"
            };
            AssetDatabase.CreateAsset(mesh, GroundMeshPath);
        }
        else
        {
            Undo.RecordObject(mesh, "Update toxic ground mesh");
            mesh.Clear();
        }

        BuildGroundMesh(mesh);
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static void BuildGroundMesh(Mesh mesh)
    {
        const int segments = 84;
        const float size = 260f;
        const float half = size * 0.5f;
        var vertices = new Vector3[(segments + 1) * (segments + 1)];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[segments * segments * 6];
        var vertexIndex = 0;

        mesh.indexFormat = IndexFormat.UInt32;

        for (var z = 0; z <= segments; z++)
        {
            for (var x = 0; x <= segments; x++)
            {
                var px = x / (float)segments;
                var pz = z / (float)segments;
                var worldX = px * size - half;
                var worldZ = pz * size - half;
                var detail = Mathf.PerlinNoise(px * 18.5f + 7.1f, pz * 18.5f + 3.2f) * 0.16f;
                var broad = Mathf.PerlinNoise(px * 5.2f + 1.7f, pz * 5.2f + 9.4f) * 0.62f;
                var centerDistance = Mathf.Clamp01(new Vector2(worldX, worldZ).magnitude / 38f);
                var height = (broad + detail - 0.36f) * Mathf.Lerp(0.18f, 1f, centerDistance);
                vertices[vertexIndex] = new Vector3(worldX, height, worldZ);
                uvs[vertexIndex] = new Vector2(px * 12f, pz * 12f);
                vertexIndex++;
            }
        }

        var triangleIndex = 0;

        for (var z = 0; z < segments; z++)
        {
            for (var x = 0; x < segments; x++)
            {
                var a = z * (segments + 1) + x;
                var b = a + 1;
                var c = a + segments + 1;
                var d = c + 1;
                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = d;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }

    [MenuItem("Tools/Old Industry/Create Toxic Ground In Current Scene")]
    public static void CreateToxicGroundInCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Exit Play Mode first", "Stop Play Mode before creating the toxic ground layer.", "OK");
            return;
        }

        var scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("No active scene", "Open demoMainScene before creating the toxic ground layer.", "OK");
            return;
        }

        var created = EnsureToxicGround(scene);
        var terrainCount = FixSceneTerrains(scene);
        var concreteFloorCount = FixSceneIndoorConcreteMaterials(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneView.RepaintAll();
        Debug.Log($"Yggdrasil toxic ground ready for {scene.name}. Created {created} ground layers, fixed {terrainCount} terrains, applied concrete to {concreteFloorCount} floor renderers.");
    }

    private static void SetRendererReceiveGiToLightProbes(Renderer renderer)
    {
        var serializedObject = new SerializedObject(renderer);
        var receiveGi = serializedObject.FindProperty("m_ReceiveGI");

        if (receiveGi == null)
            return;

        receiveGi.intValue = 2;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetTerrainReceiveGiToLightProbes(Terrain terrain)
    {
        var serializedObject = new SerializedObject(terrain);
        var receiveGi = serializedObject.FindProperty("m_ReceiveGI");

        if (receiveGi == null)
            return;

        receiveGi.intValue = 2;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int FixReflectionProbes(Scene scene)
    {
        var probes = Resources.FindObjectsOfTypeAll<ReflectionProbe>()
            .Where(probe => probe != null && probe.gameObject.scene == scene)
            .ToArray();

        foreach (var probe in probes)
        {
            Undo.RecordObject(probe, "Disable Old Industry reflection probe");
            probe.enabled = false;
            probe.intensity = 0f;
            EditorUtility.SetDirty(probe);
        }

        return probes.Length;
    }

    private static void ClearBakedLighting()
    {
        LightmapSettings.lightmaps = Array.Empty<LightmapData>();
        LightmapSettings.lightProbes = null;
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
    }

    private static int FixSceneCameras(Scene scene)
    {
        var cameras = Resources.FindObjectsOfTypeAll<Camera>()
            .Where(camera => camera != null && camera.gameObject.scene == scene)
            .ToArray();

        foreach (var camera in cameras)
        {
            Undo.RecordObject(camera, "Fix Old Industry camera");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.06f, 0.065f, 1f);
            camera.nearClipPlane = Mathf.Max(0.03f, camera.nearClipPlane);
            camera.farClipPlane = Mathf.Max(1200f, camera.farClipPlane);
            camera.allowHDR = false;
            camera.allowMSAA = true;

            foreach (var component in camera.GetComponents<Component>())
                DisableCameraPostProcessing(component);

            EditorUtility.SetDirty(camera);
        }

        return cameras.Length;
    }

    private static int EnsureRuntimeGuard(Scene scene)
    {
        var existing = Resources.FindObjectsOfTypeAll<OldIndustryRuntimeRenderGuard>()
            .FirstOrDefault(guard => guard != null && guard.gameObject.scene == scene);

        if (existing != null)
        {
            EditorUtility.SetDirty(existing);
            return 0;
        }

        var obj = new GameObject("Old Industry Runtime Render Guard");
        Undo.RegisterCreatedObjectUndo(obj, "Create Old Industry runtime render guard");
        SceneManager.MoveGameObjectToScene(obj, scene);
        obj.AddComponent<OldIndustryRuntimeRenderGuard>();
        EditorUtility.SetDirty(obj);
        return 1;
    }

    private static void DisableCameraPostProcessing(Component component)
    {
        if (component == null || component.GetType().Name != "UniversalAdditionalCameraData")
            return;

        var property = component.GetType().GetProperty("renderPostProcessing");

        if (property == null || !property.CanWrite)
            return;

        Undo.RecordObject(component, "Disable URP camera post processing");
        property.SetValue(component, false);
        EditorUtility.SetDirty(component);
    }

    private static bool ShouldDisableVolumeObject(GameObject obj)
    {
        var lowerName = obj.name.ToLowerInvariant();

        if (lowerName.Contains("volume") || lowerName.Contains("sky and fog"))
            return true;

        return obj.GetComponents<Component>()
            .Any(component => component != null && component.GetType().Name.IndexOf("Volume", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void SetTexture(Material mat, string propertyName, Texture value)
    {
        if (value == null || !mat.HasProperty(propertyName))
            return;

        mat.SetTexture(propertyName, value);
    }

    private static void SetTextureScale(Material mat, string propertyName, Vector2 value)
    {
        if (!mat.HasProperty(propertyName))
            return;

        mat.SetTextureScale(propertyName, value);
    }

    private static void SetColor(Material mat, string propertyName, Color value)
    {
        if (!mat.HasProperty(propertyName))
            return;

        mat.SetColor(propertyName, value);
    }

    private static void SetFloat(Material mat, string propertyName, float value)
    {
        if (!mat.HasProperty(propertyName))
            return;

        mat.SetFloat(propertyName, value);
    }

    private static Texture2D LoadTexture(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private sealed class MaterialData
    {
        public Texture BaseMap { get; private set; }
        public Texture NormalMap { get; private set; }
        public Texture MaskMap { get; private set; }
        public Texture EmissionMap { get; private set; }
        public Color BaseColor { get; private set; }
        public Color EmissionColor { get; private set; }
        public float Metallic { get; private set; }
        public float Smoothness { get; private set; }
        public float NormalScale { get; private set; }
        public float AlphaCutoff { get; private set; }
        public bool AlphaClip { get; private set; }
        public bool IsTransparent { get; private set; }
        public bool IsDecal { get; private set; }

        public static MaterialData Read(Material mat, string path)
        {
            var props = new SavedMaterialProperties(mat);
            var lowerPath = path.ToLowerInvariant();
            var lowerName = mat.name.ToLowerInvariant();
            var surfaceType = props.Float("_SurfaceType", "_Surface").GetValueOrDefault(0f);
            var alphaClip = props.Float("_AlphaCutoffEnable", "_AlphaClip").GetValueOrDefault(0f) > 0.5f;
            var decalFlag = props.Float("_Unity_Identify_HDRP_Decal").GetValueOrDefault(0f) > 0.5f;
            var transparentName = lowerName.Contains("glass") || lowerName.Contains("window") || lowerName.Contains("transparent") || lowerPath.Contains("decals");

            return new MaterialData
            {
                BaseMap = props.Texture("_BaseColorMap", "_BaseMap", "_MainTex", "_AlbedoMap", "_DiffuseMap"),
                NormalMap = props.Texture("_NormalMap", "_NormalMapOS", "_BumpMap"),
                MaskMap = props.Texture("_MaskMap", "_MetallicGlossMap", "_SpecGlossMap"),
                EmissionMap = props.Texture("_EmissiveColorMap", "_EmissionMap", "_EmissiveMap"),
                BaseColor = props.Color("_BaseColor", "_Color").GetValueOrDefault(Color.white),
                EmissionColor = props.Color("_EmissiveColor", "_EmissionColor").GetValueOrDefault(Color.black),
                Metallic = Mathf.Clamp01(props.Float("_Metallic", "_MetallicRemapMax").GetValueOrDefault(0.08f)),
                Smoothness = Mathf.Clamp01(props.Float("_Smoothness", "_SmoothnessRemapMax", "_Glossiness").GetValueOrDefault(0.38f)),
                NormalScale = Mathf.Clamp(props.Float("_NormalScale", "_BumpScale").GetValueOrDefault(1f), 0f, 2f),
                AlphaCutoff = Mathf.Clamp01(props.Float("_AlphaCutoff", "_Cutoff").GetValueOrDefault(0.5f)),
                AlphaClip = alphaClip,
                IsDecal = decalFlag || lowerPath.Contains("decals"),
                IsTransparent = surfaceType > 0.5f || transparentName
            };
        }
    }

    private sealed class SavedMaterialProperties
    {
        private readonly SerializedProperty texEnvs;
        private readonly SerializedProperty floats;
        private readonly SerializedProperty colors;

        public SavedMaterialProperties(Material mat)
        {
            var serializedObject = new SerializedObject(mat);
            texEnvs = serializedObject.FindProperty("m_SavedProperties.m_TexEnvs");
            floats = serializedObject.FindProperty("m_SavedProperties.m_Floats");
            colors = serializedObject.FindProperty("m_SavedProperties.m_Colors");
        }

        public Texture Texture(params string[] names)
        {
            if (texEnvs == null)
                return null;

            foreach (var name in names)
            {
                var value = FindTexture(name);

                if (value != null)
                    return value;
            }

            return null;
        }

        public float? Float(params string[] names)
        {
            if (floats == null)
                return null;

            foreach (var name in names)
            {
                var value = FindFloat(name);

                if (value.HasValue)
                    return value;
            }

            return null;
        }

        public Color? Color(params string[] names)
        {
            if (colors == null)
                return null;

            foreach (var name in names)
            {
                var value = FindColor(name);

                if (value.HasValue)
                    return value;
            }

            return null;
        }

        private Texture FindTexture(string propertyName)
        {
            for (var idx = 0; idx < texEnvs.arraySize; idx++)
            {
                var property = texEnvs.GetArrayElementAtIndex(idx);

                if (property.FindPropertyRelative("first").stringValue != propertyName)
                    continue;

                return property.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
            }

            return null;
        }

        private float? FindFloat(string propertyName)
        {
            for (var idx = 0; idx < floats.arraySize; idx++)
            {
                var property = floats.GetArrayElementAtIndex(idx);

                if (property.FindPropertyRelative("first").stringValue != propertyName)
                    continue;

                return property.FindPropertyRelative("second").floatValue;
            }

            return null;
        }

        private Color? FindColor(string propertyName)
        {
            for (var idx = 0; idx < colors.arraySize; idx++)
            {
                var property = colors.GetArrayElementAtIndex(idx);

                if (property.FindPropertyRelative("first").stringValue != propertyName)
                    continue;

                return property.FindPropertyRelative("second").colorValue;
            }

            return null;
        }
    }
}
