using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BirdController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cameraTransform;
    public Animator animator;

    [Header("Físicas y Movimiento")]
    public float walkSpeed = 5f;
    public float turnSpeed = 15f;
    public float flapForce = 7f;
    public float glideFallSpeed = 2f;
    public float groundJumpForce = 5f;

    [Header("Estamina (Plumas)")]
    public float maxStamina = 5f; // Inicia con 1 pluma
    public float currentStamina;
    public float staminaRecoveryRate = 10f;
    public float staminaCostPerFlap = 5f;

    [Header("Detección de Suelo")]
    public LayerMask groundLayer;
    public float groundCheckOffset = 0.1f;
    public float raycastLength = 0.3f;
    public bool isGrounded;

    private Rigidbody rb;
    private int flyBoolHash, hopIntHash;
    private bool isGliding;

    void Start()
    {
        BirdSkinApplier.ApplySelectedSkin(this);

        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        currentStamina = maxStamina;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (animator != null)
        {
            flyBoolHash = Animator.StringToHash("flying");
            hopIntHash = Animator.StringToHash("hop");
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        CheckGrounded();
        HandleAirInput();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * groundCheckOffset, Vector3.down, raycastLength, groundLayer);

        if (isGrounded)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRecoveryRate * Time.deltaTime);
        }
    }

    void HandleAirInput()
    {
        isGliding = Input.GetKey(KeyCode.Space);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * groundJumpForce, ForceMode.Impulse);
            }
            else if (currentStamina >= staminaCostPerFlap)
            {
                currentStamina -= staminaCostPerFlap;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * flapForce, ForceMode.Impulse);
            }
        }
    }

    void HandleMovement()
    {
        Vector3 moveInput = GetCameraRelativeInput();

        if (isGrounded)
        {
            Vector3 targetVel = moveInput * walkSpeed;
            rb.linearVelocity = new Vector3(targetVel.x, rb.linearVelocity.y, targetVel.z);

            if (moveInput.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveInput);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            Vector3 airMove = moveInput * walkSpeed;
            float currentVelocityY = rb.linearVelocity.y;

            if (isGliding && currentVelocityY < 0)
            {
                currentVelocityY = Mathf.Max(currentVelocityY, -glideFallSpeed);
            }

            rb.linearVelocity = new Vector3(
                Mathf.Lerp(rb.linearVelocity.x, airMove.x, 3f * Time.fixedDeltaTime),
                currentVelocityY,
                Mathf.Lerp(rb.linearVelocity.z, airMove.z, 3f * Time.fixedDeltaTime)
            );

            if (moveInput.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveInput);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            }
        }
    }

    Vector3 GetCameraRelativeInput()
    {
        if (cameraTransform == null) return Vector3.zero;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;

        return (camForward * v + camRight * h).normalized;
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        if (isGrounded)
        {
            animator.SetBool(flyBoolHash, false);
            bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
            animator.SetInteger(hopIntHash, isMoving ? 1 : 0);
        }
        else
        {
            animator.SetBool(flyBoolHash, isGliding || rb.linearVelocity.y > 0.1f);
            animator.SetInteger(hopIntHash, 0);
        }
    }

    public void AddMaxStamina(float amount)
    {
        maxStamina += amount;
        currentStamina = maxStamina;
    }
}