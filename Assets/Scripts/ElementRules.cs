using UnityEngine;
using Element = Block.BlockElementType;

// Elementlerin oynanışa etkileri ve hangi katta hangi elementin geleceği.
//   Buz      : %30 yavaş kayar (yardımcı)
//   Ateş     : %20 hızlı kayar, perfect toleransı 2 kat (riskli ama ödüllü)
//   Rüzgâr   : hızı dalga dalga değişir, perfect +2 puan (zor ama kazançlı)
//   Elektrik : normal hız, perfect bloğu anında büyütür (ödül)
// İlk 9 kat elementsiz; elementler 10, 20, 30, 40. katlarda birer birer tanıtılır.
public static class ElementRules
{
    // Tanıtım sırası: önce yardımcı olan
    public static readonly Element[] IntroductionOrder = { Element.Ice, Element.Fire, Element.Wind, Element.Electric };

    private const int FirstElementFloor = 10;
    private const int FloorsBetweenIntroductions = 10;
    private const float StrugglingHelpWeight = 3f;

    public const string TestEarlyPrefKey = "TowerStack.TestElementsEarly";

    // Editörde "Tower Stack > Test: Elements Early" açıkken elementler 2, 4, 6, 8. katlarda gelir.
    // Telefondaki oyunu hiç etkilemez.
    public static bool TestEarly
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(TestEarlyPrefKey, false);
#else
            return false;
#endif
        }
    }

    public static int UnlockFloor(Element element)
    {
        int index = System.Array.IndexOf(IntroductionOrder, element);
        if (index < 0) return 0;
        return TestEarly ? 2 + index * 2 : FirstElementFloor + index * FloorsBetweenIntroductions;
    }

    // Kayan bloğun hız çarpanı; age = blok ortaya çıkalı geçen süre
    public static float SpeedMultiplier(Element element, float age)
    {
        switch (element)
        {
            case Element.Ice: return 0.7f;
            case Element.Fire: return 1.2f;
            case Element.Wind: return 1f + 0.5f * Mathf.Sin(age * 2f * Mathf.PI / 1.4f);   // rüzgâr hamleleri
            default: return 1f;
        }
    }

    public static float PerfectToleranceMultiplier(Element element) => element == Element.Fire ? 2f : 1f;

    public static int PerfectBonusPoints(Element element) => element == Element.Wind ? 2 : 0;

    public static float PerfectGrowAmount(Element element) => element == Element.Electric ? 0.3f : 0f;

    // floor: yeni bloğun katı (1'den başlar). struggling: oyuncu zorlanıyorsa yardımcı elementler daha sık gelir.
    // isIntroduction: element bu katta ilk kez tanıtılıyorsa true (ekranda "Yeni element" yazısı çıkar).
    public static Element Pick(int floor, bool struggling, out bool isIntroduction)
    {
        isIntroduction = false;
        foreach (Element element in IntroductionOrder)
        {
            if (floor == UnlockFloor(element))
            {
                isIntroduction = true;
                return element;
            }
        }

        // Elementsiz bloklar da gelmeye devam eder; oyuna nefes aldırır
        float total = 1f;
        var weights = new float[IntroductionOrder.Length];
        for (int i = 0; i < IntroductionOrder.Length; i++)
        {
            Element element = IntroductionOrder[i];
            if (floor < UnlockFloor(element)) continue;

            bool helpful = element == Element.Ice || element == Element.Electric;
            weights[i] = struggling && helpful ? StrugglingHelpWeight : 1f;
            total += weights[i];
        }

        float roll = Random.value * total;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0f)
                return IntroductionOrder[i];
        }
        return Element.None;
    }
}
