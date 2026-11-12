using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Detección de Cámaras (3 Oportunidades)")]
    [Tooltip("Número máximo de veces que la cámara puede pillarte antes de que la vieja te descubra.")]
    public int maxDetections = 3;
    [Tooltip("Contador actual de detecciones acumuladas en la partida.")]
    public int currentDetections = 0;
    [Tooltip("Panel de derrota cuando la vieja te descubre (3 detecciones).")]
    public GameObject gameOverPanel;

    [Header("Selección de Skin")]
    [Tooltip("Índice del skin seleccionado. Persiste con PlayerPrefs.")]
    public int selectedSkinIndex;

    public event Action<float> OnLoadingProgress;
    public event Action<int, int> OnDetectionCountChanged;

    public const string SkinPrefKey = "SelectedSkin";
    public const string VolumePrefKey = "MasterVolume";
    public const string SensitivityPrefKey = "MouseSensitivity";
    public const string QualityPrefKey = "GraphicsQuality";
    public const string FullscreenPrefKey = "Fullscreen";

    private bool isLoadingScene;
    private bool isHandlingDetection;
    private bool adoptedEndScreenPanel;
    private bool adoptedGameOverPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Si una nueva escena trae referencias frescas de paneles, actualizarlas antes de destruir solo el componente duplicado
            Instance.AdoptMenuReferences(this);
            Destroy(this);
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
        if (incoming.endScreenPanel != null)
        {
            endScreenPanel = incoming.endScreenPanel;
            adoptedEndScreenPanel = true;
        }
        if (incoming.gameOverPanel != null)
        {
            gameOverPanel = incoming.gameOverPanel;
            adoptedGameOverPanel = true;
        }
        if (!string.IsNullOrEmpty(incoming.gameplaySceneName)) gameplaySceneName = incoming.gameplaySceneName;
        if (!string.IsNullOrEmpty(incoming.mainMenuSceneName)) mainMenuSceneName = incoming.mainMenuSceneName;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        isLoadingScene = false;
        isHandlingDetection = false;

        if (scene.name == mainMenuSceneName)
        {
            adoptedEndScreenPanel = false;
            adoptedGameOverPanel = false;
            currentDetections = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetPanel(mainMenuPanel);
        }
        else
        {
            if (endScreenPanel != null) endScreenPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Asegurar PauseMenuController disponible durante el gameplay
            if (GetComponent<PauseMenuController>() == null)
            {
                gameObject.AddComponent<PauseMenuController>();
            }

            OnDetectionCountChanged?.Invoke(currentDetections, maxDetections);
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
        ResetDetections();
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
        if (isHandlingDetection) return;
        SetPanel(endScreenPanel);
        if (adoptedEndScreenPanel && endScreenPanel != null)
        {
            // Si el GameManager local de la escena fue reemplazado por el persistente, re-enlazar el botón de reinicio
            var btn = endScreenPanel.GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                btn.onClick.RemoveListener(RestartDemo);
                btn.onClick.AddListener(RestartDemo);
            }
        }
        Time.timeScale = 0f; // Pausa el universo del juego
        Cursor.lockState = CursorLockMode.None; // Libera el mouse para poder hacer clic
        Cursor.visible = true;
    }

    /// <summary>Reinicia el contador de detecciones de cámara.</summary>
    public void ResetDetections()
    {
        currentDetections = 0;
        isHandlingDetection = false;
        OnDetectionCountChanged?.Invoke(currentDetections, maxDetections);
    }

    /// <summary>
    /// Llamado cuando una cámara de seguridad detecta al jugador.
    /// Al alcanzar <see cref="maxDetections"/> (3), la vieja descubre al jugador y pierde.
    /// </summary>
    public void OnPlayerDetectedByCamera()
    {
        if (isHandlingDetection || isLoadingScene) return;
        isHandlingDetection = true;

        currentDetections++;
        OnDetectionCountChanged?.Invoke(currentDetections, maxDetections);

        if (currentDetections >= maxDetections)
        {
            ShowGameOverScreen();
        }
        else
        {
            StartCoroutine(DetectionAlertAndRestartCoroutine());
        }
    }

    IEnumerator DetectionAlertAndRestartCoroutine()
    {
        Time.timeScale = 0.15f;
        GameObject alertOverlay = CreateDetectionAlertOverlay(
            $"¡CÁMARA DETECTADA! ({currentDetections}/{maxDetections})\n¡Si te pilla {maxDetections} veces te descubrirá la vieja!");

        yield return new WaitForSecondsRealtime(0.95f);

        if (alertOverlay != null) Destroy(alertOverlay);

        Time.timeScale = 1f;
        adoptedEndScreenPanel = false;
        adoptedGameOverPanel = false;
        endScreenPanel = null;
        gameOverPanel = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Muestra la pantalla de derrota cuando la vieja descubre al jugador.</summary>
    public void ShowGameOverScreen()
    {
        if (gameOverPanel == null)
        {
            gameOverPanel = CreateFallbackGameOverPanel();
            adoptedGameOverPanel = false;
        }

        SetPanel(gameOverPanel);
        if (adoptedGameOverPanel && gameOverPanel != null)
        {
            RebindGameOverButtons(gameOverPanel);
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartDemo()
    {
        ResetDetections();
        Time.timeScale = 1f; // Devuelve el tiempo a la normalidad
        SetPanel(null); // Oculta todos los paneles
        adoptedEndScreenPanel = false;
        adoptedGameOverPanel = false;
        endScreenPanel = null;
        gameOverPanel = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Regresar al menú principal desde la partida.</summary>
    public void ReturnToMainMenu()
    {
        if (isLoadingScene) return;
        ResetDetections();
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
        if (gameOverPanel) gameOverPanel.SetActive(target == gameOverPanel);
    }

    IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        isLoadingScene = true;
        adoptedEndScreenPanel = false;
        adoptedGameOverPanel = false;
        endScreenPanel = null;
        gameOverPanel = null;
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

    void RebindGameOverButtons(GameObject panel)
    {
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            string lowerName = btn.name.ToLowerInvariant();
            if (lowerName.Contains("menu"))
            {
                btn.onClick.RemoveListener(ReturnToMainMenu);
                btn.onClick.AddListener(ReturnToMainMenu);
            }
            else if (lowerName.Contains("reintentar") || lowerName.Contains("retry") || lowerName.Contains("volver") || buttons.Length == 1)
            {
                btn.onClick.RemoveListener(RestartDemo);
                btn.onClick.AddListener(RestartDemo);
            }
        }
    }

    GameObject CreateDetectionAlertOverlay(string message)
    {
        Canvas parentCanvas = GetOrCreateOverlayCanvas("DetectionAlertCanvas", 600);
        GameObject overlay = new GameObject("DetectionAlertBanner", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parentCanvas.transform, false);

        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = overlay.GetComponent<Image>();
        bg.color = new Color(0.55f, 0.05f, 0.05f, 0.42f);
        bg.raycastTarget = false;

        GameObject banner = new GameObject("AlertCard", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(overlay.transform, false);
        RectTransform bannerRect = banner.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.sizeDelta = new Vector2(760f, 150f);
        banner.GetComponent<Image>().color = new Color(0.09f, 0.06f, 0.08f, 0.92f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateUIText(banner.transform, "AlertText", message, font, 28, FontStyle.Bold, new Color(1f, 0.42f, 0.36f, 1f), TextAnchor.MiddleCenter, new Vector2(720f, 120f), Vector2.zero);
        return parentCanvas.gameObject;
    }

    GameObject CreateFallbackGameOverPanel()
    {
        Canvas parentCanvas = GetOrCreateOverlayCanvas("GameOverCanvas", 550);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject panel = new GameObject("Panel Derrota", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parentCanvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.10f, 0.02f, 0.03f, 0.86f);

        GameObject card = new GameObject("DerrotaCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(panel.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(700f, 380f);
        card.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.13f, 0.96f);

        CreateUIText(card.transform, "Title", "¡TE DESCUBRIÓ LA VIEJA!", font, 40, FontStyle.Bold, new Color(0.95f, 0.32f, 0.28f, 1f), TextAnchor.MiddleCenter, new Vector2(640f, 70f), new Vector2(0f, 105f));
        CreateUIText(card.transform, "Subtitle", "Las cámaras de seguridad te pillaron 3 veces.\n¡La vieja sabe que estás en la casa!", font, 23, FontStyle.Normal, new Color(0.88f, 0.90f, 0.94f, 1f), TextAnchor.MiddleCenter, new Vector2(620f, 90f), new Vector2(0f, 20f));

        CreateUIButton(card.transform, "Btn_Reintentar", "Reintentar", font, new Color(0.84f, 0.30f, 0.24f, 1f), new Vector2(260f, 64f), new Vector2(-145f, -95f), RestartDemo);
        CreateUIButton(card.transform, "Btn_MenuPrincipal", "Menú Principal", font, new Color(0.24f, 0.29f, 0.39f, 1f), new Vector2(260f, 64f), new Vector2(145f, -95f), ReturnToMainMenu);

        return panel;
    }

    Canvas GetOrCreateOverlayCanvas(string canvasName, int sortingOrder)
    {
        GameObject canvasObj = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    void CreateUIText(Transform parent, string name, string content, Font font, int size, FontStyle style, Color color, TextAnchor align, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPos;

        Text txt = obj.GetComponent<Text>();
        txt.text = content;
        txt.font = font;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.raycastTarget = false;
    }

    void CreateUIButton(Transform parent, string name, string label, Font font, Color bgColor, Vector2 sizeDelta, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPos;

        btnObj.GetComponent<Image>().color = bgColor;
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        CreateUIText(btnObj.transform, "Label", label, font, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, sizeDelta, Vector2.zero);
    }
}
