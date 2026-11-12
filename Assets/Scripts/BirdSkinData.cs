using UnityEngine;

[System.Serializable]
public class BirdSkinInfo
{
    public string id;
    public string displayName;
    public string tagline;
    public string description;
    public string resourcePrefabName;
    public Color accentColor;
    public float scaleMultiplier = 1f;
}

public static class BirdSkinCatalog
{
    public static readonly BirdSkinInfo[] Skins = new BirdSkinInfo[]
    {
        new BirdSkinInfo
        {
            id = "cardinal",
            displayName = "Cardenal Rojo",
            tagline = "El Infiltrado Clásico",
            description = "Plumaje carmesí inconfundible. Equilibrado y valiente al esquivar cámaras.",
            resourcePrefabName = "lb_cardinal",
            accentColor = new Color(0.89f, 0.25f, 0.25f, 1f),
            scaleMultiplier = 1.0f
        },
        new BirdSkinInfo
        {
            id = "bluejay",
            displayName = "Arrendajo Azul",
            tagline = "Vigía de Altura",
            description = "Cobalto y blanco brillante. Ideal para planear entre estantes altos.",
            resourcePrefabName = "lb_blueJay",
            accentColor = new Color(0.24f, 0.56f, 0.96f, 1f),
            scaleMultiplier = 1.0f
        },
        new BirdSkinInfo
        {
            id = "robin",
            displayName = "Petirrojo",
            tagline = "Vuelo Ágil",
            description = "Pecho cálido y movimientos precisos para atravesar pasillos vigilados.",
            resourcePrefabName = "lb_robin",
            accentColor = new Color(0.95f, 0.49f, 0.26f, 1f),
            scaleMultiplier = 1.0f
        },
        new BirdSkinInfo
        {
            id = "chickadee",
            displayName = "Carbonero",
            tagline = "Pequeño Explorador",
            description = "Compacto y curioso. Se desliza con facilidad junto a cada mueble.",
            resourcePrefabName = "lb_chickadee",
            accentColor = new Color(0.30f, 0.74f, 0.65f, 1f),
            scaleMultiplier = 1.05f
        },
        new BirdSkinInfo
        {
            id = "goldfinch",
            displayName = "Jilguero Dorado",
            tagline = "Buscador de Plumas",
            description = "Sus alas amarillas combinan con las plumas doradas ocultas en la casa.",
            resourcePrefabName = "lb_goldFinch",
            accentColor = new Color(0.96f, 0.76f, 0.19f, 1f),
            scaleMultiplier = 1.05f
        },
        new BirdSkinInfo
        {
            id = "sparrow",
            displayName = "Gorrión Urbano",
            tagline = "Maestro del Camuflaje",
            description = "Tonos tierra discretos para pasar inadvertido en la sala.",
            resourcePrefabName = "lb_sparrow",
            accentColor = new Color(0.77f, 0.56f, 0.36f, 1f),
            scaleMultiplier = 1.05f
        },
        new BirdSkinInfo
        {
            id = "crow",
            displayName = "Cuervo Sombra",
            tagline = "Estratega Nocturno",
            description = "Astuto y elegante. Domina el sigilo bajo la luz roja de vigilancia.",
            resourcePrefabName = "lb_crow",
            accentColor = new Color(0.56f, 0.45f, 0.96f, 1f),
            scaleMultiplier = 0.82f
        }
    };

    public static BirdSkinInfo GetSkin(int index)
    {
        if (Skins == null || Skins.Length == 0) return null;
        int clamped = Mathf.Clamp(index, 0, Skins.Length - 1);
        return Skins[clamped];
    }
}
