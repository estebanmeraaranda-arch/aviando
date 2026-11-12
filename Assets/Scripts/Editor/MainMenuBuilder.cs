using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/EscenarioCasa.unity";
    private const string TestScenePath = "Assets/Scenes/EscenadePrueba.unity";

    [MenuItem("Aviando/Construir Escena MainMenu")]
    public static void BuildMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- 1. Iluminación y Post-Proceso ---
        SetupLightingAndVolume();

        // --- 2. Diorama 3D del Apartamento ---
        GameObject dioramaRoot = new GameObject("Diorama_Apartamento");
        Vector3 perchTopPos = BuildApartmentDiorama(dioramaRoot.transform);

        // --- 3. Ave Interactiva en Pedestal ---
        GameObject showcaseObj = new GameObject("BirdShowcase");
        showcaseObj.transform.position = perchTopPos;
        GameObject anchorObj = new GameObject("BirdAnchor");
        anchorObj.transform.SetParent(showcaseObj.transform, false);
        anchorObj.transform.localPosition = Vector3.zero;
        anchorObj.transform.localRotation = Quaternion.Euler(0f, 155f, 0f);

        MenuBirdShowcase showcase = showcaseObj.AddComponent<MenuBirdShowcase>();
        showcase.birdAnchor = anchorObj.transform;
        showcase.showcaseScale = 3.2f;

        // Previsualización de referencia en editor (el runtime la reemplazará limpiamente al iniciar)
        GameObject previewPrefab = Resources.Load<GameObject>("lb_cardinal");
        if (previewPrefab != null)
        {
            GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(previewPrefab, anchorObj.transform);
            preview.name = "Preview_EditorOnly_RemoveRuntime";
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.identity;
            preview.transform.localScale = Vector3.one * 3.2f;
            // En runtime MenuBirdShowcase destruye cualquier hijo previo al instanciar el skin real,
            // pero limpiar aquí los scripts lb_Bird evita errores de lb_BirdController en MainMenu.
            foreach (var b in preview.GetComponentsInChildren<lb_Bird>(true)) Object.DestroyImmediate(b);
            foreach (var c in preview.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in preview.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(r);
            // Marcar para que MenuBirdShowcase limpie hijos previos en Awake
            Object.DestroyImmediate(preview); // Mejor: MenuBirdShowcase instancia limpio en Start!
        }

        // --- 4. Cámara Principal y Poses ---
        MenuCameraDirector cameraDirector = BuildCameraAndPoses(perchTopPos);

        // --- 5. EventSystem (New Input System) ---
        BuildEventSystem();

        // --- 6. GameManager & MainMenuController & Canvas UI ---
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.mainMenuSceneName = "MainMenu";
        gm.gameplaySceneName = "EscenarioCasa";

        GameObject menuCtrlObj = new GameObject("MainMenuController");
        MainMenuController menuCtrl = menuCtrlObj.AddComponent<MainMenuController>();
        menuCtrl.cameraDirector = cameraDirector;
        menuCtrl.birdShowcase = showcase;

        BuildUI(gm, menuCtrl);

        // --- 7. Guardar Escena y Configurar Build Settings ---
        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MainMenuBuilder] Escena MainMenu construida y guardada en " + ScenePath);
    }

    [MenuItem("Aviando/Agregar Barra de Estamina a EscenarioCasa")]
    public static void AddStaminaBarToGameplayScene()
    {
        if (!System.IO.File.Exists(GameplayScenePath))
        {
            Debug.LogError("[MainMenuBuilder] No se encontró " + GameplayScenePath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        HUDManager hud = Object.FindFirstObjectByType<HUDManager>();
        Canvas canvas = hud != null && hud.layoutGroup != null
            ? hud.layoutGroup.GetComponentInParent<Canvas>()
            : Object.FindFirstObjectByType<Canvas>();

        if (hud == null || canvas == null)
        {
            Debug.LogError("[MainMenuBuilder] No se encontró HUDManager o Canvas en EscenarioCasa.");
            return;
        }

        Transform existing = canvas.transform.Find("StaminaBarPanel");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite featherSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Img/pluma.png");
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject panel = CreateBox(canvas.transform, "StaminaBarPanel", uiSprite, new Color(0.07f, 0.09f, 0.14f, 0.86f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(36f, -92f);
        panelRect.sizeDelta = new Vector2(390f, 52f);

        // Icono de pluma a la izquierda
        if (featherSprite != null)
        {
            GameObject iconObj = new GameObject("FeatherIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(panel.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(30f, 30f);
            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = featherSprite;
            iconImg.preserveAspect = true;
            iconImg.color = new Color(1f, 0.86f, 0.42f, 1f);
            iconImg.raycastTarget = false;
        }

        // Fondo de la barra
        GameObject track = CreateBox(panel.transform, "StaminaTrack", uiSprite, new Color(0.16f, 0.20f, 0.28f, 1f));
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.pivot = new Vector2(0f, 0.5f);
        trackRect.anchoredPosition = new Vector2(52f, 0f);
        trackRect.sizeDelta = new Vector2(-146f, 20f);

        // Barra de estela (trail)
        GameObject trail = new GameObject("StaminaTrail", typeof(RectTransform), typeof(Image));
        trail.transform.SetParent(track.transform, false);
        RectTransform trailRect = trail.GetComponent<RectTransform>();
        trailRect.anchorMin = Vector2.zero;
        trailRect.anchorMax = Vector2.one;
        trailRect.offsetMin = Vector2.zero;
        trailRect.offsetMax = Vector2.zero;
        Image trailImg = trail.GetComponent<Image>();
        trailImg.sprite = uiSprite != null ? uiSprite : Texture2DToSprite();
        trailImg.type = Image.Type.Filled;
        trailImg.fillMethod = Image.FillMethod.Horizontal;
        trailImg.fillAmount = 1f;
        trailImg.color = new Color(1f, 1f, 1f, 0.42f);
        trailImg.raycastTarget = false;

        // Barra principal de relleno
        GameObject fill = new GameObject("StaminaFill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(track.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.GetComponent<Image>();
        fillImg.sprite = uiSprite != null ? uiSprite : Texture2DToSprite();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        fillImg.color = hud.fullStaminaColor;
        fillImg.raycastTarget = false;

        // Texto numérico a la derecha
        GameObject valObj = new GameObject("StaminaValue", typeof(RectTransform), typeof(Text));
        valObj.transform.SetParent(panel.transform, false);
        RectTransform valRect = valObj.GetComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1f, 0f);
        valRect.anchorMax = new Vector2(1f, 1f);
        valRect.pivot = new Vector2(1f, 0.5f);
        valRect.anchoredPosition = new Vector2(-12f, 0f);
        valRect.sizeDelta = new Vector2(78f, 0f);
        Text valTxt = valObj.GetComponent<Text>();
        valTxt.font = font;
        valTxt.text = "5 / 5";
        valTxt.fontSize = 20;
        valTxt.fontStyle = FontStyle.Bold;
        valTxt.alignment = TextAnchor.MiddleRight;
        valTxt.color = new Color(0.96f, 0.96f, 0.98f, 1f);
        valTxt.raycastTarget = false;

        hud.staminaBarRoot = panel;
        hud.staminaFillImage = fillImg;
        hud.staminaTrailImage = trailImg;
        hud.staminaValueText = valTxt;

        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        Debug.Log("[MainMenuBuilder] Barra de estamina agregada y guardada en " + GameplayScenePath);
    }

    static void SetupLightingAndVolume()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.39f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.20f, 0.18f, 0.16f);

        GameObject sunObj = new GameObject("Directional Light");
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.84f, 0.62f);
        sun.intensity = 2.4f;
        sun.shadows = LightShadows.Soft;
        sunObj.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

        GameObject rimObj = new GameObject("Rim Light");
        Light rim = rimObj.AddComponent<Light>();
        rim.type = LightType.Point;
        rim.color = new Color(0.55f, 0.78f, 1f);
        rim.intensity = 3.5f;
        rim.range = 8f;
        rimObj.transform.position = new Vector3(-1.5f, 2.2f, 1.4f);

        GameObject keyObj = new GameObject("Warm Key Light");
        Light key = keyObj.AddComponent<Light>();
        key.type = LightType.Point;
        key.color = new Color(1f, 0.88f, 0.72f);
        key.intensity = 4.0f;
        key.range = 7f;
        keyObj.transform.position = new Vector3(1.6f, 2.1f, -1.6f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
        if (profile != null)
        {
            GameObject volObj = new GameObject("Global Volume");
            Volume vol = volObj.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }
    }

    static Vector3 BuildApartmentDiorama(Transform parent)
    {
        // Suelo del apartamento
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Apt Build Kit/Interiors/Flooring & Ceilings/Int_Apt_01_Floor_01.prefab",
            parent, new Vector3(0f, 0f, 0f), Quaternion.identity, "Floor_01"
        );

        // Paredes traseras y laterales para enmarcar el rincón
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Apt Build Kit/Interiors/Walls Frames & Doors/Apt_01/Int_Apt_01_Wall_01.prefab",
            parent, new Vector3(-2.0f, 0f, 3.2f), Quaternion.identity, "Wall_Back_L"
        );
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Apt Build Kit/Interiors/Walls Frames & Doors/Apt_01/Int_Apt_01_Wall_01.prefab",
            parent, new Vector3(2.0f, 0f, 3.2f), Quaternion.identity, "Wall_Back_R"
        );
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Apt Build Kit/Interiors/Walls Frames & Doors/Apt_01/Int_Apt_01_Wall_01.prefab",
            parent, new Vector3(4.2f, 0f, 0.5f), Quaternion.Euler(0f, 90f, 0f), "Wall_Right"
        );

        // Cuadro decorativo
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Props/Art/WallArt_Apt_03.prefab",
            parent, new Vector3(1.4f, 1.85f, 3.05f), Quaternion.Euler(0f, 180f, 0f), "WallArt"
        );

        // Alfombra y muebles de ambiente
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Props/Art/Rug_Apt_01.prefab",
            parent, new Vector3(0.4f, 0.01f, 0.2f), Quaternion.identity, "Rug"
        );
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Furniture/Living Room/Shelf_Apt_01.prefab",
            parent, new Vector3(2.6f, 0f, 2.5f), Quaternion.Euler(0f, -155f, 0f), "Shelf"
        );
        InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Furniture/Living Room/Chair_Apt_01.prefab",
            parent, new Vector3(-1.3f, 0f, 1.9f), Quaternion.Euler(0f, 35f, 0f), "Chair"
        );

        // Mesa principal donde reposa el pedestal del ave
        GameObject table = InstantiatePrefabAt(
            "Assets/Brick Project Studio/Apartment Kit/_Prefabs/Furniture/Living Room/Table_Coffee_01.prefab",
            parent, new Vector3(0.35f, 0f, 0f), Quaternion.Euler(0f, 12f, 0f), "Table_Center"
        );

        float tableTopY = 0.48f;
        if (table != null)
        {
            Bounds b = CalculateHierarchyBounds(table);
            if (b.size.y > 0.05f) tableTopY = b.max.y;
        }
        else
        {
            // Fallback por si no carga el prefab
            GameObject fallbackTable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallbackTable.name = "Fallback_Table";
            fallbackTable.transform.SetParent(parent);
            fallbackTable.transform.position = new Vector3(0.35f, 0.24f, 0f);
            fallbackTable.transform.localScale = new Vector3(1.2f, 0.24f, 1.2f);
            tableTopY = 0.48f;
        }

        // Pedestal de madera para el ave sobre la mesa
        GameObject perchBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        perchBase.name = "Bird_Pedestal";
        perchBase.transform.SetParent(parent);
        perchBase.transform.position = new Vector3(0.35f, tableTopY + 0.045f, 0f);
        perchBase.transform.localScale = new Vector3(0.42f, 0.045f, 0.42f);
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Brick Project Studio/Apartment Kit/Common/Materials/Surface/Adler Dark Wood.mat");
        if (woodMat != null)
        {
            perchBase.GetComponent<Renderer>().sharedMaterial = woodMat;
        }

        // Pluma coleccionable girando cerca del ave como guiño directo al gameplay
        GameObject feather = InstantiatePrefabAt(
            "Assets/prerfabs/PlumaColeccionable.prefab",
            parent, new Vector3(0.95f, tableTopY + 0.36f, 0.25f), Quaternion.Euler(12f, 20f, -15f), "Showcase_Feather"
        );
        if (feather != null)
        {
            foreach (Collider c in feather.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
        }

        // Cámara de seguridad activa en el fondo haciendo barrido + cono de luz roja
        BuildSecurityCameraProp(parent, new Vector3(2.9f, 2.55f, 2.85f), Quaternion.Euler(22f, -145f, 0f));

        return new Vector3(0.35f, tableTopY + 0.09f, 0f);
    }

    static void BuildSecurityCameraProp(Transform parent, Vector3 pos, Quaternion rot)
    {
        GameObject camHolder = new GameObject("CamaraSeguridad_Menu");
        camHolder.transform.SetParent(parent);
        camHolder.transform.position = pos;
        camHolder.transform.rotation = rot;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "CuerpoCamara";
        body.transform.SetParent(camHolder.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        SecurityCamera secCam = camHolder.AddComponent<SecurityCamera>();
        secCam.sweepAngle = 35f;
        secCam.sweepSpeed = 1.15f;
        secCam.sweepVertical = false;

        GameObject spotObj = new GameObject("ConoVision_Luz");
        spotObj.transform.SetParent(camHolder.transform, false);
        spotObj.transform.localPosition = new Vector3(0f, 0f, 0.22f);
        Light spot = spotObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.12f, 0.12f);
        spot.intensity = 35f;
        spot.range = 8f;
        spot.spotAngle = 38f;
    }

    static MenuCameraDirector BuildCameraAndPoses(Vector3 birdPerchPos)
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 42f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 1f);
        camObj.AddComponent<AudioListener>();
        camObj.AddComponent<UniversalAdditionalCameraData>();

        MenuCameraDirector director = camObj.AddComponent<MenuCameraDirector>();

        GameObject posesRoot = new GameObject("CameraPoses");

        // Pose 1: Menú Principal (ave ligeramente a la derecha para dejar aire al panel izquierdo)
        Transform mainPose = CreatePose(
            posesRoot.transform,
            "Pose_MainMenu",
            birdPerchPos + new Vector3(-0.95f, 0.45f, -1.65f),
            birdPerchPos + new Vector3(-0.22f, 0.20f, 0.10f)
        );

        // Pose 2: Aviario / Selección de Ave (primer plano heroico del ave)
        Transform skinPose = CreatePose(
            posesRoot.transform,
            "Pose_SkinSelect",
            birdPerchPos + new Vector3(-0.42f, 0.28f, -1.05f),
            birdPerchPos + new Vector3(-0.14f, 0.18f, 0f)
        );

        // Pose 3: Manual de Vuelo (vista amplia diagonal mostrando ave + pluma + cámara de seguridad)
        Transform howToPose = CreatePose(
            posesRoot.transform,
            "Pose_HowToPlay",
            birdPerchPos + new Vector3(-1.35f, 0.72f, -1.75f),
            birdPerchPos + new Vector3(0.45f, 0.35f, 0.45f)
        );

        // Pose 4: Configuración
        Transform settingsPose = CreatePose(
            posesRoot.transform,
            "Pose_Settings",
            birdPerchPos + new Vector3(-1.05f, 0.38f, -1.45f),
            birdPerchPos + new Vector3(-0.18f, 0.22f, 0f)
        );

        // Pose 5: Cargando
        Transform loadingPose = CreatePose(
            posesRoot.transform,
            "Pose_Loading",
            birdPerchPos + new Vector3(0.10f, 0.25f, -0.95f),
            birdPerchPos + new Vector3(0f, 0.20f, 0f)
        );

        director.mainMenuPose = mainPose;
        director.skinSelectPose = skinPose;
        director.howToPlayPose = howToPose;
        director.settingsPose = settingsPose;
        director.loadingPose = loadingPose;

        camObj.transform.position = mainPose.position;
        camObj.transform.rotation = mainPose.rotation;

        return director;
    }

    static Transform CreatePose(Transform parent, string name, Vector3 pos, Vector3 lookAtTarget)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        obj.transform.LookAt(lookAtTarget);
        return obj.transform;
    }

    static void BuildEventSystem()
    {
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<InputSystemUIInputModule>();
    }

    static void BuildUI(GameManager gm, MainMenuController ctrl)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite featherSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Img/pluma.png");
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject canvasObj = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        AudioSource uiAudio = canvasObj.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.spatialBlend = 0f;
        AudioClip clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/living birds/sounds/flyAway2.wav");

        // --- Etiqueta inferior de interacción 3D (siempre visible fuera de carga) ---
        GameObject hintPill = CreateBox(canvasObj.transform, "Hint3D", uiSprite, new Color(0.06f, 0.08f, 0.13f, 0.72f));
        RectTransform hintRect = hintPill.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-42f, 32f);
        hintRect.sizeDelta = new Vector2(650f, 48f);
        CreateText(hintPill.transform, "HintTxt", "Arrastra en la escena para rotar al ave  ·  Haz clic sobre ella para oír su canto", font, 20, FontStyle.Normal, new Color(0.88f, 0.90f, 0.95f, 0.92f), TextAnchor.MiddleCenter);

        // ==========================================================
        // PANEL 1: MENÚ PRINCIPAL
        // ==========================================================
        GameObject mainPanel = CreateFullPanel(canvasObj.transform, "MainMenuPanel");
        GameObject mainCard = CreateBox(mainPanel.transform, "MainCard", uiSprite, new Color(0.07f, 0.09f, 0.14f, 0.86f));
        RectTransform mainCardRect = mainCard.GetComponent<RectTransform>();
        mainCardRect.anchorMin = new Vector2(0f, 0.5f);
        mainCardRect.anchorMax = new Vector2(0f, 0.5f);
        mainCardRect.pivot = new Vector2(0f, 0.5f);
        mainCardRect.anchoredPosition = new Vector2(85f, 0f);
        mainCardRect.sizeDelta = new Vector2(560f, 760f);

        // Emblema Pluma + Título AVIANDO
        if (featherSprite != null)
        {
            GameObject logoObj = new GameObject("LogoPluma", typeof(RectTransform), typeof(Image));
            logoObj.transform.SetParent(mainCard.transform, false);
            RectTransform lRect = logoObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 1f);
            lRect.anchorMax = new Vector2(0f, 1f);
            lRect.pivot = new Vector2(0f, 1f);
            lRect.anchoredPosition = new Vector2(46f, -44f);
            lRect.sizeDelta = new Vector2(78f, 78f);
            Image lImg = logoObj.GetComponent<Image>();
            lImg.sprite = featherSprite;
            lImg.preserveAspect = true;
            lImg.color = new Color(1f, 0.86f, 0.42f, 1f);
        }

        Text titleTxt = CreateTextAt(mainCard.transform, "Title", "AVIANDO", font, 68, FontStyle.Bold, new Color(0.98f, 0.96f, 0.91f, 1f), TextAnchor.MiddleLeft, new Vector2(140f, -44f), new Vector2(380f, 80f));
        titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

        CreateTextAt(mainCard.transform, "Subtitle", "SIGILO  ·  VUELO  ·  PLUMAS", font, 20, FontStyle.Bold, new Color(0.94f, 0.48f, 0.35f, 1f), TextAnchor.MiddleLeft, new Vector2(48f, -135f), new Vector2(460f, 34f));

        // Badge del ave activa
        GameObject badgeBox = CreateBox(mainCard.transform, "ActiveBirdBadge", uiSprite, new Color(0.14f, 0.18f, 0.27f, 0.9f));
        RectTransform badgeRect = badgeBox.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(48f, -182f);
        badgeRect.sizeDelta = new Vector2(464f, 48f);
        Text badgeTxt = CreateText(badgeBox.transform, "BadgeTxt", "Ave activa: Cardenal Rojo", font, 21, FontStyle.Bold, new Color(0.92f, 0.94f, 0.98f, 1f), TextAnchor.MiddleCenter);
        ctrl.activeBirdBadgeText = badgeTxt;

        // Botones principales
        CreateMenuButton(mainCard.transform, "Btn_Jugar", "JUGAR  (ELEGIR AVE)", new Vector2(48f, -268f), new Vector2(464f, 84f),
            new Color(0.84f, 0.30f, 0.24f, 0.96f), new Color(0.95f, 0.38f, 0.30f, 1f),
            font, uiSprite, featherSprite, uiAudio, clickSound, ctrl.OpenSkinSelection);

        CreateMenuButton(mainCard.transform, "Btn_ComoJugar", "CÓMO JUGAR", new Vector2(48f, -374f), new Vector2(464f, 76f),
            new Color(0.13f, 0.17f, 0.25f, 0.92f), new Color(0.20f, 0.27f, 0.39f, 1f),
            font, uiSprite, featherSprite, uiAudio, clickSound, ctrl.OpenHowToPlay);

        CreateMenuButton(mainCard.transform, "Btn_Configuracion", "CONFIGURACIÓN", new Vector2(48f, -470f), new Vector2(464f, 76f),
            new Color(0.13f, 0.17f, 0.25f, 0.92f), new Color(0.20f, 0.27f, 0.39f, 1f),
            font, uiSprite, featherSprite, uiAudio, clickSound, ctrl.OpenSettings);

        CreateMenuButton(mainCard.transform, "Btn_Salir", "SALIR", new Vector2(48f, -566f), new Vector2(464f, 72f),
            new Color(0.11f, 0.13f, 0.19f, 0.88f), new Color(0.25f, 0.16f, 0.19f, 0.98f),
            font, uiSprite, featherSprite, uiAudio, clickSound, ctrl.QuitGame);

        CreateTextAt(mainCard.transform, "FooterNote", "Escapa de la vigilancia dentro de la casa", font, 18, FontStyle.Italic, new Color(0.68f, 0.72f, 0.80f, 0.85f), TextAnchor.MiddleLeft, new Vector2(48f, -682f), new Vector2(464f, 36f));

        // ==========================================================
        // PANEL 2: AVIARIO (SELECCIÓN DE SKIN)
        // ==========================================================
        GameObject skinPanel = CreateFullPanel(canvasObj.transform, "SkinSelectionPanel");
        GameObject skinCard = CreateBox(skinPanel.transform, "SkinCard", uiSprite, new Color(0.07f, 0.09f, 0.14f, 0.90f));
        RectTransform skinCardRect = skinCard.GetComponent<RectTransform>();
        skinCardRect.anchorMin = new Vector2(0f, 0.5f);
        skinCardRect.anchorMax = new Vector2(0f, 0.5f);
        skinCardRect.pivot = new Vector2(0f, 0.5f);
        skinCardRect.anchoredPosition = new Vector2(75f, 0f);
        skinCardRect.sizeDelta = new Vector2(610f, 840f);

        GameObject accentBarObj = CreateBox(skinCard.transform, "AccentBar", uiSprite, new Color(0.89f, 0.25f, 0.25f, 1f));
        RectTransform accentRect = accentBarObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 10f);
        ctrl.skinAccentBar = accentBarObj.GetComponent<Image>();

        CreateTextAt(skinCard.transform, "AviarioHeader", "AVIARIO  ·  ELIGE TU AVE", font, 22, FontStyle.Bold, new Color(0.95f, 0.76f, 0.28f, 1f), TextAnchor.MiddleLeft, new Vector2(42f, -34f), new Vector2(400f, 34f));
        ctrl.skinCounterText = CreateTextAt(skinCard.transform, "SkinCounter", "1 / 7", font, 22, FontStyle.Bold, new Color(0.75f, 0.80f, 0.88f, 1f), TextAnchor.MiddleRight, new Vector2(430f, -34f), new Vector2(138f, 34f));

        ctrl.skinNameText = CreateTextAt(skinCard.transform, "SkinName", "CARDENAL ROJO", font, 44, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, new Vector2(42f, -84f), new Vector2(526f, 60f));
        ctrl.skinTaglineText = CreateTextAt(skinCard.transform, "SkinTagline", "El Infiltrado Clásico", font, 22, FontStyle.Bold, new Color(0.76f, 0.84f, 0.96f, 1f), TextAnchor.MiddleLeft, new Vector2(42f, -148f), new Vector2(526f, 36f));
        ctrl.skinDescriptionText = CreateTextAt(skinCard.transform, "SkinDesc", "Descripción...", font, 22, FontStyle.Normal, new Color(0.84f, 0.86f, 0.90f, 1f), TextAnchor.UpperLeft, new Vector2(42f, -196f), new Vector2(526f, 88f));

        // Botones Anterior / Siguiente
        CreateActionButton(skinCard.transform, "Btn_PrevSkin", "<  Anterior", new Vector2(42f, -304f), new Vector2(252f, 58f), new Color(0.15f, 0.19f, 0.28f, 1f), font, uiSprite, 22, ctrl.PreviousSkin);
        CreateActionButton(skinCard.transform, "Btn_NextSkin", "Siguiente  >", new Vector2(316f, -304f), new Vector2(252f, 58f), new Color(0.15f, 0.19f, 0.28f, 1f), font, uiSprite, 22, ctrl.NextSkin);

        // Pills de las 7 especies
        CreateTextAt(skinCard.transform, "SpeciesLabel", "ESPECIES DISPONIBLES", font, 17, FontStyle.Bold, new Color(0.65f, 0.70f, 0.78f, 1f), TextAnchor.MiddleLeft, new Vector2(42f, -382f), new Vector2(400f, 28f));

        ctrl.skinPillButtons = new List<Button>();
        ctrl.skinPillBackgrounds = new List<Image>();
        for (int i = 0; i < BirdSkinCatalog.Skins.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            float px = 42f + col * 268f;
            float py = -418f - row * 60f;
            int capturedIndex = i;

            Button pillBtn = CreateIntActionButton(
                skinCard.transform,
                $"Pill_{BirdSkinCatalog.Skins[i].id}",
                BirdSkinCatalog.Skins[i].displayName,
                new Vector2(px, py),
                new Vector2(256f, 50f),
                new Color(0.14f, 0.17f, 0.24f, 0.88f),
                font,
                uiSprite,
                20,
                ctrl.SelectSkin,
                capturedIndex
            );
            ctrl.skinPillButtons.Add(pillBtn);
            ctrl.skinPillBackgrounds.Add(pillBtn.GetComponent<Image>());
        }

        // Botones interactivos para probar al ave en 3D
        CreateActionButton(skinCard.transform, "Btn_Chirp", "Escuchar Canto", new Vector2(42f, -664f), new Vector2(252f, 54f), new Color(0.21f, 0.28f, 0.40f, 1f), font, uiSprite, 20, ctrl.ChirpShowcase);
        CreateActionButton(skinCard.transform, "Btn_Flap", "Probar Aleteo", new Vector2(316f, -664f), new Vector2(252f, 54f), new Color(0.21f, 0.28f, 0.40f, 1f), font, uiSprite, 20, ctrl.ToggleFlapShowcase);

        // Confirmar y Volver
        CreateActionButton(skinCard.transform, "Btn_BackFromSkin", "Volver", new Vector2(42f, -742f), new Vector2(180f, 68f), new Color(0.14f, 0.17f, 0.24f, 1f), font, uiSprite, 24, ctrl.ShowMainMenu);
        CreateActionButton(skinCard.transform, "Btn_PlayNow", "¡A VOLAR!", new Vector2(236f, -742f), new Vector2(332f, 68f), new Color(0.84f, 0.30f, 0.24f, 1f), font, uiSprite, 28, ctrl.ConfirmAndPlay);

        // ==========================================================
        // PANEL 3: CÓMO JUGAR (CONTROLES Y MECÁNICAS)
        // ==========================================================
        GameObject howPanel = CreateFullPanel(canvasObj.transform, "HowToPlayPanel");
        GameObject howCard = CreateBox(howPanel.transform, "HowCard", uiSprite, new Color(0.07f, 0.09f, 0.14f, 0.91f));
        RectTransform howRect = howCard.GetComponent<RectTransform>();
        howRect.anchorMin = new Vector2(0f, 0.5f);
        howRect.anchorMax = new Vector2(0f, 0.5f);
        howRect.pivot = new Vector2(0f, 0.5f);
        howRect.anchoredPosition = new Vector2(80f, 0f);
        howRect.sizeDelta = new Vector2(760f, 780f);

        CreateTextAt(howCard.transform, "HowTitle", "MANUAL DE VUELO", font, 42, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, new Vector2(46f, -40f), new Vector2(650f, 56f));
        CreateTextAt(howCard.transform, "HowSub", "Domina el aire y evita ser visto dentro de la casa", font, 21, FontStyle.Italic, new Color(0.95f, 0.76f, 0.28f, 1f), TextAnchor.MiddleLeft, new Vector2(46f, -100f), new Vector2(650f, 34f));

        string controlsCopy =
            "CONTROLES\n" +
            "•  W A S D  /  Flechas  —  Caminar y dirigir el vuelo según la cámara\n" +
            "•  ESPACIO (en el suelo)  —  Salto inicial para despegar\n" +
            "•  ESPACIO (en el aire)  —  Aletear hacia arriba (consume 1 pluma)\n" +
            "•  Mantener ESPACIO al caer  —  Planear suavemente\n" +
            "•  Ratón + Rueda  —  Rotar cámara y ajustar zoom  |  ESC  —  Pausa";

        GameObject ctrlBox = CreateBox(howCard.transform, "ControlsBox", uiSprite, new Color(0.11f, 0.15f, 0.22f, 0.92f));
        RectTransform ctrlBoxRect = ctrlBox.GetComponent<RectTransform>();
        ctrlBoxRect.anchorMin = new Vector2(0f, 1f);
        ctrlBoxRect.anchorMax = new Vector2(0f, 1f);
        ctrlBoxRect.pivot = new Vector2(0f, 1f);
        ctrlBoxRect.anchoredPosition = new Vector2(46f, -155f);
        ctrlBoxRect.sizeDelta = new Vector2(668f, 245f);
        Text ctrlTxt = CreateTextAt(ctrlBox.transform, "ControlsTxt", controlsCopy, font, 21, FontStyle.Normal, new Color(0.90f, 0.92f, 0.96f, 1f), TextAnchor.UpperLeft, new Vector2(24f, -20f), new Vector2(620f, 210f));
        ctrlTxt.lineSpacing = 1.25f;

        string rulesCopy =
            "REGLAS DEL ESCENARIO\n" +
            "•  Plumas Doradas: Cada pluma recogida amplía tu estamina máxima (+1 aleteo).\n" +
            "•  Descanso Seguro: Tocar el suelo o muebles recarga rápidamente tus plumas.\n" +
            "•  Cámaras de Vigilancia: Si entras en su cono rojo, rebobinarás a tu último punto seguro.";

        GameObject rulesBox = CreateBox(howCard.transform, "RulesBox", uiSprite, new Color(0.11f, 0.15f, 0.22f, 0.92f));
        RectTransform rulesBoxRect = rulesBox.GetComponent<RectTransform>();
        rulesBoxRect.anchorMin = new Vector2(0f, 1f);
        rulesBoxRect.anchorMax = new Vector2(0f, 1f);
        rulesBoxRect.pivot = new Vector2(0f, 1f);
        rulesBoxRect.anchoredPosition = new Vector2(46f, -422f);
        rulesBoxRect.sizeDelta = new Vector2(668f, 215f);
        Text rulesTxt = CreateTextAt(rulesBox.transform, "RulesTxt", rulesCopy, font, 21, FontStyle.Normal, new Color(0.90f, 0.92f, 0.96f, 1f), TextAnchor.UpperLeft, new Vector2(24f, -20f), new Vector2(620f, 180f));
        rulesTxt.lineSpacing = 1.25f;

        CreateActionButton(howCard.transform, "Btn_BackFromHow", "Volver", new Vector2(46f, -674f), new Vector2(210f, 66f), new Color(0.14f, 0.17f, 0.24f, 1f), font, uiSprite, 24, ctrl.ShowMainMenu);
        CreateActionButton(howCard.transform, "Btn_GoPlayFromHow", "Elegir Ave y Jugar", new Vector2(280f, -674f), new Vector2(434f, 66f), new Color(0.84f, 0.30f, 0.24f, 1f), font, uiSprite, 24, ctrl.OpenSkinSelection);

        // ==========================================================
        // PANEL 4: CONFIGURACIÓN
        // ==========================================================
        GameObject settingsPanel = CreateFullPanel(canvasObj.transform, "SettingsPanel");
        GameObject settingsCard = CreateBox(settingsPanel.transform, "SettingsCard", uiSprite, new Color(0.07f, 0.09f, 0.14f, 0.91f));
        RectTransform setRect = settingsCard.GetComponent<RectTransform>();
        setRect.anchorMin = new Vector2(0f, 0.5f);
        setRect.anchorMax = new Vector2(0f, 0.5f);
        setRect.pivot = new Vector2(0f, 0.5f);
        setRect.anchoredPosition = new Vector2(85f, 0f);
        setRect.sizeDelta = new Vector2(620f, 700f);

        CreateTextAt(settingsCard.transform, "SettingsTitle", "CONFIGURACIÓN", font, 42, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, new Vector2(46f, -42f), new Vector2(520f, 56f));

        // Volumen General
        CreateTextAt(settingsCard.transform, "VolLabel", "Volumen General", font, 24, FontStyle.Bold, new Color(0.90f, 0.92f, 0.96f, 1f), TextAnchor.MiddleLeft, new Vector2(46f, -130f), new Vector2(360f, 36f));
        ctrl.volumeValueText = CreateTextAt(settingsCard.transform, "VolValue", "100%", font, 24, FontStyle.Bold, new Color(0.95f, 0.76f, 0.28f, 1f), TextAnchor.MiddleRight, new Vector2(424f, -130f), new Vector2(150f, 36f));
        ctrl.volumeSlider = CreateSlider(settingsCard.transform, "Slider_Volume", new Vector2(46f, -178f), new Vector2(528f, 32f), 0f, 1f, 1f, uiSprite);

        // Sensibilidad del Ratón
        CreateTextAt(settingsCard.transform, "SensLabel", "Sensibilidad de Cámara", font, 24, FontStyle.Bold, new Color(0.90f, 0.92f, 0.96f, 1f), TextAnchor.MiddleLeft, new Vector2(46f, -246f), new Vector2(360f, 36f));
        ctrl.sensitivityValueText = CreateTextAt(settingsCard.transform, "SensValue", "3.0", font, 24, FontStyle.Bold, new Color(0.95f, 0.76f, 0.28f, 1f), TextAnchor.MiddleRight, new Vector2(424f, -246f), new Vector2(150f, 36f));
        ctrl.sensitivitySlider = CreateSlider(settingsCard.transform, "Slider_Sensitivity", new Vector2(46f, -294f), new Vector2(528f, 32f), 0.5f, 8f, 3f, uiSprite);

        // Calidad Gráfica
        Button qualBtn = CreateActionButton(settingsCard.transform, "Btn_Quality", "Calidad Gráfica: Alta", new Vector2(46f, -372f), new Vector2(528f, 64f), new Color(0.14f, 0.18f, 0.27f, 1f), font, uiSprite, 23, ctrl.CycleQualityLevel);
        ctrl.qualityLabelText = qualBtn.GetComponentInChildren<Text>();

        // Pantalla Completa
        ctrl.fullscreenToggle = CreateToggle(settingsCard.transform, "Toggle_Fullscreen", "Pantalla Completa", new Vector2(46f, -464f), new Vector2(528f, 48f), font, uiSprite);

        // Restablecer y Volver
        CreateActionButton(settingsCard.transform, "Btn_ResetSettings", "Restablecer", new Vector2(46f, -586f), new Vector2(236f, 64f), new Color(0.17f, 0.20f, 0.28f, 1f), font, uiSprite, 22, ctrl.ResetDefaultSettings);
        CreateActionButton(settingsCard.transform, "Btn_BackFromSettings", "Volver", new Vector2(304f, -586f), new Vector2(270f, 64f), new Color(0.84f, 0.30f, 0.24f, 1f), font, uiSprite, 24, ctrl.ShowMainMenu);

        // ==========================================================
        // PANEL 5: PANTALLA DE CARGA
        // ==========================================================
        GameObject loadPanel = CreateFullPanel(canvasObj.transform, "LoadingPanel");
        Image loadBg = loadPanel.AddComponent<Image>();
        loadBg.color = new Color(0.05f, 0.07f, 0.11f, 0.90f);

        GameObject spinnerObj = new GameObject("LoadingFeather", typeof(RectTransform), typeof(Image));
        spinnerObj.transform.SetParent(loadPanel.transform, false);
        RectTransform spinRect = spinnerObj.GetComponent<RectTransform>();
        spinRect.anchoredPosition = new Vector2(0f, 80f);
        spinRect.sizeDelta = new Vector2(110f, 110f);
        Image spinImg = spinnerObj.GetComponent<Image>();
        if (featherSprite != null) spinImg.sprite = featherSprite;
        spinImg.preserveAspect = true;
        spinImg.color = new Color(1f, 0.84f, 0.38f, 1f);
        ctrl.loadingFeatherIcon = spinImg;

        Text loadTitle = CreateText(loadPanel.transform, "LoadingTitle", "DESPEGANDO HACIA LA CASA...", font, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        loadTitle.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        loadTitle.rectTransform.sizeDelta = new Vector2(800f, 52f);

        GameObject barBg = CreateBox(loadPanel.transform, "ProgressBg", uiSprite, new Color(0.16f, 0.20f, 0.28f, 1f));
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.anchoredPosition = new Vector2(0f, -86f);
        barBgRect.sizeDelta = new Vector2(620f, 22f);

        GameObject barFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform fillRect = barFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = barFill.GetComponent<Image>();
        fillImg.sprite = uiSprite != null ? uiSprite : Texture2DToSprite();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;
        fillImg.color = new Color(0.92f, 0.42f, 0.26f, 1f);
        ctrl.loadingProgressFill = fillImg;

        Text pctTxt = CreateText(loadPanel.transform, "LoadingPercent", "0%", font, 24, FontStyle.Bold, new Color(0.95f, 0.78f, 0.35f, 1f), TextAnchor.MiddleCenter);
        pctTxt.rectTransform.anchoredPosition = new Vector2(0f, -132f);
        pctTxt.rectTransform.sizeDelta = new Vector2(200f, 36f);
        ctrl.loadingPercentText = pctTxt;

        Text tipTxt = CreateText(loadPanel.transform, "LoadingTip", "", font, 22, FontStyle.Italic, new Color(0.80f, 0.84f, 0.90f, 1f), TextAnchor.MiddleCenter);
        tipTxt.rectTransform.anchoredPosition = new Vector2(0f, -210f);
        tipTxt.rectTransform.sizeDelta = new Vector2(1050f, 70f);
        ctrl.loadingTipText = tipTxt;

        // Asignar referencias en GameManager y MainMenuController
        ctrl.mainMenuPanel = mainPanel;
        ctrl.skinSelectionPanel = skinPanel;
        ctrl.howToPlayPanel = howPanel;
        ctrl.settingsPanel = settingsPanel;
        ctrl.loadingPanel = loadPanel;

        gm.mainMenuPanel = mainPanel;
        gm.skinSelectionPanel = skinPanel;
        gm.howToPlayPanel = howPanel;
        gm.settingsPanel = settingsPanel;
        gm.loadingPanel = loadPanel;

        skinPanel.SetActive(false);
        howPanel.SetActive(false);
        settingsPanel.SetActive(false);
        loadPanel.SetActive(false);
    }

    static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        if (System.IO.File.Exists(ScenePath))
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        if (System.IO.File.Exists(GameplayScenePath))
            scenes.Add(new EditorBuildSettingsScene(GameplayScenePath, true));
        if (System.IO.File.Exists(TestScenePath))
            scenes.Add(new EditorBuildSettingsScene(TestScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // --- Helpers de Prefabs y UI ---

    static GameObject InstantiatePrefabAt(string assetPath, Transform parent, Vector3 pos, Quaternion rot, string customName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return null;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = customName;
        instance.transform.position = pos;
        instance.transform.rotation = rot;
        return instance;
    }

    static Bounds CalculateHierarchyBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    static GameObject CreateFullPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    static GameObject CreateBox(Transform parent, string name, Sprite bgSprite, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image img = obj.GetComponent<Image>();
        if (bgSprite != null)
        {
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
        }
        img.color = color;
        return obj;
    }

    static Text CreateText(Transform parent, string name, string text, Font font, int size, FontStyle style, Color color, TextAnchor align)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text t = obj.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static Text CreateTextAt(Transform parent, string name, string text, Font font, int size, FontStyle style, Color color, TextAnchor align, Vector2 topLeftPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeftPos;
        rect.sizeDelta = sizeDelta;

        Text t = obj.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static Button CreateMenuButton(
        Transform parent, string name, string label, Vector2 topLeftPos, Vector2 sizeDelta,
        Color normalCol, Color hoverCol, Font font, Sprite bgSprite, Sprite featherSprite,
        AudioSource audioSource, AudioClip clickClip, UnityAction action)
    {
        GameObject obj = CreateBox(parent, name, bgSprite, normalCol);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeftPos;
        rect.sizeDelta = sizeDelta;

        Button btn = obj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(btn.onClick, action);

        // Indicador de pluma al hacer hover
        GameObject indicator = new GameObject("FeatherIndicator", typeof(RectTransform), typeof(Image));
        indicator.transform.SetParent(obj.transform, false);
        RectTransform indRect = indicator.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0f, 0.5f);
        indRect.anchorMax = new Vector2(0f, 0.5f);
        indRect.pivot = new Vector2(0f, 0.5f);
        indRect.anchoredPosition = new Vector2(18f, 0f);
        indRect.sizeDelta = new Vector2(34f, 34f);
        Image indImg = indicator.GetComponent<Image>();
        if (featherSprite != null) indImg.sprite = featherSprite;
        indImg.preserveAspect = true;
        indImg.color = new Color(1f, 0.88f, 0.45f, 1f);
        indImg.raycastTarget = false;
        indicator.SetActive(false);

        Text txt = CreateText(obj.transform, "Label", label, font, 26, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        txt.rectTransform.offsetMin = new Vector2(64f, 0f);
        txt.rectTransform.offsetMax = new Vector2(-20f, 0f);

        MenuUIButtonFX fx = obj.AddComponent<MenuUIButtonFX>();
        fx.hoverIndicator = indicator;
        fx.tintTarget = obj.GetComponent<Image>();
        fx.normalColor = normalCol;
        fx.hoverColor = hoverCol;
        fx.uiAudioSource = audioSource;
        fx.clickClip = clickClip;

        return btn;
    }

    static Button CreateActionButton(Transform parent, string name, string label, Vector2 topLeftPos, Vector2 sizeDelta, Color bgColor, Font font, Sprite bgSprite, int fontSize, UnityAction action)
    {
        GameObject obj = CreateBox(parent, name, bgSprite, bgColor);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeftPos;
        rect.sizeDelta = sizeDelta;

        Button btn = obj.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(btn.onClick, action);

        MenuUIButtonFX fx = obj.AddComponent<MenuUIButtonFX>();
        fx.hoverOffsetX = 0f;
        fx.hoverScale = 1.03f;
        fx.tintTarget = obj.GetComponent<Image>();
        fx.normalColor = bgColor;
        fx.hoverColor = Color.Lerp(bgColor, Color.white, 0.16f);

        CreateText(obj.transform, "Label", label, font, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        return btn;
    }

    static Button CreateIntActionButton(Transform parent, string name, string label, Vector2 topLeftPos, Vector2 sizeDelta, Color bgColor, Font font, Sprite bgSprite, int fontSize, UnityAction<int> action, int arg)
    {
        GameObject obj = CreateBox(parent, name, bgSprite, bgColor);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeftPos;
        rect.sizeDelta = sizeDelta;

        Button btn = obj.AddComponent<Button>();
        UnityEventTools.AddIntPersistentListener(btn.onClick, action, arg);

        CreateText(obj.transform, "Label", label, font, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        return btn;
    }

    static Slider CreateSlider(Transform parent, string name, Vector2 topLeftPos, Vector2 sizeDelta, float minVal, float maxVal, float defaultVal, Sprite uiSprite)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        RectTransform sRect = sliderObj.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0f, 1f);
        sRect.anchorMax = new Vector2(0f, 1f);
        sRect.pivot = new Vector2(0f, 1f);
        sRect.anchoredPosition = topLeftPos;
        sRect.sizeDelta = sizeDelta;

        GameObject bg = CreateBox(sliderObj.transform, "Background", uiSprite, new Color(0.16f, 0.20f, 0.28f, 1f));
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRect = fillArea.GetComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0.25f);
        faRect.anchorMax = new Vector2(1f, 0.75f);
        faRect.offsetMin = new Vector2(5f, 0f);
        faRect.offsetMax = new Vector2(-5f, 0f);

        GameObject fill = CreateBox(fillArea.transform, "Fill", uiSprite, new Color(0.88f, 0.35f, 0.26f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRect = handleArea.GetComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10f, 0f);
        haRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = CreateBox(handleArea.transform, "Handle", uiSprite, new Color(0.98f, 0.96f, 0.92f, 1f));
        RectTransform hRect = handle.GetComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(28f, 34f);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = minVal;
        slider.maxValue = maxVal;
        slider.value = defaultVal;
        slider.fillRect = fillRect;
        slider.handleRect = hRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    static Toggle CreateToggle(Transform parent, string name, string label, Vector2 topLeftPos, Vector2 sizeDelta, Font font, Sprite uiSprite)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeftPos;
        rect.sizeDelta = sizeDelta;

        GameObject bg = CreateBox(obj.transform, "Background", uiSprite, new Color(0.16f, 0.20f, 0.28f, 1f));
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.pivot = new Vector2(0f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(40f, 40f);

        GameObject check = CreateBox(bg.transform, "Checkmark", uiSprite, new Color(0.88f, 0.35f, 0.26f, 1f));
        RectTransform chkRect = check.GetComponent<RectTransform>();
        chkRect.anchorMin = Vector2.zero;
        chkRect.anchorMax = Vector2.one;
        chkRect.offsetMin = new Vector2(7f, 7f);
        chkRect.offsetMax = new Vector2(-7f, -7f);

        Text lbl = CreateText(obj.transform, "Label", label, font, 24, FontStyle.Bold, new Color(0.90f, 0.92f, 0.96f, 1f), TextAnchor.MiddleLeft);
        lbl.rectTransform.offsetMin = new Vector2(58f, 0f);

        Toggle t = obj.GetComponent<Toggle>();
        t.targetGraphic = bg.GetComponent<Image>();
        t.graphic = check.GetComponent<Image>();
        t.isOn = true;
        return t;
    }

    static Sprite Texture2DToSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
