using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuUIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Animación Hover")]
    public float hoverScale = 1.045f;
    public float hoverOffsetX = 10f;
    public float animSpeed = 12f;

    [Header("Indicador Visual Opcional")]
    public GameObject hoverIndicator;
    public Graphic tintTarget;
    public Color normalColor = new Color(0.11f, 0.14f, 0.20f, 0.88f);
    public Color hoverColor = new Color(0.18f, 0.24f, 0.34f, 0.96f);

    [Header("Sonido Opcional")]
    public AudioSource uiAudioSource;
    public AudioClip hoverClip;
    public AudioClip clickClip;

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;
    private bool initialized;
    private bool isHovered;

    void Awake()
    {
        EnsureInit();
    }

    void OnEnable()
    {
        EnsureInit();
        isHovered = false;
        if (hoverIndicator != null) hoverIndicator.SetActive(false);
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
            rectTransform.anchoredPosition = baseAnchoredPos;
        }
        if (tintTarget != null) tintTarget.color = normalColor;
    }

    void EnsureInit()
    {
        if (initialized) return;
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
            baseAnchoredPos = rectTransform.anchoredPosition;
        }
        initialized = true;
    }

    void Update()
    {
        if (rectTransform == null) return;

        Vector3 targetScale = isHovered ? baseScale * hoverScale : baseScale;
        Vector2 targetPos = isHovered ? baseAnchoredPos + new Vector2(hoverOffsetX, 0f) : baseAnchoredPos;

        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, animSpeed * Time.unscaledDeltaTime);
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPos, animSpeed * Time.unscaledDeltaTime);

        if (tintTarget != null)
        {
            tintTarget.color = Color.Lerp(tintTarget.color, isHovered ? hoverColor : normalColor, animSpeed * Time.unscaledDeltaTime);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (hoverIndicator != null) hoverIndicator.SetActive(true);
        if (uiAudioSource != null && hoverClip != null)
        {
            uiAudioSource.PlayOneShot(hoverClip, 0.25f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (hoverIndicator != null) hoverIndicator.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (uiAudioSource != null && clickClip != null)
        {
            uiAudioSource.PlayOneShot(clickClip, 0.45f);
        }
    }
}
