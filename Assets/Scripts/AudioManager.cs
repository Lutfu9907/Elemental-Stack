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

    // Perfect serisinde çalınan notalar: majör gamın basamakları (yarım ses cinsinden)
    private static readonly int[] PerfectScale = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };

    private AudioSource audioSource;
    private AudioSource perfectSource;  // ton değiştirmek diğer sesleri etkilemesin diye ayrı kaynak

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

    public void PlayButton()
    {
        PlaySound(buttonClickSound);
    }
}
