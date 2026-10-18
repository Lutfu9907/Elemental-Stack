using UnityEngine;
using UnityEngine.UI;

// Başlangıç ekranındaki dil butonu: her dokunuşta sıradaki dile geçer (EN → TR → EN).
// Butonun yazısı LocalizedText ("language" anahtarı) ile o anki dili gösterir.
[RequireComponent(typeof(Button))]
public class LanguageButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Localization.NextLanguage);
    }
}
