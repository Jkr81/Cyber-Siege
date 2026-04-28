using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
#if UNITY_2021_1_OR_NEWER
using UnityEngine.Search;
using UnityEditor.Search;
#endif

namespace petabytes.shaderinspector
{
public class ShaderAnalyse: EditorWindow
{
    private string shaderCode = "";
    private string vertCode = "";
    private string fragCode = "";
    private string analyseResultVert = "";
    private AnalyzeReport vertReport;
    private string analyseResultFrag = "";
    private AnalyzeReport fragReport;
    
    private List<string[]> keywordTuples = null;
    private int[] keywordSelections;
    
    private Vector2 shaderCodeScrollPositionVert;
    private Vector2 shaderCodeScrollPositionFrag;
    private Vector2 keywordTupleScrollPosition;
    private Shader targetShader;
    private Material materialForKeyword;

    private int _subShaderCount = -1;
    private int _passCount = -1;

    private int _selectedSubShader = -1;
    private int _selectedPass = -1;

    private int[] _passSelectValues;
    private string[] _passDisplayNames;
    
    private int[] _subshaderSelectValues;
    private string[] _subshaderDisplayNames;
    private ShaderDataUtil.ParsedKeywordData _parsedData;

    private const string tempPath = "Temp/ShaderInspector";
    private List<ValueChange> vertChanges = null;
    private List<ValueChange> fragChanges = null;

    private List<ValueChange> refVertChanges = null;
    private List<ValueChange> refFragChanges = null;

    private int changeStyleIndex;
    private string maliocPath;
    private GUIStyle reportStyleUp;
    private GUIStyle reportStyleEq;
    private GUIStyle reportStyleDown;
    private GUIStyle headerStyle;

    private float toolbarHeight;

    private MaliocInfo maliocInfo;
    private string[] gpuSelectionDisplayNames;
    private string[] gpuSelectionNames;
    private int[] gpuSelectionValues;
    private int selectedGpu = 0;
    private string maliocVersion = "None";

    private string[] sectionNames = new string[]
        {"Analyze Result", "Shader Variant Code"};

    private int sectionSelection = 0;

    private string[] resultTypeNames = new string[] { "Vertex", "Fragment" };
    private int resultTypeSelection = 0;

    private float controlWidth;
    private float resultWidth;
    private bool compareWithReference;
#if UNITY_2021_1_OR_NEWER
    private bool useAdvancedSearch = true;
#endif
    private const bool outputDebugReport = false;
    private MethodInfo clearSearchMethod = null;
    private Dictionary<string, AnalyzeReport.Pipeline> pipelineDescription = new Dictionary<string, AnalyzeReport.Pipeline>();

    private Rect splitterRect;
    private float splitterPos;
    private bool isDraggingSplitter;

    // Data get from here: 
    // https://www.epey.co.uk/phone/gpu/immortalis-g715/
    // https://www.notebookcheck.net/ARM-Mali-G715-MP7-GPU-Benchmarks-and-Specs.762528.0.html
    private Dictionary<string, string> gpuNameToDeviceName = new Dictionary<string, string>()
    {
        {"Immortalis-G720", "Vivo X100 Pro,Vivo X100"},
        {"Mali-G720", "None"},
        {"Mali-G620", "None"},
        {"Immortalis-G715", "Xiaomi 13T Pro,Redmi K60 Ultra,Oppo Find X6,Vivo X90"},
        {"Mali-G715", "Google Pixel 8,Google Pixel 8 Pro"},
        {"Mali-G710", "OnePlus Nord 3,Oppo Find N2 Flip,Vivo iQOO Neo7,Vivo X80"},
        {"Mali-G615", "Redmi K70E"},
        {"Mali-G610", "Xiaomi 13T,Vivo iQOO Z8,Xiaomi Redmi K60E,Oppo Reno9 Pro"},
        {"Mali-G510", "None"},
        {"Mali-G310", "None"},
        {"Mali-G78AE", "Automotive and Industry"},
        {"Mali-G78", "Vivo X60,Vivo X70,Huawei Mate 60,Huawei Mate X2"},
        {"Mali-G77", "Samsung Galaxy S20,Huawei Nova 8"},
        {"Mali-G68", "Samsung Galaxy A53 5G,Samsung Galaxy Tab S9"},
        {"Mali-G57", "realme C31,Huawei Nova 7 SE,Honor X10"},
        {"Mali-G76", "Redmi Note 10S,Xiaomi Redmi Note 8 Pro,realme 7"},
        {"Mali-G72", "Samsung Galaxy M31s,Oppo A91,Vivo V15,ZTE Blade V10"},
        {"Mali-G71", "Samsung Galaxy M30,Samsung Galaxy A8+ Plus,Samsung Galaxy A7"},
        {"Mali-G52", "Vivo S1,Redmi 10 2022,Huawei Y9a"},
        {"Mali-G51", "Huawei P30 Lite New Edition,Huawei Nova 3i,Huawei P20 Lite 2019"},
        {"Mali-G31", "None"},
        {"Mali-T880", "Huawei P9 Plus,Huawei Mate 8,Huawei Honor 8"},
        {"Mali-T860", "HTC One A9s,LG Q7+,Meizu m3 note,Sony Xperia XA"},
        {"Mali-T830", "Huawei P20 Lite,Honor 7X,Huawei Nova 3e"},
        {"Mali-T820", "Nokia C10,Alcatel 1C 2019"},
        {"Mali-T760", "Samsung Galaxy S6,Samsung Galaxy Note 5,Samsung Galaxy S6 Edge"},
        {"Mali-T720", "Motorola Moto E4 Plus,Samsung Galaxy J4,Nokia 3"},
    };

