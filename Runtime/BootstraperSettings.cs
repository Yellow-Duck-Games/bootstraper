using UnityEngine;

namespace Bootstrapper {
    public class BootstrapperSettings : ScriptableObject {
        [Header("Scene Configuration")]
        [Tooltip("Services scene with persistent objects")]
        public SceneReference ServicesScene;
        
        [Tooltip("Scene to load after services in build")]
        public SceneReference DefaultScene;
        
        private static BootstrapperSettings _instance;
        
        public static BootstrapperSettings Instance {
            get {
                if (_instance == null) {
                    _instance = Resources.Load<BootstrapperSettings>("BootstrapperSettings");
#if UNITY_EDITOR
                    if (_instance == null) {
                        _instance = CreateAndSave();
                    }
#endif
                }
                return _instance;
            }
        }
        
#if UNITY_EDITOR
        private static BootstrapperSettings CreateAndSave() {
            var settings = CreateInstance<BootstrapperSettings>();
            
            var dir = "Assets/Resources";
            if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            UnityEditor.AssetDatabase.CreateAsset(settings, $"{dir}/BootstrapperSettings.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            
            return settings;
        }
#endif
    }
    
    [System.Serializable]
    public class SceneReference {
#if UNITY_EDITOR
        public UnityEditor.SceneAsset SceneAsset;
#endif
        public string SceneName;
        public string ScenePath;
    }
}