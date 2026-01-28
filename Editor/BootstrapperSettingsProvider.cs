using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Bootstrapper.Editor {
    public class BootstrapperSettingsProvider : SettingsProvider {
        private SerializedObject _serializedSettings;
        private string _servicesFolder = "Assets/Scripts/Services";
        private Dictionary<string, bool> _selectedServices = new();
        private bool _overwriteExisting = false;
        private bool _initialized = false;
        
        private GUIStyle _greenLabelStyle;
        private GUIStyle _yellowLabelStyle;
        
        public BootstrapperSettingsProvider() : base("Project/Bootstrapper", SettingsScope.Project) { }
        
        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement) {
            _serializedSettings = new SerializedObject(BootstrapperSettings.Instance);
            _initialized = false;
        }
        
        private void InitStyles() {
            if (_greenLabelStyle == null) {
                _greenLabelStyle = new GUIStyle(EditorStyles.label) {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_yellowLabelStyle == null) {
                _yellowLabelStyle = new GUIStyle(EditorStyles.label) {
                    normal = { textColor = new Color(0.9f, 0.7f, 0.1f) }
                };
            }
        }
        
        private void InitServiceStates() {
            if (_initialized) return;
            
            foreach (var kvp in ServiceGenerator.AvailableServices) {
                var serviceName = kvp.Key;
                var exists = ServiceGenerator.ServiceScriptExists(serviceName, _servicesFolder);
                _selectedServices[serviceName] = exists;
            }
            
            _initialized = true;
        }
        
        public override void OnGUI(string searchContext) {
            InitStyles();
            InitServiceStates();
            
            EditorGUILayout.Space(10);
            
            _serializedSettings.Update();
            
            EditorGUILayout.LabelField("Scene Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Services Scene
            var servicesSceneConfigured = BootstrapperSettings.Instance.ServicesScene.SceneAsset != null;
            var servicesSceneProp = _serializedSettings.FindProperty("ServicesScene");
            var servicesAssetProp = servicesSceneProp.FindPropertyRelative("SceneAsset");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newServicesScene = EditorGUILayout.ObjectField("Services Scene", servicesAssetProp.objectReferenceValue, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck()) {
                servicesAssetProp.objectReferenceValue = newServicesScene;
                if (newServicesScene != null) {
                    var path = AssetDatabase.GetAssetPath(newServicesScene);
                    servicesSceneProp.FindPropertyRelative("ScenePath").stringValue = path;
                    servicesSceneProp.FindPropertyRelative("SceneName").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                } else {
                    servicesSceneProp.FindPropertyRelative("ScenePath").stringValue = "";
                    servicesSceneProp.FindPropertyRelative("SceneName").stringValue = "";
                }
            }
            
            if (!servicesSceneConfigured) {
                if (GUILayout.Button("Initialize", GUILayout.Width(70))) {
                    CreateServicesScene();
                }
            } else {
                GUILayout.Label("✓", _greenLabelStyle, GUILayout.Width(20));
            }
            EditorGUILayout.EndHorizontal();
            
            // Default Scene
            var defaultSceneConfigured = BootstrapperSettings.Instance.DefaultScene.SceneAsset != null;
            var defaultSceneProp = _serializedSettings.FindProperty("DefaultScene");
            var defaultAssetProp = defaultSceneProp.FindPropertyRelative("SceneAsset");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newDefaultScene = EditorGUILayout.ObjectField("Default Scene", defaultAssetProp.objectReferenceValue, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck()) {
                defaultAssetProp.objectReferenceValue = newDefaultScene;
                if (newDefaultScene != null) {
                    var path = AssetDatabase.GetAssetPath(newDefaultScene);
                    defaultSceneProp.FindPropertyRelative("ScenePath").stringValue = path;
                    defaultSceneProp.FindPropertyRelative("SceneName").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                } else {
                    defaultSceneProp.FindPropertyRelative("ScenePath").stringValue = "";
                    defaultSceneProp.FindPropertyRelative("SceneName").stringValue = "";
                }
            }
            
            if (defaultSceneConfigured) {
                GUILayout.Label("✓", _greenLabelStyle, GUILayout.Width(20));
            } else {
                GUILayout.Label("", GUILayout.Width(20));
            }
            EditorGUILayout.EndHorizontal();
            
            if (_serializedSettings.ApplyModifiedProperties()) {
                AssetDatabase.SaveAssets();
            }
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Service Generator", EditorStyles.boldLabel);
            
            if (!servicesSceneConfigured) {
                EditorGUILayout.HelpBox("Configure Services Scene above to enable service generation.", MessageType.Warning);
            } else {
                DrawServiceGenerator();
            }
        }
        
        private void DrawServiceGenerator() {
            EditorGUILayout.Space(5);
            
            EditorGUI.BeginChangeCheck();
            _servicesFolder = EditorGUILayout.TextField("Output Folder", _servicesFolder);
            if (EditorGUI.EndChangeCheck()) {
                _initialized = false;
            }
            
            EditorGUILayout.Space(10);
            
            DrawServiceCategory("Core Services", ServiceCategory.Core);
            DrawServiceCategory("Player Services", ServiceCategory.Player);
            DrawServiceCategory("Online Services", ServiceCategory.Online);
            
            EditorGUILayout.Space(10);
            
            _overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing scripts", _overwriteExisting);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // Generate button
            var toGenerate = _selectedServices
                .Where(kvp => kvp.Value && (!ServiceGenerator.ServiceScriptExists(kvp.Key, _servicesFolder) || _overwriteExisting))
                .Select(kvp => kvp.Key)
                .ToList();
            
            EditorGUI.BeginDisabledGroup(toGenerate.Count == 0);
            if (GUILayout.Button($"Generate ({toGenerate.Count})", GUILayout.Height(30))) {
                ServiceGenerator.Generate(toGenerate, _servicesFolder, _overwriteExisting);
                _initialized = false;
            }
            EditorGUI.EndDisabledGroup();
            
            // Delete button
            var toDelete = _selectedServices
                .Where(kvp => !kvp.Value && ServiceGenerator.ServiceScriptExists(kvp.Key, _servicesFolder))
                .Select(kvp => kvp.Key)
                .ToList();
            
            EditorGUI.BeginDisabledGroup(toDelete.Count == 0);
            if (GUILayout.Button($"Delete ({toDelete.Count})", GUILayout.Height(30))) {
                if (EditorUtility.DisplayDialog(
                    "Delete Services",
                    $"Delete {toDelete.Count} service(s)?\n\n{string.Join("\n", toDelete)}\n\nThis will remove scripts and GameObjects.",
                    "Delete",
                    "Cancel")) {
                    ServiceGenerator.Delete(toDelete, _servicesFolder);
                    _initialized = false;
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawServiceCategory(string label, ServiceCategory category) {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            foreach (var kvp in ServiceGenerator.AvailableServices.Where(s => s.Value.Category == category)) {
                var info = kvp.Value;
                var serviceName = kvp.Key;
                
                var scriptExists = ServiceGenerator.ServiceScriptExists(serviceName, _servicesFolder);
                var onScene = ServiceGenerator.ServiceExistsOnScene(serviceName);
                
                EditorGUILayout.BeginHorizontal();
                
                var readyLabel = info.IsReady ? " (Ready)" : " (Stub)";
                
                _selectedServices[serviceName] = EditorGUILayout.ToggleLeft(
                    info.Name + readyLabel,
                    _selectedServices[serviceName],
                    GUILayout.Width(200)
                );
                
                if (scriptExists && onScene) {
                    GUILayout.Label("✓ Exists", _greenLabelStyle);
                } else if (scriptExists) {
                    GUILayout.Label("● Script only", _yellowLabelStyle);
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
        
        private void CreateServicesScene() {
            // Create folder if not exists
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            
            var path = "Assets/Scenes/Services.unity";
            
            // Check if file already exists
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) {
                // Scene exists, just assign it
                var existingAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                AssignServicesScene(existingAsset, path);
                return;
            }
            
            // Create empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Save scene
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            
            // Set in settings
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            AssignServicesScene(sceneAsset, path);
            
            Debug.Log($"Created Services scene at {path}");
        }
        
        private void AssignServicesScene(SceneAsset sceneAsset, string path) {
            var settings = BootstrapperSettings.Instance;
            
            var so = new SerializedObject(settings);
            var servicesSceneProp = so.FindProperty("ServicesScene");
            servicesSceneProp.FindPropertyRelative("SceneAsset").objectReferenceValue = sceneAsset;
            servicesSceneProp.FindPropertyRelative("ScenePath").stringValue = path;
            servicesSceneProp.FindPropertyRelative("SceneName").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
            so.ApplyModifiedProperties();
            
            // Refresh serialized settings
            _serializedSettings = new SerializedObject(settings);
            
            AssetDatabase.SaveAssets();
        }
        
        [SettingsProvider]
        public static SettingsProvider Create() => new BootstrapperSettingsProvider();
    }
}