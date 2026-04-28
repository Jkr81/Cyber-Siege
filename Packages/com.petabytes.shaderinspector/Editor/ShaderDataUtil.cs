using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using System.Text;
using System.Diagnostics.Tracing;
using System;
namespace petabytes.shaderinspector
{
public class ShaderDataUtil
{
    public class ParsedKeywordData
    {
        public struct Pass
        {
            public string name;
            public int snippetId;
        }
        public struct SubShader
        {
            public Pass[] passes;
        }

        public struct SnippetData
        {
            public HashSet<string> uniqueKeywordTuples;
        }

        public SubShader[] subShaders;
        public Dictionary<int, SnippetData> snippetKeywords;
    }
    
    //[MenuItem("Tools/TestShaderAnalyze")]
    public static void Test()
    {
        Debug.Log(EditorApplication.applicationContentsPath);
        Debug.Log(GetBinary2TextPath());

        if (Selection.activeObject != null && Selection.activeObject.GetType() == typeof(Shader))
        {

        }
        /*if (Selection.assetGUIDs.Length > 0)
        {
            var artifactKey = new ArtifactKey(new GUID(Selection.assetGUIDs[0]));
            var artifactID = AssetDatabaseExperimental.LookupArtifact(artifactKey);

            AssetDatabaseExperimental.GetArtifactPaths(artifactID, out var paths);
            foreach(var p in paths)
            {
                Debug.Log($"{p} => {Path.GetFullPath(p)}");
            }
        }*/
    }


    private static string GetBinary2TextPath()
    {
#if UNITY_EDITOR_OSX
        return Path.Combine(EditorApplication.applicationContentsPath, "Tools/binary2text");
#elif UNITY_EDITOR_WIN
        return Path.Combine(EditorApplication.applicationContentsPath, "Tools/binary2text.exe");
#endif
    }

    public static ParsedKeywordData GetShaderKeywordData(Shader shader)
    {
        // Read keyword tuples from Artifact files
        var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(shader));
        var artifactKey = new ArtifactKey(guid);
        var artifactID = AssetDatabaseExperimental.LookupArtifact(artifactKey);

        AssetDatabaseExperimental.GetArtifactPaths(artifactID, out var paths);

        if (paths.Length > 0)
        {
            StringBuilder errorMsg = new StringBuilder();
            string b2tResultPath = $"Temp/{Path.GetFileNameWithoutExtension(paths[0])}_b2t.txt";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.CreateNoWindow = true; // no visible
            process.StartInfo.FileName = GetBinary2TextPath();
            process.StartInfo.Arguments = $"{Path.GetFullPath(paths[0])} {b2tResultPath}";
            process.OutputDataReceived += (sender, a) => errorMsg.Append(a.Data);
            process.ErrorDataReceived += (sender, a) => errorMsg.Append(a.Data);

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Debug.LogError($"Error when obtaining shader data! {errorMsg}, cmd is {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                return default;
            }

            // Parse binary2text reuslts get shader keyword tuples
            using (FileStream fs = new FileStream(b2tResultPath, FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(fs))
                {
                    var content = reader.ReadToEnd();
                    return ParseKeywordData(content);
                }
        }

        return default;
    }

    private static int currentIndex;

    private static bool SeekField(string content, string tag)
    {
        currentIndex = content.IndexOf(tag, currentIndex);
        return currentIndex != -1;
    }

    private static T ReadValue<T>(string content, string name)
    {
        if (SeekField(content, name))
        {
            int start = currentIndex;
            if (SeekField(content, "\n"))
            {
                String valueStr = content.Substring(start, currentIndex - start);
                
                // name, value, type triple
                // String value may contains white space
                int startIdx = valueStr.IndexOf("\"");
                int endIdx = valueStr.LastIndexOf("\"");
                string[] triple = null;
                
                if (startIdx != -1) // a string value
                {
                    triple = new string[3];
                    triple[0] = valueStr.Substring(0, startIdx - 1);
                    triple[1] = valueStr.Substring(startIdx, endIdx - startIdx + 1);
                    triple[2] = valueStr.Substring(endIdx + 2);
                }
                else
                {
                    triple = valueStr.Split(' ');   
                }
                Debug.Assert(triple.Length == 3);

                if (typeof(T) == typeof(int))
                {
                    Debug.Assert(triple[2].StartsWith("(int)"), $"type not match! {triple[2]}");
                }
                else if (typeof(T) == typeof(string))
                {
                    Debug.Assert(triple[2].StartsWith("(string)"), $"type not match! {triple[2]}");
                }
                else
                {
                    Debug.Assert(false);
                }
                return (T)Convert.ChangeType(triple[1], typeof(T));
            }
        }
        return default;
    }

