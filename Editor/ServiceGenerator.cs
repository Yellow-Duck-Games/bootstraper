using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Bootstrapper.Editor {
    [InitializeOnLoad]
    public static class ServiceGenerator {
        private const string PendingServicesKey = "Bootstrapper_PendingServices";
        private const string ServicesFolderKey = "Bootstrapper_ServicesFolder";
        
        static ServiceGenerator() {
            if (EditorPrefs.HasKey(PendingServicesKey)) {
                EditorApplication.delayCall += CreateGameObjects;
            }
        }
        
        public static readonly Dictionary<string, ServiceInfo> AvailableServices = new() {
            // Готовые рабочие
            { "AudioService", new ServiceInfo("AudioService", ServiceCategory.Core, true, GenerateAudioService, SetupAudioService) },
            { "SceneService", new ServiceInfo("SceneService", ServiceCategory.Core, true, GenerateSceneService, null) },
            { "SaveService", new ServiceInfo("SaveService", ServiceCategory.Core, true, GenerateSaveService, null) },
            { "SettingsService", new ServiceInfo("SettingsService", ServiceCategory.Core, true, GenerateSettingsService, null) },
            
            // Заглушки
            { "InputService", new ServiceInfo("InputService", ServiceCategory.Core, false, GenerateInputServiceStub, null) },
            { "UIService", new ServiceInfo("UIService", ServiceCategory.Core, false, GenerateUIServiceStub, null) },
            { "PlayerProgressService", new ServiceInfo("PlayerProgressService", ServiceCategory.Player, false, GeneratePlayerProgressServiceStub, null) },
            { "InventoryService", new ServiceInfo("InventoryService", ServiceCategory.Player, false, GenerateInventoryServiceStub, null) },
            { "CurrencyService", new ServiceInfo("CurrencyService", ServiceCategory.Player, false, GenerateCurrencyServiceStub, null) },
            { "AuthService", new ServiceInfo("AuthService", ServiceCategory.Online, false, GenerateAuthServiceStub, null) },
            { "NetworkService", new ServiceInfo("NetworkService", ServiceCategory.Online, false, GenerateNetworkServiceStub, null) },
            { "LeaderboardService", new ServiceInfo("LeaderboardService", ServiceCategory.Online, false, GenerateLeaderboardServiceStub, null) },
            { "AnalyticsService", new ServiceInfo("AnalyticsService", ServiceCategory.Online, false, GenerateAnalyticsServiceStub, null) },
            { "AdsService", new ServiceInfo("AdsService", ServiceCategory.Online, false, GenerateAdsServiceStub, null) },
            { "IAPService", new ServiceInfo("IAPService", ServiceCategory.Online, false, GenerateIAPServiceStub, null) },
        };
        
        public static void Delete(List<string> services, string folder) {
            // Сначала удалить GameObjects (пока типы ещё существуют)
            var settings = BootstrapperSettings.Instance;
            if (settings.ServicesScene.SceneAsset != null) {
                var scenePath = settings.ServicesScene.ScenePath;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
                foreach (var serviceName in services) {
                    // Искать по имени GameObject, не по типу
                    var go = GameObject.Find(serviceName);
                    if (go != null) {
                        Object.DestroyImmediate(go);
                    }
                }
        
                EditorSceneManager.SaveScene(scene);
            }
    
            // Потом удалить скрипты
            foreach (var serviceName in services) {
                var path = Path.Combine(folder, $"{serviceName}.cs");
                if (File.Exists(path)) {
                    AssetDatabase.DeleteAsset(path);
                }
            }
    
            AssetDatabase.Refresh();
            Debug.Log($"Deleted {services.Count} services");
        }
        
        public static bool ServiceScriptExists(string serviceName, string folder) {
            var path = Path.Combine(folder, $"{serviceName}.cs");
            return File.Exists(path);
        }
        
        public static bool ServiceExistsOnScene(string serviceName) {
            var type = GetType(serviceName);
            if (type == null) return false;
            return Object.FindFirstObjectByType(type) != null;
        }
        
        public static void Generate(List<string> selectedServices, string folder, bool overwrite = false) {
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }
            
            foreach (var serviceName in selectedServices) {
                if (AvailableServices.TryGetValue(serviceName, out var info)) {
                    var path = Path.Combine(folder, $"{serviceName}.cs");
                    if (!File.Exists(path) || overwrite) {
                        var content = info.Generator();
                        File.WriteAllText(path, content);
                    }
                }
            }
            
            EditorPrefs.SetString(PendingServicesKey, string.Join(",", selectedServices));
            EditorPrefs.SetString(ServicesFolderKey, folder);
            
            AssetDatabase.Refresh();
        }
        
        private static void CreateGameObjects() {
            var servicesString = EditorPrefs.GetString(PendingServicesKey, "");
            EditorPrefs.DeleteKey(PendingServicesKey);
            EditorPrefs.DeleteKey(ServicesFolderKey);
    
            if (string.IsNullOrEmpty(servicesString)) return;
    
            var services = servicesString.Split(',');
            var settings = BootstrapperSettings.Instance;
    
            if (settings.ServicesScene.SceneAsset == null) {
                Debug.LogError("Bootstrapper: Services scene not configured");
                return;
            }
    
            var scenePath = settings.ServicesScene.ScenePath;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    
            var created = 0;
            foreach (var serviceName in services) {
                var type = GetType(serviceName);
                if (type == null) {
                    Debug.LogWarning($"Type {serviceName} not found. Try again after compilation.");
                    continue;
                }
        
                var existing = Object.FindFirstObjectByType(type);
                if (existing != null) continue;
        
                var go = new GameObject(serviceName);
                go.AddComponent(type);
        
                if (AvailableServices.TryGetValue(serviceName, out var info) && info.Setup != null) {
                    info.Setup(go);
                }
        
                created++;
            }
    
            if (created > 0) {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
    
            Debug.Log($"Created {created} services on {scenePath}");
        }
        
        private static System.Type GetType(string typeName) {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
        
        #region Service Setup
        
        private static void SetupAudioService(GameObject go) {
            var musicSource = go.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
    
            var sfxSource = go.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
    
            var service = go.GetComponent("AudioService");
            if (service != null) {
                var so = new SerializedObject(service);
                var musicProp = so.FindProperty("_musicSource");
                var sfxProp = so.FindProperty("_sfxSource");
        
                if (musicProp != null) musicProp.objectReferenceValue = musicSource;
                if (sfxProp != null) sfxProp.objectReferenceValue = sfxSource;
        
                so.ApplyModifiedProperties();
            }
        }
        
        #endregion
        
        #region Ready Services
        
        private static string GenerateAudioService() => @"using UnityEngine;

public class AudioService : MonoBehaviour {
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;
    
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;
    
    public void PlayMusic(AudioClip clip) {
        if (_musicSource == null) return;
        _musicSource.clip = clip;
        _musicSource.Play();
    }
    
    public void StopMusic() {
        if (_musicSource == null) return;
        _musicSource.Stop();
    }
    
    public void PlaySFX(AudioClip clip) {
        if (_sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, _sfxVolume);
    }
    
    public void SetMusicVolume(float volume) {
        _musicVolume = Mathf.Clamp01(volume);
        if (_musicSource != null) _musicSource.volume = _musicVolume;
    }
    
    public void SetSFXVolume(float volume) {
        _sfxVolume = Mathf.Clamp01(volume);
    }
    
    public float GetMusicVolume() => _musicVolume;
    public float GetSFXVolume() => _sfxVolume;
}";
        
        private static string GenerateSceneService() => @"using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class SceneService : MonoBehaviour {
    public event Action<string> OnSceneLoadStarted;
    public event Action<string> OnSceneLoaded;
    public event Action<float> OnLoadProgress;
    
    private string _currentScene;
    
    public string CurrentScene => _currentScene;
    
    void Start() {
        _currentScene = SceneManager.GetActiveScene().name;
    }
    
    public void LoadScene(string sceneName) {
        _currentScene = sceneName;
        OnSceneLoadStarted?.Invoke(sceneName);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        OnSceneLoaded?.Invoke(sceneName);
    }
    
    public void LoadSceneAsync(string sceneName, Action onComplete = null) {
        StartCoroutine(LoadSceneAsyncRoutine(sceneName, onComplete));
    }
    
    private IEnumerator LoadSceneAsyncRoutine(string sceneName, Action onComplete) {
        OnSceneLoadStarted?.Invoke(sceneName);
        
        var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        operation.allowSceneActivation = false;
        
        while (operation.progress < 0.9f) {
            OnLoadProgress?.Invoke(operation.progress);
            yield return null;
        }
        
        OnLoadProgress?.Invoke(1f);
        operation.allowSceneActivation = true;
        
        yield return operation;
        
        _currentScene = sceneName;
        OnSceneLoaded?.Invoke(sceneName);
        onComplete?.Invoke();
    }
    
    public void ReloadCurrentScene() {
        LoadScene(_currentScene);
    }
}";
        
        private static string GenerateSaveService() => @"using UnityEngine;

public class SaveService : MonoBehaviour {
    public void Save<T>(string key, T data) {
        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }
    
    public T Load<T>(string key, T defaultValue = default) {
        if (!PlayerPrefs.HasKey(key)) return defaultValue;
        
        var json = PlayerPrefs.GetString(key);
        return JsonUtility.FromJson<T>(json);
    }
    
    public bool HasKey(string key) {
        return PlayerPrefs.HasKey(key);
    }
    
    public void Delete(string key) {
        PlayerPrefs.DeleteKey(key);
    }
    
    public void DeleteAll() {
        PlayerPrefs.DeleteAll();
    }
}";
        
        private static string GenerateSettingsService() => @"using UnityEngine;
using System;

public class SettingsService : MonoBehaviour {
    private const string MusicVolumeKey = ""Settings_MusicVolume"";
    private const string SFXVolumeKey = ""Settings_SFXVolume"";
    private const string FullscreenKey = ""Settings_Fullscreen"";
    private const string ResolutionIndexKey = ""Settings_ResolutionIndex"";
    
    public event Action OnSettingsChanged;
    
    public float MusicVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;
    public bool IsFullscreen { get; private set; } = true;
    public int ResolutionIndex { get; private set; } = 0;
    
    void Awake() {
        Load();
    }
    
    public void SetMusicVolume(float volume) {
        MusicVolume = Mathf.Clamp01(volume);
        Save();
    }
    
    public void SetSFXVolume(float volume) {
        SFXVolume = Mathf.Clamp01(volume);
        Save();
    }
    
    public void SetFullscreen(bool fullscreen) {
        IsFullscreen = fullscreen;
        Screen.fullScreen = fullscreen;
        Save();
    }
    
    public void SetResolution(int index) {
        var resolutions = Screen.resolutions;
        if (index < 0 || index >= resolutions.Length) return;
        
        ResolutionIndex = index;
        var res = resolutions[index];
        Screen.SetResolution(res.width, res.height, IsFullscreen);
        Save();
    }
    
    public Resolution[] GetAvailableResolutions() {
        return Screen.resolutions;
    }
    
    private void Load() {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SFXVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
        IsFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        ResolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, Screen.resolutions.Length - 1);
    }
    
    private void Save() {
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.SetFloat(SFXVolumeKey, SFXVolume);
        PlayerPrefs.SetInt(FullscreenKey, IsFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(ResolutionIndexKey, ResolutionIndex);
        PlayerPrefs.Save();
        
        OnSettingsChanged?.Invoke();
    }
}";
        
        #endregion
        
        #region Stub Services
        
        private static string GenerateInputServiceStub() => @"using UnityEngine;

public class InputService : MonoBehaviour {
    // TODO: Implement using Input System or legacy Input
    
    public Vector2 GetMovement() {
        // Example for legacy Input:
        // return new Vector2(Input.GetAxis(""Horizontal""), Input.GetAxis(""Vertical""));
        return Vector2.zero;
    }
    
    public bool IsActionPressed(string action) {
        // TODO: Map action names to keys/buttons
        return false;
    }
    
    public bool IsActionDown(string action) {
        // TODO: Map action names to keys/buttons
        return false;
    }
    
    public bool IsActionUp(string action) {
        // TODO: Map action names to keys/buttons
        return false;
    }
}";
        
        private static string GenerateUIServiceStub() => @"using UnityEngine;

public class UIService : MonoBehaviour {
    // TODO: Implement based on your UI architecture
    
    public void ShowScreen(string screenName) {
        // TODO: Implement screen management
    }
    
    public void HideScreen(string screenName) {
        // TODO: Implement screen management
    }
    
    public void ShowPopup(string popupName) {
        // TODO: Implement popup management
    }
    
    public void HidePopup(string popupName) {
        // TODO: Implement popup management
    }
    
    public void HideAllPopups() {
        // TODO: Implement
    }
}";
        
        private static string GeneratePlayerProgressServiceStub() => @"using UnityEngine;

public class PlayerProgressService : MonoBehaviour {
    // TODO: Define your progress data structure
    
    public void Load() {
        // TODO: Load progress from SaveService or server
    }
    
    public void Save() {
        // TODO: Save progress to SaveService or server
    }
    
    public void Reset() {
        // TODO: Reset progress to default
    }
}";
        
        private static string GenerateInventoryServiceStub() => @"using UnityEngine;
using System.Collections.Generic;

public class InventoryService : MonoBehaviour {
    // TODO: Define your item structure
    
    public void AddItem(string itemId, int amount = 1) {
        // TODO: Implement
    }
    
    public void RemoveItem(string itemId, int amount = 1) {
        // TODO: Implement
    }
    
    public bool HasItem(string itemId, int amount = 1) {
        // TODO: Implement
        return false;
    }
    
    public int GetItemCount(string itemId) {
        // TODO: Implement
        return 0;
    }
    
    public List<string> GetAllItems() {
        // TODO: Implement
        return new List<string>();
    }
}";
        
        private static string GenerateCurrencyServiceStub() => @"using UnityEngine;
using System;

public class CurrencyService : MonoBehaviour {
    // TODO: Define your currency types
    
    public event Action<string, int> OnCurrencyChanged;
    
    public void Add(string currencyType, int amount) {
        // TODO: Implement
        OnCurrencyChanged?.Invoke(currencyType, GetBalance(currencyType));
    }
    
    public bool Spend(string currencyType, int amount) {
        // TODO: Check if enough and subtract
        return false;
    }
    
    public int GetBalance(string currencyType) {
        // TODO: Implement
        return 0;
    }
    
    public bool HasEnough(string currencyType, int amount) {
        return GetBalance(currencyType) >= amount;
    }
}";
        
        private static string GenerateAuthServiceStub() => @"using UnityEngine;
using System;
using System.Threading.Tasks;

public class AuthService : MonoBehaviour {
    // TODO: Implement using Unity Gaming Services, Firebase, PlayFab, etc.
    
    public event Action OnSignedIn;
    public event Action OnSignedOut;
    
    public bool IsAuthenticated { get; private set; }
    public string UserId { get; private set; }
    
    public async Task SignInAnonymously() {
        // TODO: Implement
        await Task.Yield();
    }
    
    public async Task SignInWithEmail(string email, string password) {
        // TODO: Implement
        await Task.Yield();
    }
    
    public void SignOut() {
        // TODO: Implement
        IsAuthenticated = false;
        UserId = null;
        OnSignedOut?.Invoke();
    }
}";
        
        private static string GenerateNetworkServiceStub() => @"using UnityEngine;
using System;
using System.Threading.Tasks;

public class NetworkService : MonoBehaviour {
    // TODO: Implement based on your backend
    
    public event Action OnConnected;
    public event Action OnDisconnected;
    
    public bool IsConnected { get; private set; }
    
    public async Task Connect() {
        // TODO: Implement
        await Task.Yield();
    }
    
    public void Disconnect() {
        // TODO: Implement
    }
    
    public async Task<T> Get<T>(string endpoint) {
        // TODO: Implement HTTP GET
        await Task.Yield();
        return default;
    }
    
    public async Task<T> Post<T>(string endpoint, object data) {
        // TODO: Implement HTTP POST
        await Task.Yield();
        return default;
    }
}";
        
        private static string GenerateLeaderboardServiceStub() => @"using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class LeaderboardService : MonoBehaviour {
    // TODO: Implement using Unity Gaming Services, PlayFab, etc.
    
    public async Task SubmitScore(string leaderboardId, int score) {
        // TODO: Implement
        await Task.Yield();
    }
    
    public async Task<List<LeaderboardEntry>> GetTopScores(string leaderboardId, int count = 10) {
        // TODO: Implement
        await Task.Yield();
        return new List<LeaderboardEntry>();
    }
    
    public async Task<LeaderboardEntry> GetPlayerScore(string leaderboardId) {
        // TODO: Implement
        await Task.Yield();
        return null;
    }
}

public class LeaderboardEntry {
    public string PlayerId;
    public string PlayerName;
    public int Score;
    public int Rank;
}";
        
        private static string GenerateAnalyticsServiceStub() => @"using UnityEngine;
using System.Collections.Generic;

public class AnalyticsService : MonoBehaviour {
    // TODO: Implement using Unity Analytics, Firebase, etc.
    
    public void TrackEvent(string eventName) {
        // TODO: Implement
        Debug.Log($""[Analytics] Event: {eventName}"");
    }
    
    public void TrackEvent(string eventName, Dictionary<string, object> parameters) {
        // TODO: Implement
        Debug.Log($""[Analytics] Event: {eventName} with {parameters.Count} parameters"");
    }
    
    public void TrackScreen(string screenName) {
        // TODO: Implement
        Debug.Log($""[Analytics] Screen: {screenName}"");
    }
    
    public void SetUserProperty(string property, string value) {
        // TODO: Implement
    }
}";
        
        private static string GenerateAdsServiceStub() => @"using UnityEngine;
using System;

public class AdsService : MonoBehaviour {
    // TODO: Implement using Unity Ads, AdMob, IronSource, etc.
    
    public event Action OnInterstitialClosed;
    public event Action<bool> OnRewardedCompleted;
    
    public bool IsInterstitialReady { get; private set; }
    public bool IsRewardedReady { get; private set; }
    
    public void LoadInterstitial() {
        // TODO: Implement
    }
    
    public void ShowInterstitial() {
        // TODO: Implement
    }
    
    public void LoadRewarded() {
        // TODO: Implement
    }
    
    public void ShowRewarded() {
        // TODO: Implement
    }
}";
        
        private static string GenerateIAPServiceStub() => @"using UnityEngine;
using System;
using System.Threading.Tasks;

public class IAPService : MonoBehaviour {
    // TODO: Implement using Unity IAP
    
    public event Action<string> OnPurchaseCompleted;
    public event Action<string> OnPurchaseFailed;
    
    public async Task Initialize() {
        // TODO: Initialize Unity IAP
        await Task.Yield();
    }
    
    public void Purchase(string productId) {
        // TODO: Implement
    }
    
    public async Task RestorePurchases() {
        // TODO: Implement for iOS
        await Task.Yield();
    }
    
    public string GetLocalizedPrice(string productId) {
        // TODO: Implement
        return """";
    }
    
    public bool IsProductOwned(string productId) {
        // TODO: Implement for non-consumables
        return false;
    }
}";
        
        #endregion
    }
    
    public class ServiceInfo {
        public string Name;
        public ServiceCategory Category;
        public bool IsReady;
        public Func<string> Generator;
        public Action<GameObject> Setup;
        
        public ServiceInfo(string name, ServiceCategory category, bool isReady, Func<string> generator, Action<GameObject> setup) {
            Name = name;
            Category = category;
            IsReady = isReady;
            Generator = generator;
            Setup = setup;
        }
    }
    
    public enum ServiceCategory {
        Core,
        Player,
        Online
    }
}