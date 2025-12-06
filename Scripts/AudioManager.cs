using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sounds")]
    public AudioClip placeBlockSound;   // blok yerleþtiðinde
    public AudioClip sliceSound;        // kesildiðinde
    public AudioClip gameOverSound;     // game over
    public AudioClip buttonClickSound;  // buton sesi

    private AudioSource audioSource;

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
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}
