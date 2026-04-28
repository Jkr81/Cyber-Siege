using UnityEngine;
using UnityEditor;

public class FindMaterialsByShader : EditorWindow
{
    [MenuItem("Tools/Find Materials Using Shader")]
    static void FindMaterials()
    {
        string targetShaderName = "Universal Render Pipeline/Particles/Lit";
        Shader targetShader = Shader.Find(targetShaderName);

        if (targetShader == null)
        {
            Debug.LogError($"Shader '{targetShaderName}' not found.");
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

        Debug.Log($"Search complete. Found {found} material(s) using '{targetShaderName}'.");
    }
    
    [MenuItem("Tools/Find Models Using Autodesk Interactive")]
static void FindModels()
{
    string targetShaderName = "Autodesk Interactive";
    Shader targetShader = Shader.Find(targetShaderName);

    // First collect all matching materials
    var matchingMaterials = new System.Collections.Generic.HashSet<string>();
    foreach (string guid in AssetDatabase.FindAssets("t:Material"))
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null && mat.shader == targetShader)
            matchingMaterials.Add(path);
    }

    // Then find all prefabs/models that reference those materials
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

    Debug.Log($"Found {found} model(s)/prefab(s) referencing Autodesk Interactive materials.");
}
}