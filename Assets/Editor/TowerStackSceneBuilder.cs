using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
    private const string SquareSpritePath = "Assets/Art/Square.png";

    private const float BlockWidth = 3f;
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

        GameObject blockPrefab = PrepareBlockPrefab();
        if (blockPrefab == null)
        {
            Debug.LogError("Tower Stack: " + BlockPrefabPath + " bulunamadı.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreatePopupPrefab("PerfectPopup", "PERFECT!", typeof(PerfectPopup), 64, AccentColor);
        CreatePopupPrefab("ComboPopup", "Combo x2", typeof(ComboPopup), 48, Color.white);

        CreateCamera();
        CreateGlobalLight();
        TowerController tower = CreateTower(blockPrefab);

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

        UnityEventTools.AddPersistentListener(startButton.onClick, new UnityAction(gm.StartGame));
        UnityEventTools.AddPersistentListener(restartButton.onClick, new UnityAction(gm.RestartGame));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        Debug.Log("Tower Stack: oyun sahnesi kuruldu → " + ScenePath + ". Play'e basarak deneyebilirsin.");
    }

    // ------------------------------------------------------------------ world

    private static void CreateCamera()
    {
        // The rig follows the tower; the camera inside it shakes. Keeping them on
        // separate objects stops the shake from snapping the camera back down.
        var rig = new GameObject("CameraRig");
        rig.transform.position = new Vector3(0f, 2f, -10f);
        CameraFollow follow = rig.AddComponent<CameraFollow>();
        follow.offset = new Vector3(0f, 2f, 0f);
        follow.smoothSpeed = 3f;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig.transform, false);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BackgroundColor;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraShake>();
        // Widens the view on tall phones so the sliding block stays on screen
        camGo.AddComponent<CameraFitWidth>().minOrthographicSize = cam.orthographicSize;
    }

    private static void CreateGlobalLight()
    {
        var lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static TowerController CreateTower(GameObject blockPrefab)
    {
        var towerGo = new GameObject("Tower");
        TowerController tower = towerGo.AddComponent<TowerController>();
        tower.blockPrefab = blockPrefab;
        tower.blockHeight = 1f;
        tower.moveSpeed = 2f;
        tower.speedIncreasePerScore = 0.1f;
        tower.moveRange = 2.5f;
        tower.spawnPoint = towerGo.transform;

        // Base platform the first block lands on
        var baseBlock = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, towerGo.transform);
        baseBlock.name = "BaseBlock";
        baseBlock.transform.position = new Vector3(0f, -1f, 0f);
        baseBlock.transform.localScale = new Vector3(BlockWidth, 1f, 1f);
        Block block = baseBlock.GetComponent<Block>();
        if (block != null)
            block.elementType = Block.BlockElementType.None;

        return tower;
    }

    private static GameObject PrepareBlockPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BlockPrefabPath) == null)
            return null;

        GameObject root = PrefabUtility.LoadPrefabContents(BlockPrefabPath);
        // Older versions resized the prefab asset itself while playing, leaving it very thin.
        root.transform.localScale = new Vector3(BlockWidth, 1f, 1f);

        SpriteRenderer sr = root.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
            sr.sprite = GetOrCreateSquareSprite();

        PrefabUtility.SaveAsPrefabAsset(root, BlockPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(BlockPrefabPath);
    }

    private static Sprite GetOrCreateSquareSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(Path.GetDirectoryName(SquareSpritePath));
        var tex = new Texture2D(32, 32);
        var pixels = new Color32[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        File.WriteAllBytes(SquareSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(SquareSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
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