    [MenuItem("Window/Analysis/Shader Inspector")]
    static void Start()
    {
        var window = EditorWindow.GetWindow<ShaderAnalyse>("Shader Inspector");
        window.Show();
    }

    private void OnEnable()
    {
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }

        if (ReportHistory.Instance.MaliOcPath != String.Empty)
        {
            maliocPath = ReportHistory.Instance.MaliOcPath;
        }
        else
        {
#if UNITY_EDITOR_OSX
            maliocPath = Path.GetFullPath("Packages/com.petabytes.shaderinspector/malioc~/OSX/malioc");
#elif UNITY_EDITOR_WIN
            maliocPath = Path.GetFullPath("Packages\\com.petabytes.shaderinspector\\malioc~\\Windows\\malioc.exe");
#endif   
        }
        // UnityEngine.Debug.Log(File.Exists(window.maliocPath));
        // Parse malioc --list

        StringBuilder sb = new StringBuilder();
        try
        {
            var process = ExecuteCmd(maliocPath, "--list --format json",
                (sender, a) => sb.Append(a.Data),
                (sender, a) => UnityEngine.Debug.LogError(a.Data));
            process.BeginOutputReadLine();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Debug.LogError("Can't find mali compiler, please set correct Mali Compiler Path");
            return;
        }
        
        // Get gpu list and version info
        maliocInfo = JsonUtility.FromJson<MaliocInfo>(sb.ToString());
        ref var cores = ref maliocInfo.cores;
        gpuSelectionDisplayNames = new string[cores.Length];
        gpuSelectionValues = new int[cores.Length];
        gpuSelectionNames = new string[cores.Length];
        for (int i = 0; i < cores.Length; ++i)
        {
            gpuNameToDeviceName.TryGetValue(cores[i].core, out var devices);
            gpuSelectionDisplayNames[i] = $"{cores[i].core} ({devices})";
            gpuSelectionValues[i] = i;
            gpuSelectionNames[i] = cores[i].core;
        }

        ref int[] version = ref maliocInfo.producer.version;
        maliocVersion = $"Mali Compiler Version: {version[0]}.{version[1]}.{version[2]}";
    }

    private void OnLostFocus()
    {
        ReportHistory.Instance.Save();
    }

    Process ExecuteCmd(string cmd, string args, DataReceivedEventHandler actionMsg, DataReceivedEventHandler actionError)
    {
        System.Diagnostics.Process p = new System.Diagnostics.Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.RedirectStandardInput = false;
        p.StartInfo.CreateNoWindow = true; // no visible
     
#if UNITY_EDITOR_WIN
        p.StartInfo.FileName = Path.Combine(System.IO.Directory.GetCurrentDirectory(), cmd);
#else
        p.StartInfo.FileName = cmd;
#endif
        p.StartInfo.Arguments = args;
        p.OutputDataReceived += actionMsg;
        //p.ErrorDataReceived += actionError;
 
        p.Start();
        return p;
    }

    void GeneratePopInfo(int count, ref int[] indices, ref string[] names, string prefix)
    {
        indices = new int[count];
        names = new string[count];

        for (int i = 0; i < count; ++i)
        {
            indices[i] = i;
            names[i] = $"{prefix} {i}";
        }
    }

