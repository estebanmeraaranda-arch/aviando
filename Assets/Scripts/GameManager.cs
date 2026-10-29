using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Nombres de Escena")]
    public string gameplaySceneName = "Level1";

    [Header("Paneles de UI (asignar en Inspector)")]
    public GameObject mainMenuPanel;
    public GameObject skinSelectionPanel;
    public GameObject settingsPanel;
    public GameObject loadingPanel;
    public GameObject endScreenPanel; // NUEVO: Panel de victoria

    [Header("Selección de Skin")]
    public int selectedSkinIndex;
    private const string SkinPrefKey = "SelectedSkin";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        selectedSkinIndex = PlayerPrefs.GetInt(SkinPrefKey, 0);
    }

    // --- Funciones públicas para botones del Canvas ---
    public void OnStartGame() { SetPanel(skinSelectionPanel); }
    public void OnSkinSelected(int index) { selectedSkinIndex = index; PlayerPrefs.SetInt(SkinPrefKey, index); }
    public void OnConfirmSkinAndPlay() { SetPanel(loadingPanel); SceneManager.LoadScene(gameplaySceneName); }
    public void OnOpenSettings() { SetPanel(settingsPanel); }
    public void OnBackToMenu() { SetPanel(mainMenuPanel); }

    // --- NUEVO: Funciones de Fin de Juego ---
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

    // --- Utilidad ---
    void SetPanel(GameObject target)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(target == mainMenuPanel);
        if (skinSelectionPanel) skinSelectionPanel.SetActive(target == skinSelectionPanel);
        if (settingsPanel) settingsPanel.SetActive(target == settingsPanel);
        if (loadingPanel) loadingPanel.SetActive(target == loadingPanel);
        if (endScreenPanel) endScreenPanel.SetActive(target == endScreenPanel);
    }
}