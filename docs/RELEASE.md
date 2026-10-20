# Yayın rehberi (Google Play)

Oyun tarafındaki her şey hazır. Aşağıdakiler hesap, imza ve mağaza işleri; sırayla yapılır.

## 1. Hesaplar
- **Google Play Console** geliştirici hesabı (tek seferlik 25 $) ve kimlik doğrulaması.
- **AdMob** hesabı (ücretsiz). Play Console'daki uygulamayla bağlanır.

## 2. Paket adı ve sürüm
- Paket adı: `com.lutfudolay.elementalstack`. Google Play'e yüklendikten sonra **hiç değiştirilemez**. Değiştirmek istersen `Assets/Editor/ReleaseTools.cs` içindeki `PackageName`'i ilk yüklemeden önce değiştir.
- Sürüm 1.0.0 (kod 1). Her güncellemede Player Settings'ten *Version* ve *Bundle Version Code* bir artırılır.
- Android ayarları (IL2CPP, ARM64, min API 25, .aab) sahne kurulurken ya da **Elemental Stack → Release → Apply Android Settings** ile uygulanır.

## 3. İmza anahtarı (keystore)
- Unity: *Project Settings → Player → Android → Publishing Settings → Keystore Manager → Create New*.
- Dosyayı ve şifreleri **güvenli bir yerde yedekle**. Repoya koyma; `.gitignore` zaten `*.keystore` / `*.jks` dosyalarını dışarıda tutmalı.
- Play Console'da "Play App Signing" açık kalsın.

## 4. Reklamlar (AdMob)
1. Google Mobile Ads Unity eklentisini kur (GitHub sürümleri ya da OpenUPM).
2. *Assets → Google Mobile Ads → Settings*: AdMob **App ID**'yi gir.
3. *Player Settings → Scripting Define Symbols*: `GOOGLE_MOBILE_ADS` ekle.
4. AdMob'da iki reklam birimi aç: **Ödüllü** ve **Geçiş (interstitial)**. Kimlikleri `Assets/Scripts/AdProviders.cs` içindeki `LiveRewardedId` ve `LiveInterstitialId` alanlarına yaz. Bu alanlar boşsa ya da sürüm "Development Build" ise Google'ın test reklamları gösterilir.
5. AdMob → Gizlilik ve mesajlaşma: GDPR (Avrupa) izin mesajı oluştur. Oyun bunu açılışta kendisi gösterir, Ayarlar'da "Reklam izin seçenekleri" de çıkar.

## 5. Reklamsız sürüm (satın alma)
1. Package Manager → **In-App Purchasing 4.x** paketini kur (kod 4.x API'sini kullanıyor).
2. Scripting Define Symbols: `IAP_ENABLED` ekle.
3. Play Console → Monetize → Products → In-app products: kimliği **`remove_ads`** olan ürünü oluştur (fiyat örneğin 1,99 $). Bunun için önce bir .aab yüklenmiş olmalı.

## 6. Derleme
*File → Build Profiles → Android → Build* ile `.aab` çıkar. Önce bir telefonda "Build and Run" ile dene.

## 7. Play Console formları
- **Gizlilik politikası**: `docs/privacy-policy.html` dosyasını `https://lutfudolay.com/elemental-stack/privacy` adresine koy (adres `Assets/Scripts/GameLinks.cs` içinde).
- **Uygulama içeriği**: reklam içeriyor = Evet; hedef kitle **13+** (çocuklara yönelik değil; AdMob ile daha basit); içerik derecelendirme anketi.
- **Veri güvenliği**: oyun kendisi veri toplamıyor. AdMob için "Cihaz veya diğer kimlikler", "Uygulama etkileşimleri", "Teşhis" seçeneklerini *paylaşılıyor / reklam* amacıyla işaretle; veriler aktarımda şifreli.
- **Mağaza girişi**: metinler ve görseller `docs/store/` klasöründe (`listing.md`, `icon-512.png`, `feature-graphic.png`). Ekran görüntüleri: Game view'i 1080x1920 yap, Play'de **Elemental Stack → Release → Save Store Screenshot**.

## 8. Kapalı test
Yeni kişisel hesaplarda üretime çıkmadan önce **en az 12 test kullanıcısı 14 gün boyunca** kapalı testte olmalı. Testing → Closed testing → kanal aç, testçilerin e-postalarını ekle, .aab yükle, katılım bağlantısını paylaş.

## 9. Üretim
14 gün dolunca Play Console'dan üretime erişim başvurusu yapılır, onaydan sonra sürüm yayınlanır.

## Görselleri yeniden çizmek
`node tools/store-art.js` → ikon, adaptive ikon katmanları, Play ikonu ve tanıtım görseli yeniden oluşur.
