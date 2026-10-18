using System.Collections.Generic;
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
using UnityEngine.TextCore.LowLevel;
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
    private const string RippleMaterialPath = MaterialsFolder + "/Ripple.mat";
    private const string PipelinePath = "Assets/Settings/URP-Mobile3D.asset";
    private const string RendererPath = "Assets/Settings/URP-Mobile3D_Renderer.asset";
    private const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

    private const float BlockSize = 3f;
    private const float BlockHeight = 0.5f;
    private static readonly Color BackgroundColor = new Color(0.10f, 0.12f, 0.20f);
    private static readonly Color AccentColor = new Color(1f, 0.78f, 0.25f);

    // UI style A
    private const string FontFolder = "Assets/Fonts/Fredoka";
    private const string FontPath = FontFolder + "/Fredoka-SemiBold-Latin.ttf";
    private const string FontExtPath = FontFolder + "/Fredoka-SemiBold-LatinExt.ttf";
    private const string FontAssetPath = FontFolder + "/Fredoka SDF.asset";
    private const string FontExtAssetPath = FontFolder + "/Fredoka Ext SDF.asset";
    private const string OutlineMaterialPath = FontFolder + "/Fredoka SDF Outline.mat";
    private const string RoundedSpritePath = "Assets/UI/Rounded.png";
    private const float RoundedSpriteRadius = 64f;

    private static readonly Color Navy = new Color(0.137f, 0.224f, 0.357f);
    private static readonly Color MutedNavy = new Color(0.42f, 0.5f, 0.62f);
    private static readonly Color Yellow = new Color(1f, 0.824f, 0.302f);
    private static readonly Color YellowText = new Color(0.353f, 0.263f, 0f);
    private static readonly Color Coral = new Color(0.91f, 0.34f, 0.29f);

    private static TMP_FontAsset uiFont;
    private static Material outlineMaterial;
    private static Sprite roundedSprite;

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
        // Unlit, vertex-coloured and transparent: the perfect ripple fades by its line colour
        Material rippleMaterial = CreateMaterial(RippleMaterialPath, "Sprites/Default");
        SoundSynth.Clips sounds = SoundSynth.GenerateAll();
        GameObject blockPrefab = CreateBlockPrefab(blockMaterial);

        // UI style A: Fredoka font, rounded pills, navy text on a pastel sky
        uiFont = SetupFont();
        outlineMaterial = CreateOutlineMaterial(uiFont);
        roundedSprite = GetOrCreateRoundedSprite();
        CreatePerfectPopupPrefab();

        SetupLighting();
        TowerController tower = CreateTower(blockPrefab);
        tower.rippleMaterial = rippleMaterial;
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
        // Big centred score in white with a navy outline so it reads on every sky colour
        TextMeshProUGUI scoreText = CreateText("ScoreText", hud, "0", 170, Color.white, TextAlignmentOptions.Center);
        UseOutline(scoreText);
        AnchorTop(scoreText.rectTransform, new Vector2(0f, -100f), new Vector2(800f, 210f));
        scoreText.gameObject.AddComponent<ScorePunchEffect>();
        TextMeshProUGUI highScoreText = CreateText("HighScoreText", hud, "Best: 0", 46, Color.white, TextAlignmentOptions.Center);
        UseOutline(highScoreText);
        AnchorTop(highScoreText.rectTransform, new Vector2(0f, -310f), new Vector2(800f, 70f));

        // Start screen: tap anywhere to start. The panel itself is the (invisible) button.
        Image startPanel = CreateImage("StartPanel", canvas.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(startPanel.rectTransform);
        Button startButton = startPanel.gameObject.AddComponent<Button>();
        startButton.targetGraphic = startPanel;
        startButton.transition = Selectable.Transition.None;
        RectTransform startContent = CreateSafeArea(startPanel.transform);

        Image bestPill = CreateRounded("BestPill", startContent, Color.white, 46f);
        bestPill.raycastTarget = false;
        AnchorTop(bestPill.rectTransform, new Vector2(0f, -70f), new Vector2(400f, 92f));
        TextMeshProUGUI startBestText = CreatePillLabel(bestPill, "Best: 0", 44, Navy, null);

        // Language switch (EN / TR); its own button, so tapping it never starts the game
        Image languagePill = CreateRounded("LanguageButton", startContent, Color.white, 46f);
        RectTransform languageRt = languagePill.rectTransform;
        languageRt.anchorMin = languageRt.anchorMax = languageRt.pivot = new Vector2(1f, 1f);
        languageRt.anchoredPosition = new Vector2(-48f, -70f);
        languageRt.sizeDelta = new Vector2(170f, 92f);
        languagePill.gameObject.AddComponent<Button>().targetGraphic = languagePill;
        languagePill.gameObject.AddComponent<LanguageButton>();
        CreatePillLabel(languagePill, "EN", 42, Navy, "language");

        TextMeshProUGUI title = CreateText("Title", startContent, "Tower\nStack", 200, Navy, TextAlignmentOptions.Center);
        title.lineSpacing = -25f;
        title.gameObject.AddComponent<LocalizedText>().key = "title";
        Place(title.rectTransform, new Vector2(0f, 500f), new Vector2(1000f, 480f));

        // Tap hint sits at the bottom, away from the tower, and gently pulses
        Image tapHint = CreateRounded("TapHint", startContent, Yellow, 75f);
        tapHint.raycastTarget = false;
        RectTransform tapRt = tapHint.rectTransform;
        tapRt.anchorMin = tapRt.anchorMax = tapRt.pivot = new Vector2(0.5f, 0f);
        tapRt.anchoredPosition = new Vector2(0f, 180f);
        tapRt.sizeDelta = new Vector2(660f, 150f);
        CreatePillLabel(tapHint, "Tap to start", 58, YellowText, "tap_to_start");
        tapHint.gameObject.AddComponent<PulseScale>();

        // Game over screen: a white card over a navy dim
        Image gameOverBg = CreateImage("GameOverPanel", canvas.transform, new Color(Navy.r, Navy.g, Navy.b, 0.55f));
        Stretch(gameOverBg.rectTransform);
        CanvasGroup gameOverGroup = gameOverBg.gameObject.AddComponent<CanvasGroup>();
        RectTransform gameOverContent = CreateSafeArea(gameOverBg.transform);
        Image card = CreateRounded("Card", gameOverContent, Color.white, 64f);
        Place(card.rectTransform, new Vector2(0f, 40f), new Vector2(840f, 1000f));

        TextMeshProUGUI gameOverText = CreateText("GameOverText", card.transform, "Game over", 90, Navy, TextAlignmentOptions.Center);
        gameOverText.gameObject.AddComponent<LocalizedText>().key = "game_over";
        Place(gameOverText.rectTransform, new Vector2(0f, 380f), new Vector2(760f, 120f));
        TextMeshProUGUI scoreLabel = CreateText("ScoreLabel", card.transform, "Score", 44, MutedNavy, TextAlignmentOptions.Center);
        scoreLabel.gameObject.AddComponent<LocalizedText>().key = "score_label";
        Place(scoreLabel.rectTransform, new Vector2(0f, 245f), new Vector2(760f, 70f));
        TextMeshProUGUI finalScoreText = CreateText("FinalScore", card.transform, "0", 200, Navy, TextAlignmentOptions.Center);
        Place(finalScoreText.rectTransform, new Vector2(0f, 100f), new Vector2(760f, 230f));

        Image newBestBadge = CreateRounded("NewBestBadge", card.transform, Coral, 40f);
        newBestBadge.raycastTarget = false;
        Place(newBestBadge.rectTransform, new Vector2(0f, -60f), new Vector2(380f, 80f));
        CreatePillLabel(newBestBadge, "New best!", 40, Color.white, "new_best");
        newBestBadge.gameObject.SetActive(false);

        TextMeshProUGUI finalBestText = CreateText("FinalBest", card.transform, "Best: 0", 48, MutedNavy, TextAlignmentOptions.Center);
        Place(finalBestText.rectTransform, new Vector2(0f, -165f), new Vector2(760f, 70f));

        Image restartPill = CreateRounded("RestartButton", card.transform, Yellow, 75f);
        Place(restartPill.rectTransform, new Vector2(0f, -350f), new Vector2(620f, 150f));
        Button restartButton = restartPill.gameObject.AddComponent<Button>();
        restartButton.targetGraphic = restartPill;
        CreatePillLabel(restartPill, "Play again", 60, YellowText, "play_again");

        GameOverPanelController gameOverPanel = gameOverBg.gameObject.AddComponent<GameOverPanelController>();
        gameOverPanel.canvasGroup = gameOverGroup;
        gameOverPanel.restartButton = (RectTransform)restartButton.transform;
        gameOverPanel.gameOverText = gameOverText.rectTransform;

        // Combo feedback ("Great!", "Amazing!"...)
        TextMeshProUGUI comboText = CreateText("ComboPopupText", canvas.transform, "", 96, Yellow, TextAlignmentOptions.Center);
        UseOutline(comboText);
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
        AudioManager audio = audioGo.AddComponent<AudioManager>();
        audio.placeBlockSound = sounds.place;
        audio.sliceSound = sounds.slice;
        audio.perfectSound = sounds.perfect;
        audio.gameOverSound = sounds.gameOver;
        audio.buttonClickSound = sounds.button;

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
        gm.startBestText = startBestText;
        gm.finalScoreText = finalScoreText;
        gm.finalBestText = finalBestText;
        gm.newBestBadge = newBestBadge.gameObject;
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

    // "Perfect! x3" bubble: a white pill that grows to fit its text
    private static void CreatePerfectPopupPrefab()
    {
        Directory.CreateDirectory(ResourcesFolder);

        var root = new GameObject("PerfectPopup", typeof(RectTransform), typeof(CanvasGroup));
        root.layer = LayerMask.NameToLayer("UI");
        Image background = root.AddComponent<Image>();
        MakeRounded(background, Color.white, 52f);
        background.raycastTarget = false;

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 12, 12);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var fitter = root.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI label = CreateText("Text", root.transform, "Perfect!", 60, Coral, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        root.AddComponent<PerfectPopup>();

        PrefabUtility.SaveAsPrefabAsset(root, ResourcesFolder + "/PerfectPopup.prefab");
        Object.DestroyImmediate(root);
    }

    // ------------------------------------------------------------- UI style

    private static TMP_FontAsset SetupFont()
    {
        TMP_FontAsset main = LoadOrCreateFontAsset(FontPath, FontAssetPath);
        TMP_FontAsset extended = LoadOrCreateFontAsset(FontExtPath, FontExtAssetPath);
        if (main == null) return null;

        // Turkish letters (ş, ğ, ı, İ) live in the Latin Extended file
        if (extended != null)
        {
            if (main.fallbackFontAssetTable == null)
                main.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!main.fallbackFontAssetTable.Contains(extended))
                main.fallbackFontAssetTable.Add(extended);
            EditorUtility.SetDirty(main);
        }
        return main;
    }

    private static TMP_FontAsset LoadOrCreateFontAsset(string fontPath, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            Debug.LogError("Tower Stack: font bulunamadı → " + fontPath);
            return null;
        }

        // Dynamic atlas: glyphs are added on demand, so any language's letters work
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);
        asset.atlasTextures[0].name = asset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static Material CreateOutlineMaterial(TMP_FontAsset font)
    {
        if (font == null) return null;

        var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (material == null)
        {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);
        }
        else
        {
            material.CopyPropertiesFromMaterial(font.material);
        }
        material.SetColor("_OutlineColor", new Color(Navy.r, Navy.g, Navy.b, 0.85f));
        material.SetFloat("_OutlineWidth", 0.18f);
        material.SetFloat("_FaceDilate", 0.18f);
        material.EnableKeyword("OUTLINE_ON");
        EditorUtility.SetDirty(material);
        return material;
    }

    // White circle sprite, nine-sliced into pills and rounded cards of any size
    private static Sprite GetOrCreateRoundedSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        if (existing != null)
            return existing;

        const int size = (int)(RoundedSpriteRadius * 2f);
        float radius = RoundedSpriteRadius;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        Directory.CreateDirectory(Path.GetDirectoryName(RoundedSpritePath));
        File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(RoundedSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(radius - 1f, radius - 1f, radius - 1f, radius - 1f);
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
    }

    private static Image CreateRounded(string name, Transform parent, Color color, float cornerRadius)
    {
        Image image = CreateImage(name, parent, color);
        MakeRounded(image, color, cornerRadius);
        return image;
    }

    private static void MakeRounded(Image image, Color color, float cornerRadius)
    {
        image.color = color;
        if (roundedSprite == null) return;
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        // The sprite's corners are RoundedSpriteRadius px; scale them to the wanted radius
        image.pixelsPerUnitMultiplier = RoundedSpriteRadius / cornerRadius;
    }

    // Centred label that fills a pill and shrinks to fit longer translations
    private static TextMeshProUGUI CreatePillLabel(Image pill, string text, float size, Color color, string localizationKey)
    {
        TextMeshProUGUI label = CreateText("Label", pill.transform, text, size, color, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.margin = new Vector4(24f, 0f, 24f, 0f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMax = size;
        label.fontSizeMin = size * 0.6f;
        if (!string.IsNullOrEmpty(localizationKey))
            label.gameObject.AddComponent<LocalizedText>().key = localizationKey;
        return label;
    }

    private static void UseOutline(TextMeshProUGUI text)
    {
        if (outlineMaterial != null)
            text.fontSharedMaterial = outlineMaterial;
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
        tmp.raycastTarget = false;
        if (uiFont != null)
        {
            // Fredoka file is already SemiBold; synthetic bold on top would look smeared
            tmp.font = uiFont;
            tmp.fontStyle = FontStyles.Normal;
        }
        else
        {
            tmp.fontStyle = FontStyles.Bold;
        }
        return tmp;
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

    private static void AnchorTop(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }
}
