using UnityEngine;
using UnityEngine.EventSystems;

public class MenuBirdShowcase : MonoBehaviour
{
    [Header("Configuración del Pedestal")]
    public Transform birdAnchor;
    public float showcaseScale = 3.2f;
    public float rotationSensitivity = 0.45f;
    public float autoIdleRotateSpeed = 8f;
    public float damping = 6f;

    [Header("Audio")]
    public AudioSource audioSource;

    private GameObject currentBirdObj;
    private Animator currentAnimator;
    private AudioClip songClip1;
    private AudioClip songClip2;
    private AudioClip flyClip;

    private float angularVelocity;
    private bool isDragging;
    private Vector3 lastMousePos;
    private Vector3 pressStartPos;
    private bool pointerPressedOn3D;
    private float idleTimer;
    private float popTimer;
    private Vector3 targetBaseScale;
    private float defaultYaw = 155f;

    void Awake()
    {
        if (birdAnchor == null) birdAnchor = transform;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.75f;
        }
    }

    void Start()
    {
        int savedIndex = GameManager.Instance != null
            ? GameManager.Instance.selectedSkinIndex
            : PlayerPrefs.GetInt(GameManager.SkinPrefKey, 0);

        ShowSkin(savedIndex, playSound: false);
    }

    void Update()
    {
        HandlePointerInteraction();
        UpdateRotation();
        UpdatePopAnimation();
        UpdateIdleAnimations();
    }

    public void ShowSkin(int index, bool playSound = true)
    {
        BirdSkinInfo skin = BirdSkinCatalog.GetSkin(index);
        if (skin == null) return;

        if (currentBirdObj != null)
        {
            Destroy(currentBirdObj);
        }

        GameObject prefab = Resources.Load<GameObject>(skin.resourcePrefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"[MenuBirdShowcase] No se encontró prefab '{skin.resourcePrefabName}' en Resources.");
            return;
        }

        currentBirdObj = Instantiate(prefab, birdAnchor);
        currentBirdObj.name = $"Showcase_{skin.id}";
        currentBirdObj.transform.localPosition = Vector3.zero;
        currentBirdObj.transform.localRotation = Quaternion.identity;

        // Extraer los cantos reales del ave antes de limpiar componentes de IA/física
        lb_Bird birdComp = currentBirdObj.GetComponent<lb_Bird>();
        if (birdComp != null)
        {
            songClip1 = birdComp.song1;
            songClip2 = birdComp.song2;
            flyClip = birdComp.flyAway1;
        }

        BirdSkinApplier.StripGameplayConflicts(currentBirdObj);

        if (currentBirdObj.GetComponent<BirdAnimationReceiver>() == null)
        {
            currentBirdObj.AddComponent<BirdAnimationReceiver>();
        }

        // Añadir un collider limpio para poder hacer clic directo sobre el ave
        SphereCollider clickCol = currentBirdObj.AddComponent<SphereCollider>();
        clickCol.center = new Vector3(0f, 0.08f, 0f);
        clickCol.radius = 0.14f;

        currentAnimator = currentBirdObj.GetComponent<Animator>();
        if (currentAnimator != null)
        {
            currentAnimator.applyRootMotion = false;
            currentAnimator.updateMode = AnimatorUpdateMode.Normal;
            currentAnimator.SetFloat("IdleAgitated", 0.35f);
        }

        targetBaseScale = Vector3.one * (showcaseScale * skin.scaleMultiplier);
        currentBirdObj.transform.localScale = targetBaseScale * 0.6f;
        popTimer = 0f;

        birdAnchor.localRotation = Quaternion.Euler(0f, defaultYaw, 0f);
        angularVelocity = 0f;
        idleTimer = 1.5f;

        if (playSound)
        {
            TriggerChirp();
        }
    }

    public void TriggerChirp()
    {
        if (currentAnimator != null)
        {
            currentAnimator.SetBool("flying", false);
            currentAnimator.SetTrigger("sing");
        }

        AudioClip clip = (Random.value > 0.5f && songClip2 != null) ? songClip2 : songClip1;
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.05f);
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    public void TriggerFlapPreview()
    {
        if (currentAnimator != null)
        {
            bool nextFly = !currentAnimator.GetBool("flying");
            currentAnimator.SetBool("flying", nextFly);

            if (nextFly && flyClip != null && audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(flyClip, 0.45f);
            }
        }
    }

    public void SetFlyingState(bool flying)
    {
        if (currentAnimator != null)
        {
            currentAnimator.SetBool("flying", flying);
        }
        if (flying && flyClip != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(flyClip, 0.5f);
        }
    }

    void HandlePointerInteraction()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0) && !overUI)
        {
            isDragging = true;
            pointerPressedOn3D = true;
            pressStartPos = Input.mousePosition;
            lastMousePos = Input.mousePosition;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;
            float rotDelta = -delta.x * rotationSensitivity;
            birdAnchor.Rotate(Vector3.up, rotDelta, Space.World);
            angularVelocity = rotDelta / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (pointerPressedOn3D)
            {
                float dragDist = Vector3.Distance(Input.mousePosition, pressStartPos);
                if (dragDist < 10f && Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 25f))
                    {
                        if (currentBirdObj != null && hit.transform.IsChildOf(currentBirdObj.transform))
                        {
                            TriggerChirp();
                        }
                    }
                }
            }

            isDragging = false;
            pointerPressedOn3D = false;
        }
    }

    void UpdateRotation()
    {
        if (birdAnchor == null || isDragging) return;

        if (Mathf.Abs(angularVelocity) > 1f)
        {
            birdAnchor.Rotate(Vector3.up, angularVelocity * Time.unscaledDeltaTime, Space.World);
            angularVelocity = Mathf.Lerp(angularVelocity, 0f, damping * Time.unscaledDeltaTime);
        }
        else
        {
            birdAnchor.Rotate(Vector3.up, autoIdleRotateSpeed * Time.unscaledDeltaTime, Space.World);
        }
    }

    void UpdatePopAnimation()
    {
        if (currentBirdObj == null || popTimer >= 1f) return;

        popTimer += Time.unscaledDeltaTime * 4.5f;
        float t = Mathf.Clamp01(popTimer);
        float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.14f;
        currentBirdObj.transform.localScale = Vector3.Lerp(targetBaseScale * 0.65f, targetBaseScale * overshoot, t);
        if (t >= 1f)
        {
            currentBirdObj.transform.localScale = targetBaseScale;
        }
    }

    void UpdateIdleAnimations()
    {
        if (currentAnimator == null) return;
        if (currentAnimator.GetBool("flying")) return;

        idleTimer -= Time.unscaledDeltaTime;
        if (idleTimer <= 0f)
        {
            idleTimer = Random.Range(3.5f, 6.5f);
            float roll = Random.value;
            if (roll < 0.4f)
            {
                currentAnimator.SetBool("preen", true);
                Invoke(nameof(ResetIdleFlags), 1.4f);
            }
            else if (roll < 0.8f)
            {
                currentAnimator.SetBool("ruffle", true);
                Invoke(nameof(ResetIdleFlags), 1.0f);
            }
            else
            {
                currentAnimator.SetTrigger("sing");
            }
        }
    }

    void ResetIdleFlags()
    {
        if (currentAnimator == null) return;
        currentAnimator.SetBool("preen", false);
        currentAnimator.SetBool("ruffle", false);
    }
}
