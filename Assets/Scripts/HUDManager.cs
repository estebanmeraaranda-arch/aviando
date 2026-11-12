using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Referencias de Jugador y Plumas")]
    public BirdController player;
    public GameObject featherIconPrefab; // Prefab con componente Image
    public Transform layoutGroup; // Horizontal o Vertical Layout Group

    [Header("Barra de Estamina")]
    public GameObject staminaBarRoot;
    public Image staminaFillImage;
    public Image staminaTrailImage;
    public Text staminaValueText;

    [Header("Estilo de Barra de Estamina")]
    public Color fullStaminaColor = new Color(0.32f, 0.85f, 0.52f, 1f);
    public Color midStaminaColor = new Color(0.96f, 0.72f, 0.22f, 1f);
    public Color lowStaminaColor = new Color(0.92f, 0.28f, 0.24f, 1f);
    public float fillSmoothSpeed = 12f;
    public float trailSmoothSpeed = 3.5f;

    [Header("Indicador de Detección de Cámaras")]
    public GameObject detectionPanelRoot;
    public Text detectionStatusText;
    public Image[] detectionDots;
    public Color safeDotColor = new Color(0.24f, 0.30f, 0.40f, 1f);
    public Color alertDotColor = new Color(0.95f, 0.25f, 0.22f, 1f);

    private List<GameObject> featherIcons = new List<GameObject>();
    private float displayedFill = 1f;
    private float displayedTrail = 1f;
    private float lastMaxStamina = -1f;
    private float flashTimer;

    void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<BirdController>();
        }

        if (GameManager.Instance == null)
        {
            GameManager existingGm = GetComponent<GameManager>() ?? FindFirstObjectByType<GameManager>();
            if (existingGm == null)
            {
                gameObject.AddComponent<GameManager>();
            }
        }

        if (staminaFillImage == null)
        {
            CreateFallbackStaminaBar();
        }

        if (detectionPanelRoot == null)
        {
            CreateFallbackDetectionPanel();
        }

        if (player != null && player.maxStamina > 0f)
        {
            float ratio = Mathf.Clamp01(player.currentStamina / player.maxStamina);
            displayedFill = ratio;
            displayedTrail = ratio;
            lastMaxStamina = player.maxStamina;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDetectionCountChanged += UpdateDetectionUI;
            UpdateDetectionUI(GameManager.Instance.currentDetections, GameManager.Instance.maxDetections);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDetectionCountChanged -= UpdateDetectionUI;
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateFeatherIcons();
        UpdateStaminaBar();
    }

    void UpdateFeatherIcons()
    {
        if (featherIconPrefab == null || layoutGroup == null) return;

        // Calcular plumas totales y actuales (cada 5 pts = 1 pluma)
        int totalFeathers = Mathf.FloorToInt(player.maxStamina / 5f);
        int filledFeathers = Mathf.FloorToInt(player.currentStamina / 5f);

        // Añadir iconos si la estamina máxima aumentó
        while (featherIcons.Count < totalFeathers)
        {
            GameObject newIcon = Instantiate(featherIconPrefab, layoutGroup);
            featherIcons.Add(newIcon);
        }

        // Actualizar transparencia para simular gasto y recarga
        for (int i = 0; i < featherIcons.Count; i++)
        {
            Image img = featherIcons[i].GetComponent<Image>();
            if (img == null) continue;

            if (i < filledFeathers)
            {
                img.color = new Color(1f, 1f, 1f, 1f); // Llena
            }
            else
            {
                img.color = new Color(1f, 1f, 1f, 0.2f); // Gastada (translúcida)
            }
        }
    }

    void UpdateStaminaBar()
    {
        if (staminaFillImage == null) return;

        float max = Mathf.Max(0.01f, player.maxStamina);
        float current = Mathf.Clamp(player.currentStamina, 0f, max);
        float targetRatio = Mathf.Clamp01(current / max);

        // Destello breve cuando aumenta la estamina máxima al recoger una pluma
        if (lastMaxStamina > 0f && max > lastMaxStamina + 0.01f)
        {
            flashTimer = 0.5f;
        }
        lastMaxStamina = max;

        displayedFill = Mathf.Lerp(displayedFill, targetRatio, fillSmoothSpeed * Time.deltaTime);
        if (displayedFill > displayedTrail)
        {
            displayedTrail = displayedFill;
        }
        else
        {
            displayedTrail = Mathf.Lerp(displayedTrail, displayedFill, trailSmoothSpeed * Time.deltaTime);
        }

        staminaFillImage.fillAmount = displayedFill;
        if (staminaTrailImage != null)
        {
            staminaTrailImage.fillAmount = displayedTrail;
        }

        // Color según nivel de estamina o falta de energía para aletear
        bool cantFlap = current < player.staminaCostPerFlap;
        Color targetColor;
        if (cantFlap || targetRatio < 0.25f)
        {
            float pulse = 0.78f + Mathf.Sin(Time.time * 9f) * 0.22f;
            targetColor = lowStaminaColor * pulse;
            targetColor.a = 1f;
        }
        else if (targetRatio < 0.55f)
        {
            float t = (targetRatio - 0.25f) / 0.30f;
            targetColor = Color.Lerp(lowStaminaColor, midStaminaColor, t);
        }
        else
        {
            float t = (targetRatio - 0.55f) / 0.45f;
            targetColor = Color.Lerp(midStaminaColor, fullStaminaColor, t);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            targetColor = Color.Lerp(targetColor, Color.white, Mathf.Clamp01(flashTimer * 2f) * 0.65f);
        }

        staminaFillImage.color = targetColor;

        if (staminaValueText != null)
        {
            staminaValueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.RoundToInt(max)}";
        }
    }

    Canvas FindTargetCanvas()
    {
        if (layoutGroup != null)
        {
            Canvas fromLayout = layoutGroup.GetComponentInParent<Canvas>();
            if (fromLayout != null) return fromLayout;
        }
        return FindFirstObjectByType<Canvas>();
    }

    void UpdateDetectionUI(int current, int max)
    {
        if (detectionStatusText != null)
        {
            detectionStatusText.text = $"Vigilancia: {current} / {max}";
            detectionStatusText.color = current >= max - 1 && current > 0
                ? new Color(1f, 0.42f, 0.36f, 1f)
                : new Color(0.95f, 0.96f, 0.98f, 1f);
        }

        if (detectionDots != null)
        {
            for (int i = 0; i < detectionDots.Length; i++)
            {
                if (detectionDots[i] == null) continue;
                detectionDots[i].color = i < current ? alertDotColor : safeDotColor;
            }
        }
    }

    void CreateFallbackDetectionPanel()
    {
        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null) return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        detectionPanelRoot = new GameObject("AlertasPanel", typeof(RectTransform), typeof(Image));
        detectionPanelRoot.transform.SetParent(targetCanvas.transform, false);
        RectTransform rootRect = detectionPanelRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(36f, -152f);
        rootRect.sizeDelta = new Vector2(390f, 44f);
        Image rootImg = detectionPanelRoot.GetComponent<Image>();
        rootImg.color = new Color(0.07f, 0.09f, 0.14f, 0.86f);
        rootImg.raycastTarget = false;

        GameObject labelObj = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(detectionPanelRoot.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(14f, 0f);
        labelRect.sizeDelta = new Vector2(210f, 0f);
        detectionStatusText = labelObj.GetComponent<Text>();
        detectionStatusText.font = font;
        detectionStatusText.fontSize = 18;
        detectionStatusText.fontStyle = FontStyle.Bold;
        detectionStatusText.alignment = TextAnchor.MiddleLeft;
        detectionStatusText.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        detectionStatusText.text = "Vigilancia: 0 / 3";
        detectionStatusText.raycastTarget = false;

        int dotCount = GameManager.Instance != null ? GameManager.Instance.maxDetections : 3;
        detectionDots = new Image[dotCount];
        for (int i = 0; i < dotCount; i++)
        {
            GameObject dotObj = new GameObject($"Dot_{i + 1}", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(detectionPanelRoot.transform, false);
            RectTransform dotRect = dotObj.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(1f, 0.5f);
            dotRect.anchorMax = new Vector2(1f, 0.5f);
            dotRect.pivot = new Vector2(1f, 0.5f);
            dotRect.anchoredPosition = new Vector2(-14f - (dotCount - 1 - i) * 32f, 0f);
            dotRect.sizeDelta = new Vector2(22f, 22f);
            Image dotImg = dotObj.GetComponent<Image>();
            dotImg.color = safeDotColor;
            dotImg.raycastTarget = false;
            detectionDots[i] = dotImg;
        }
    }

    void CreateFallbackStaminaBar()
    {
        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null) return;

        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Sprite featherSprite = null;
        if (featherIconPrefab != null)
        {
            Image prefabImg = featherIconPrefab.GetComponent<Image>();
            if (prefabImg != null) featherSprite = prefabImg.sprite;
        }

        staminaBarRoot = new GameObject("StaminaBarPanel", typeof(RectTransform), typeof(Image));
        staminaBarRoot.transform.SetParent(targetCanvas.transform, false);
        RectTransform rootRect = staminaBarRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(36f, -92f);
        rootRect.sizeDelta = new Vector2(390f, 52f);
        staminaBarRoot.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.14f, 0.86f);

        float trackLeftOffset = 14f;
        if (featherSprite != null)
        {
            GameObject iconObj = new GameObject("FeatherIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(staminaBarRoot.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(30f, 30f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = featherSprite;
            iconImg.preserveAspect = true;
            iconImg.color = new Color(1f, 0.86f, 0.42f, 1f);
            iconImg.raycastTarget = false;
            trackLeftOffset = 52f;
        }

        GameObject track = new GameObject("StaminaTrack", typeof(RectTransform), typeof(Image));
        track.transform.SetParent(staminaBarRoot.transform, false);
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.pivot = new Vector2(0f, 0.5f);
        trackRect.anchoredPosition = new Vector2(trackLeftOffset, 0f);
        trackRect.sizeDelta = new Vector2(-(trackLeftOffset + 94f), 20f);
        track.GetComponent<Image>().color = new Color(0.16f, 0.20f, 0.28f, 1f);

        GameObject trail = new GameObject("Trail", typeof(RectTransform), typeof(Image));
        trail.transform.SetParent(track.transform, false);
        RectTransform trailRect = trail.GetComponent<RectTransform>();
        trailRect.anchorMin = Vector2.zero;
        trailRect.anchorMax = Vector2.one;
        trailRect.offsetMin = Vector2.zero;
        trailRect.offsetMax = Vector2.zero;
        staminaTrailImage = trail.GetComponent<Image>();
        staminaTrailImage.sprite = whiteSprite;
        staminaTrailImage.type = Image.Type.Filled;
        staminaTrailImage.fillMethod = Image.FillMethod.Horizontal;
        staminaTrailImage.color = new Color(1f, 1f, 1f, 0.45f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(track.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        staminaFillImage = fill.GetComponent<Image>();
        staminaFillImage.sprite = whiteSprite;
        staminaFillImage.type = Image.Type.Filled;
        staminaFillImage.fillMethod = Image.FillMethod.Horizontal;
        staminaFillImage.color = fullStaminaColor;

        GameObject txtObj = new GameObject("ValueText", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(staminaBarRoot.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(1f, 0f);
        txtRect.anchorMax = new Vector2(1f, 1f);
        txtRect.pivot = new Vector2(1f, 0.5f);
        txtRect.anchoredPosition = new Vector2(-10f, 0f);
        txtRect.sizeDelta = new Vector2(82f, 0f);
        staminaValueText = txtObj.GetComponent<Text>();
        staminaValueText.font = font;
        staminaValueText.fontSize = 18;
        staminaValueText.fontStyle = FontStyle.Bold;
        staminaValueText.alignment = TextAnchor.MiddleRight;
        staminaValueText.color = new Color(0.95f, 0.96f, 0.98f, 1f);
    }
}
