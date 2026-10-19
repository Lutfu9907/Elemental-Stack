# Elemental Stack

A relaxing 3D tower-stacking game for mobile, made with Unity by **LD Games**.

Tap to drop the sliding block. Whatever hangs over the block below is sliced off and tumbles away, so the tower gets narrower with every miss. Land it almost exactly on top for a **Perfect**, keep the streak going and watch the sky change from day to night as the tower climbs.

> The repository is still called *Tower-Stack*; the game itself was renamed to *Elemental Stack*.

## Features

**Gameplay**
- Clean isometric 3D look: cube blocks, soft shadows and a sky gradient that shifts as the tower grows
- Blocks slide on alternating X and Z axes; the overhang is cut and falls with real physics
- Perfect placements build a combo: rising musical notes, an expanding ripple, combo callouts and screen flash
- Reward for long streaks: after five perfects in a row each perfect grows the block back a little
- Saved best score and a "New best!" badge
- Altitude zones: Clouds (25), High sky (50), Aurora (75), Space (100) and Moon (150) each give a celebration, a coin bonus and a change in the sky; a line around the tower marks your best floor and the start screen shows the next goal
- Daily missions: three new missions every day (perfects, streaks, floors, element blocks, games, coins) with coin rewards and a bonus for finishing all three, plus a daily login reward that grows over a seven-day streak
- Share: the game-over card shares a picture of your tower and your floor count to WhatsApp, Instagram and other apps (no extra plugin; on Android 9 and older the text alone is shared)
- Coins: one per floor plus one per perfect, counted up on the game-over card and shown on the start screen
- Second chance: after a miss from floor 5 on, keep going once per game by watching an ad or paying 100 coins (5 second countdown)
- Fair ads: no ads in the first three games, then at most one interstitial every three games and 90 seconds; a rewarded "x2" doubles the coins of a game; a one-time "Remove ads" purchase turns interstitials off (restore from Settings)
- Coin shop: five block themes (Classic, Candy, Wood, Neon, Gold) and three sky themes (Day, Sunset, Space); element colours stay the same in every theme
- Living skies: drifting clouds and passing birds by day, a glowing setting sun with light motes at sunset, twinkling stars, shooting stars, nebulae and a ringed planet in space; layers scroll at different speeds as the tower climbs
- Rich block themes: candy stripes with a sprinkle burst, wood grain with flying chips, neon blocks whose edges glow in changing colours with a neon ring on landing, and brushed gold with glittering dust and twinkles around the top of the tower; every theme has its own picture in the shop

**Elements**

| Element | While sliding | On a perfect |
|---|---|---|
| Ice | 30% slower | – |
| Fire | 20% faster | perfect window twice as wide |
| Wind | speed comes in gusts | +2 points |
| Electric | normal speed | the block grows back a little |

- The first nine floors are plain; Ice, Fire, Wind and Electric are introduced on floors 10, 20, 30 and 40 with a "New element" bubble (shown in full for the first three games, then as a small reminder)
- Element rewards are spelled out when they happen: Wind's +2 and "Block grew!" for Electric appear in the element's colour, and an Elements page on the start screen explains all four (unknown ones stay locked until you reach them)
- A helping hand: after bad cuts or when the block gets narrow, Ice and Electric come more often
- Only element blocks are coloured. While sliding they glow, wear a bright frame, float their icon and leave a particle trail (embers, frost, gusts, sparks); landing bursts into a ring of their colour
- Editor shortcut for testing: **Tower Stack → Test: Elements Early** brings the elements in on floors 2 to 8

**Feel and sound**
- All sound effects, the intro jingle and the background music are synthesised in code, so there are no third-party audio licences
- Short haptic feedback on Android (light, medium and heavy)
- Settings screen: music, sound effects, vibration, graphics quality, language and the privacy policy

**Presentation**
- Animated *LD Games* studio intro synced to its jingle (replaces the Unity splash)
- Rounded, friendly UI in the Baloo 2 font
- English by default, with a Turkish option on the start screen (choice is saved)

