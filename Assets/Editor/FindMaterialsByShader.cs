using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

public class FindMaterialsByShader : EditorWindow
{
    private const string URP_SIMPLE_LIT = "Universal Render Pipeline/Simple Lit";
    private const string URP_LIT = "Universal Render Pipeline/Lit";
    private const string URP_COMPLEX_LIT = "Universal Render Pipeline/Complex Lit";
    private const string URP_PARTICLES_LIT = "Universal Render Pipeline/Particles/Lit";
    private const string URP_PARTICLES_SIMPLE_LIT = "Universal Render Pipeline/Particles/Simple Lit";
    private const string URP_PARTICLES_UNLIT = "Universal Render Pipeline/Particles/Unlit";
    private const string AUTODESK_INTERACTIVE = "Autodesk Interactive";

    [MenuItem("Tools/Find Materials Using URP Simple Lit")]
    static void FindSimpleLitMaterials() => FindMaterialsWithShader(URP_SIMPLE_LIT);

    [MenuItem("Tools/Find Materials Using URP Lit")]
    static void FindLitMaterials() => FindMaterialsWithShader(URP_LIT);

    [MenuItem("Tools/Find Materials Using URP Complex Lit")]
    static void FindComplexLitMaterials() => FindMaterialsWithShader(URP_COMPLEX_LIT);

    [MenuItem("Tools/Find Materials Using URP Particles Lit")]
    static void FindParticlesLitMaterials() => FindMaterialsWithShader(URP_PARTICLES_LIT);

    static void FindMaterialsWithShader(string shaderName)
    {
        Shader targetShader = Shader.Find(shaderName);
        if (targetShader == null)
        {
            Debug.LogError($"Shader '{shaderName}' not found.");
            return;
        }

        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
        int found = 0;

        foreach (string guid in allMaterialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader == targetShader)
            {
                Debug.Log($"Material: {mat.name} | Path: {path}", mat);
                found++;
            }
        }

