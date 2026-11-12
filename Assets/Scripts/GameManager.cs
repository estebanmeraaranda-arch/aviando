using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Nombres de Escena")]
    [Tooltip("Nombre de la escena del menú de inicio.")]
    public string mainMenuSceneName = "MainMenu";
    [Tooltip("Nombre de la escena principal de gameplay (Build Settings).")]
    public string gameplaySceneName = "EscenarioCasa";

    [Header("Paneles de UI (asignar en Inspector)")]
    public GameObject mainMenuPanel;
    public GameObject skinSelectionPanel;
    public GameObject howToPlayPanel;
    public GameObject settingsPanel;
    public GameObject loadingPanel;
    public GameObject endScreenPanel; // Panel de victoria

    [Header("Selección de Skin")]
    [Tooltip("Índice del skin seleccionado. Persiste con PlayerPrefs.")]
    public int selectedSkinIndex;

    public event Action<float> OnLoadingProgress;

    public const string SkinPrefKey = "SelectedSkin";
    public const string VolumePrefKey = "MasterVolume";
    public const string SensitivityPrefKey = "MouseSensitivity";
    public const string QualityPrefKey = "GraphicsQuality";
    public const string FullscreenPrefKey = "Fullscreen";

    private bool isLoadingScene;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Si una nueva escena trae referencias frescas de paneles, actualizarlas antes de destruir el duplicado
            Instance.AdoptMenuReferences(this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        selectedSkinIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(SkinPrefKey, 0),
            0,
            BirdSkinCatalog.Skins.Length - 1
        );

        ApplySavedSettings();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    void AdoptMenuReferences(GameManager incoming)
    {
        if (incoming == null) return;
        if (incoming.mainMenuPanel != null) mainMenuPanel = incoming.mainMenuPanel;
        if (incoming.skinSelectionPanel != null) skinSelectionPanel = incoming.skinSelectionPanel;
        if (incoming.howToPlayPanel != null) howToPlayPanel = incoming.howToPlayPanel;
        if (incoming.settingsPanel != null) settingsPanel = incoming.settingsPanel;
        if (incoming.loadingPanel != null) loadingPanel = incoming.loadingPanel;
        if (incoming.endScreenPanel != null) endScreenPanel = incoming.endScreenPanel;
        if (!string.IsNullOrEmpty(incoming.gameplaySceneName)) gameplaySceneName = incoming.gameplaySceneName;
        if (!string.IsNullOrEmpty(incoming.mainMenuSceneName)) mainMenuSceneName = incoming.mainMenuSceneName;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        isLoadingScene = false;

        if (scene.name == mainMenuSceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPanel(mainMenuPanel);
        }
        else
        {
            if (endScreenPanel != null) endScreenPanel.SetActive(false);

            // Asegurar PauseMenuController disponible durante el gameplay
            if (GetComponent<PauseMenuController>() == null)
            {
                gameObject.AddComponent<PauseMenuController>();
            }
        }
    }

    public void ApplySavedSettings()
    {
        float savedVol = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
        AudioListener.volume = Mathf.Clamp01(savedVol);

        if (PlayerPrefs.HasKey(QualityPrefKey))
        {
            int q = Mathf.Clamp(PlayerPrefs.GetInt(QualityPrefKey), 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(q, true);
        }

        if (PlayerPrefs.HasKey(FullscreenPrefKey))
        {
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenPrefKey, 1) == 1;
        }
    }

    // --- Funciones públicas para botones del Canvas ---

    /// <summary>Botón "Iniciar Juego" -> muestra selección de skin.</summary>
    public void OnStartGame()
    {
        SetPanel(skinSelectionPanel);
    }

    /// <summary>Botón de skin individual. Llamar con el índice del skin.</summary>
    public void OnSkinSelected(int index)
    {
        selectedSkinIndex = Mathf.Clamp(index, 0, BirdSkinCatalog.Skins.Length - 1);
        PlayerPrefs.SetInt(SkinPrefKey, selectedSkinIndex);
        PlayerPrefs.Save();
    }

    /// <summary>Botón "Confirmar Skin" -> carga la escena de gameplay.</summary>
    public void OnConfirmSkinAndPlay()
    {
        if (isLoadingScene) return;
        SetPanel(loadingPanel);
        StartCoroutine(LoadSceneAsyncCoroutine(gameplaySceneName));
    }

    /// <summary>Botón "Cómo Jugar".</summary>
    public void OnOpenHowToPlay()
    {
        SetPanel(howToPlayPanel);
    }

    /// <summary>Botón "Configuraciones".</summary>
    public void OnOpenSettings()
    {
        SetPanel(settingsPanel);
    }

    /// <summary>Botón "Volver" desde submenús al menú principal.</summary>
    public void OnBackToMenu()
    {
        SetPanel(mainMenuPanel);
    }

    // --- Funciones de Fin de Juego (Victoria / Reinicio) ---

    public void ShowEndScreen()
    {
        SetPanel(endScreenPanel);
        Time.timeScale = 0f; // Pausa el universo del juego
        Cursor.lockState = CursorLockMode.None; // Libera el mouse para poder hacer clic
        Cursor.visible = true;
    }

    public void RestartDemo()
    {
        Time.timeScale = 1f; // Devuelve el tiempo a la normalidad
        SetPanel(null); // Oculta todos los paneles
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Regresar al menú principal desde la partida.</summary>
    public void ReturnToMainMenu()
    {
        if (isLoadingScene) return;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(LoadSceneAsyncCoroutine(mainMenuSceneName));
    }

    /// <summary>Botón "Salir".</summary>
    public void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Utilidad ---

    public void SetPanel(GameObject target)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(target == mainMenuPanel);
        if (skinSelectionPanel) skinSelectionPanel.SetActive(target == skinSelectionPanel);
        if (howToPlayPanel) howToPlayPanel.SetActive(target == howToPlayPanel);
        if (settingsPanel) settingsPanel.SetActive(target == settingsPanel);
        if (loadingPanel) loadingPanel.SetActive(target == loadingPanel);
        if (endScreenPanel) endScreenPanel.SetActive(target == endScreenPanel);
    }

    IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        isLoadingScene = true;
        OnLoadingProgress?.Invoke(0f);

        float minDuration = 0.65f;
        float elapsed = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[GameManager] No se pudo cargar la escena '{sceneName}'. Verifica Build Settings.");
            isLoadingScene = false;
            SetPanel(mainMenuPanel);
            yield break;
        }

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float loadRatio = Mathf.Clamp01(op.progress / 0.9f);
            float timeRatio = Mathf.Clamp01(elapsed / minDuration);
            float displayProgress = Mathf.Min(loadRatio, timeRatio);

            OnLoadingProgress?.Invoke(displayProgress);

            if (op.progress >= 0.9f && elapsed >= minDuration)
            {
                OnLoadingProgress?.Invoke(1f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
