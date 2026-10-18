using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Builds the complete game scene (camera, tower, managers and UI) and wires every
/// reference, so the project can be set up without hand-placing objects.
/// Menu: Tower Stack > Build Game Scene
/// </summary>
public static class TowerStackSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string BlockPrefabPath = "Assets/Prefabs/Block.prefab";
    private const string ResourcesFolder = "Assets/Resources";
    private const string MaterialsFolder = "Assets/Materials";
    private const string BlockMaterialPath = MaterialsFolder + "/Block.mat";
    private const string BackgroundMaterialPath = MaterialsFolder + "/Background.mat";
    private const string PipelinePath = "Assets/Settings/URP-Mobile3D.asset";
    private const string RendererPath = "Assets/Settings/URP-Mobile3D_Renderer.asset";
    private const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

    private const float BlockSize = 3f;
    private const float BlockHeight = 0.5f;
    private static readonly Color BackgroundColor = new Color(0.10f, 0.12f, 0.20f);
    private static readonly Color AccentColor = new Color(1f, 0.78f, 0.25f);

    [MenuItem("Tower Stack/Build Game Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Tower Stack",
                "Assets/Scenes/Game.unity zaten var. Yeniden oluşturulsun mu? Sahnede elle yaptığın değişiklikler silinir.",
                "Evet, yeniden kur", "Vazgeç"))
            return;

        ApplyMobilePlayerSettings();
        SetupRenderPipeline();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material blockMaterial = CreateMaterial(BlockMaterialPath, "Universal Render Pipeline/Lit");
        blockMaterial.SetFloat("_Smoothness", 0.15f);
        Material backgroundMaterial = CreateMaterial(BackgroundMaterialPath, "Universal Render Pipeline/Unlit");
        GameObject blockPrefab = CreateBlockPrefab(blockMaterial);

        CreatePopupPrefab("PerfectPopup", "PERFECT!", typeof(PerfectPopup), 64, AccentColor);
        CreatePopupPrefab("ComboPopup", "Combo x2", typeof(ComboPopup), 48, Color.white);

        SetupLighting();
        TowerController tower = CreateTower(blockPrefab);
        CreateCamera(backgroundMaterial, tower);

        // ---------- UI ----------
        Canvas canvas = CreateCanvas();
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // In-game HUD. Panel backgrounds fill the whole screen; their content sits in a
        // SafeArea child so notches and rounded corners never cover it.
        RectTransform gameUI = CreateUI("GameUIPanel", canvas.transform);
        Stretch(gameUI);
        // Invisible full-screen tap target behind the HUD. Buttons added above it receive
        // their own taps, so pressing UI never drops a block.
        Image tapArea = CreateImage("TapArea", gameUI, new Color(0f, 0f, 0f, 0f));
        Stretch(tapArea.rectTransform);
        tapArea.gameObject.AddComponent<TapInputArea>().tower = tower;
        RectTransform hud = CreateSafeArea(gameUI);
        TextMeshProUGUI scoreText = CreateText("ScoreText", hud, "SCORE: 0", 64, Color.white, TextAlignmentOptions.Left);
        AnchorTopLeft(scoreText.rectTransform, new Vector2(48f, -48f), new Vector2(700f, 90f));
        scoreText.gameObject.AddComponent<ScorePunchEffect>();
        TextMeshProUGUI highScoreText = CreateText("HighScoreText", hud, "BEST: 0", 40, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Left);
        AnchorTopLeft(highScoreText.rectTransform, new Vector2(48f, -138f), new Vector2(700f, 60f));

        // Start screen: tap anywhere to start
        Image startPanel = CreateImage("StartPanel", canvas.transform, new Color(0f, 0f, 0f, 0.35f));
        Stretch(startPanel.rectTransform);
        Button startButton = startPanel.gameObject.AddComponent<Button>();
        startButton.targetGraphic = startPanel;
        startButton.transition = Selectable.Transition.None;
        RectTransform startContent = CreateSafeArea(startPanel.transform);
        TextMeshProUGUI title = CreateText("Title", startContent, "TOWER\nSTACK", 150, Color.white, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0f, 380f), new Vector2(1000f, 400f));
        TextMeshProUGUI tapHint = CreateText("TapHint", startContent, "TAP TO START", 52, AccentColor, TextAlignmentOptions.Center);
        Place(tapHint.rectTransform, new Vector2(0f, -320f), new Vector2(900f, 100f));

        // Game over screen
        Image gameOverBg = CreateImage("GameOverPanel", canvas.transform, new Color(0f, 0f, 0f, 0.6f));
        Stretch(gameOverBg.rectTransform);
        CanvasGroup gameOverGroup = gameOverBg.gameObject.AddComponent<CanvasGroup>();
        RectTransform gameOverContent = CreateSafeArea(gameOverBg.transform);
        TextMeshProUGUI gameOverText = CreateText("GameOverText", gameOverContent, "GAME OVER", 120, Color.white, TextAlignmentOptions.Center);
        Place(gameOverText.rectTransform, new Vector2(0f, 220f), new Vector2(1000f, 180f));
        Button restartButton = CreateButton("RestartButton", gameOverContent, "RESTART", new Vector2(560f, 160f), AccentColor, BackgroundColor);
        Place((RectTransform)restartButton.transform, new Vector2(0f, -160f), new Vector2(560f, 160f));
        GameOverPanelController gameOverPanel = gameOverBg.gameObject.AddComponent<GameOverPanelController>();
        gameOverPanel.canvasGroup = gameOverGroup;
        gameOverPanel.restartButton = (RectTransform)restartButton.transform;
        gameOverPanel.gameOverText = gameOverText.rectTransform;

        // Combo feedback
        TextMeshProUGUI comboText = CreateText("ComboPopupText", canvas.transform, "", 96, AccentColor, TextAlignmentOptions.Center);
        Place(comboText.rectTransform, new Vector2(0f, 520f), new Vector2(1000f, 150f));
        comboText.alpha = 0f;
        Image flashImage = CreateImage("FlashImage", canvas.transform, new Color(1f, 1f, 1f, 0f));
        Stretch(flashImage.rectTransform);
        flashImage.raycastTarget = false;

        // Screen fade (restart)
        Image fadeImage = CreateImage("FadeImage", canvas.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(fadeImage.rectTransform);
        fadeImage.raycastTarget = false;
        FadeController fade = fadeImage.gameObject.AddComponent<FadeController>();
        fade.fadeImage = fadeImage;
        gameOverPanel.fadeImage = fadeImage;

        // ---------- Managers ----------
        var audioGo = new GameObject("AudioManager");
        AudioSource audioSource = audioGo.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioGo.AddComponent<AudioManager>();

        var comboGo = new GameObject("ComboEffectManager");
        ComboEffectManager combo = comboGo.AddComponent<ComboEffectManager>();
        combo.flashImage = flashImage;
        combo.comboPopupText = comboText;

        var gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.startPanel = startPanel.gameObject;
        gm.gameUIPanel = gameUI.gameObject;
        gm.gameOverPanel = gameOverPanel;
        gm.scoreText = scoreText;
        gm.highScoreText = highScoreText;
        gm.fade = fade;
        gm.tower = tower;

        tower.scoreText = scoreText;

        // Studio intro goes last so it covers everything until it fades out
        CreateStudioIntro(canvas.transform);

        UnityEventTools.AddPersistentListener(startButton.onClick, new UnityAction(gm.StartGame));
        UnityEventTools.AddPersistentListener(restartButton.onClick, new UnityAction(gm.RestartGame));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AddSceneToBuildSettings(ScenePath);

        Debug.Log("Tower Stack: oyun sahnesi kuruldu → " + ScenePath + ". Play'e basarak deneyebilirsin.");
    }

    // ------------------------------------------------------------------ world

    private static void CreateCamera(Material backgroundMaterial, TowerController tower)
    {
        // The rig follows the tower; the camera inside it shakes. Keeping them on
        // separate objects stops the shake from snapping the camera back down.
        var rig = new GameObject("CameraRig");
        CameraFollow follow = rig.AddComponent<CameraFollow>();
        follow.offset = new Vector3(0f, 1.5f, 0f);
        follow.smoothSpeed = 3f;
        rig.transform.position = follow.offset;

        // Angled orthographic view: looking down 30° from a 45° corner shows the top
        // and two sides of every block, like a classic isometric stacker.
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig.transform, false);
        Quaternion viewRotation = Quaternion.Euler(30f, 45f, 0f);
        camGo.transform.localRotation = viewRotation;
        camGo.transform.localPosition = viewRotation * new Vector3(0f, 0f, -25f);

        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BackgroundColor;
        camGo.AddComponent<UniversalAdditionalCameraData>();
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraShake>();
        // Widens the view on tall phones so the sliding block stays on screen
        camGo.AddComponent<CameraFitWidth>().minOrthographicSize = cam.orthographicSize;

        // Gradient backdrop: a quad behind everything, coloured at runtime
        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        Object.DestroyImmediate(background.GetComponent<Collider>());
        background.transform.SetParent(camGo.transform, false);
        MeshRenderer bgRenderer = background.GetComponent<MeshRenderer>();
        bgRenderer.sharedMaterial = backgroundMaterial;
        bgRenderer.shadowCastingMode = ShadowCastingMode.Off;
        bgRenderer.receiveShadows = false;

        BackgroundGradient gradient = camGo.AddComponent<BackgroundGradient>();
        gradient.backgroundRenderer = bgRenderer;
        gradient.tower = tower;
    }

    private static void SetupLighting()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.68f);
        RenderSettings.fog = false;

        // Sun from the front-left so the top and the two visible sides get three distinct shades
        var lightGo = new GameObject("Directional Light");
        lightGo.transform.rotation = Quaternion.Euler(50f, 20f, 0f);
        Light light = lightGo.AddComponent<Light>();
        light.type = UnityEngine.LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.5f;
        lightGo.AddComponent<UniversalAdditionalLightData>();
    }

    private static TowerController CreateTower(GameObject blockPrefab)
    {
        var towerGo = new GameObject("Tower");
        TowerController tower = towerGo.AddComponent<TowerController>();
        tower.blockPrefab = blockPrefab;
        tower.blockHeight = BlockHeight;
        tower.moveSpeed = 3f;
        tower.speedIncreasePerScore = 0.05f;
        tower.maxMoveSpeed = 7f;
        tower.moveRange = 2.6f;

        // Tall pedestal the first block lands on; its top surface is at y = 0
        const float baseHeight = 6f;
        var baseBlock = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, towerGo.transform);
        baseBlock.name = "BaseBlock";
        baseBlock.transform.position = new Vector3(0f, -baseHeight * 0.5f, 0f);
        baseBlock.transform.localScale = new Vector3(BlockSize, baseHeight, BlockSize);
        Block block = baseBlock.GetComponent<Block>();
        if (block != null)
            block.elementType = Block.BlockElementType.None;

        return tower;
    }

    private static GameObject CreateBlockPrefab(Material material)
    {
        // Overwrites the old 2D sprite prefab in place, so its GUID stays the same.
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Block";
        root.transform.localScale = new Vector3(BlockSize, BlockHeight, BlockSize);
        MeshRenderer renderer = root.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        Block block = root.AddComponent<Block>();
        block.rend = renderer;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BlockPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Material CreateMaterial(string path, string shaderName)
    {
        Directory.CreateDirectory(MaterialsFolder);
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = Shader.Find(shaderName);
        }
        material.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SetupRenderPipeline()
    {
        // The project started on the 2D renderer, which cannot light or shadow 3D meshes.
        // Create a mobile-friendly Universal Renderer pipeline once and make it the default.
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(PostProcessDataPath);
            AssetDatabase.CreateAsset(rendererData, RendererPath);

            pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 50f;
            pipeline.shadowCascadeCount = 1;
            pipeline.mainLightShadowmapResolution = 2048;
            AssetDatabase.CreateAsset(pipeline, PipelinePath);

            // Soft shadows have no public setter
            var so = new SerializedObject(pipeline);
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = null; // quality levels fall back to the default above
        AssetDatabase.SaveAssets();
    }

    private static void CreatePopupPrefab(string name, string text, System.Type component, float fontSize, Color color)
    {
        Directory.CreateDirectory(ResourcesFolder);
        string path = ResourcesFolder + "/" + name + ".prefab";

        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        root.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)root.transform;
        rt.sizeDelta = new Vector2(600f, 120f);
        TextMeshProUGUI label = CreateText("Text", rt, text, fontSize, color, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        root.AddComponent(component);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void AddSceneToBuildSettings(string path)
    {
        // Game is the only scene the build needs; the old SampleScene is left out.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
    }

    private static void ApplyMobilePlayerSettings()
    {
        // Portrait only: the tower is built for a tall phone screen.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // Our own LD Games intro replaces the "Made with Unity" splash (allowed on Unity 6 Personal)
        PlayerSettings.SplashScreen.show = false;
    }

    private static void CreateStudioIntro(Transform parent)
    {
        Image background = CreateImage("StudioIntro", parent, new Color(0.04f, 0.05f, 0.09f));
        Stretch(background.rectTransform);
        CanvasGroup group = background.gameObject.AddComponent<CanvasGroup>();

        RectTransform logo = CreateUI("Logo", background.transform);
        Place(logo, new Vector2(0f, 60f), new Vector2(900f, 800f));

        // A tiny three-block tower that drops in above the name, in element colours
        var blockSpecs = new[]
        {
            (width: 210f, x: 0f, color: new Color(1f, 0.42f, 0.33f)),
            (width: 180f, x: 14f, color: new Color(0.36f, 0.72f, 0.98f)),
            (width: 150f, x: -10f, color: new Color(1f, 0.82f, 0.3f)),
        };
        var blocks = new RectTransform[blockSpecs.Length];
        for (int i = 0; i < blockSpecs.Length; i++)
        {
            Image block = CreateImage("Block" + (i + 1), logo, blockSpecs[i].color);
            block.raycastTarget = false;
            Place(block.rectTransform, new Vector2(blockSpecs[i].x, 200f + i * 54f), new Vector2(blockSpecs[i].width, 48f));
            blocks[i] = block.rectTransform;
        }

        TextMeshProUGUI ld = CreateText("LDText", logo, "L<color=#FFD14D>D</color>", 230, Color.white, TextAlignmentOptions.Center);
        Place(ld.rectTransform, new Vector2(0f, 20f), new Vector2(800f, 260f));

        Image line = CreateImage("AccentLine", logo, AccentColor);
        line.raycastTarget = false;
        Place(line.rectTransform, new Vector2(0f, -110f), new Vector2(440f, 6f));

        TextMeshProUGUI games = CreateText("GamesText", logo, "GAMES", 64, new Color(0.85f, 0.88f, 0.95f), TextAlignmentOptions.Center);
        Place(games.rectTransform, new Vector2(0f, -180f), new Vector2(900f, 100f));
        games.characterSpacing = 28f;

        StudioIntro intro = background.gameObject.AddComponent<StudioIntro>();
        intro.canvasGroup = group;
        intro.logoGroup = logo;
        intro.blocks = blocks;
        intro.ldText = ld;
        intro.accentLine = line.rectTransform;
        intro.gamesText = games;
    }

    // --------------------------------------------------------------------- UI

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static RectTransform CreateSafeArea(Transform parent)
    {
        RectTransform rt = CreateUI("SafeArea", parent);
        Stretch(rt);
        rt.gameObject.AddComponent<SafeArea>();
        return rt;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rt = CreateUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rt = CreateUI(name, parent);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 size, Color background, Color textColor)
    {
        Image img = CreateImage(name, parent, background);
        img.rectTransform.sizeDelta = size;
        Button button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        TextMeshProUGUI text = CreateText("Label", img.transform, label, 60, textColor, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }

    private static void AnchorTopLeft(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }
}
