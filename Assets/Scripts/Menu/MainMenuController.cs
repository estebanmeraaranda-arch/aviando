using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Referencias de Escena")]
    public MenuCameraDirector cameraDirector;
    public MenuBirdShowcase birdShowcase;

    [Header("Paneles")]
    public GameObject mainMenuPanel;
    public GameObject skinSelectionPanel;
    public GameObject howToPlayPanel;
    public GameObject settingsPanel;
    public GameObject loadingPanel;

    [Header("Selección de Ave (Aviario)")]
    public Text skinNameText;
    public Text skinTaglineText;
    public Text skinDescriptionText;
    public Text skinCounterText;
    public Image skinAccentBar;
    public List<Button> skinPillButtons = new List<Button>();
    public List<Image> skinPillBackgrounds = new List<Image>();

    [Header("Configuración")]
    public Slider volumeSlider;
    public Text volumeValueText;
    public Slider sensitivitySlider;
    public Text sensitivityValueText;
    public Toggle fullscreenToggle;
    public Text qualityLabelText;

    [Header("Pantalla de Carga")]
    public Image loadingFeatherIcon;
    public Image loadingProgressFill;
    public Text loadingPercentText;
    public Text loadingTipText;

    [Header("UI Dinámica")]
    public Text activeBirdBadgeText;

    private int currentSkinIndex;
    private int currentQualityIndex;

    private static readonly string[] GameplayTips = new string[]
    {
        "Consejo: Mantén pulsado ESPACIO en el aire para planear suavemente y ahorrar plumas.",
        "Consejo: Recoge plumas doradas en la casa para aumentar tu número máximo de aleteos.",
        "Consejo: Evita la luz roja de las cámaras de seguridad o te devolverán a tu último punto seguro.",
        "Consejo: Al aterrizar sobre muebles o el suelo, tu estamina de vuelo se recarga rápidamente.",
        "Consejo: Usa la rueda del ratón durante el vuelo para acercar o alejar la cámara."
    };

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.mainMenuPanel = mainMenuPanel;
            GameManager.Instance.skinSelectionPanel = skinSelectionPanel;
            GameManager.Instance.howToPlayPanel = howToPlayPanel;
            GameManager.Instance.settingsPanel = settingsPanel;
            GameManager.Instance.loadingPanel = loadingPanel;
            GameManager.Instance.OnLoadingProgress += HandleLoadingProgress;
            currentSkinIndex = GameManager.Instance.selectedSkinIndex;
        }
        else
        {
            currentSkinIndex = PlayerPrefs.GetInt(GameManager.SkinPrefKey, 0);
        }

        InitSettingsUI();
        RefreshSkinUI(currentSkinIndex, updateShowcase: false);
        ShowMainMenu();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoadingProgress -= HandleLoadingProgress;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool inSubPanel =
                (skinSelectionPanel != null && skinSelectionPanel.activeSelf) ||
                (howToPlayPanel != null && howToPlayPanel.activeSelf) ||
                (settingsPanel != null && settingsPanel.activeSelf);

            if (inSubPanel)
            {
                ShowMainMenu();
            }
        }

        if (loadingPanel != null && loadingPanel.activeSelf && loadingFeatherIcon != null)
        {
            loadingFeatherIcon.rectTransform.Rotate(0f, 0f, -140f * Time.unscaledDeltaTime);
        }
    }

    // --- Navegación Principal ---

    public void ShowMainMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnBackToMenu();
        else SetActivePanel(mainMenuPanel);

        if (cameraDirector != null) cameraDirector.SetView(MenuCameraDirector.MenuView.MainMenu);
        if (birdShowcase != null) birdShowcase.SetFlyingState(false);
    }

    public void OpenSkinSelection()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStartGame();
        else SetActivePanel(skinSelectionPanel);

        if (cameraDirector != null) cameraDirector.SetView(MenuCameraDirector.MenuView.SkinSelect);
        RefreshSkinUI(currentSkinIndex, updateShowcase: false);
    }

    public void OpenHowToPlay()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnOpenHowToPlay();
        else SetActivePanel(howToPlayPanel);

        if (cameraDirector != null) cameraDirector.SetView(MenuCameraDirector.MenuView.HowToPlay);
    }

    public void OpenSettings()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnOpenSettings();
        else SetActivePanel(settingsPanel);

        if (cameraDirector != null) cameraDirector.SetView(MenuCameraDirector.MenuView.Settings);
    }

    public void ConfirmAndPlay()
    {
        if (loadingTipText != null)
        {
            loadingTipText.text = GameplayTips[Random.Range(0, GameplayTips.Length)];
        }

        if (cameraDirector != null) cameraDirector.SetView(MenuCameraDirector.MenuView.Loading);
        if (birdShowcase != null) birdShowcase.SetFlyingState(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnConfirmSkinAndPlay();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("EscenarioCasa");
        }
    }

    public void QuitGame()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnQuitGame();
        else Application.Quit();
    }

    // --- Selector de Aves ---

    public void SelectSkin(int index)
    {
        int total = BirdSkinCatalog.Skins.Length;
        currentSkinIndex = ((index % total) + total) % total;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSkinSelected(currentSkinIndex);
        }
        else
        {
            PlayerPrefs.SetInt(GameManager.SkinPrefKey, currentSkinIndex);
            PlayerPrefs.Save();
        }

        RefreshSkinUI(currentSkinIndex, updateShowcase: true);
    }

    public void NextSkin()
    {
        SelectSkin(currentSkinIndex + 1);
    }

    public void PreviousSkin()
    {
        SelectSkin(currentSkinIndex - 1);
    }

    public void ChirpShowcase()
    {
        if (birdShowcase != null) birdShowcase.TriggerChirp();
    }

    public void ToggleFlapShowcase()
    {
        if (birdShowcase != null) birdShowcase.TriggerFlapPreview();
    }

    void RefreshSkinUI(int index, bool updateShowcase)
    {
        BirdSkinInfo skin = BirdSkinCatalog.GetSkin(index);
        if (skin == null) return;

        if (skinNameText != null) skinNameText.text = skin.displayName.ToUpper();
        if (skinTaglineText != null) skinTaglineText.text = skin.tagline;
        if (skinDescriptionText != null) skinDescriptionText.text = skin.description;
        if (skinCounterText != null) skinCounterText.text = $"{index + 1} / {BirdSkinCatalog.Skins.Length}";
        if (skinAccentBar != null) skinAccentBar.color = skin.accentColor;
        if (activeBirdBadgeText != null) activeBirdBadgeText.text = $"Ave activa: {skin.displayName}";

        for (int i = 0; i < skinPillBackgrounds.Count; i++)
        {
            if (skinPillBackgrounds[i] == null) continue;
            bool selected = i == index;
            BirdSkinInfo pillSkin = BirdSkinCatalog.GetSkin(i);
            skinPillBackgrounds[i].color = selected && pillSkin != null
                ? new Color(pillSkin.accentColor.r, pillSkin.accentColor.g, pillSkin.accentColor.b, 0.95f)
                : new Color(0.14f, 0.17f, 0.24f, 0.85f);
        }

        if (updateShowcase && birdShowcase != null)
        {
            birdShowcase.ShowSkin(index, playSound: true);
        }
    }

    // --- Configuración ---

    void InitSettingsUI()
    {
        float vol = PlayerPrefs.GetFloat(GameManager.VolumePrefKey, 1f);
        float sens = PlayerPrefs.GetFloat(GameManager.SensitivityPrefKey, 3f);
        bool fs = PlayerPrefs.GetInt(GameManager.FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;
        currentQualityIndex = PlayerPrefs.GetInt(GameManager.QualityPrefKey, QualitySettings.GetQualityLevel());
        currentQualityIndex = Mathf.Clamp(currentQualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(vol);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        UpdateVolumeText(vol);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(sens);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        UpdateSensitivityText(sens);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fs);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        UpdateQualityText();
    }

    public void OnVolumeChanged(float value)
    {
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;
        PlayerPrefs.SetFloat(GameManager.VolumePrefKey, clamped);
        PlayerPrefs.Save();
        UpdateVolumeText(clamped);
    }

    public void OnSensitivityChanged(float value)
    {
        float clamped = Mathf.Clamp(value, 0.5f, 8f);
        PlayerPrefs.SetFloat(GameManager.SensitivityPrefKey, clamped);
        PlayerPrefs.Save();
        UpdateSensitivityText(clamped);
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(GameManager.FullscreenPrefKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void CycleQualityLevel()
    {
        if (QualitySettings.names == null || QualitySettings.names.Length == 0) return;
        currentQualityIndex = (currentQualityIndex + 1) % QualitySettings.names.Length;
        QualitySettings.SetQualityLevel(currentQualityIndex, true);
        PlayerPrefs.SetInt(GameManager.QualityPrefKey, currentQualityIndex);
        PlayerPrefs.Save();
        UpdateQualityText();
    }

    public void ResetDefaultSettings()
    {
        OnVolumeChanged(1f);
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(1f);

        OnSensitivityChanged(3f);
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(3f);

        OnFullscreenChanged(true);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(true);
    }

    void UpdateVolumeText(float v)
    {
        if (volumeValueText != null) volumeValueText.text = $"{Mathf.RoundToInt(v * 100f)}%";
    }

    void UpdateSensitivityText(float s)
    {
        if (sensitivityValueText != null) sensitivityValueText.text = s.ToString("0.0");
    }

    void UpdateQualityText()
    {
        if (qualityLabelText == null) return;
        if (QualitySettings.names != null && QualitySettings.names.Length > 0)
        {
            qualityLabelText.text = $"Calidad Gráfica: {QualitySettings.names[currentQualityIndex]}";
        }
    }

    void HandleLoadingProgress(float progress)
    {
        if (loadingProgressFill != null)
        {
            loadingProgressFill.fillAmount = Mathf.Clamp01(progress);
        }
        if (loadingPercentText != null)
        {
            loadingPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }
    }

    void SetActivePanel(GameObject target)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(target == mainMenuPanel);
        if (skinSelectionPanel) skinSelectionPanel.SetActive(target == skinSelectionPanel);
        if (howToPlayPanel) howToPlayPanel.SetActive(target == howToPlayPanel);
        if (settingsPanel) settingsPanel.SetActive(target == settingsPanel);
        if (loadingPanel) loadingPanel.SetActive(target == loadingPanel);
    }
}
