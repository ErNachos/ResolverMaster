# Architettura di Resolver Master

## Struttura delle cartelle
```
Assets/
  Scripts/
    Core/                      ResolverMaster.Core (tutto ciò che è condiviso)
      App/                     AppBootstrap (avvio), AppServices (servizi), AppUi (costruzione UI)
      Games/                   GameInfo, MinigameModule, GameRegistry
      Localization/            Language, LanguagePicker, LocalizationTable, Localizer, LocKeys, LocalizationLoader, LocalizedText
      Save/                    SaveData (+SettingsBlock), SaveSerializer, SaveStore
      UI/                      UiKitStyle, IconLibrary, UiFactory, SafeArea, UiScreen, ScreenManager
        Screens/               HomeScreen, ModeSelectScreen, SettingsScreen, ScreenLayout, ScreenIds
    Games/                     ResolverMaster.Games (un sotto-cartella per gioco)
      Plumber/                 PlumberModule (Idraulico)
      TrafficJam/              TrafficJamModule (Traffic jam)
    Editor/                    ResolverMaster.Editor: ProjectSetup, MainSceneBuilder, UiKitGenerator, UiKitAssets,
                               ArtImportRules, ReleaseBuild, VersionTools, PreviewMaker, Validation/
  Tests/EditMode/              ResolverMaster.Tests.EditMode
  Resources/ResolverMaster/    UiKitStyle.asset, IconLibrary.asset
  Resources/Localization/      it/*.txt, en/*.txt (core.txt + un file per gioco)
  Art/Icons, Art/UI, Art/Fonts icone Material Symbols, sprite 9 fette generati, font Fredoka
  Scenes/Main.unity            l'unica scena
ArtSource/                     originali di Gemini (mai modificati dal codice)
Tools/Art/                     script di generazione grafica (make_icons.py)
Previews/                      anteprime e report delle misure (ignorata da git)
Builds/Release/                AAB e APK (ignorata da git)
Release/Site, Release/PlayStore  materiale di pubblicazione
```

## Core e minigiochi
- **Core**: servizi comuni e UI dell'app. Oggi: lingue, salvataggio, registro dei minigiochi, UI kit, schermate Home / Modalità / Impostazioni. Nei prossimi Update: griglia, livelli, mosse, stelle, Storia, Challenge, audio, pubblicità.
- **Minigioco**: una cartella in `Games/` con una sottoclasse di `MinigameModule` che espone `GameInfo` (id, chiave del nome, icona, disponibilità, ordine). In seguito aggiungerà regole, pezzi, generatore/risolutore e grafica, appoggiandosi alle astrazioni del Core.
- Registrazione: `GameRegistry.Discover()` scandisce gli assembly `ResolverMaster*` e crea un'istanza di ogni `MinigameModule`. Il Core non referenzia mai un gioco.
- Validazione: i controlli (`IValidationCheck`) sono trovati automaticamente; ogni gioco potrà aggiungere i suoi.

## Avvio
`Main.unity` contiene: camera (solo sfondo), EventSystem con `InputSystemUIInputModule`, oggetto `App` con `AppBootstrap`.
`AppBootstrap.Awake`:
1. carica `UiKitStyle` e `IconLibrary` da Resources;
2. legge il salvataggio (`SaveStore.Default().Load()`);
3. sceglie la lingua (`LanguagePicker.Resolve(salvata, lingua del telefono)`) e carica i testi;
4. scopre i minigiochi (`GameRegistry.Discover()`);
5. crea il canvas (Screen Space Overlay, Canvas Scaler 1080x1920 *Expand*) e costruisce tutte le schermate con `AppUi.Build`.

## Flusso delle schermate
```
Home ──(scheda gioco)──> Scelta modalità ──(Storia / Challenge)──> messaggio segnaposto
  │                           │
  └──(ingranaggio)──> Impostazioni (lingua, suono, vibrazione)
Indietro (pulsante o tasto Android): chiude la schermata in cima; sulla Home chiude l'app.
```
- `ScreenManager` tiene una pila di `UiScreen`: `Push` apre con dissolvenza (`UiKitStyle.screenFade`), `Back` chiude.
- Ogni schermata: sfondo a tutto schermo → `SafeArea` → barra in alto + colonna del contenuto.
- Cambio lingua: `Localizer.Changed` aggiorna subito ogni `LocalizedText`; la scelta viene salvata.

## Salvataggio
```json
{ "version": 1, "settings": { "language": "it", "sound": true, "vibration": true } }
```
Blocchi futuri: uno per gioco (per id), con progressi della Storia e record della Challenge.
