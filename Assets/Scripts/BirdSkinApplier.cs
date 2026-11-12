using UnityEngine;

public static class BirdSkinApplier
{
    private const string SkinPrefKey = "SelectedSkin";

    /// <summary>
    /// Aplica al jugador el skin guardado en GameManager o PlayerPrefs.
    /// Si el índice es 0 (Cardenal por defecto), mantiene el modelo base intacto.
    /// </summary>
    public static void ApplySelectedSkin(BirdController controller)
    {
        if (controller == null) return;

        int skinIndex = GameManager.Instance != null
            ? GameManager.Instance.selectedSkinIndex
            : PlayerPrefs.GetInt(SkinPrefKey, 0);

        if (skinIndex <= 0) return;

        BirdSkinInfo skin = BirdSkinCatalog.GetSkin(skinIndex);
        if (skin == null || string.IsNullOrEmpty(skin.resourcePrefabName)) return;

        GameObject prefab = Resources.Load<GameObject>(skin.resourcePrefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"[BirdSkinApplier] No se encontró el prefab '{skin.resourcePrefabName}' en Resources.");
            return;
        }

        // Ocultar el renderer y desactivar el animator base del PlayerCardinal sin romper su jerarquía
        foreach (SkinnedMeshRenderer smr in controller.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.enabled = false;
        }

        Animator rootAnimator = controller.GetComponent<Animator>();
        RuntimeAnimatorController sharedController = rootAnimator != null ? rootAnimator.runtimeAnimatorController : null;
        if (rootAnimator != null)
        {
            rootAnimator.enabled = false;
        }

        GameObject visual = Object.Instantiate(prefab, controller.transform);
        visual.name = $"SkinVisual_{skin.id}";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * skin.scaleMultiplier;

        StripGameplayConflicts(visual);

        if (visual.GetComponent<BirdAnimationReceiver>() == null)
        {
            visual.AddComponent<BirdAnimationReceiver>();
        }

        Animator visualAnimator = visual.GetComponent<Animator>();
        if (visualAnimator != null)
        {
            if (sharedController != null && visualAnimator.runtimeAnimatorController == null)
            {
                visualAnimator.runtimeAnimatorController = sharedController;
            }
            visualAnimator.applyRootMotion = false;
            visualAnimator.updateMode = AnimatorUpdateMode.Fixed;
            controller.animator = visualAnimator;
        }
    }

    public static void StripGameplayConflicts(GameObject target)
    {
        if (target == null) return;

        foreach (lb_Bird bird in target.GetComponentsInChildren<lb_Bird>(true))
        {
            Object.Destroy(bird);
        }

        foreach (lb_CrowProximity crowProx in target.GetComponentsInChildren<lb_CrowProximity>(true))
        {
            Object.Destroy(crowProx);
        }

        foreach (Collider col in target.GetComponentsInChildren<Collider>(true))
        {
            Object.Destroy(col);
        }

        foreach (Rigidbody body in target.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.Destroy(body);
        }
    }
}
