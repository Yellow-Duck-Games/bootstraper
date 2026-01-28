using UnityEditor;
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
                _greenLabelStyle = new GUIStyle(EditorStyles.miniLabel) {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
                };
            }
            
            if (_yellowLabelStyle == null) {
                _yellowLabelStyle = new GUIStyle(EditorStyles.miniLabel) {
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
            
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("ServicesScene"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("DefaultScene"));
            
            if (_serializedSettings.ApplyModifiedProperties()) {
                AssetDatabase.SaveAssets();
            }
            
            var servicesSceneConfigured = BootstrapperSettings.Instance.ServicesScene.SceneAsset != null;
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Service Generator", EditorStyles.boldLabel);
            
            if (!servicesSceneConfigured) {
                EditorGUILayout.HelpBox("Configure Services Scene above to enable service generation.", MessageType.Warning);
                return;
            }
            
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
        
        [SettingsProvider]
        public static SettingsProvider Create() => new BootstrapperSettingsProvider();
    }
}