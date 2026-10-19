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
    private const string ParticleMaterialPath = MaterialsFolder + "/Particle.mat";
    private const string FrameMaterialPath = MaterialsFolder + "/ElementFrame.mat";
    private const string IconFolder = "Assets/UI/Icons";
    private const string VolumeProfilePath = "Assets/Settings/GameVolumeProfile.asset";
    private const string EffectsFolder = "Assets/Effects";

    private struct ElementEffects
    {
        public GameObject fire;
        public GameObject ice;
        public GameObject wind;
        public GameObject electric;
    }
    private const string PipelinePath = "Assets/Settings/URP-Mobile3D.asset";
    private const string RendererPath = "Assets/Settings/URP-Mobile3D_Renderer.asset";
    private const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

    private const float BlockSize = 3f;
    private const float BlockHeight = 0.5f;
    private static readonly Color BackgroundColor = new Color(0.10f, 0.12f, 0.20f);
    private static readonly Color AccentColor = new Color(1f, 0.78f, 0.25f);

    // UI style A
    private const string FontFolder = "Assets/Fonts/Baloo2";
    private const string FontPath = FontFolder + "/Baloo2-ExtraBold-Latin.ttf";
    private const string FontExtPath = FontFolder + "/Baloo2-ExtraBold-LatinExt.ttf";
    private const string FontAssetPath = FontFolder + "/Baloo2 SDF.asset";
    private const string FontExtAssetPath = FontFolder + "/Baloo2 Ext SDF.asset";
    private const string OutlineMaterialPath = FontFolder + "/Baloo2 SDF Outline.mat";
    private const string RoundedSpritePath = "Assets/UI/Rounded.png";
    private const string MusicIconPath = "Assets/UI/MusicNote.png";
    private const string CoinIconPath = "Assets/UI/Coin.png";
    private const string SkyArtFolder = "Assets/UI/Sky";
    private const string SkyDecorMaterialPath = MaterialsFolder + "/SkyDecor.mat";
    private const string SkyGlowMaterialPath = MaterialsFolder + "/SkyGlow.mat";
    private const float RoundedSpriteRadius = 64f;

    private static readonly Color Navy = new Color(0.137f, 0.224f, 0.357f);
    private static readonly Color MutedNavy = new Color(0.42f, 0.5f, 0.62f);
    private static readonly Color Yellow = new Color(1f, 0.824f, 0.302f);
    private static readonly Color YellowText = new Color(0.353f, 0.263f, 0f);
    private static readonly Color Coral = new Color(0.91f, 0.34f, 0.29f);
    private static readonly Color CoinText = new Color(0.78f, 0.5f, 0.05f);
    private static readonly Color ItemColor = new Color(0.95f, 0.96f, 0.98f);

    private static TMP_FontAsset uiFont;
    private static Material outlineMaterial;
    private static Sprite roundedSprite;

    private const string TestEarlyMenu = "Tower Stack/Test: Elements Early";

    // Editor-only switch: elements arrive on floors 2, 4, 6 and 8 so all four can be tried quickly
    [MenuItem(TestEarlyMenu)]
    private static void ToggleTestElementsEarly()
    {
        bool enabled = !EditorPrefs.GetBool(ElementRules.TestEarlyPrefKey, false);
        EditorPrefs.SetBool(ElementRules.TestEarlyPrefKey, enabled);
        Menu.SetChecked(TestEarlyMenu, enabled);
    }

    [MenuItem(TestEarlyMenu, true)]
    private static bool ToggleTestElementsEarlyValidate()
    {
        Menu.SetChecked(TestEarlyMenu, EditorPrefs.GetBool(ElementRules.TestEarlyPrefKey, false));
        return true;
    }

    // Editor-only test helpers for the shop; they work in and out of Play mode
    [MenuItem("Tower Stack/Test: Add 5000 Coins")]
    private static void AddTestCoins()
    {
        PlayerData.AddCoins(5000);
        Debug.Log("Tower Stack: 5000 coin eklendi. Toplam: " + PlayerData.Coins);
    }

    [MenuItem("Tower Stack/Test: Reset Save Data")]
    private static void ResetSaveData()
    {
        if (!EditorUtility.DisplayDialog("Tower Stack",
                "Coin, en yüksek skor ve satın alınan temalar silinsin mi?", "Evet, sıfırla", "Vazgeç"))
            return;
        PlayerData.ResetForTesting();
        Debug.Log("Tower Stack: kayıt sıfırlandı.");
    }

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
        // Emission stays black on plain blocks; element blocks set their own glow per block
        blockMaterial.EnableKeyword("_EMISSION");
        blockMaterial.SetColor("_EmissionColor", Color.black);
        blockMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        Material frameMaterial = CreateMaterial(FrameMaterialPath, "Sprites/Default");
        frameMaterial.SetColor("_Color", new Color(1.8f, 1.8f, 1.8f, 1f));   // glowing element frame
        EditorUtility.SetDirty(frameMaterial);
        Material backgroundMaterial = CreateMaterial(BackgroundMaterialPath, "Universal Render Pipeline/Unlit");
        // Unlit, vertex-coloured and transparent: the perfect ripple fades by its line colour
        Material rippleMaterial = CreateMaterial(RippleMaterialPath, "Sprites/Default");
        SoundSynth.Clips sounds = SoundSynth.GenerateAll();
        GameObject blockPrefab = CreateBlockPrefab(blockMaterial);

        // UI style A: Baloo 2 font, rounded pills, navy text on a pastel sky
        uiFont = SetupFont();
        outlineMaterial = CreateOutlineMaterial(uiFont);
        roundedSprite = GetOrCreateRoundedSprite();
        CreatePerfectPopupPrefab();
        ElementEffects effects = CreateElementEffects();

        SetupLighting();
        TowerController tower = CreateTower(blockPrefab);
        tower.rippleMaterial = rippleMaterial;
        tower.fireEffect = effects.fire;
        tower.iceEffect = effects.ice;
        tower.windEffect = effects.wind;
        tower.electricEffect = effects.electric;
        tower.fireIcon = GetOrCreateIcon("Fire", FlameShape);
        tower.iceIcon = GetOrCreateIcon("Ice", SnowflakeShape);
        tower.windIcon = GetOrCreateIcon("Wind", WindShape);
        tower.electricIcon = GetOrCreateIcon("Electric", BoltShape);
        tower.iconBadge = roundedSprite;
        tower.iconMaterial = rippleMaterial;
        tower.frameMaterial = frameMaterial;
        CreateBloomVolume();
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

        // "New element: Ice / slides slowly" bubble, shown the first time each element appears
        Image bannerPill = CreateRounded("ElementBanner", hud, Color.white, 48f);
        bannerPill.raycastTarget = false;
        AnchorTop(bannerPill.rectTransform, new Vector2(0f, -410f), new Vector2(780f, 170f));
        CanvasGroup bannerGroup = bannerPill.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;
        TextMeshProUGUI bannerLabel = CreatePillLabel(bannerPill, "New element: Ice\n<size=70%>Slides slowly</size>", 54, Navy, null);
        ElementBanner banner = bannerPill.gameObject.AddComponent<ElementBanner>();
        banner.canvasGroup = bannerGroup;
        banner.label = bannerLabel;
        tower.elementBanner = banner;

        // Start screen: tap anywhere to start. The panel itself is the (invisible) button.
        Image startPanel = CreateImage("StartPanel", canvas.transform, new Color(0f, 0f, 0f, 0f));
        Stretch(startPanel.rectTransform);
        Button startButton = startPanel.gameObject.AddComponent<Button>();
        startButton.targetGraphic = startPanel;
        startButton.transition = Selectable.Transition.None;
        RectTransform startContent = CreateSafeArea(startPanel.transform);

        Image bestPill = CreateRounded("BestPill", startContent, Color.white, 46f);
        bestPill.raycastTarget = false;
        AnchorTop(bestPill.rectTransform, new Vector2(0f, -70f), new Vector2(520f, 92f));   // wide enough for "En yüksek skor: 128"
        TextMeshProUGUI startBestText = CreatePillLabel(bestPill, "Best: 0", 44, Navy, null);

        // Total coins, under the best score
        Image coinPill = CreateRounded("CoinPill", startContent, Color.white, 40f);
        coinPill.raycastTarget = false;
        AnchorTop(coinPill.rectTransform, new Vector2(0f, -178f), new Vector2(300f, 80f));
        RectTransform startCoinRow = CreateCoinRow("Coins", coinPill.transform, GetOrCreateCoinIcon(), "0", 42, Navy, out TextMeshProUGUI startCoinsText);
        Stretch(startCoinRow);

        // Language switch (EN / TR); its own button, so tapping it never starts the game
        Image languagePill = CreateRounded("LanguageButton", startContent, Color.white, 46f);
        RectTransform languageRt = languagePill.rectTransform;
        languageRt.anchorMin = languageRt.anchorMax = languageRt.pivot = new Vector2(1f, 1f);
        languageRt.anchoredPosition = new Vector2(-48f, -70f);
        languageRt.sizeDelta = new Vector2(170f, 92f);
        languagePill.gameObject.AddComponent<Button>().targetGraphic = languagePill;
        languagePill.gameObject.AddComponent<LanguageButton>();
        CreatePillLabel(languagePill, "EN", 42, Navy, "language");

        // Music on/off, top-left: a note icon, crossed out while the music is off
        Image musicPill = CreateRounded("MusicButton", startContent, Color.white, 46f);
        RectTransform musicRt = musicPill.rectTransform;
        musicRt.anchorMin = musicRt.anchorMax = musicRt.pivot = new Vector2(0f, 1f);
        musicRt.anchoredPosition = new Vector2(48f, -70f);
        musicRt.sizeDelta = new Vector2(92f, 92f);
        musicPill.gameObject.AddComponent<Button>().targetGraphic = musicPill;
        Image noteIcon = CreateImage("Icon", musicPill.transform, Navy);
        noteIcon.sprite = GetOrCreateMusicIcon();
        noteIcon.raycastTarget = false;
        Place(noteIcon.rectTransform, Vector2.zero, new Vector2(58f, 58f));
        Image slash = CreateImage("MutedSlash", musicPill.transform, Coral);
        slash.raycastTarget = false;
        Place(slash.rectTransform, Vector2.zero, new Vector2(9f, 78f));
        slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        musicPill.gameObject.AddComponent<MusicButton>().mutedSlash = slash.gameObject;

        TextMeshProUGUI title = CreateText("Title", startContent, "Elemental\nStack", 180, Navy, TextAlignmentOptions.Center);
        title.lineSpacing = -45f;
        title.enableAutoSizing = true;
        title.fontSizeMax = 180f;
        title.fontSizeMin = 110f;
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

        // Shop button above the tap hint; it is its own button, so it never starts the game
        Image shopPill = CreateRounded("ShopButton", startContent, Color.white, 55f);
        RectTransform shopRt = shopPill.rectTransform;
        shopRt.anchorMin = shopRt.anchorMax = shopRt.pivot = new Vector2(0.5f, 0f);
        shopRt.anchoredPosition = new Vector2(0f, 360f);
        shopRt.sizeDelta = new Vector2(360f, 110f);
        Button shopButton = shopPill.gameObject.AddComponent<Button>();
        shopButton.targetGraphic = shopPill;
        CreatePillLabel(shopPill, "Shop", 50, Navy, "shop");

        ShopPanel shop = CreateShopPanel(canvas.transform);

        // Game over screen: a white card over a navy dim
        Image gameOverBg = CreateImage("GameOverPanel", canvas.transform, new Color(Navy.r, Navy.g, Navy.b, 0.55f));
        Stretch(gameOverBg.rectTransform);
        CanvasGroup gameOverGroup = gameOverBg.gameObject.AddComponent<CanvasGroup>();
        RectTransform gameOverContent = CreateSafeArea(gameOverBg.transform);
        Image card = CreateRounded("Card", gameOverContent, Color.white, 64f);
        Place(card.rectTransform, new Vector2(0f, 30f), new Vector2(840f, 1080f));

        TextMeshProUGUI gameOverText = CreateText("GameOverText", card.transform, "Game over", 90, Navy, TextAlignmentOptions.Center);
        gameOverText.gameObject.AddComponent<LocalizedText>().key = "game_over";
        Place(gameOverText.rectTransform, new Vector2(0f, 405f), new Vector2(760f, 120f));
        TextMeshProUGUI scoreLabel = CreateText("ScoreLabel", card.transform, "Score", 44, MutedNavy, TextAlignmentOptions.Center);
        scoreLabel.gameObject.AddComponent<LocalizedText>().key = "score_label";
        Place(scoreLabel.rectTransform, new Vector2(0f, 270f), new Vector2(760f, 70f));
        TextMeshProUGUI finalScoreText = CreateText("FinalScore", card.transform, "0", 200, Navy, TextAlignmentOptions.Center);
        Place(finalScoreText.rectTransform, new Vector2(0f, 125f), new Vector2(760f, 230f));

        Image newBestBadge = CreateRounded("NewBestBadge", card.transform, Coral, 40f);
        newBestBadge.raycastTarget = false;
        Place(newBestBadge.rectTransform, new Vector2(0f, -35f), new Vector2(380f, 80f));
        CreatePillLabel(newBestBadge, "New best!", 40, Color.white, "new_best");
        newBestBadge.gameObject.SetActive(false);

        TextMeshProUGUI finalBestText = CreateText("FinalBest", card.transform, "Best: 0", 48, MutedNavy, TextAlignmentOptions.Center);
        Place(finalBestText.rectTransform, new Vector2(0f, -135f), new Vector2(760f, 70f));

        // "+33" with a coin, counted up from zero when the card appears
        Sprite coinIcon = GetOrCreateCoinIcon();
        RectTransform coinsRow = CreateCoinRow("CoinsEarned", card.transform, coinIcon, "+0", 64, CoinText, out TextMeshProUGUI coinsEarnedText);
        Place(coinsRow, new Vector2(0f, -240f), new Vector2(600f, 90f));

        Image restartPill = CreateRounded("RestartButton", card.transform, Yellow, 75f);
        Place(restartPill.rectTransform, new Vector2(0f, -395f), new Vector2(620f, 150f));
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
        audio.coinSound = sounds.coin;
        audio.introJingle = sounds.introJingle;
        audio.musicLoop = sounds.music;

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
        gm.coinsEarnedText = coinsEarnedText;
        gm.startCoinsText = startCoinsText;
        gm.titleText = title;
        gm.fade = fade;
        gm.tower = tower;

        tower.scoreText = scoreText;

        // Studio intro goes last so it covers everything until it fades out
        CreateStudioIntro(canvas.transform);

        UnityEventTools.AddPersistentListener(startButton.onClick, new UnityAction(gm.StartGame));
        UnityEventTools.AddPersistentListener(restartButton.onClick, new UnityAction(gm.RestartGame));
        UnityEventTools.AddPersistentListener(shopButton.onClick, new UnityAction(shop.Open));

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
        camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;   // for the bloom
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

        // Living sky: clouds and birds, a setting sun, or stars and a ringed planet, by sky theme
        SkyDecor decor = camGo.AddComponent<SkyDecor>();
        decor.cloud = GetOrCreateSkySprite("Cloud", 256, 128, CloudPixel, 1);
        decor.softGlow = GetOrCreateSkySprite("SoftGlow", 128, 128, SoftGlowPixel, 1);
        decor.bird = GetOrCreateSkySprite("Bird", 64, 32, BirdPixel, 4);
        decor.planet = GetOrCreateSkySprite("Planet", 256, 256, PlanetPixel, 3);
        decor.disc = roundedSprite;
        decor.heightSource = rig.transform;
        decor.decorMaterial = CreateMaterial(SkyDecorMaterialPath, "Sprites/Default");
        decor.glowMaterial = CreateMaterial(SkyGlowMaterialPath, "Sprites/Default");
        decor.glowMaterial.mainTexture = decor.softGlow.texture;
        decor.glowMaterial.SetColor("_Color", new Color(1.6f, 1.6f, 1.6f, 1f));   // bright enough for bloom
        EditorUtility.SetDirty(decor.glowMaterial);

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
        tower.maxMoveSpeed = 6f;
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

    // ------------------------------------------------------- element effects

    // Particle trails for the sliding element blocks. They overwrite the old placeholder
    // prefabs in Assets/Effects (which had no material) so their file names and GUIDs stay.
    private static ElementEffects CreateElementEffects()
    {
        Material material = CreateMaterial(ParticleMaterialPath, "Sprites/Default");
        if (roundedSprite != null)
            material.mainTexture = roundedSprite.texture;   // soft round dots
        // Tint above 1 makes the particles bright enough to catch the bloom
        material.SetColor("_Color", new Color(2.2f, 2.2f, 2.2f, 1f));
        EditorUtility.SetDirty(material);
        Directory.CreateDirectory(EffectsFolder);

        return new ElementEffects
        {
            // Fire: warm embers rising off the block
            fire = SaveEffect("FireEffect", material, ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.3f), new Color(1f, 0.35f, 0.2f));
                var emission = ps.emission;
                emission.rateOverTime = 40f;
                SetVelocity(ps, new Vector2(-0.15f, 0.15f), new Vector2(0.6f, 1.4f), new Vector2(-0.15f, 0.15f));
            }),
            // Ice: small frosty crystals drifting down
            ice = SaveEffect("IceEffect", material, ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.88f, 0.98f, 1f), new Color(0.6f, 0.85f, 1f));
                var emission = ps.emission;
                emission.rateOverTime = 28f;
                SetVelocity(ps, new Vector2(-0.1f, 0.1f), new Vector2(-0.4f, -0.1f), new Vector2(-0.1f, 0.1f));
            }),
            // Wind: pale streaks blown sideways
            wind = SaveEffect("WindEffect", material, ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.12f);
                main.startColor = new Color(1f, 1f, 1f, 0.75f);
                var emission = ps.emission;
                emission.rateOverTime = 32f;
                SetVelocity(ps, new Vector2(-2.5f, 2.5f), new Vector2(0f, 0.3f), new Vector2(-2.5f, 2.5f));
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 3f;
                renderer.velocityScale = 0.15f;
            }),
            // Electric: quick bright sparks in bursts
            electric = SaveEffect("ElectricEffect", material, ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f), Color.white);
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 16, 0, 0.25f) });
                SetVelocity(ps, new Vector2(-3f, 3f), new Vector2(-3f, 3f), new Vector2(-3f, 3f));
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 2f;
                renderer.velocityScale = 0.05f;
            }),
        };
    }

    // Shared setup: emits from the block's volume, stays behind in world space,
    // shrinks and fades out over its lifetime
    private static GameObject SaveEffect(string name, Material material, System.Action<ParticleSystem> configure)
    {
        var go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startSpeed = 0f;
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;   // box follows the block, particle size does not

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = Vector3.one;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = fade;

        configure(ps);

        string path = EffectsFolder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Velocity ranges per axis in world space (all three axes must use the same curve mode)
    private static void SetVelocity(ParticleSystem ps, Vector2 x, Vector2 y, Vector2 z)
    {
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(x.x, x.y);
        velocity.y = new ParticleSystem.MinMaxCurve(y.x, y.y);
        velocity.z = new ParticleSystem.MinMaxCurve(z.x, z.y);
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

    // White eighth-note icon (head, stem and flag), drawn with 4x4 supersampling for smooth edges
    // ------------------------------------------------------------ sky art

    // Draws a sprite pixel by pixel. `pixel` returns a colour with alpha for a point given in
    // pixels (y up); `samples` x samples points per pixel are averaged for smooth edges.
    private static Sprite GetOrCreateSkySprite(string name, int width, int height, System.Func<Vector2, Color> pixel, int samples)
    {
        string path = SkyArtFolder + "/" + name + ".png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color colorSum = Color.clear;
                float alphaSum = 0f;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        Color c = pixel(new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples));
                        colorSum += c * c.a;
                        alphaSum += c.a;
                    }
                }
                Color result = alphaSum > 0f ? colorSum / alphaSum : Color.clear;
                result.a = alphaSum / (samples * samples);
                pixels[y * width + x] = result;
            }
        }
        texture.SetPixels32(pixels);
        Directory.CreateDirectory(SkyArtFolder);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = true;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Fluffy cloud: overlapping puffs with soft edges, a flatter base and a slightly grey underside
    private static Color CloudPixel(Vector2 p)
    {
        Vector3[] puffs =
        {
            new Vector3(60f, 52f, 34f), new Vector3(100f, 72f, 44f), new Vector3(150f, 70f, 48f),
            new Vector3(196f, 54f, 34f), new Vector3(128f, 46f, 40f),
        };
        float inside = -999f;
        foreach (Vector3 puff in puffs)
            inside = Mathf.Max(inside, puff.z - Vector2.Distance(p, new Vector2(puff.x, puff.y)));
        float alpha = Mathf.Clamp01(inside / 10f) * Mathf.Clamp01((p.y - 16f) / 12f);
        float shade = Mathf.Lerp(0.86f, 1f, Mathf.Clamp01((p.y - 20f) / 80f));
        return new Color(shade, shade, Mathf.Min(1f, shade + 0.03f), alpha);
    }

    // Round glow fading from the centre; used for the sun, nebulae, stars and motes
    private static Color SoftGlowPixel(Vector2 p)
    {
        float r = Vector2.Distance(p, new Vector2(64f, 64f)) / 64f;
        float alpha = Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
        return new Color(1f, 1f, 1f, alpha);
    }

    // Simple bird: two curved wings meeting in the middle
    private static Color BirdPixel(Vector2 p)
    {
        Vector2[] left = Bezier(new Vector2(4f, 20f), new Vector2(18f, 30f), new Vector2(32f, 13f));
        Vector2[] right = Bezier(new Vector2(32f, 13f), new Vector2(46f, 30f), new Vector2(60f, 20f));
        bool on = OnPolyline(p, left, 2.6f) || OnPolyline(p, right, 2.6f);
        return new Color(1f, 1f, 1f, on ? 1f : 0f);
    }

    private static Vector2[] Bezier(Vector2 a, Vector2 b, Vector2 c)
    {
        var points = new Vector2[13];
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (points.Length - 1f);
            points[i] = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;
        }
        return points;
    }

    // Ringed planet: a lit, softly banded purple globe with a tilted ring passing behind and in front
    private static Color PlanetPixel(Vector2 p)
    {
        Vector2 c = new Vector2(128f, 128f);
        const float radius = 62f;
        Vector2 d = p - c;

        // Ring in tilted coordinates
        float angle = -18f * Mathf.Deg2Rad;
        float rx = d.x * Mathf.Cos(angle) - d.y * Mathf.Sin(angle);
        float ry = d.x * Mathf.Sin(angle) + d.y * Mathf.Cos(angle);
        bool inRing = (rx * rx) / (118f * 118f) + (ry * ry) / (33f * 33f) <= 1f
                      && (rx * rx) / (84f * 84f) + (ry * ry) / (23.5f * 23.5f) >= 1f;
        float band = 0.85f + 0.15f * Mathf.Sin(Mathf.Sqrt(rx * rx / 118f / 118f + ry * ry / 33f / 33f) * 40f);
        Color ring = new Color(0.92f * band, 0.82f * band, 1f * band, 0.85f);

        float dist = d.magnitude;
        if (dist <= radius)
        {
            if (inRing && ry < 0f)
                return ring;   // front half of the ring covers the planet

            float nx = d.x / radius, ny = d.y / radius;
            float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
            Vector3 light = new Vector3(-0.5f, 0.6f, 0.62f).normalized;
            float lit = Mathf.Clamp(Vector3.Dot(new Vector3(nx, ny, nz), light), 0.18f, 1f);
            float stripes = 1f + 0.07f * Mathf.Sin(ny * 18f);
            Color baseColor = new Color(0.56f, 0.42f, 0.92f) * stripes;
            Color color = baseColor * (0.35f + 0.75f * lit);
            color.a = 1f;
            return color;
        }
        return inRing ? ring : Color.clear;
    }

    // ------------------------------------------------------------------ shop

    // Shop overlay: a white card with the coin total, a grid of block themes, a grid of sky themes
    // and a Close button. Items are spawned at runtime from SkinCatalog using the hidden template.
    private static ShopPanel CreateShopPanel(Transform canvas)
    {
        Image background = CreateImage("ShopPanel", canvas, new Color(Navy.r, Navy.g, Navy.b, 0.6f));
        Stretch(background.rectTransform);
        RectTransform content = CreateSafeArea(background.transform);
        Image card = CreateRounded("Card", content, Color.white, 64f);
        Place(card.rectTransform, Vector2.zero, new Vector2(900f, 1560f));

        TextMeshProUGUI title = CreateText("Title", card.transform, "Shop", 84, Navy, TextAlignmentOptions.Center);
        title.gameObject.AddComponent<LocalizedText>().key = "shop";
        Place(title.rectTransform, new Vector2(0f, 690f), new Vector2(800f, 110f));

        Sprite coinIcon = GetOrCreateCoinIcon();
        Image coinPill = CreateRounded("CoinPill", card.transform, ItemColor, 40f);
        coinPill.raycastTarget = false;
        Place(coinPill.rectTransform, new Vector2(0f, 590f), new Vector2(300f, 80f));
        RectTransform coinRow = CreateCoinRow("Coins", coinPill.transform, coinIcon, "0", 42, Navy, out TextMeshProUGUI coinsText);
        Stretch(coinRow);

        TextMeshProUGUI blocksLabel = CreateText("BlocksLabel", card.transform, "Blocks", 48, MutedNavy, TextAlignmentOptions.Left);
        blocksLabel.gameObject.AddComponent<LocalizedText>().key = "shop_blocks";
        Place(blocksLabel.rectTransform, new Vector2(0f, 490f), new Vector2(800f, 60f));
        RectTransform blockGrid = CreateItemGrid("BlockGrid", card.transform);
        Place(blockGrid, new Vector2(0f, 190f), new Vector2(800f, 540f));

        TextMeshProUGUI skyLabel = CreateText("SkyLabel", card.transform, "Sky", 48, MutedNavy, TextAlignmentOptions.Left);
        skyLabel.gameObject.AddComponent<LocalizedText>().key = "shop_sky";
        Place(skyLabel.rectTransform, new Vector2(0f, -115f), new Vector2(800f, 60f));
        RectTransform skyGrid = CreateItemGrid("SkyGrid", card.transform);
        Place(skyGrid, new Vector2(0f, -285f), new Vector2(800f, 260f));

        TextMeshProUGUI message = CreateText("Message", card.transform, "Not enough coins", 44, Coral, TextAlignmentOptions.Center);
        Place(message.rectTransform, new Vector2(0f, -460f), new Vector2(800f, 60f));

        Image closePill = CreateRounded("CloseButton", card.transform, Yellow, 60f);
        Place(closePill.rectTransform, new Vector2(0f, -640f), new Vector2(420f, 120f));
        Button closeButton = closePill.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closePill;
        CreatePillLabel(closePill, "Close", 54, YellowText, "close");

        ShopItemView template = CreateShopItemTemplate(card.transform, coinIcon);

        ShopPanel shop = background.gameObject.AddComponent<ShopPanel>();
        shop.blockGrid = blockGrid;
        shop.skyGrid = skyGrid;
        shop.itemTemplate = template;
        shop.coinsText = coinsText;
        shop.messageText = message;
        UnityEventTools.AddPersistentListener(closeButton.onClick, new UnityAction(shop.Close));

        background.gameObject.SetActive(false);
        return shop;
    }

    private static RectTransform CreateItemGrid(string name, Transform parent)
    {
        RectTransform grid = CreateUI(name, parent);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(250f, 260f);
        layout.spacing = new Vector2(25f, 20f);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        return grid;
    }

    // One theme card: a small three-stripe preview, the name, and either a price or a status
    private static ShopItemView CreateShopItemTemplate(Transform parent, Sprite coinIcon)
    {
        Image item = CreateRounded("ItemTemplate", parent, ItemColor, 32f);
        Place(item.rectTransform, Vector2.zero, new Vector2(250f, 260f));
        Button button = item.gameObject.AddComponent<Button>();
        button.targetGraphic = item;

        var swatches = new Image[3];
        for (int i = 0; i < swatches.Length; i++)
        {
            swatches[i] = CreateRounded("Swatch" + (i + 1), item.transform, Color.white, 8f);
            swatches[i].raycastTarget = false;
            Place(swatches[i].rectTransform, new Vector2(0f, 78f - i * 32f), new Vector2(150f, 28f));
        }

        TextMeshProUGUI nameText = CreateText("Name", item.transform, "Classic", 38, Navy, TextAlignmentOptions.Center);
        nameText.enableAutoSizing = true;
        nameText.fontSizeMax = 38f;
        nameText.fontSizeMin = 24f;
        Place(nameText.rectTransform, new Vector2(0f, -45f), new Vector2(230f, 50f));

        RectTransform priceRow = CreateCoinRow("Price", item.transform, coinIcon, "300", 36, CoinText, out TextMeshProUGUI priceText);
        Place(priceRow, new Vector2(0f, -100f), new Vector2(230f, 50f));

        TextMeshProUGUI status = CreateText("Status", item.transform, "Selected", 34, MutedNavy, TextAlignmentOptions.Center);
        Place(status.rectTransform, new Vector2(0f, -100f), new Vector2(230f, 50f));

        ShopItemView view = item.gameObject.AddComponent<ShopItemView>();
        view.background = item;
        view.swatches = swatches;
        view.nameText = nameText;
        view.priceRow = priceRow.gameObject;
        view.priceText = priceText;
        view.statusText = status;
        view.button = button;
        return view;
    }

    // A coin icon followed by a number, centred as a group whatever the number's width
    private static RectTransform CreateCoinRow(string name, Transform parent, Sprite icon, string text, float size, Color color, out TextMeshProUGUI label)
    {
        RectTransform row = CreateUI(name, parent);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = size * 0.25f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image coin = CreateImage("Icon", row, Color.white);
        coin.sprite = icon;
        coin.preserveAspect = true;
        coin.raycastTarget = false;
        var iconSize = coin.gameObject.AddComponent<LayoutElement>();
        iconSize.preferredWidth = size * 1.1f;
        iconSize.preferredHeight = size * 1.1f;

        label = CreateText("Amount", row, text, size, color, TextAlignmentOptions.Left);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return row;
    }

    // Gold coin: darker rim, lighter face and an embossed slot in the middle (full colour, not tinted)
    private static Sprite GetOrCreateCoinIcon()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
        if (existing != null)
            return existing;

        const int size = 128;
        const int samples = 4;
        Color rim = new Color(0.86f, 0.55f, 0.08f);
        Color face = new Color(1f, 0.78f, 0.2f);
        Color shine = new Color(1f, 0.9f, 0.5f);
        Vector2 c = new Vector2(64f, 64f);

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color sum = Color.clear;
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        Vector2 p = new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                        float d = Vector2.Distance(p, c);
                        if (d > 60f) continue;
                        Color col = d > 49f ? rim : face;
                        bool slot = Mathf.Abs(p.x - 64f) <= 7f && Mathf.Abs(p.y - 64f) <= 24f;
                        if (slot) col = rim;
                        else if (d <= 49f && p.y - p.x > 30f && d > 36f) col = shine;   // highlight on the upper left
                        sum += col;
                        hits++;
                    }
                }
                // Average only the covered samples so the edge keeps its colour and just fades out
                Color pixel = hits > 0 ? sum / hits : Color.clear;
                pixel.a = hits / (float)(samples * samples);
                pixels[y * size + x] = pixel;
            }
        }
        texture.SetPixels32(pixels);
        Directory.CreateDirectory(Path.GetDirectoryName(CoinIconPath));
        File.WriteAllBytes(CoinIconPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(CoinIconPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(CoinIconPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
    }

    private static Sprite GetOrCreateMusicIcon()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(MusicIconPath);
        if (existing != null)
            return existing;

        const int size = 128;
        const int samples = 4;
        // Flag: a quadratic curve leaving the top of the stem
        Vector2 p0 = new Vector2(70f, 112f), p1 = new Vector2(104f, 100f), p2 = new Vector2(98f, 64f);
        var flag = new Vector2[17];
        for (int i = 0; i < flag.Length; i++)
        {
            float t = i / (flag.Length - 1f);
            flag[i] = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
        }

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float cos = Mathf.Cos(-20f * Mathf.Deg2Rad), sin = Mathf.Sin(-20f * Mathf.Deg2Rad);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        float px = x + (sx + 0.5f) / samples;
                        float py = y + (sy + 0.5f) / samples;

                        // Tilted oval note head
                        float dx = px - 48f, dy = py - 34f;
                        float rx = dx * cos - dy * sin, ry = dx * sin + dy * cos;
                        bool head = (rx * rx) / (25f * 25f) + (ry * ry) / (17f * 17f) <= 1f;
                        bool stem = px >= 64f && px <= 73f && py >= 34f && py <= 114f;
                        bool flagHit = false;
                        for (int i = 0; i < flag.Length - 1 && !flagHit; i++)
                        {
                            float thickness = Mathf.Lerp(8f, 3f, i / (flag.Length - 2f));
                            flagHit = DistanceToSegment(new Vector2(px, py), flag[i], flag[i + 1]) <= thickness;
                        }
                        if (head || stem || flagHit) hits++;
                    }
                }
                byte alpha = (byte)(255 * hits / (samples * samples));
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        texture.SetPixels32(pixels);
        Directory.CreateDirectory(Path.GetDirectoryName(MusicIconPath));
        File.WriteAllBytes(MusicIconPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(MusicIconPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(MusicIconPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(MusicIconPath);
    }

    // ------------------------------------------------------ element icons

    // White 128 px element symbol, drawn from a shape test with 4x4 supersampling.
    // Coordinates are in pixels with y pointing up.
    private static Sprite GetOrCreateIcon(string name, System.Func<Vector2, bool> inside)
    {
        string path = IconFolder + "/" + name + ".png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        const int size = 128;
        const int samples = 4;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                    for (int sx = 0; sx < samples; sx++)
                        if (inside(new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples)))
                            hits++;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (samples * samples)));
            }
        }
        texture.SetPixels32(pixels);
        Directory.CreateDirectory(IconFolder);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = true;   // stays smooth when shown small in the world
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Flame: a teardrop leaning slightly right, with a hollow inner flame
    private static bool FlameShape(Vector2 p)
    {
        return Teardrop(p, 64f, 44f, 32f, 74f) && !Teardrop(p, 64f, 38f, 13f, 32f);
    }

    private static bool Teardrop(Vector2 p, float cx, float cy, float radius, float height)
    {
        if (Vector2.Distance(p, new Vector2(cx, cy)) <= radius) return true;
        if (p.y < cy || p.y > cy + height) return false;
        float t = (p.y - cy) / height;
        float center = cx + radius * 0.3f * t * t;
        float halfWidth = radius * Mathf.Pow(1f - t, 1.3f);
        return Mathf.Abs(p.x - center) <= halfWidth;
    }

    // Snowflake: six arms with a pair of small branches each
    private static bool SnowflakeShape(Vector2 p)
    {
        Vector2 c = new Vector2(64f, 64f);
        if (Vector2.Distance(p, c) <= 9f) return true;
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (DistanceToSegment(p, c, c + dir * 54f) <= 5f) return true;
            Vector2 branchRoot = c + dir * 34f;
            for (int side = -1; side <= 1; side += 2)
            {
                float branchAngle = angle + side * 45f * Mathf.Deg2Rad;
                Vector2 branchDir = new Vector2(Mathf.Cos(branchAngle), Mathf.Sin(branchAngle));
                if (DistanceToSegment(p, branchRoot, branchRoot + branchDir * 15f) <= 4.5f) return true;
            }
        }
        return false;
    }

    // Wind: three strokes, two of them curling at the end
    private static bool WindShape(Vector2 p)
    {
        return OnPolyline(p, Curl(new Vector2(12f, 86f), new Vector2(82f, 86f), new Vector2(82f, 98f), 12f, -90f, 180f), 5f)
            || OnPolyline(p, new[] { new Vector2(22f, 62f), new Vector2(108f, 62f) }, 5f)
            || OnPolyline(p, Curl(new Vector2(12f, 38f), new Vector2(76f, 38f), new Vector2(76f, 26f), 12f, 90f, -180f), 5f);
    }

    // A straight line from `start` to `end`, then an arc around `center` from `fromDeg` to `toDeg`
    private static Vector2[] Curl(Vector2 start, Vector2 end, Vector2 center, float radius, float fromDeg, float toDeg)
    {
        const int steps = 12;
        var points = new Vector2[steps + 2];
        points[0] = start;
        points[1] = end;
        for (int i = 1; i <= steps; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
            points[i + 1] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        return points;
    }

    // Lightning bolt polygon
    private static bool BoltShape(Vector2 p)
    {
        Vector2[] bolt =
        {
            new Vector2(76f, 124f), new Vector2(30f, 58f), new Vector2(58f, 58f), new Vector2(44f, 4f),
            new Vector2(98f, 74f), new Vector2(68f, 74f), new Vector2(88f, 124f),
        };
        return InsidePolygon(p, bolt);
    }

    private static bool OnPolyline(Vector2 p, Vector2[] points, float halfThickness)
    {
        for (int i = 0; i < points.Length - 1; i++)
            if (DistanceToSegment(p, points[i], points[i + 1]) <= halfThickness)
                return true;
        return false;
    }

    private static bool InsidePolygon(Vector2 p, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    // ------------------------------------------------------------- bloom

    // Soft glow for bright things only (element blocks, frames, particles). The threshold sits
    // above sun-lit white blocks so the plain tower does not glow.
    private static void CreateBloomVolume()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }
        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
            bloom.name = "Bloom";
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.active = true;
        bloom.threshold.Override(1.5f);
        bloom.intensity.Override(1f);
        bloom.scatter.Override(0.6f);
        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
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
        PlayerSettings.productName = "Elemental Stack";

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
            // Baloo 2 file is already ExtraBold; synthetic bold on top would look smeared
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