    string[] GetShaderKeywordTuples(int selectedSubShader, int selectedPass)
    {
        return _parsedData.snippetKeywords[_parsedData.subShaders[selectedSubShader].passes[selectedPass].snippetId].uniqueKeywordTuples.ToArray();
    }
    // Update is called once per frame
    void OnGUI()
    {
        reportStyleUp = new GUIStyle(EditorStyles.linkLabel);
        reportStyleUp.normal.textColor = Color.red;
        reportStyleEq = new GUIStyle(EditorStyles.linkLabel);
        reportStyleEq.normal.textColor = EditorStyles.label.normal.textColor;
        reportStyleDown = new GUIStyle(EditorStyles.linkLabel);
        reportStyleDown.normal.textColor = new Color(0.2f, 0.5f, 0.2f);
        headerStyle = EditorStyles.largeLabel;
        toolbarHeight = EditorStyles.toolbar.CalcHeight(new GUIContent("Test"), 1000) + EditorGUIUtility.standardVerticalSpacing;
        
        controlWidth = Math.Max(400, splitterPos);
        resultWidth = position.width - controlWidth;
        splitterPos = controlWidth;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(maliocVersion, EditorStyles.miniBoldLabel);
        if (GUILayout.Button("..."))
        {
            ReportHistory.Instance.MaliOcPath = EditorUtility.OpenFilePanel("Select Mali Compiler", "", "");
            OnEnable();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        if (maliocVersion == "None")
        {
            //using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox("Please set correct mali offline compiler path", MessageType.Warning);
                if (GUILayout.Button("Click to visit ARM official site about installation"))
                {
                    Application.OpenURL("https://developer.arm.com/documentation/101863/8-8/Using-Mali-Offline-Compiler/Install-Mali-Offline-Compiler?lang=en");
                }    
            }
            return;
        }
        
        EditorGUILayout.BeginHorizontal();
        DoControls();
        DrawSplitter(new Vector2(5, position.height));
        DoResults();
        EditorGUILayout.EndHorizontal();
        
        HandleSplitterDrag();
    }
    
    private void HandleSplitterDrag()
    {
        Event e = Event.current;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (splitterRect.Contains(e.mousePosition))
                {
                    isDraggingSplitter = true;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isDraggingSplitter)
                {
                    // Update splitterPos based on mouse movement
                    splitterPos = e.mousePosition.x;
                    // Clamp to reasonable limits (e.g., min 50, max window width - 50)
                    splitterPos = Mathf.Clamp(splitterPos, 50f, position.width - 50f);
                    Repaint(); // Force immediate update
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (isDraggingSplitter)
                {
                    isDraggingSplitter = false;
                    e.Use();
                }
                break;
        }
    }

    private void DrawSplitter(Vector2 size)
    {
        // Get the rect for the splitter area based on the current layout
        //splitterRect = GUILayoutUtility.GetRect(size.x, size.y, GUIStyle.none);
        splitterRect = new Rect(new Vector2(splitterPos, 0), new Vector2(size.x, size.y));

        // Draw a visible bar (you can customize the color/style)
        // EditorGUI.DrawRect(splitterRect, new Color(0.5f, 0.0f, 0.0f));

        // Change cursor when hovering over the splitter
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
    }

    private void CompareReferenceReportIfNeeded()
    {
        if (compareWithReference)
        {
            if (ReportHistory.Instance.referenceShaderReports?.Count > (selectedGpu * 2 + 1))
            {
                refVertChanges = CompareReports(vertReport, ReportHistory.Instance.referenceShaderReports[selectedGpu * 2]);
                refFragChanges = CompareReports(fragReport, ReportHistory.Instance.referenceShaderReports[selectedGpu * 2 + 1]);
            }
            else
            {
                Debug.LogWarning("Missing reference performance data");
            }
        }
    }

    public void DoResults()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(resultWidth));
        sectionSelection = GUILayout.Toolbar(sectionSelection, sectionNames,GUILayout.Width(resultWidth));
        EditorGUILayout.BeginHorizontal();
        resultTypeSelection = GUILayout.Toolbar(resultTypeSelection, resultTypeNames);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        if (sectionSelection == 0)
        {
            DoAnalyzeReport();

            // EditorGUI.BeginChangeCheck();
            // compareWithReference = EditorGUILayout.Toggle(new GUIContent("Compare with URP Lit", "Pass: ForwardLit\nKeywords:\n_MAIN_LIGHT_SHADOWS_CASCADE\n_ADDITIONAL_LIGHTS\n_ADDITIONAL_LIGHTS_SHADOWS\n_SHAODWS_SOFT\n"), compareWithReference);
            // if (EditorGUI.EndChangeCheck())
            //     CompareReferenceReportIfNeeded();
        }
        else if (sectionSelection == 1)
        {
            DoShaderCode();
        }

        EditorGUILayout.EndVertical();
    }