    private static ParsedKeywordData ParseKeywordData(string content)
    {
        currentIndex = 0;
        ParsedKeywordData keywordData = new ParsedKeywordData();
        keywordData.snippetKeywords = new Dictionary<int, ParsedKeywordData.SnippetData>();
        
        // Parse subshaders
        if (SeekField(content, "m_SubShaders  (vector)"))
        {
            int subShaderCount = ReadValue<int>(content, "size");
            keywordData.subShaders = new ParsedKeywordData.SubShader[subShaderCount];
            for (int i = 0; i < subShaderCount; ++i)
            {
                SeekField(content, "m_Passes  (vector)");
                int passCount = ReadValue<int>(content, "size");
                keywordData.subShaders[i].passes = new ParsedKeywordData.Pass[passCount];
                for (int passIdx = 0; passIdx < passCount; ++passIdx)
                {
                    SeekField(content, "m_State  (SerializedShaderState)");
                    var passName = ReadValue<string>(content, "m_Name").Trim('\"');
                    if (passName == "")
                        passName = "No Name";
                    keywordData.subShaders[i].passes[passIdx].name = passName;
                    keywordData.subShaders[i].passes[passIdx].snippetId = ReadValue<int>(content, "gpuProgramID");
                }
            }

            // read keyword tuples
            SeekField(content, "m_CompileInfo  (ShaderCompilationInfo)");
            int snippetSize = ReadValue<int>(content, "size");
            for (int i = 0; i < snippetSize; ++i)
            {
                int snippetId = ReadValue<int>(content, "first");
                ParsedKeywordData.SnippetData snippetData = new ParsedKeywordData.SnippetData();
                snippetData.uniqueKeywordTuples = new HashSet<string>();
                // keywords for each ProgramType
                ReadKeywordTuples(content, "m_VariantsUserGlobal", snippetData.uniqueKeywordTuples);
                ReadKeywordTuples(content, "m_VariantsUserLocal", snippetData.uniqueKeywordTuples);
                ReadKeywordTuples(content, "m_VariantsBuiltin", snippetData.uniqueKeywordTuples);
                keywordData.snippetKeywords.Add(snippetId, snippetData);
            }

        }
        return keywordData;
    }

    private static void ReadKeywordTuples(string content, string variantPrefix, HashSet<string> uniqueKeywordTuples)
    {
        for (int programType = 0; programType < 7; ++programType)
        {
            if (SeekField(content, $"{variantPrefix}{programType}"))
            {
                int keywordLineCount = ReadValue<int>(content, "size");
                for (int lineIdx = 0; lineIdx < keywordLineCount; ++lineIdx)
                {
                    StringBuilder sb = new StringBuilder();
                    int keywordCount = ReadValue<int>(content, "size");
                    for (int keywordIdx = 0; keywordIdx < keywordCount; ++keywordIdx)
                    {
                        string keyword = ReadValue<string>(content, "data");
                        sb.Append(keyword.Trim('\"'));
                        if (keywordIdx != keywordCount - 1)
                            sb.Append(' ');
                    }
                    uniqueKeywordTuples.Add(sb.ToString());
                }
            }
        }
    }
    
    public static string GetShaderSubProgramSnippet(Shader shader, int subShaderIndex, int passIndex, ShaderType shaderType,
                                             string[] keywords, ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildPlatform)
    {


        var sd = ShaderUtil.GetShaderData(shader);
        var passData = sd.GetSubshader(subShaderIndex).GetPass(passIndex);

        // on gles all shader are combined in vertex shader
        var compileInfo = passData.CompileVariant(shaderType, keywords, shaderCompilerPlatform, buildPlatform);
        if (!compileInfo.Success)
        {
            foreach (var err in compileInfo.Messages)
                Debug.LogError($"Compile variant failed! Error: {err.message}");
            return string.Empty;
        }
        var data = compileInfo.ShaderData;
        return System.Text.Encoding.Default.GetString(data);
    }
}
}