using System;
using System.Collections.Generic;
using UnityEngine;
namespace petabytes.shaderinspector
{
[Serializable]
public struct MaliocInfo
{
    [Serializable]
    public struct GPUCoreInfo
    {
        public string[] apis;
        public string core;
    }

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
    
    public GPUCoreInfo[] cores;
    public Producer producer;
    public Schema schema;
}
}