#if UNITY_2021_1_OR_NEWER
    public void SelectMaterial(Shader shader)
    {
        var searchWindow = SearchService.ShowObjectPicker(SelectHandler, TrackingHandler, $"t:material (a:assets or a:packages) shader:\"{Path.GetFileName(shader.name)}\"", "Material", typeof(Material));

        // Must call ClearSearch each time, otherwise search text will be the last saved value.
        if (clearSearchMethod == null)
        {
            var assembly = searchWindow.GetType().Assembly;
            var qsType = assembly.GetType("UnityEditor.Search.QuickSearch");
            clearSearchMethod = qsType.GetMethod("ClearSearch", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        clearSearchMethod.Invoke(searchWindow, null);
    }

    void SelectHandler(UnityEngine.Object searchItem, bool canceled)
    {
        materialForKeyword = searchItem as Material;
        // Debug.Log($"Select {materialForKeyword.shader.ToString()} canceled {canceled}");
        ExtractKeywordFromMaterial();

        Repaint();
    }

    void TrackingHandler(UnityEngine.Object searchItem)
    {
        //Debug.Log($"Tracking {searchItem}");
        materialForKeyword = searchItem as Material;
        ExtractKeywordFromMaterial();

        Repaint();
    }
    
#endif
    public void DoControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(controlWidth));
        EditorGUI.BeginChangeCheck();
        selectedGpu = EditorGUILayout.IntPopup("GPU name", selectedGpu, gpuSelectionDisplayNames, gpuSelectionValues);
        if (EditorGUI.EndChangeCheck())
        {
            CompareReferenceReportIfNeeded();
        }

        bool needsRefreshList = false;
        EditorGUI.BeginChangeCheck();
        targetShader = (Shader)EditorGUILayout.ObjectField("Select Shader", targetShader, typeof(Shader), true);
        if (EditorGUI.EndChangeCheck())
        {
            _selectedPass = -1;
            _selectedSubShader = -1;
            needsRefreshList = true;
            if (targetShader != null)
            {
                _parsedData = ShaderDataUtil.GetShaderKeywordData(targetShader);
                if (_parsedData == null)
                {
                    Debug.LogError("Builtin shader not supported!");
                }
                else
                {
                    _subShaderCount = _parsedData.subShaders.Length;
                }
                
                if (_subShaderCount > 0)
                {
                    _selectedSubShader = 0;
                    GeneratePopInfo(_subShaderCount, ref _subshaderSelectValues, ref _subshaderDisplayNames, "SubShader");
                }
            }
            else
            {
                _subShaderCount = -1;
                _passCount = -1;
            }
            materialForKeyword = null;
        }

        if (_subShaderCount > 0)
        {
            EditorGUI.BeginChangeCheck();
            _selectedSubShader = EditorGUILayout.IntPopup("SubShader Index", _selectedSubShader, _subshaderDisplayNames, _subshaderSelectValues);
            if (EditorGUI.EndChangeCheck() || needsRefreshList)
            {
                _selectedPass = -1;
                needsRefreshList = true;
                _passCount = _parsedData.subShaders[_selectedSubShader].passes.Length;
                if (_passCount > 0)
                {
                    _selectedPass = 0;
                    _passSelectValues = new int[_passCount];
                    _passDisplayNames = new string[_passCount];
                    for (int i = 0; i < _passCount; ++i)
                    {
                        _passSelectValues[i] = i;
                        _passDisplayNames[i] = _parsedData.subShaders[_selectedSubShader].passes[i].name;
                    }
                }
                
                materialForKeyword = null;
            }
        }

        // For special case when we have no subshader and no pass, we also determine if subshaderCount > 0
        if (_subShaderCount > 0 && _passCount > 0)
        {
            EditorGUI.BeginChangeCheck();
            _selectedPass = EditorGUILayout.IntPopup("Pass Index", _selectedPass, _passDisplayNames, _passSelectValues);
            if (EditorGUI.EndChangeCheck() || needsRefreshList)
            {
                needsRefreshList = true;
                var tupleString = GetShaderKeywordTuples(_selectedSubShader, _selectedPass);
                keywordTuples = new List<string[]>(tupleString.Length);
                for (int i = 0; i < tupleString.Length; ++i)
                {
                    keywordTuples.Add(tupleString[i].Trim(' ').Split(' '));
                }
                
                if (keywordSelections == null || keywordSelections.Length != keywordTuples.Count)
                {
                    keywordSelections = new int[keywordTuples.Count];
                }
                for (int i = 0; i < keywordSelections.Length; ++i)
                    keywordSelections[i] = -1;
                materialForKeyword = null;
            }
        }
        
        if (keywordTuples != null)
        {
            
#if UNITY_2021_1_OR_NEWER
            EditorGUILayout.BeginHorizontal();
            useAdvancedSearch = GUILayout.Toggle(useAdvancedSearch, "Use advance filter");
            if (useAdvancedSearch)
            {
                if (GUILayout.Button("Choose keyword from material"))
                    SelectMaterial(targetShader);
            }
            else
#endif
            {
                EditorGUI.BeginChangeCheck();
                materialForKeyword = (Material)EditorGUILayout.ObjectField("Keywords From Material", materialForKeyword, typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    ExtractKeywordFromMaterial();
                }
            }
#if UNITY_2021_1_OR_NEWER
            EditorGUILayout.EndHorizontal();
#endif
            DoKeywordTuplesSelection();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            
            if (GUILayout.Button("Analyze Shader Snippet"))
            {
                AnalyzeShader();
                CompareReferenceReportIfNeeded();
            }
            
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.IconContent("_Help", "Arm Mali Docs")))
            {
                Application.OpenURL("https://developer.arm.com/documentation/101863/0800/Using-Mali-Offline-Compiler/Performance-analysis?lang=en");
            }

