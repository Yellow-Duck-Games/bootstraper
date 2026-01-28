#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bootstrapper {
    public static class Bootstrapper {
        private const string EditorPrefsRequestedSceneKey = "Bootstrapper_RequestedScene";
        
        private static BootstrapperSettings Settings => BootstrapperSettings.Instance;
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInit() {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        static void OnPlayModeChanged(PlayModeStateChange state) {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            
            var currentScene = EditorSceneManager.GetActiveScene().path;
            EditorPrefs.SetString(EditorPrefsRequestedSceneKey, currentScene);
            
            if (Settings.ServicesScene.SceneAsset == null) {
                Debug.LogError("Bootstrapper: Services scene not configured in Project Settings");
                return;
            }
            
            EditorSceneManager.playModeStartScene = Settings.ServicesScene.SceneAsset;
        }
#endif
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init() {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects()) {
                Object.DontDestroyOnLoad(go);
            }
            
            SceneManager.LoadScene(GetTargetScene(), LoadSceneMode.Single);
        }
        
        static string GetTargetScene() {
#if UNITY_EDITOR
            var saved = EditorPrefs.GetString(EditorPrefsRequestedSceneKey, "");
            if (!string.IsNullOrEmpty(saved) && !saved.Contains(Settings.ServicesScene.ScenePath)) {
                return saved;
            }
#endif
            return Settings.DefaultScene.SceneName;
        }
    }
}