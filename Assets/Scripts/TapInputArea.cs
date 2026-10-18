using UnityEngine;
using UnityEngine.EventSystems;

// Oyun ekranının arkasındaki görünmez, tam ekran dokunma alanı.
// Butonlar bunun üstünde olduğu için onlara dokunmak blok düşürmez.
public class TapInputArea : MonoBehaviour, IPointerDownHandler
{
    public TowerController tower;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (tower != null)
            tower.AddOrDropBlock();
    }
}
