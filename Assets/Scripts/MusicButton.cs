using UnityEngine;
using UnityEngine.UI;

// Başlangıç ekranındaki müzik butonu: dokununca müziği açar/kapatır.
// Kapalıyken nota simgesinin üstünde çapraz bir çizgi görünür.
[RequireComponent(typeof(Button))]
public class MusicButton : MonoBehaviour
{
    public GameObject mutedSlash;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    void Start()
    {
        Refresh();
    }

    private void Toggle()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;

        audio.MusicEnabled = !audio.MusicEnabled;
        audio.PlayButton();
        Refresh();
    }

    private void Refresh()
    {
        if (mutedSlash != null && AudioManager.Instance != null)
            mutedSlash.SetActive(!AudioManager.Instance.MusicEnabled);
    }
}
