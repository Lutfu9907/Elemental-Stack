using System.Collections.Generic;
using UnityEngine;

// Seçili gökyüzü temasına göre arka planı canlandırır:
//   Gündüz    : iki katman halinde süzülen bulutlar, ara sıra geçen kuş sürüsü
//   Gün batımı: ufukta parlayan güneş, ince bulut şeritleri, havada süzülen ışık zerreleri
//   Uzay      : göz kırpan yıldızlar, kayan yıldızlar, halkalı gezegen, renkli nebula
// Hepsi kameranın çocuğudur (kule ile arka plan düzlemi arasında). Kule yükseldikçe
// katmanlar farklı hızda aşağı kayar; uzaktakiler yavaş, yakındakiler hızlı (paralaks).
[RequireComponent(typeof(Camera))]
public class SkyDecor : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite cloud;
    public Sprite softGlow;
    public Sprite bird;
    public Sprite planet;
    public Sprite disc;

    [Header("Materials")]
    [Tooltip("Normal saydam materyal")]
    public Material decorMaterial;
    [Tooltip("Parlak (bloom) materyal; doku olarak yumuşak ışıltı kullanır")]
    public Material glowMaterial;

    [Tooltip("Yüksekliği okunacak obje (kamera rig'i); boşsa kameranın ebeveyni")]
    public Transform heightSource;

    private class Item
    {
        public Transform transform;
        public Vector2 start;        // görüş alanına göre başlangıç (-1..1)
        public float depth;          // kameradan uzaklık
        public float parallax;       // kule her yükseldiğinde ne kadar aşağı kayar
        public float drift;          // yatay süzülme hızı (dünya birimi/sn)
        public bool wrap = true;     // ekrandan çıkınca öbür taraftan geri gelir
        public float spawnTime;      // eklendiği an (süzülme buradan itibaren hesaplanır)
    }

    private Camera cam;
    private Transform root;
    private readonly List<Item> items = new List<Item>();
    private float startHeight;
    private string builtFor;
    private float time;

    // Temaya özel zamanlayıcılar
    private float nextBirds;
    private float nextShootingStar;
    private ParticleSystem stars;
    private ParticleSystem shootingStars;
    private ParticleSystem motes;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (heightSource == null)
            heightSource = transform.parent != null ? transform.parent : transform;
        startHeight = heightSource.position.y;
    }

    void OnEnable()
    {
        SkinCatalog.SkinChanged += Rebuild;
    }

    void OnDisable()
    {
        SkinCatalog.SkinChanged -= Rebuild;
    }

    void Start()
    {
        Rebuild();
    }

    void Update()
    {
        time += Time.unscaledDeltaTime;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float height = heightSource.position.y - startHeight;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item item = items[i];
            if (item.transform == null)
            {
                items.RemoveAt(i);
                continue;
            }

            float x = item.start.x * halfW + item.drift * (time - item.spawnTime);
            float y = item.start.y * halfH - height * item.parallax;
            if (item.wrap)
            {
                float marginX = halfW * 0.5f;
                float marginY = halfH * 0.4f;
                x = Wrap(x, -halfW - marginX, halfW + marginX);
                y = Wrap(y, -halfH - marginY, halfH + marginY);
            }
            item.transform.localPosition = new Vector3(x, y, item.depth);
            item.transform.localRotation = Quaternion.identity;
        }

        switch (builtFor)
        {
            case "day": UpdateBirds(halfW, halfH); break;
            case "space": UpdateShootingStars(halfW, halfH); break;
        }

        FitParticleArea(stars, halfW, halfH);
        FitParticleArea(motes, halfW, halfH);
    }

    // Tema değişince eski dekoru silip yenisini kurar
    private void Rebuild()
    {
        string sky = SkinCatalog.CurrentSky.id;
        if (sky == builtFor && root != null) return;

        if (root != null)
            Destroy(root.gameObject);
        items.Clear();
        stars = shootingStars = motes = null;

        root = new GameObject("SkyDecor").transform;
        root.SetParent(transform, false);
        builtFor = sky;

        var rng = new System.Random(sky.GetHashCode());
        switch (sky)
        {
            case "sunset": BuildSunset(rng); break;
            case "space": BuildSpace(rng); break;
            default: BuildDay(rng); break;
        }
    }

    // ------------------------------------------------------------------ day

    private void BuildDay(System.Random rng)
    {
        // Uzak katman: küçük, soluk, yavaş
        for (int i = 0; i < 5; i++)
            AddCloud(rng, 130f, Range(rng, 1.8f, 2.6f), 0.55f, 0.15f, Range(rng, 0.08f, 0.14f), Color.white);
        // Yakın katman: büyük, belirgin, daha hızlı
        for (int i = 0; i < 4; i++)
            AddCloud(rng, 110f, Range(rng, 3f, 4.2f), 0.85f, 0.35f, Range(rng, 0.18f, 0.3f), Color.white);

        nextBirds = time + 3f;
    }

    private void UpdateBirds(float halfW, float halfH)
    {
        if (time < nextBirds || bird == null) return;
        nextBirds = time + Random.Range(12f, 20f);

        // 3-5 kuş, V düzeninde, soldan sağa geçer
        var flock = new GameObject("Birds").transform;
        flock.SetParent(root, false);
        int count = Random.Range(3, 6);
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer b = AddSprite(flock, bird, decorMaterial, new Color(0.15f, 0.22f, 0.35f, 0.7f));
            int row = (i + 1) / 2;
            float side = i % 2 == 0 ? 1f : -1f;
            b.transform.localPosition = new Vector3(-row * 0.45f, side * row * 0.25f, 0f);
            b.transform.localScale = Vector3.one * 0.45f;
            b.gameObject.AddComponent<WingFlap>().phase = i * 0.7f;
        }

        items.Add(new Item
        {
            transform = flock,
            start = new Vector2(-1.4f, Random.Range(0.1f, 0.7f)),
            depth = 100f,
            parallax = 0.3f,
            drift = Random.Range(1f, 1.5f),
            wrap = false,
            spawnTime = time,
        });
        Destroy(flock.gameObject, 2f * halfW / 1f + 6f);
    }

    // --------------------------------------------------------------- sunset

    private void BuildSunset(System.Random rng)
    {
        // Güneş: geniş sıcak ışıltı + parlak çekirdek; kule yükseldikçe ufka doğru alçalır
        var sun = new GameObject("Sun").transform;
        sun.SetParent(root, false);
        SpriteRenderer glow = AddSprite(sun, softGlow, glowMaterial, new Color(1f, 0.6f, 0.35f, 0.9f));
        glow.transform.localScale = Vector3.one * 9f;
        SpriteRenderer core = AddSprite(sun, disc, decorMaterial, new Color(1f, 0.88f, 0.62f));
        core.transform.localScale = Vector3.one * SpriteScale(disc, 2.6f);
        items.Add(new Item { transform = sun, start = new Vector2(-0.35f, 0.15f), depth = 140f, parallax = 0.05f, wrap = false });

        // İnce, uzun bulut şeritleri
        for (int i = 0; i < 6; i++)
        {
            Item streak = AddCloud(rng, i < 3 ? 135f : 115f, 1f, i < 3 ? 0.45f : 0.7f, i < 3 ? 0.12f : 0.3f,
                Range(rng, 0.1f, 0.25f), new Color(1f, 0.72f, 0.68f));
            streak.transform.localScale = new Vector3(Range(rng, 3.5f, 5f), Range(rng, 0.45f, 0.7f), 1f);
        }

        // Yavaşça yükselen sıcak ışık zerreleri
        motes = CreateParticles("Motes", 105f, 45, glowMaterial);
        var main = motes.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.5f, 0.8f), new Color(1f, 0.6f, 0.4f, 0.6f));
        var emission = motes.emission;
        emission.rateOverTime = 8f;
        SetVelocity(motes, new Vector2(-0.1f, 0.1f), new Vector2(0.15f, 0.35f));
        motes.Play();
    }

    // ---------------------------------------------------------------- space

    private void BuildSpace(System.Random rng)
    {
        // Renkli nebula bulutları (çok uzak, neredeyse sabit)
        Color[] nebula =
        {
            new Color(0.55f, 0.2f, 0.85f, 0.32f),
            new Color(0.2f, 0.35f, 0.95f, 0.28f),
            new Color(0.85f, 0.25f, 0.6f, 0.22f),
        };
        foreach (Color tint in nebula)
        {
            var go = new GameObject("Nebula").transform;
            go.SetParent(root, false);
            SpriteRenderer sr = AddSprite(go, softGlow, decorMaterial, tint);
            sr.transform.localScale = Vector3.one * Range(rng, 9f, 13f);
            items.Add(new Item
            {
                transform = go,
                start = new Vector2(Range(rng, -0.8f, 0.8f), Range(rng, -0.6f, 0.9f)),
                depth = 145f, parallax = 0.03f, drift = 0f,
            });
        }

        // Halkalı gezegen
        if (planet != null)
        {
            var p = new GameObject("Planet").transform;
            p.SetParent(root, false);
            SpriteRenderer sr = AddSprite(p, planet, decorMaterial, Color.white);
            sr.transform.localScale = Vector3.one * SpriteScale(planet, 3.2f);
            items.Add(new Item { transform = p, start = new Vector2(0.55f, 0.55f), depth = 138f, parallax = 0.06f, wrap = false });
        }

        // Göz kırpan yıldızlar (parlak materyal; bloom ile ışıldar)
        stars = CreateParticles("Stars", 142f, 180, glowMaterial);
        var main = stars.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.13f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.7f, 0.8f, 1f));
        main.prewarm = true;
        var emission = stars.emission;
        emission.rateOverTime = 35f;
        var twinkle = new Gradient();
        twinkle.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.4f, 0.45f),
                    new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        var color = stars.colorOverLifetime;
        color.enabled = true;
        color.color = twinkle;
        stars.Play();

        // Kayan yıldızlar: zamanı gelince elle fırlatılır
        shootingStars = CreateParticles("ShootingStars", 125f, 6, glowMaterial);
        var shootMain = shootingStars.main;
        shootMain.startLifetime = 0.8f;
        shootMain.startSize = 0.09f;
        var shootEmission = shootingStars.emission;
        shootEmission.rateOverTime = 0f;
        var shootRenderer = shootingStars.GetComponent<ParticleSystemRenderer>();
        shootRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        shootRenderer.lengthScale = 1f;
        shootRenderer.velocityScale = 0.12f;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        var shootColor = shootingStars.colorOverLifetime;
        shootColor.enabled = true;
        shootColor.color = fade;
        shootingStars.Play();
        nextShootingStar = time + 2f;
    }

    private void UpdateShootingStars(float halfW, float halfH)
    {
        if (shootingStars == null || time < nextShootingStar) return;
        nextShootingStar = time + Random.Range(3f, 8f);

        var emit = new ParticleSystem.EmitParams
        {
            position = new Vector3(Random.Range(0f, halfW), Random.Range(halfH * 0.2f, halfH), 0f),
            velocity = new Vector3(-Random.Range(8f, 11f), -Random.Range(3.5f, 5f), 0f),
            applyShapeToPosition = false,
        };
        shootingStars.Emit(emit, 1);
    }

    // -------------------------------------------------------------- helpers

    private Item AddCloud(System.Random rng, float depth, float scale, float alpha, float parallax, float drift, Color tint)
    {
        var go = new GameObject("Cloud").transform;
        go.SetParent(root, false);
        tint.a = alpha;
        SpriteRenderer sr = AddSprite(go, cloud, decorMaterial, tint);
        sr.flipX = rng.NextDouble() < 0.5;
        go.localScale = Vector3.one * scale;
        var item = new Item
        {
            transform = go,
            start = new Vector2(Range(rng, -1f, 1f), Range(rng, -1f, 1f)),
            depth = depth,
            parallax = parallax,
            drift = drift,
            spawnTime = time,
        };
        items.Add(item);
        return item;
    }

    private SpriteRenderer AddSprite(Transform parent, Sprite sprite, Material material, Color color)
    {
        var go = new GameObject(sprite != null ? sprite.name : "Sprite");
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = material;
        sr.color = color;
        return sr;
    }

    // Kameraya bağlı, görüş alanını dolduran bir parçacık sistemi
    private ParticleSystem CreateParticles(string name, float depth, int maxParticles, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 0f, depth);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.useUnscaledTime = true;   // oyun duraklamışken (başlangıç ekranı) de canlı kalsın

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 10f, 0.1f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private static void SetVelocity(ParticleSystem ps, Vector2 x, Vector2 y)
    {
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(x.x, x.y);
        velocity.y = new ParticleSystem.MinMaxCurve(y.x, y.y);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    // Parçacık alanı görüş alanını (biraz taşarak) kaplasın; ekran oranı değişirse de
    private static void FitParticleArea(ParticleSystem ps, float halfW, float halfH)
    {
        if (ps == null) return;
        var shape = ps.shape;
        shape.scale = new Vector3(halfW * 2.4f, halfH * 2.4f, 0.1f);
    }

    // Sprite'ı istenen dünya genişliğine getiren ölçek
    private static float SpriteScale(Sprite sprite, float worldWidth)
    {
        return sprite != null && sprite.bounds.size.x > 0f ? worldWidth / sprite.bounds.size.x : 1f;
    }

    private static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static float Wrap(float value, float min, float max)
    {
        float range = max - min;
        return min + Mathf.Repeat(value - min, range);
    }
}

