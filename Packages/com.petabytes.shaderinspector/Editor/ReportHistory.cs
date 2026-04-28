using System;
using System.Collections.Generic;
using System.IO;
using Codice.Client.BaseCommands;
using UnityEditor;
using UnityEngine;
namespace petabytes.shaderinspector
{
public class ReportHistory : ScriptableObject
{
    public static ReportHistory Instance
    {
        get
        {
            if (_instance == null)
            {
                var guids = AssetDatabase.FindAssets("t:ReportHistory");
                if (guids == null || guids.Length == 0)
                {
                    _instance = ScriptableObject.CreateInstance<ReportHistory>();
                    const string historyAssetPath = "Assets/ShaderInspector/";
                    if (!Directory.Exists(historyAssetPath))
                    {
                        Directory.CreateDirectory(historyAssetPath);
                    }
                    _instance.assetPath = $"{historyAssetPath}/ShaderReportHistory.asset";
                    AssetDatabase.CreateAsset(_instance, _instance.assetPath);
                }
                else
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _instance = AssetDatabase.LoadAssetAtPath<ReportHistory>(assetPath);
                    _instance.assetPath = assetPath;
                }
                _instance.RebuildDictionary();
            }

            return _instance;
        }
    }

    public void SaveLastReport(string name, AnalyzeReport[] reports)
    {
        if (history.ContainsKey(name))
        {
            history.Remove(name);
            history.Add(name, reports);
        }
        else
        {
            history.Add(name, reports);
        }
    }

    public bool GetLastReport(string name, out AnalyzeReport[] result)
    {
        return history.TryGetValue(name, out result);
    }

    public void Save()
    {
        shadernames.Clear();
        reports.Clear();
        
        foreach(var kv in history)
        {
            shadernames.Add(kv.Key);
            reports.Add(kv.Value[0]);
            reports.Add(kv.Value[1]);
        }
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [SerializeField]
    public List<AnalyzeReport> referenceShaderReports;
    public string MaliOcPath = String.Empty;
    
    private Dictionary<string, AnalyzeReport[]> history = new Dictionary<string, AnalyzeReport[]>();
    // for serialization
    [SerializeField]
    private List<string> shadernames = new List<string>();
    [SerializeField]
    private List<AnalyzeReport> reports = new List<AnalyzeReport>();
    private bool initialized = false;
    private static ReportHistory _instance = null;
    private string assetPath;
    
    private void RebuildDictionary()
    {
        if (initialized)
            return;
        if (shadernames != null)
        {
            for (int i = 0; i < shadernames.Count; ++i)
            {
                history.Add(shadernames[i], new AnalyzeReport[] {reports[2 * i], reports[2*i + 1]});
            }
        }

        initialized = true;
    }
}
}