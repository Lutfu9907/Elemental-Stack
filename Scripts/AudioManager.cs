using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sounds")]
    public AudioClip placeBlockSound;   // blok yerleştiğinde
    public AudioClip sliceSound;        // kesildiğinde
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
        Debug.Log("AudioManager awoke: " + gameObject.name);
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource == null) return;

        audioSource.PlayOneShot(clip);
    }

}
