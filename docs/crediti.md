# Crediti e licenze

| Risorsa | Tipo | Dove | Fonte | Licenza |
|---|---|---|---|---|
| Fredoka (Regular 400, SemiBold 600) | Font | `Assets/Art/Fonts/Fredoka-*.ttf` (+ font asset SDF di TextMesh Pro generati dal progetto) | Google Fonts — https://fonts.google.com/specimen/Fredoka (Copyright 2016 The Fredoka Project Authors) | SIL Open Font License 1.1 (`Assets/Art/Fonts/Fredoka-OFL.txt`), uso commerciale consentito |
| TMP Essential Resources | Shader, impostazioni, font di riserva | `Assets/TextMesh Pro/` | Unity (pacchetto `com.unity.ugui`) | Unity Companion License |
| Liberation Sans (dentro TMP Essential Resources) | Font di riserva di TextMesh Pro | `Assets/TextMesh Pro/Fonts/` | Red Hat / Google | SIL Open Font License 1.1 (`LiberationSans - OFL.txt`) |
| EmojiOne (dentro TMP Essential Resources) | Sprite di esempio di TextMesh Pro (non usati dal gioco) | `Assets/TextMesh Pro/Sprites/` | EmojiOne | CC BY 4.0 (`EmojiOne Attribution.txt`) |
| Material Symbols Rounded | Icone segnaposto | `Assets/Art/Icons/*.png` (generate da `Tools/Art/make_icons.py`) | Google — https://github.com/google/material-design-icons | Apache License 2.0, uso commerciale consentito |

Icone Material ancora usate (nome asset ← simbolo), finché arrivano i disegni: story ← menu_book, game_plumber ← plumbing, game_trafficjam ← directions_car, water_drop ← water_drop, fork ← call_split, palette ← palette.
Dall'ErNa-005 le icone dell'interfaccia (indietro, impostazioni, annulla, ricomincia, aiuto, lucchetto, suono, vibrazione, lingua, rubinetto, fulmine…) sono disegni di Gemini: le icone Material corrispondenti sono state rimosse.

**Grafica disegnata con Google Gemini** su richiesta del team (uso secondo i termini di Google per i contenuti generati con Gemini, da verificare per l'uso commerciale prima della pubblicazione). Gli originali sono in `ArtSource/` e non vengono mai modificati:
- interfaccia: `ArtSource/UI/ui_1..8_*.png` (pulsanti, pannelli, caselle, stelle, icone) → `Assets/Art/UI/` con `Tools/Art/make_ui_art.py`;
- schede della Home: `ArtSource/Cards/Idraulico.png`, `Trafic.png` → `Assets/Art/Cards/card_plumber.png`, `card_trafficjam.png` con lo stesso script;
- fonti e arrivi dell'Idraulico: `ArtSource/Plumber/Fonte_1..4.png`, `Arrivo_1..4.png` → `Assets/Art/Plumber/` con `Tools/Art/make_plumber_art.py`;
- veicoli e ostacoli di Traffic jam (TJ-002): `ArtSource/TJ/1..4.png` → `Assets/Art/TrafficJam/` con `Tools/Art/make_trafficjam_art.py`.
Riferimento di stile previsto in `ArtSource/Reference/` (non ancora presente).

Generati dal progetto, nessuna licenza esterna: cornice delle schede della Home e pillole (`UiKitGenerator`, `Assets/Art/UI/Generated/`), tubi, acqua, ombre e gocce dell'Idraulico (`PipeSprites`, `WaterGraphic`), terreni e sfondi dei temi (`ThemeArt`), shader dei pulsanti disattivati (`Assets/Art/Shaders/UiDesaturate.shader`).

Audio: nessuno per ora.
