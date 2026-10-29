using UnityEngine;

public class FeatherCollectible : MonoBehaviour
{
    public float staminaValue = 5f; // Equivale a 1 pluma extra
    public float rotationSpeed = 100f; // Grados por segundo

    void Update()
    {
        // Rota la pluma de forma continua sobre su eje Y (eje vertical)
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<BirdController>().AddMaxStamina(staminaValue);
            Destroy(gameObject);
        }
    }
}