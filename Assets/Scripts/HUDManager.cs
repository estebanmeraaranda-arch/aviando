using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    public BirdController player;
    public GameObject featherIconPrefab; // Prefab con componente Image
    public Transform layoutGroup; // Horizontal o Vertical Layout Group

    private List<GameObject> featherIcons = new List<GameObject>();

    void Update()
    {
        if (player == null) return;

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
}