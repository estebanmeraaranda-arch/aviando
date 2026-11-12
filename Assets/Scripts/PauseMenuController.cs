using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private GameObject pauseCanvasObj;
    private bool isPaused;

    void Update()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (GameManager.Instance != null && activeScene == GameManager.Instance.mainMenuSceneName)
        {
            if (pauseCanvasObj != null && pauseCanvasObj.activeSelf)
            {
                pauseCanvasObj.SetActive(false);
            }
            isPaused = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        EnsureUI();
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseCanvasObj != null) pauseCanvasObj.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (pauseCanvasObj != null) pauseCanvasObj.SetActive(false);
    }

    public void RestartScene()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseCanvasObj != null) pauseCanvasObj.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseCanvasObj != null) pauseCanvasObj.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
        }
    }

    void EnsureUI()
    {
        if (pauseCanvasObj != null) return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        pauseCanvasObj = new GameObject("PauseMenuCanvas");
        DontDestroyOnLoad(pauseCanvasObj);

        Canvas canvas = pauseCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = pauseCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        pauseCanvasObj.AddComponent<GraphicRaycaster>();

        GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(pauseCanvasObj.transform, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.10f, 0.72f);

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(overlay.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(480f, 440f);
        card.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.18f, 0.96f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 150f);
        titleRect.sizeDelta = new Vector2(400f, 60f);
        Text titleTxt = titleObj.GetComponent<Text>();
        titleTxt.font = font;
        titleTxt.text = "PAUSA";
        titleTxt.fontSize = 42;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.96f, 0.94f, 0.88f, 1f);

        CreatePauseButton(card.transform, "Reanudar", new Vector2(0f, 50f), new Color(0.86f, 0.34f, 0.24f, 1f), font, Resume);
        CreatePauseButton(card.transform, "Reiniciar Nivel", new Vector2(0f, -45f), new Color(0.18f, 0.23f, 0.32f, 1f), font, RestartScene);
        CreatePauseButton(card.transform, "Menú Principal", new Vector2(0f, -140f), new Color(0.18f, 0.23f, 0.32f, 1f), font, BackToMainMenu);

        pauseCanvasObj.SetActive(false);
    }

    void CreatePauseButton(Transform parent, string label, Vector2 pos, Color bgColor, Font font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(340f, 68f);

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        Text txt = txtObj.GetComponent<Text>();
        txt.font = font;
        txt.text = label;
        txt.fontSize = 26;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }
}
