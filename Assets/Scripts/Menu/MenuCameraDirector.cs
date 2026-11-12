using UnityEngine;

public class MenuCameraDirector : MonoBehaviour
{
    public enum MenuView
    {
        MainMenu,
        SkinSelect,
        HowToPlay,
        Settings,
        Loading
    }

    [Header("Poses de Cámara")]
    public Transform mainMenuPose;
    public Transform skinSelectPose;
    public Transform howToPlayPose;
    public Transform settingsPose;
    public Transform loadingPose;

    [Header("Suavizado y Parallax")]
    public float moveSmoothSpeed = 4.5f;
    public float rotateSmoothSpeed = 5.0f;
    public float parallaxPositionAmount = 0.09f;
    public float parallaxRotationAmount = 1.4f;

    private MenuView currentView = MenuView.MainMenu;
    private Vector3 smoothVelocityPos;

    public void SetView(MenuView view)
    {
        currentView = view;
    }

    void LateUpdate()
    {
        Transform target = GetTargetPose();
        if (target == null) return;

        // Parallax sutil según la posición del ratón en pantalla
        float nx = 0f;
        float ny = 0f;
        if (Screen.width > 0 && Screen.height > 0)
        {
            nx = Mathf.Clamp((Input.mousePosition.x / Screen.width - 0.5f) * 2f, -1f, 1f);
            ny = Mathf.Clamp((Input.mousePosition.y / Screen.height - 0.5f) * 2f, -1f, 1f);
        }

        Vector3 parallaxOffset =
            target.right * (nx * parallaxPositionAmount) +
            target.up * (ny * parallaxPositionAmount * 0.6f);

        Vector3 desiredPos = target.position + parallaxOffset;
        Quaternion desiredRot = target.rotation * Quaternion.Euler(-ny * parallaxRotationAmount, nx * parallaxRotationAmount, 0f);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref smoothVelocityPos,
            1f / Mathf.Max(moveSmoothSpeed, 0.1f),
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRot,
            rotateSmoothSpeed * Time.unscaledDeltaTime
        );
    }

    Transform GetTargetPose()
    {
        switch (currentView)
        {
            case MenuView.SkinSelect: return skinSelectPose != null ? skinSelectPose : mainMenuPose;
            case MenuView.HowToPlay:  return howToPlayPose != null ? howToPlayPose : mainMenuPose;
            case MenuView.Settings:   return settingsPose != null ? settingsPose : mainMenuPose;
            case MenuView.Loading:    return loadingPose != null ? loadingPose : skinSelectPose;
            default:                  return mainMenuPose;
        }
    }
}
