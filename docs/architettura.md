# Architettura di Resolver Master

## Struttura delle cartelle
```
Assets/
  Scripts/
    Core/                      ResolverMaster.Core (tutto ciò che è condiviso)
      App/                     AppBootstrap (avvio), AppServices (servizi), AppUi (costruzione UI)
      Games/                   GameInfo, MinigameModule (hook Storia + board), GameRegistry (moduli)
      Puzzle/                  basi comuni dei rompicapi (C# puro): Grid/ (IGridTopology, SquareTopology, DirectionMask),
                               SeededRandom (PCG32), PuzzleBase/IPuzzle/IPuzzleSession, UndoStack, StarRating/StarRules,
                               DifficultyTable, BucketQueue, GameMode/LevelRequest, PuzzleConfig (asset delle stelle)
      Localization/            Language, LanguagePicker, LocalizationTable, Localizer, LocKeys, LocalizationLoader, LocalizedText
      Save/                    SaveData (+SettingsBlock, ProgressBlock), SaveSerializer, SaveStore
      UI/                      UiKitStyle, IconLibrary, UiFactory, SafeArea, UiScreen, ScreenManager,
                               PuzzleBoard (classe base della board di un gioco), ComponentPool, ModalOverlay
        Screens/               Home, ModeSelect, Settings, LevelSelect, Game (+ pannello di vittoria), ScreenLayout, ScreenIds
    Games/                     ResolverMaster.Games (un sotto-cartella per gioco)
      Plumber/                 PlumberModule, PlumberConfig (Idraulico)
        Logic/                 PipePiece, PipeGrid, WaterFlow, PlumberPuzzle, PlumberSolver, PlumberGenerator,
                               PlumberDifficulty, PlumberLevel(+Set), PlumberLevelCodec
        View/                  PlumberBoardView (: PuzzleBoard), PipeCellView, PipeSprites (grafica generata)
      TrafficJam/              TrafficJamModule (Traffic jam)
    Editor/                    ResolverMaster.Editor: ProjectSetup, MainSceneBuilder, UiKitGenerator, UiKitAssets,
                               ArtImportRules, ReleaseBuild, VersionTools, PreviewMaker, ConfigAssets, Validation/,
                               Plumber/PlumberLevelTools (menu Idraulico > Genera livelli Storia)
  Tests/EditMode/              ResolverMaster.Tests.EditMode
  Resources/ResolverMaster/    UiKitStyle.asset, IconLibrary.asset, PuzzleConfig.asset
  Resources/Games/Plumber/     PlumberConfig.asset, plumber_story.json (livelli della Storia)
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
- **Core**: servizi comuni e UI dell'app: lingue, salvataggio (impostazioni + stelle per livello), registro dei minigiochi, UI kit, schermate Home / Modalità / Impostazioni / Scelta livello / Gioco con pannello di vittoria; basi dei rompicapi (griglia astratta, casualità con seed, contratto del puzzle, annulla, stelle, tabella di difficoltà, coda di ricerca, pool). Prossimi: Challenge, aiuto, audio, pubblicità.
- **Cosa resta nell'Idraulico**: i pezzi e le loro aperture, il calcolo dell'acqua, le regole della mossa (`PlumberPuzzle : PuzzleBase`), il risolutore, il generatore, i valori del suo gradino (`size`, `minMoves`, `path`, `tees`, `crosses`), la grafica dei tubi e la sua board (`PlumberBoardView : PuzzleBoard`).
- **Contratto gioco ↔ Core**: `MinigameModule.StoryLevelCount` e `CreateBoard(ctx, area)`. La `GameScreen` del Core possiede barre, contatori, annulla/ricomincia, stelle, salvataggio e pannello; la board disegna, riceve i tocchi, anima e segnala `Changed` / `Completed`. `LevelRequest` (modalità, indice, seed) prepara già la Challenge.
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
Home ──(scheda gioco)──> Scelta modalità ──(Storia)──> Scelta livello ──> Gioco ──(vinto)──> pannello: Ripeti / Avanti
                              └──(Challenge, o gioco senza livelli)──> messaggio segnaposto
  │                           │
  └──(ingranaggio)──> Impostazioni (lingua, suono, vibrazione)
Indietro (pulsante o tasto Android): chiude la schermata in cima; sulla Home chiude l'app.
```
- `ScreenManager` tiene una pila di `UiScreen`: `Push` apre con dissolvenza (`UiKitStyle.screenFade`), `Back` chiude.
- Ogni schermata: sfondo a tutto schermo → `SafeArea` → barra in alto + colonna del contenuto.
- Cambio lingua: `Localizer.Changed` aggiorna subito ogni `LocalizedText`; la scelta viene salvata.

## Salvataggio
```json
{ "version": 1,
  "settings": { "language": "it", "sound": true, "vibration": true },
  "progress": { "entries": [ { "game": "plumber", "mode": "story", "stars": [3, 2, 1] } ] } }
```
`progress` è opzionale: le stelle migliori per livello (indice = livello - 1), per gioco e modalità. Il livello n si sblocca se il livello n-1 ha almeno una stella.

## Idraulico
- Livelli della Storia generati una volta nell'Editor (*Idraulico > Genera livelli Storia*, seed fissi) e salvati in `plumber_story.json`; il telefono non li rigenera. Tabella in `docs/idraulico-livelli.md`.
- Risolutore esatto: costo minimo = percorso più economico fonte → arrivo (le perdite sono permesse, gli altri pezzi restano come sono); A* su (cella, lato d'ingresso, celle usate) con euristica ammissibile. Supporta una coppia fonte/arrivo (i colori multipli arriveranno con i loro livelli).
- Tocco: la mossa vale subito nel modello; la rotazione visiva si accoda (0,12 s per quarto di giro); a rotazioni finite l'acqua si propaga cella dopo cella; alla vittoria l'arrivo si riempie e poi compare il pannello.
