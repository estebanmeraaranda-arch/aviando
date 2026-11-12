using UnityEngine;
using UnityEngine.SceneManagement;

public class SecurityCamera : MonoBehaviour
{
    [Header("Movimiento de la Cámara")]
    public float sweepAngle = 45f;
    public float sweepSpeed = 2f;
    public bool sweepVertical = false;

    private Quaternion startRotation;
    private bool hasTriggered;

    void Start()
    {
        startRotation = transform.rotation;
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * sweepSpeed) * sweepAngle;

        if (sweepVertical)
            transform.rotation = startRotation * Quaternion.Euler(angle, 0, 0);
        else
            transform.rotation = startRotation * Quaternion.Euler(0, angle, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDetectedByCamera();
            }
            else
            {
                Debug.Log("¡Jugador detectado! Reiniciando el nivel...");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