        Debug.Log($"Search complete. Found {found} material(s) using '{shaderName}'.");
    }

    [MenuItem("Tools/Find Models Using Autodesk Interactive")]
    static void FindModels()
    {
        Shader targetShader = Shader.Find(AUTODESK_INTERACTIVE);
        if (targetShader == null)
        {
            Debug.LogError($"Shader '{AUTODESK_INTERACTIVE}' not found.");
            return;
        }

        var matchingMaterials = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Material"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader == targetShader)
                matchingMaterials.Add(path);
        }

        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab t:Model");
        int found = 0;

        foreach (string guid in allPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string[] deps = AssetDatabase.GetDependencies(path, true);
            foreach (string dep in deps)
            {
                if (matchingMaterials.Contains(dep))
                {
                    Debug.Log($"Model/Prefab: {path} uses material: {dep}",
                               AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    found++;
                    break;
                }
            }
        }

        Debug.Log($"Found {found} model(s)/prefab(s) referencing '{AUTODESK_INTERACTIVE}' materials.");
    }

    [MenuItem("Tools/Convert All Lit Materials To Simple Lit")]
    static void ConvertLitToSimpleLit()
    {
        Shader litShader = Shader.Find(URP_LIT);
        Shader complexLitShader = Shader.Find(URP_COMPLEX_LIT);
        Shader simpleLitShader = Shader.Find(URP_SIMPLE_LIT);

        if (simpleLitShader == null)
        {
            Debug.LogError("URP Simple Lit shader not found.");
            return;
        }

        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
        int converted = 0;

        foreach (string guid in allMaterialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && (mat.shader == litShader || mat.shader == complexLitShader))
            {
                string oldShader = mat.shader.name;
                mat.shader = simpleLitShader;
                EditorUtility.SetDirty(mat);
                Debug.Log($"Converted: {mat.name} ({oldShader} → Simple Lit) | Path: {path}", mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Conversion complete. Converted {converted} material(s) to URP Simple Lit.");
    }

    [MenuItem("Tools/Convert Particle Lit Materials To Particle Simple Lit")]
    static void ConvertParticleLitToSimpleLit()
    {
        Shader particleLitShader = Shader.Find(URP_PARTICLES_LIT);
        Shader particleSimpleLitShader = Shader.Find(URP_PARTICLES_SIMPLE_LIT);

        if (particleSimpleLitShader == null)
        {
            Debug.LogError("URP Particles/Simple Lit shader not found.");
            return;
        }

        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
        int converted = 0;

        foreach (string guid in allMaterialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader == particleLitShader)
            {
                mat.shader = particleSimpleLitShader;
                EditorUtility.SetDirty(mat);
                Debug.Log($"Converted: {mat.name} (Particles/Lit → Particles/Simple Lit) | Path: {path}", mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Conversion complete. Converted {converted} particle material(s) to Simple Lit.");
    }
}

public class QuestSafeShaderStripper : IPreprocessShaders
{
    static readonly ShaderKeyword[] s_SafeToStrip = new[]
    {
        // Not supported on Quest
        new ShaderKeyword("_LIGHT_COOKIES"),
        new ShaderKeyword("_DECALS_DBUFFER"),

        // Shadow variants - Quest uses simple shadows only
        new ShaderKeyword("_MAIN_LIGHT_SHADOWS"),
        new ShaderKeyword("_MAIN_LIGHT_SHADOWS_CASCADE"),
        new ShaderKeyword("_MAIN_LIGHT_SHADOWS_SCREEN"),
        new ShaderKeyword("_ADDITIONAL_LIGHT_SHADOWS"),
        new ShaderKeyword("_SHADOWS_SOFT"),
        new ShaderKeyword("_SHADOWS_SOFT_LOW"),
        new ShaderKeyword("_SHADOWS_SOFT_MEDIUM"),
        new ShaderKeyword("_SHADOWS_SOFT_HIGH"),

        // Additional lights - Quest handles these differently
        new ShaderKeyword("_ADDITIONAL_LIGHTS"),
        new ShaderKeyword("_ADDITIONAL_LIGHTS_VERTEX"),

        // Complex Lit features not used with Simple Lit
        new ShaderKeyword("_DETAIL_MULX2"),
        new ShaderKeyword("_DETAIL_SCALED"),
        new ShaderKeyword("_CLEARCOAT"),
        new ShaderKeyword("_CLEARCOATMAP"),
        new ShaderKeyword("_PARALLAXMAP"),
        new ShaderKeyword("_BENTNORMAL"),

        // No terrain in a space game
        new ShaderKeyword("TERRAIN_SPLAT_ADDPASS"),
        new ShaderKeyword("TERRAIN_INSTANCED_PERPIXEL_NORMAL"),

        // Debug and editor only
        new ShaderKeyword("DEBUG_DISPLAY"),
        new ShaderKeyword("EDITOR_VISUALIZATION"),

        // Post processing Quest can't run
        new ShaderKeyword("_FXAA"),
        new ShaderKeyword("_FILM_GRAIN"),
        new ShaderKeyword("_RCAS"),
        new ShaderKeyword("_DITHERING"),
        new ShaderKeyword("_EASU_RCAS_AND_HDR_INPUT"),
        new ShaderKeyword("SCREEN_COORD_OVERRIDE"),
        new ShaderKeyword("HDR_COLORSPACE_CONVERSION"),
        new ShaderKeyword("HDR_COLORSPACE_CONVERSION_AND_ENCODING"),
        new ShaderKeyword("HDR_ENCODING"),
        new ShaderKeyword("_BLOOM_HQ"),
        new ShaderKeyword("_BLOOM_HQ_DIRT"),
        new ShaderKeyword("_BLOOM_LQ_DIRT"),
        new ShaderKeyword("_CHROMATIC_ABERRATION"),
        new ShaderKeyword("_TONEMAP_ACES"),
        new ShaderKeyword("_TONEMAP_NEUTRAL"),
        new ShaderKeyword("_HDR_GRADING"),
        new ShaderKeyword("_GAMMA_20"),

        // Particle features not used in your game
        new ShaderKeyword("_FLIPBOOKBLENDING_ON"),
        new ShaderKeyword("_SOFTPARTICLES_ON"),
        new ShaderKeyword("_FADING_ON"),
        new ShaderKeyword("_DISTORTION_ON"),
    };

    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet,
                                IList<ShaderCompilerData> data)
    {
        int beforeCount = data.Count;

        for (int i = data.Count - 1; i >= 0; i--)
        {
            foreach (var keyword in s_SafeToStrip)
            {
                if (data[i].shaderKeywordSet.IsEnabled(keyword))
                {
                    data.RemoveAt(i);
                    break;
                }
            }
        }

        if (beforeCount != data.Count)
            Debug.Log($"[QuestStripper] {shader.name}: {beforeCount} → {data.Count} variants");
    }
} 