**Mobile ready**
- Portrait only, notch and rounded-corner safe areas, 60 FPS target, screen stays awake while playing
- Camera widens on tall phones so the sliding block never leaves the screen
- Taps on buttons never drop a block
- First-game guide with a pulsing tap ring and slower first blocks
- Pause button, and the game pauses itself when the phone goes to the background
- Low graphics mode (no bloom or shadows) picked automatically on low-end phones

**Release ready**
- Android settings applied by the scene builder: package `com.lutfudolay.elementalstack`, IL2CPP, ARM64 + ARMv7, minimum Android 7.1, app bundle (.aab)
- App icon with Android adaptive layers, Play Store icon and feature graphic, all drawn in code (`node tools/store-art.js`)
- Google AdMob (with the European consent form and an "Ad privacy choices" setting) and Unity In-App Purchasing plug in through the `GOOGLE_MOBILE_ADS` and `IAP_ENABLED` defines; the editor uses simulated ads and purchases
- Store screenshot tool: **Tower Stack → Release → Save Store Screenshot** (Play mode)
- Privacy policy, store listing texts and a step-by-step release guide in `docs/`

## Coming next

- Element streaks: special powers when three blocks of the same element land in a row
- A Google Play leaderboard and cloud save
- Google Play closed test, then release (see `docs/RELEASE.md`); App Store after that

## Open the project

1. Install **Unity 6.3 LTS** (6000.3.x) with **Android Build Support** from Unity Hub.
2. Clone this repository and add the folder in Unity Hub (*Projects → Add → Add project from disk*).
3. Open the project, then run **Tower Stack → Build Game Scene** from the menu bar.
   This builds `Assets/Scenes/Game.unity` from scratch and wires everything up: render pipeline, materials, fonts, UI, sounds and music.
4. Press Play. To check notches and phone sizes, switch the Game view to **Simulator** and pick a phone.

The scene is generated by `Assets/Editor/TowerStackSceneBuilder.cs`, so layout and settings changes go there rather than into the scene by hand.

## Project layout

| Folder | Contents |
|---|---|
| `Assets/Scripts` | Gameplay, UI, audio, localisation and haptics |
| `Assets/Editor` | Scene builder, sound synthesiser and release settings |
| `Assets/Audio` | Generated sound effects and music (rebuilt by the scene builder) |
| `Assets/Fonts/Baloo2` | Baloo 2 font and its licence |
| `Assets/Settings` | URP render pipeline assets |
| `Assets/Art` | App icon and its adaptive layers |
| `docs` | Privacy policy, store listing, store art and the release guide |
| `tools` | `store-art.js`, which draws the icon and store pictures |

## Credits

- Font: [Baloo 2](https://github.com/EkType/Baloo2) by Ek Type, SIL Open Font License 1.1 (`Assets/Fonts/Baloo2/OFL.txt`)
- Sounds and music: generated in code (`Assets/Editor/SoundSynth.cs`)

---

### Türkçe özet

**Elemental Stack**, LD Games'in Unity ile geliştirdiği bir mobil kule dizme oyunu. Kayan bloğa dokununca düşer, alttaki bloğun dışında kalan kısmı kesilir. Tam üstüne koyarsan "Perfect" olur ve seri devam eder. Buz, ateş, rüzgâr ve elektrik blokları oynanışı değiştirir: kimi yavaşlatır, kimi hızlandırır, kimi ödül verir. Oyun İngilizce ve Türkçe oynanabilir. Önce Google Play'de, sonra App Store'da yayınlanacak.

Projeyi açmak için Unity 6.3 LTS ile açıp menüden **Tower Stack → Build Game Scene** çalıştırman yeterli. Yayın adımları (hesaplar, imza anahtarı, AdMob, satın alma, Play Console) `docs/RELEASE.md` dosyasında.
