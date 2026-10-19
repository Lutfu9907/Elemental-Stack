using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sounds")]
    public AudioClip placeBlockSound;   // blok yerleştiğinde
    public AudioClip sliceSound;        // kesildiğinde
    public AudioClip perfectSound;      // perfect'te; seri uzadıkça tonu yükselir
    public AudioClip gameOverSound;     // game over
    public AudioClip buttonClickSound;  // buton sesi
    public AudioClip coinSound;         // coin kazanınca

    [Header("Music")]
    public AudioClip introJingle;       // LD Games intro'sunun altındaki kısa melodi
    public AudioClip musicLoop;         // menüde ve oyunda dönen arka plan müziği
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    public float musicFadeSeconds = 2f;

    // Perfect serisinde çalınan notalar: majör gamın basamakları (yarım ses cinsinden)
    private static readonly int[] PerfectScale = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };
    private const string MusicPrefKey = "Music";

    private AudioSource audioSource;
    private AudioSource perfectSource;  // ton değiştirmek diğer sesleri etkilemesin diye ayrı kaynak
    private AudioSource musicSource;
    private AudioSource jingleSource;
    private bool musicStarted;

    // Müzik ve intro melodisi açık mı (kalıcı ayar); efekt sesleri bundan etkilenmez
    public bool MusicEnabled
    {
        get => PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(MusicPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            if (musicSource != null)
                musicSource.mute = !value;
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        perfectSource = gameObject.AddComponent<AudioSource>();
        perfectSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.mute = !MusicEnabled;
    }

    void Update()
    {
        // Müzik başladıktan sonra yavaşça hedef sese yükselir; oyun duraklasa da çalışsın diye ölçeksiz zaman
        if (!musicStarted) return;
        float step = musicVolume / Mathf.Max(0.01f, musicFadeSeconds) * Time.unscaledDeltaTime;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicVolume, step);
    }

    // Melodiyi ses saatinde tam verilen ana zamanlar (AudioSettings.dspTime); intro animasyonu da bu saati izler
    public void PlayIntroJingle(double dspTime)
    {
        if (introJingle == null || !MusicEnabled) return;

        if (jingleSource == null)
        {
            jingleSource = gameObject.AddComponent<AudioSource>();
            jingleSource.playOnAwake = false;
        }
        jingleSource.clip = introJingle;
        jingleSource.PlayScheduled(dspTime);
    }

    // Intro bitince bir kez çağrılır; sahne yeniden yüklense de müzik kesilmeden devam eder
    public void StartMusic()
    {
        if (musicStarted || musicLoop == null) return;
        musicStarted = true;
        musicSource.clip = musicLoop;
        musicSource.Play();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        audioSource.PlayOneShot(clip);
    }

    // combo 1 → temel nota, her yeni perfect gamda bir basamak yukarı
    public void PlayPerfect(int combo)
    {
        if (perfectSound == null || perfectSource == null) return;

        int step = PerfectScale[Mathf.Clamp(combo - 1, 0, PerfectScale.Length - 1)];
        perfectSource.pitch = Mathf.Pow(2f, step / 12f);
        perfectSource.PlayOneShot(perfectSound);
    }

    public void PlayCoin()
    {
        PlaySound(coinSound);
    }

    public void PlayButton()
    {
        PlaySound(buttonClickSound);
    }
}
