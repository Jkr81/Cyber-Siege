using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace petabytes.shaderinspector
{
[Serializable]
public struct AnalyzeReport
{
    [Serializable]
    public struct Producer
    {
        public int build;
        public string documentation;
        public string name;
        public int[] version;
    }

    [Serializable]
    public struct Schema
    {
        public string name;
        public int version;
    }

    [Serializable]
    public struct Pipeline
    {
        public string description;
        public string display_name;
        public string name;
    }

    [Serializable]
    public struct Property
    {
        public string description;
        public string display_name;
        public string name;
        public string value;
    }
    
    [Serializable]
    public struct Hardware
    {
        public string architecture;
        public string core;
        public Pipeline[] pipelines;
        public string revision;
    }

    [Serializable]
    public struct Shader
    {
        public string api;
        public string type;
    }

    [Serializable]
    public struct PerformanceStats
    {
        public string[] bound_pipelines;
        public float[] cycle_count;
    }
    
    [Serializable]
    public struct Performance
    {
        public PerformanceStats longest_path_cycles;
        public string[] pipelines;
        public PerformanceStats shortest_path_cycles;
        public PerformanceStats total_cycles;
    }
    
    [Serializable]
    public struct Variant
    {
        public string name;
        public Performance performance;
        public Property[] properties;
    }
    
    [Serializable]
    public struct Shaders
    {
        public string driver;
        public string filename;
        public Hardware hardware;
        public string[] notes;
        public Property[] properties;
        public Shader shader;
        public Variant[] variants;
        public string[] warnings;
    }

    public Producer producer;
    public Schema schema;
    public Shaders[] shaders;
}
}