using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float sensitivity = 3f;
    public float heightOffset = 0.2f;

    [Header("Zoom Settings")]
    public float distance = 3f;
    public float minDistance = 1f;
    public float maxDistance = 6f;
    public float zoomSpeed = 2f;

    [Header("Collision Settings")]
    public LayerMask collisionMask; // Capas con las que la cámara va a chocar (Obstáculos, Paredes, etc.)
    public float cameraRadius = 0.2f; // Volumen de la cámara para que no atraviese esquinas

    private float currentX = 0f;
    private float currentY = 15f;

    void Start()
    {
        sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", sensitivity);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RefreshSensitivity()
    {
        sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", sensitivity);
    }

    void LateUpdate()
    {
        if (target == null || Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked) return;

        // Entradas del ratón
        currentX += Input.GetAxis("Mouse X") * sensitivity;
        currentY -= Input.GetAxis("Mouse Y") * sensitivity;
        currentY = Mathf.Clamp(currentY, -20f, 60f);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // 1. Calcular la posición ideal de la cámara (como si no hubieran paredes)
        Vector3 targetPos = target.position + (Vector3.up * heightOffset);
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 desiredDirection = new Vector3(0, 0, -distance);
        Vector3 desiredPosition = targetPos + rotation * desiredDirection;

        // 2. Preparar el vector para el Raycast
        Vector3 directionToCamera = (desiredPosition - targetPos).normalized;
        float finalDistance = distance;

        // 3. Lanzar un SphereCast (mejor que Raycast normal porque le da volumen a la cámara)
        if (Physics.SphereCast(targetPos, cameraRadius, directionToCamera, out RaycastHit hit, distance, collisionMask))
        {
            // Si choca con algo en su camino, recorta la distancia hasta el punto de choque
            finalDistance = hit.distance;
        }

        // 4. Aplicar la posición final ajustada
        transform.position = targetPos + (directionToCamera * finalDistance);
        transform.LookAt(targetPos);
    }
}