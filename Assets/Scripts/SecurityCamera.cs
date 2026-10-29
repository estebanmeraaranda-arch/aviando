using UnityEngine;
using UnityEngine.SceneManagement;

public class SecurityCamera : MonoBehaviour
{
    [Header("Movimiento de la Cámara")]
    public float sweepAngle = 45f;
    public float sweepSpeed = 2f;
    public bool sweepVertical = false;

    private Quaternion startRotation;

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
        if (other.CompareTag("Player"))
        {
            Debug.Log("¡Jugador detectado! Reiniciando el nivel...");
            // Recarga la escena actual completa, reseteando plumas y estamina
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}