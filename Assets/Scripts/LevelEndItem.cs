using UnityEngine;

public class LevelEndItem : MonoBehaviour
{
    [Header("Animación Visual")]
    public float rotationSpeed = 90f;
    public float bobAmplitude = 0.08f;
    public float bobSpeed = 2f;

    private Vector3 startPos;
    private bool triggered;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        if (bobAmplitude > 0f)
        {
            float offsetY = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = startPos + new Vector3(0f, offsetY, 0f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            GameManager gm = GameManager.Instance != null
                ? GameManager.Instance
                : FindFirstObjectByType<GameManager>();

            if (gm != null)
            {
                gm.ShowEndScreen();
            }
            else
            {
                Debug.LogWarning("[LevelEndItem] No se encontró GameManager en la escena para mostrar la pantalla de victoria.");
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
