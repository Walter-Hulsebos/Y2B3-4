// Copyright (C) 2018 KAMGAM e.U. - All rights reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

#if UNITY_EDITOR
#define KAMGAM_FREE_FORM_SELECTION
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace kamgam.editor.freeformselection
{
    [System.Serializable]
    public class FreeFormSelection_Settings : ScriptableObject
    {
        private static FreeFormSelection_Settings _instance;

        /// <summary>
        /// Gets or creates a single instance of the settings.
        /// </summary>
        public static FreeFormSelection_Settings instance
        {
            get
            {
                if (FreeFormSelection_Settings._instance == null)
                {
                    FreeFormSelection_Settings._instance = LoadOrCreateSettingsInstance( false );
#if UNITY_5_5_OR_NEWER
                    AssetDatabase.importPackageCompleted += OnPackageImported;
#endif
                }

                return FreeFormSelection_Settings._instance;
            }
        }

        public static FreeFormSelection_Settings LoadOrCreateSettingsInstance( bool suppressWarning )
        {
            FreeFormSelection_Settings settings = getSettingsFromFile();

            // no settings from file, create an instance
            if ( settings == null )
            {
                settings = createSettingsInstance();
            }

            if( settings == null && !suppressWarning)
            {
                Debug.LogWarning("Free Form Selection: no settings file found. Call Tools > Free Form Selection > Settings to create one. Falling back to default settings.");
            }

            return settings;
        }

        public static FreeFormSelection_Settings CreateSettingsFileIfNotExisting()
        {
            // no settings file found, try to create one from the settings instance
            FreeFormSelection_Settings settings = getSettingsFromFile(); 
            if ( settings == null )
            {
                // fetch or create instance
                settings = LoadOrCreateSettingsInstance( true );

                // select asset file location
                string path = "Assets";
                if( AssetDatabase.IsValidFolder("Assets/FreeFormSelection/Editor") )
                {
                    path = "Assets/FreeFormSelection/Editor";
                }
                else if( AssetDatabase.IsValidFolder("Assets/Plugins/FreeFormSelection/Editor") )
                {
                    path = "Assets/Plugins/FreeFormSelection/Editor";
                }
                else
                {
                    if( !AssetDatabase.IsValidFolder("Assets/Editor") )
                    {
                        AssetDatabase.CreateFolder("Assets", "Editor");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    path = "Assets/Editor";
                }
                path = path + "/FreeFormSelection Settings.asset";

                // create asset file
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if( settings != null )
                {
                    _instance = settings;
                }

                // notify user
                EditorUtility.DisplayDialog("FreeFormSelection settings created.", "The 'FreeFormSelection Settings' file has been created in:\n'" + path + "'\n\nYou can also find it through the menu:\nTools > Free Form Selection > Settings", "Ok");
                        
                // select settings file
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }

            return settings;
        }

        static FreeFormSelection_Settings getSettingsFromFile()
        {
            FreeFormSelection_Settings settings = null;
            string[] foundPathGuids = AssetDatabase.FindAssets("t:FreeFormSelection_Settings");
            if (foundPathGuids.Length > 0)
            {
                //Debug.Log("Num of Assets: " + foundPathGuids.Length);
                settings = AssetDatabase.LoadAssetAtPath<FreeFormSelection_Settings>(AssetDatabase.GUIDToAssetPath(foundPathGuids[0]));
            }
            return settings;
        }

        static FreeFormSelection_Settings createSettingsInstance()
        {
            FreeFormSelection_Settings settings = ScriptableObject.CreateInstance<FreeFormSelection_Settings>();
            return settings;
        }

        public static void OnPackageImported(string packageName)
        {
            if( packageName.IndexOf("FreeFormSelection", System.StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                packageName.IndexOf("Free Form Selection", System.StringComparison.CurrentCultureIgnoreCase) >= 0 )
            {
                EditorUtility.DisplayDialog("FreeFormSelection", "Free Form Selection imported.\nYou can open the settings through the menu:\n\nTools > Free Form Selection > Settings\n\nPlease read the manual.", "Ok");

                if (_instance != null) // TODO: _instance seems to be null at import time
                {
                    // select settings file
                    Selection.activeObject = _instance;
                    EditorGUIUtility.PingObject(_instance);
                }
            }
        }

        public void ForceSave()
        {
            EditorUtility.SetDirty(this); // TODO: maybe use AssetDatabase.ForceReserializeAssets() in newer versions.
        }

        // Plugin On/Off
        [MenuItem("Tools/Free Form Selection/Turn Plugin On", priority = 1)]
        public static void TurnPluginOn()
        {
            instance._enablePlugin = true;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Turn Plugin On", true)]
        public static bool ValidateTurnPluginOn()
        {
            return !instance._enablePlugin;
        }

        [MenuItem("Tools/Free Form Selection/Turn Plugin Off", priority = 1)]
        public static void TurnPluginOff()
        {
            instance._enablePlugin = false;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Turn Plugin Off", true)]
        public static bool ValidateTurnPluginOff()
        {
            return instance._enablePlugin;
        }

        // Selection Modes
        [MenuItem("Tools/Free Form Selection/Mode: FREE FROM", priority = 100)]
        public static void ModeFreeFrom()
        {
            instance._selectionMode = FreeFormSelection.SelectionMode.FreeForm;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Mode: FREE FROM", true)]
        public static bool ValidateModeFreeFrom()
        {
            return instance._enablePlugin && instance._selectionMode != FreeFormSelection.SelectionMode.FreeForm;
        }

        [MenuItem("Tools/Free Form Selection/Mode: EDGES", priority = 100)]
        public static void ModeEdges()
        {
            instance._selectionMode = FreeFormSelection.SelectionMode.Edges;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Mode: EDGES", true)]
        public static bool ValidateModeEdges()
        {
            return instance._enablePlugin && instance._selectionMode != FreeFormSelection.SelectionMode.Edges;
        }

        [MenuItem("Tools/Free Form Selection/Mode: BRUSH", priority = 100)]
        public static void ModeBrush()
        {
            instance._selectionMode = FreeFormSelection.SelectionMode.Brush;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Mode: BRUSH", true)]
        public static bool ValidateModeBrush()
        {
            return instance._enablePlugin && instance._selectionMode != FreeFormSelection.SelectionMode.Brush;
        }






        // Greey Prefab Selection On/Off
        [MenuItem("Tools/Free Form Selection/Turn 'Greedy Prefab Selection' On", priority = 200)]
        public static void TurnGreedyPrefabSelectinOn()
        {
            instance._greedyPrefabSelection = true;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Turn 'Greedy Prefab Selection' On", true)]
        public static bool ValidateTurnGreedyPrefabSelectinOn()
        {
            return !instance._greedyPrefabSelection && instance._enablePlugin;
        }

        [MenuItem("Tools/Free Form Selection/Turn 'Greedy Prefab Selection' Off", priority = 200)]
        public static void TurnGreedyPrefabSelectinOff()
        {
            instance._greedyPrefabSelection = false;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Turn 'Greedy Prefab Selection' Off", true)]
        public static bool ValidateTurnGreedyPrefabSelectinOff()
        {
            return instance._greedyPrefabSelection && instance._enablePlugin;
        }

        // Include UI On/Off
        [MenuItem("Tools/Free Form Selection/Turn 'Include UI' On", priority = 300)]
        public static void TurnIgnoreUIOn()
        {
            instance._includeUI = true;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Turn 'Include UI' On", true)]
        public static bool ValidateTurnIgnoreUIOn()
        {
            return !instance._includeUI && instance._enablePlugin;
        }

        [MenuItem("Tools/Free Form Selection/Turn 'Include UI' Off", priority = 300)]
        public static void TurnIgnoreUIOff()
        {
            instance._includeUI = false;
            instance.ForceSave();
        }

        [MenuItem("Tools/Free Form Selection/Turn 'Include UI' Off", true)]
        public static bool ValidateTurnIgnoreUIOff()
        {
            return instance._includeUI && instance._enablePlugin;
        }

        // settings
        [MenuItem("Tools/Free Form Selection/Settings", priority = 500)]
        public static void SelectSettingsFile()
        {
            var settings = CreateSettingsFileIfNotExisting();
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                EditorUtility.DisplayDialog("FreeFormSelection settings could not be found.", "Settings file not found.\nPlease create it in Assets/Editor/Resources with Right-Click > Create > Free Form Selection > Settings.", "Ok");
            }
        }

        // manual
        [MenuItem("Tools/Free Form Selection/Manual", priority = 400)]
        public static void SelectManualFile()
        {
            string[] foundPathGuids = AssetDatabase.FindAssets("FreeFormSelection-manual");
            if (foundPathGuids.Length > 0)
            {
                var manual = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(foundPathGuids[0]));
                Selection.activeObject = manual;
                EditorGUIUtility.PingObject(manual);
            }
            else
            {
                Debug.Log("FreeFormSelection: manual not found.");
            }
        }

        // support
        [MenuItem("Tools/Free Form Selection/Feedback and Support (Web)", priority = 500)]
        public static void SelectFeedbackWeb()
        {
            Application.OpenURL("https://kamgam.com/unity/support/free_form_selection");
        }

        [MenuItem("Tools/Free Form Selection/Feedback and Support (office@kamgam.com)", priority = 500)]
        public static void SelectFeedbackMail()
        {
            Application.OpenURL("mailto:office@kamgam.com");
        }

        // review
        [MenuItem("Tools/Free Form Selection/Please write a review, thank you.", priority = 600)]
        public static void WriteAReview()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/154671");
        }

        public static string Version
        {
            get { return instance._version; }
        }
        private string _version = "1.1.0"; // semver of course

        // GENERNAL SETTINGS

        // enabled
        public static bool enablePlugin
        {
            get { return instance != null && instance._enablePlugin; }
        }
        [Header("Free Form Selection - v1.1.0")]
        [Tooltip("Enables or disables the whole plugin.")]
        [SerializeField]
        private bool _enablePlugin = true;

        // push To Select Key
        public static KeyCode pushSelectionKey
        {
            get { return instance._pushToSelectKey; }
        }
        [Tooltip("Push and HOLD this key to enable the current selection mode.")]
        [SerializeField]
        public KeyCode _pushToSelectKey = KeyCode.S;

        // push To change mode
        public static KeyCode pushToChangeModeKey
        {
            get { return instance._pushToChangeModeKey; }
        }
        [Tooltip("Push this key while HOLDING the 'selection key' to change the selection mode. You can also change it in the Tools menu.")]
        [SerializeField]
        public KeyCode _pushToChangeModeKey = KeyCode.Space;

        // selection mode
        public static FreeFormSelection.SelectionMode selectionMode
        {
            get { return instance._selectionMode; }
            set { instance._selectionMode = value; }
        }
        [Tooltip("Defines how you will specify the selection area:\n  FreeForm = draw the outline freely\n  Edges = draw the outline edges\n  Brush = draw the selection like in paint")]
        [SerializeField]
        public FreeFormSelection.SelectionMode _selectionMode = FreeFormSelection.SelectionMode.FreeForm;

        // brush size
        public static float brushSize
        {
            get { return instance._brushSize; }
            set {
                instance._brushSize = Mathf.Clamp(value, 6, 8000);
            }
        }
        [Tooltip("Defines how big the brush will be. Measured in scene view 2D coordinates (min. 6).")]
        [SerializeField]
        public float _brushSize = 60f;

        // use mouse wheel
        public static bool useMouseWheel
        {
            get { return instance._useMouseWheel; }
        }
        [Tooltip("If enabled then you can change the brush size with the mouse wheel while selecting.")]
        [SerializeField]
        public bool _useMouseWheel = true;

        // greedyPrefabSelection
        public static bool greedyPrefabSelection
        {
            get { return instance._greedyPrefabSelection; }
        }
        [Tooltip("Should we select the whole prefab if the selection area overlaps a child of the prefab (default is true).")]
        [SerializeField]
        private bool _greedyPrefabSelection = true;

        // Include UI
        public static bool includeUI
        {
            get { return instance._includeUI; }
        }
        [Tooltip("Include UI elements in the selection?")]
        [SerializeField]
        private bool _includeUI = true;

        // max vertices per mesh
        public static int maxVerticesPerMesh
        {
            get { return instance._maxVerticesPerMesh; }
        }
        [Tooltip("Do a vertex precise selection check up to this number of vertices per mesh. Otherwise bounding boxes are used.")]
        [SerializeField]
        private int _maxVerticesPerMesh = 10;

        // hide handles while selecting
        public static bool hideHandlesWhileSelecting
        {
            get { return instance._hideHandlesWhileSelecting; }
        }
        [Tooltip("Hide the handles (transform, rotate, ...) while selecting?")]
        [SerializeField]
        private bool _hideHandlesWhileSelecting = true;

        void OnValidate()
        {
            if( _maxVerticesPerMesh <= 0 )
            {
                _maxVerticesPerMesh = 1;
            }

            if (_brushSize <= 6)
            {
                _brushSize = 6;
            }
        }
    }
}
#endif