#if (outputDebugReport)
            {
                // Dump shader report for all gpu name
                if (GUILayout.Button("Dump Shader Report"))
                {
                    ReportHistory.Instance.referenceShaderReports = new List<AnalyzeReport>();
                    for (int i = 0; i < gpuSelectionDisplayNames.Length; ++i)
                    {
                        selectedGpu = i;
                        AnalyzeShader();
                        ReportHistory.Instance.referenceShaderReports.Add(vertReport);
                        ReportHistory.Instance.referenceShaderReports.Add(fragReport);
                    }
                }
            }
#endif
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    public void DoKeywordTuplesSelection()
    {
        keywordTupleScrollPosition = GUILayout.BeginScrollView(keywordTupleScrollPosition);
        for (int i = 0; i < keywordTuples.Count; ++i)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int selectionTmp = GUILayout.Toolbar(keywordSelections[i], keywordTuples[i]);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectionTmp != keywordSelections[i])
                {
                    keywordSelections[i] = selectionTmp;
                }
                else
                {
                    keywordSelections[i] = -1;
                }    
            }
                
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    public void ExtractKeywordFromMaterial()
    {
        if (materialForKeyword != null && materialForKeyword.shader == targetShader)
        {
            // Extract keywords from material
            var enabledKeywords = materialForKeyword.shaderKeywords.ToList();
#if UNITY_2021_2_OR_NEWER
            foreach (var global in Shader.enabledGlobalKeywords)
            {
                enabledKeywords.Add(global.name);
            }
#endif
            // Reset all selections
            for(int i = 0; i < keywordSelections.Length; ++i)
            {
                keywordSelections[i] = -1;
            }

            foreach (var keyword in enabledKeywords)
            {
                for (int tupleIdx = 0; tupleIdx < keywordTuples.Count; ++tupleIdx)
                {
                    for (int keywordIdx = 0; keywordIdx < keywordTuples[tupleIdx].Length; ++keywordIdx)
                    {
                        if (keywordTuples[tupleIdx][keywordIdx] == keyword)
                        {
                            keywordSelections[tupleIdx] = keywordIdx;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Material does not match selected shader");
        }
    }
    public void DoAnalyzeReport()
    {
        if (resultTypeSelection == 0)
        {
            GUILayout.Label("Vertex Shader report", headerStyle);
            DoShaderReport(vertReport, compareWithReference?refVertChanges:vertChanges);
            //GUILayout.TextArea(analyseResultVert);
        }
        else
        {
            GUILayout.Label("Fragment Shader report", headerStyle);
            //GUILayout.TextArea(analyseResultFrag);
            DoShaderReport(fragReport, compareWithReference?refFragChanges:fragChanges);
        }
    }
    public void DoShaderCode()
    {
        if (resultTypeSelection == 0)
        {
            GUILayout.Label("Vertex Shader", headerStyle);
            shaderCodeScrollPositionVert =
                GUILayout.BeginScrollView(shaderCodeScrollPositionVert);
            GUILayout.TextArea(vertCode);
            GUILayout.EndScrollView();    
        }
        else
        {

            GUILayout.Label("Fragment Shader", headerStyle);
            shaderCodeScrollPositionFrag =
                GUILayout.BeginScrollView(shaderCodeScrollPositionFrag);
            GUILayout.TextArea(fragCode);
            GUILayout.EndScrollView();            
        }
    }

    public void DoReferenceReport()
    {
        try
        {
            refVertChanges = CompareReports(vertReport, ReportHistory.Instance.referenceShaderReports[selectedGpu * 2]);
            refFragChanges = CompareReports(fragReport, ReportHistory.Instance.referenceShaderReports[selectedGpu * 2 + 1]);
        }
        catch(Exception)
        {
            // Has different report format with reference shader
            //Debug.LogError("Failed to show reference shader report");
        }

        if (resultTypeSelection == 0)
        {
            GUILayout.Label("URP Lit Vertex report", headerStyle);
            DoShaderReport(ReportHistory.Instance.referenceShaderReports[selectedGpu * 2], refVertChanges);
            //GUILayout.TextArea(analyseResultVert);
        }
        else
        {
            GUILayout.Label("URP Lit Fragment report", headerStyle);
            //GUILayout.TextArea(analyseResultFrag);
            DoShaderReport(ReportHistory.Instance.referenceShaderReports[selectedGpu * 2 + 1], refFragChanges);
        }
    }

    public void AnalyzeShader()
    {
        if (targetShader != null)
        {
            var selectedKeywords = new List<string>();
            for (int i = 0; i < keywordSelections.Length; ++i)
            {
                if (keywordSelections[i] != -1 && keywordTuples != null)
                {
                    selectedKeywords.Add(keywordTuples[i][keywordSelections[i]]);
                }
            }

            // Call ShaderCompiler disassemble the byte code
            shaderCode = ShaderDataUtil.GetShaderSubProgramSnippet(targetShader, _selectedSubShader, _selectedPass, ShaderType.Vertex, selectedKeywords.ToArray(), ShaderCompilerPlatform.GLES3x, BuildTarget.Android);
            // var platformKeywords = ShaderUtil.GetShaderPlatformKeywordsForBuildTarget(ShaderCompilerPlatform.GLES3x, BuildTarget.Android);
            //shaderCode = ShaderUtil.GetShaderSubProgramSnippet(targetShader, _selectedSubShader, _selectedPass, ShaderType.Vertex, platformKeywords, selectedKeywords.ToArray(),
            //    ShaderCompilerPlatform.GLES3x, BuildTarget.Android);
            if (shaderCode.Length == 0)
            {
                Debug.LogError($"Failed to get shader snippet, unsupported subshader maybe selected!");
                return;
            }
                
            // Feed to malioc, replace version 300 to 310 to avoid malioc compiler error
            shaderCode = shaderCode.Replace("#version 300 es", "#version 310 es");//.Replace("#version 310 es", "#version 320 es");
            
            int vertStart = shaderCode.IndexOf("#ifdef VERTEX") + 14;
            int fragStart = shaderCode.IndexOf("#ifdef FRAGMENT") + 16;
            if (vertStart != -1 && fragStart != -1)
            {
                vertCode = shaderCode.Substring(vertStart, shaderCode.LastIndexOf("#endif", fragStart - 1) - vertStart);
                fragCode = shaderCode.Substring(fragStart, shaderCode.LastIndexOf("#endif") - fragStart );
            }
            else
            {
                if (vertStart != -1)
                {
                    vertCode = shaderCode.Substring(vertStart, shaderCode.LastIndexOf("#endif") - vertStart);
                }
            }

            string vertPath = $"{tempPath}/{DateTime.Now.Ticks}.vert";
            string fragPath = $"{tempPath}/{DateTime.Now.Ticks}.frag";
            SaveShaderToFile(vertCode, fragCode, vertPath, fragPath);
        
            StringBuilder sb = new StringBuilder();
            string cmdStr = $"-d --format json -c \"{gpuSelectionNames[selectedGpu]}\" {vertPath}"; 
            var process = ExecuteCmd(maliocPath, cmdStr, 
                (sender, a) => sb.AppendLine(a.Data),
                (sender, a) => UnityEngine.Debug.LogError(a.Data));

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            analyseResultVert = sb.ToString();
            sb.Clear();
#if (outputDebugReport)
            {
                Debug.Log(analyseResultVert);
            }
#endif
            pipelineDescription.Clear();
            try
            {
                vertReport = JsonUtility.FromJson<AnalyzeReport>(analyseResultVert);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e}, cmd {cmdStr}, json content {analyseResultVert}");
            }
            if (vertReport.shaders?.Length > 0)
            {
                foreach(var pipe in vertReport.shaders[0].hardware.pipelines)
                {
                    if (!pipelineDescription.ContainsKey(pipe.name))
                        pipelineDescription.Add(pipe.name, pipe);
                }
            }
            
            cmdStr = $"-d --format json -c \"{gpuSelectionNames[selectedGpu]}\" {fragPath}";
            process = ExecuteCmd($"{maliocPath}", cmdStr, 
                (sender, a) => sb.AppendLine(a.Data),
                (sender, a) => UnityEngine.Debug.LogError(a.Data));

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            analyseResultFrag = sb.ToString();
#if (outputDebugReport)
            {
                Debug.Log(analyseResultFrag);
            }
#endif
            try
            {
                fragReport = JsonUtility.FromJson<AnalyzeReport>(analyseResultFrag);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e}, cmd {cmdStr}, json content {analyseResultFrag}");
            }
            if (fragReport.shaders?.Length > 0)
            {
                foreach (var pipe in fragReport.shaders[0].hardware.pipelines)
                {
                    if (!pipelineDescription.ContainsKey(pipe.name))
                        pipelineDescription.Add(pipe.name, pipe);
                }
            }
            if (ReportHistory.Instance.GetLastReport(targetShader.name, out var reports))
            {
                // compare
                try
                {
                    vertChanges = CompareReports(vertReport, reports[0]);
                    fragChanges = CompareReports(fragReport, reports[1]);
                }
                catch (Exception)
                {
                    vertChanges = null;
                    fragChanges = null;
                }
            }
            
            ReportHistory.Instance.SaveLastReport(targetShader.name, new []{vertReport, fragReport});
        }
    }

    GUIStyle GetStyle(ChangeType change)
    {
        switch (change)
        {
            case ChangeType.Down:
                return reportStyleDown;
            case ChangeType.Equal:
                return reportStyleEq;
            case ChangeType.Up:
                return reportStyleUp;
        }

        return EditorStyles.label;
    }

    private void DoLabelsHorizontalFixedWidthWithToolTip(Func<int, GUIContent> getString, int count, Func<int, GUIStyle> getStyle, out float[] width)
    {
        var indent = EditorGUI.IndentedRect(new Rect(0, 0, 0, 0));
        if (count <= 0)
        {
            width = null;
            return;
        }

        width = new float[count];
        GUILayout.BeginHorizontal();
        GUILayout.Space(indent.x);

        float totalWidth = 0;
        for (int i = 0; i < count; ++i)
        {
            var str = getString(i);
            var style = getStyle(i);
            width[i] = style.CalcSize(new GUIContent(str)).x;
            totalWidth += width[i];
        }
        
        float space = (resultWidth - totalWidth) / count - 5;

        for (int i = 0; i < count; ++i)
        {
            var str = getString(i);
            var  style = getStyle(i);
            width[i] += space;
            GUILayout.Label(str, style, GUILayout.Width(width[i]));
        }
        GUILayout.EndHorizontal();
    }
    
    private void DoLabelsHorizontalFixedWidth(Func<int, string> getString, int count, Func<int, GUIStyle> getStyle, float[] width)
    {
        var indent = EditorGUI.IndentedRect(new Rect(0, 0, 0, 0));
        if (count <= 0 || width == null)
            return;

        GUILayout.BeginHorizontal();
        GUILayout.Space(indent.x);
        for (int i = 0; i < count; ++i)
        {
            GUILayout.Label(getString(i), getStyle(i), GUILayout.Width(width[i]));
        }
        GUILayout.EndHorizontal();
    }
    
    private void DoShaderReport(AnalyzeReport report, List<ValueChange> changes)
    {
        if (report.shaders == null)
            return;
        for (int i = 0; i < report.shaders.Length; ++i)
        {
            var shader = report.shaders[i];
            if (shader.properties == null)
            {
                GUILayout.Label("Failed to analyze shader, unsupported keywords maybe selected!");
                continue;
            }

            GUILayout.Label("Shader Properties", EditorStyles.boldLabel);
            float[] widths;
            using (new EditorGUI.IndentLevelScope())
            {
                DoLabelsHorizontalFixedWidthWithToolTip((num) =>
                {
                    var property = shader.properties?[num];
                    return new GUIContent(property?.display_name, property?.description);
                }, shader.properties.Length, (num) => EditorStyles.linkLabel, out widths);
                DoLabelsHorizontalFixedWidth((num) =>shader.properties?[num].value, shader.properties.Length, (num) => reportStyleEq, widths);
            }
            
            
            GUILayout.Label("Shader Variants", EditorStyles.boldLabel);

            changeStyleIndex = 0;
            for (int j = 0; j < shader.variants.Length; ++j)
            {
                var variant = shader.variants[j];
                EditorGUILayout.LabelField($"{variant.name}:");
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Properties");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        DoLabelsHorizontalFixedWidthWithToolTip((num) =>
                            {
                                var prop = variant.properties[num];
                                return new GUIContent(prop.display_name, prop.description);
                            },
                            variant.properties.Length, (num) => EditorStyles.linkLabel, out widths);
                        DoLabelsHorizontalFixedWidth((num) =>
                        {
                            if (changes?.Count > 0)
                            {
                                return $"{variant.properties[num].value} [{(compareWithReference?"ref ":"last ")}{changes[changeStyleIndex].oldValue}]";
                            }
                            return variant.properties[num].value;
                        }, variant.properties.Length,(num) =>changes?.Count > 0 ?GetStyle(changes[changeStyleIndex++].type) : reportStyleEq, widths);
                    }
                    
                    EditorGUILayout.LabelField("Performance(total cycles)");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        DoLabelsHorizontalFixedWidthWithToolTip((num) => 
                        {
                            var pipeName = variant.performance.pipelines[num];
                            if (pipelineDescription.TryGetValue(pipeName, out var pipeDesc))
                                return new GUIContent(pipeDesc.display_name, pipeDesc.description);
                            return new GUIContent(pipeName, "");
                        }, 
                        variant.performance.pipelines.Length, (num) => EditorStyles.linkLabel, out widths);
                        DoLabelsHorizontalFixedWidth(
                            (num) => 
                            {
                                if (changes?.Count > 0)
                                {
                                    return $"{variant.performance.total_cycles.cycle_count[num].ToString()} [{(compareWithReference ? "ref " : "last ")}{changes[changeStyleIndex].oldValue}]";
                                }
                                return variant.performance.total_cycles.cycle_count[num].ToString();
                            },
                            variant.performance.total_cycles.cycle_count.Length,(num) => changes ?.Count > 0 ? GetStyle(changes[changeStyleIndex++].type): reportStyleEq, widths);
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Bound Pipeline: ", reportStyleEq);

                        for (int boundPipeIdx = 0; boundPipeIdx < variant.performance.total_cycles.bound_pipelines.Length; ++boundPipeIdx)
                        {
                            var pipeName = string.Empty;
                            if (pipelineDescription.TryGetValue(variant.performance.total_cycles.bound_pipelines[boundPipeIdx], out var pipeDesc))
                                pipeName = pipeDesc.display_name;
                            else
                                pipeName = variant.performance.total_cycles.bound_pipelines[boundPipeIdx];

                            EditorGUILayout.LabelField(pipeName, reportStyleEq);
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

    }

    private void SaveShaderToFile(string vert, string frag, string vertPath, string fragPath)
    {
        if (vert.Length > 0)
        {
            FileStream vertFs = File.Open(vertPath, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(vertFs);
            writer.Write(vert);
            writer.Close();
        }

        if (frag.Length > 0)
        {
            FileStream fragFs = File.Open(fragPath, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(fragFs);
            writer.Write(frag);
            writer.Close();
        }
    }

    enum ChangeType
    {
        None,
        Up,
        Down,
        Equal
    }

    struct ValueChange
    {
        public ChangeType type;
        public string oldValue;
    }

    private ValueChange ValueChangeType(string s1, string oldVal)
    {
        float.TryParse(s1, out float v1);
        float.TryParse(oldVal, out float v2);
        var type = v1 > v2 ? ChangeType.Up : (Mathf.Approximately(v1,v2) ? ChangeType.Equal : ChangeType.Down);
        return new ValueChange() { type = type, oldValue = oldVal };
    }
    private ValueChange ValueChangeType(float v1, float oldVal)
    {
        var type = v1 > oldVal ? ChangeType.Up : (Mathf.Approximately(v1,oldVal) ? ChangeType.Equal : ChangeType.Down);
        return new ValueChange() { type = type, oldValue = oldVal.ToString()};
    }

    private int CountAttribCount(ref AnalyzeReport rep)
    {
        int ret = 0;
        if (rep.shaders == null)
            return ret;

        for (int i = 0; i < rep.shaders.Length; ++i)
        {
            var shaders = rep.shaders[i];
            for (int j = 0; j < shaders.variants?.Length; ++j)
            {
                var variant = shaders.variants[j];
                if (variant.properties != null)
                    ret += variant.properties.Length;
                if (variant.performance.total_cycles.cycle_count != null)
                    ret += variant.performance.total_cycles.cycle_count.Length;
            }
        }
        return ret;
    }

    // change of each variant, vertex shader contains 2 variant, frag contains 1
    private List<ValueChange> CompareReports(AnalyzeReport rep1, AnalyzeReport rep2)
    {   
        var ret = new List<ValueChange>();
        var attrbNum1 = CountAttribCount(ref rep1);
        var attrbNum2 = CountAttribCount(ref rep2);

        if (attrbNum1 != attrbNum2 || attrbNum1 == 0)
            return ret;
        
        for (int i = 0; i < rep1.shaders.Length; ++i)
        {
            var shaders = rep1.shaders[i];
            for (int j = 0; j < shaders.variants?.Length; ++j)
            {   
                if (shaders.variants.Length != rep2.shaders[0].variants.Length)
                    continue;
                var variant = shaders.variants[j];
                for (int k = 0; k < variant.properties?.Length; ++k)
                {
                    ret.Add(ValueChangeType(variant.properties[k].value,
                        rep2.shaders[i].variants[j].properties[k].value));
                }

                for (int k = 0; k < variant.performance.total_cycles.cycle_count?.Length; ++k)
                {
                    ret.Add(ValueChangeType(variant.performance.total_cycles.cycle_count[k],
                        rep2.shaders[i].variants[j].performance.total_cycles.cycle_count[k]));
                }
            }
        }

        Debug.Assert(ret.Count == attrbNum1);
        return ret;
    }
}
}