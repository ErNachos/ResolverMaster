# Architettura di Resolver Master

## Struttura delle cartelle
```
Assets/
  Scripts/
    Core/                      ResolverMaster.Core (tutto ciò che è condiviso)
      App/                     AppBootstrap (avvio), AppServices (servizi), AppUi (costruzione UI)
      Games/                   GameInfo (+ immagine e colore della scheda, HomeSelection), MinigameModule (hook Storia + board), GameRegistry (moduli)
      Puzzle/                  basi comuni dei rompicapi (C# puro): Grid/ (IGridTopology, SquareTopology, DirectionMask),
                               SeededRandom (PCG32), PuzzleBase/IPuzzle/IPuzzleSession, UndoStack, StarRating/StarRules,
                               DifficultyTable, BucketQueue, GameMode/LevelRequest, PuzzleConfig (asset delle stelle),
                               Story (LevelId, StoryChapter, StoryBook, StoryProgress: capitoli e sblocco), ChapterTheme,
                               ArtVariants (disegno scelto dal seed del livello)
      Localization/            Language, LanguagePicker, LocalizationTable, Localizer, LocKeys, LocalizationLoader, LocalizedText
      Save/                    SaveData (+SettingsBlock, ProgressBlock, HomeBlock), SaveSerializer, SaveStore
      UI/                      UiKitStyle, IconLibrary (libreria di sprite: icone, schede, disegni dei giochi), UiFactory,
                               PressableButton (pulsante che scende), CardCarousel (logica delle schede della Home),
                               SafeArea, UiScreen, ScreenManager, PuzzleBoard (board di un gioco + MainActionInfo),
                               ComponentPool, ModalOverlay, Decoration
        Screens/               Home (schede a scorrimento), ModeSelect, Settings, ChapterSelect (capitoli), LevelSelect (griglia 3x3 di un capitolo),
                               Game (+ pulsante d'azione, messaggio breve, pannello di vittoria), ScreenLayout, ScreenIds
    Games/                     ResolverMaster.Games (un sotto-cartella per gioco)
      Plumber/                 PlumberModule, PlumberConfig, PlumberKeys (Idraulico)
        Logic/                 PipePiece, PipeGrid, WaterFlow, PlumberPuzzle, PlumberSolver, PlumberGenerator,
                               PlumberDifficulty, PlumberLevel(+Set), PlumberLevelCodec
        View/                  PlumberBoardView (: PuzzleBoard), PipeCellView, PipeSprites (tubi generati), PlumberArt (fonti e arrivi)
      TrafficJam/              TrafficJamModule (Traffic jam)
    Editor/                    ResolverMaster.Editor: ProjectSetup, MainSceneBuilder, UiKitGenerator, UiKitAssets,
                               ArtImportRules, ReleaseBuild, VersionTools, PreviewMaker, ConfigAssets, Validation/,
                               Plumber/PlumberLevelTools (menu Idraulico > Genera livelli Storia)
  Tests/EditMode/              ResolverMaster.Tests.EditMode
  Resources/ResolverMaster/    UiKitStyle.asset, IconLibrary.asset, CardLibrary.asset, PuzzleConfig.asset
  Resources/Games/Plumber/     PlumberConfig.asset, PlumberArt.asset (libreria dei disegni), plumber_story.json (livelli della Storia)
  Resources/Localization/      it/*.txt, en/*.txt (core.txt + un file per gioco)
  Art/Icons, Art/UI, Art/Fonts icone Material Symbols, sprite 9 fette generati, font Fredoka
  Art/Plumber/                 fonti e arrivi preparati da Tools/Art/make_plumber_art.py
  Art/Cards/                   illustrazioni delle schede della Home (in arrivo da Gemini)
  Scenes/Main.unity            l'unica scena
ArtSource/                     originali di Gemini (mai modificati dal codice)
Tools/Art/                     script di grafica (make_icons.py, make_plumber_art.py); vedi docs/grafica.md
Previews/                      anteprime e report delle misure (ignorata da git)
Builds/Release/                AAB e APK (ignorata da git)
Release/Site, Release/PlayStore  materiale di pubblicazione
```

## Core e minigiochi
- **Core**: servizi comuni e UI dell'app.
  - Lingue, salvataggio (impostazioni + stelle per livello con id stabile + ultima scheda della Home), registro dei minigiochi, UI kit (stile cartoon, pulsanti che scendono, librerie di sprite).
  - Schermate: Home, Modalità, Impostazioni, Capitoli, Livelli del capitolo, Gioco (con pulsante d'azione, messaggio breve e pannello di vittoria).
  - Basi dei rompicapi: griglia astratta, casualità con seed, contratto del puzzle, annulla, stelle, tabella di difficoltà, capitoli e sblocco, coda di ricerca, pool.
  - Prossimi: Challenge, aiuto, audio, pubblicità.
- **Cosa resta nell'Idraulico**:
  - i pezzi e le loro aperture;
  - il calcolo dell'acqua con i suoi errori (`WaterFlow`);
  - le regole della mossa e dell'apertura (`PlumberPuzzle : PuzzleBase`);
  - il risolutore e il generatore;
  - i valori del suo gradino (`size`, `minMoves`, `path`, `tees`, `crosses`, `sinks`, `colors`);
  - nomi e temi dei suoi capitoli (in `PlumberConfig`);
  - la grafica dei tubi, i suoi disegni di fonti e arrivi e la sua board (`PlumberBoardView : PuzzleBoard`).
  - Regole complete: `docs/idraulico-regole.md`.
- **Contratto gioco ↔ Core**:
  - Il gioco fornisce `MinigameModule.Story` (uno `StoryBook`: capitoli di 9 livelli numerati di seguito) e `CreateBoard(ctx, area)`.
  - La `GameScreen` del Core possiede barre, contatori, annulla/ricomincia, il pulsante d'azione **opzionale**, i messaggi brevi, stelle, salvataggio e pannello.
  - La board disegna, riceve i tocchi, anima e segnala `Changed`, `Completed` e `MessageRequested(chiave)`.
  - Se vuole il pulsante, la board dichiara `MainAction` (testo e icona) e riceve `RunMainAction()`. Con `IsBusy` blocca pulsante, annulla e ricomincia durante le animazioni.
  - Un gioco senza `MainAction` (Traffic jam) non ha il pulsante e la sua area del puzzle è più alta.
  - `LevelRequest` (modalità, indice, seed) prepara già la Challenge.
- **Minigioco**: una cartella in `Games/` con una sottoclasse di `MinigameModule` che espone `GameInfo` (id, chiave del nome, icona, disponibilità, ordine, immagine e colore della scheda della Home). In seguito aggiungerà regole, pezzi, generatore/risolutore e grafica, appoggiandosi alle astrazioni del Core.
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
Home ──(GIOCA! sulla scheda al centro)──> Scelta modalità ──(Storia)──> Capitoli ──> Livelli del capitolo (3x3) ──> Gioco ──(vinto)──> pannello: Ripeti / Avanti
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
  "progress": { "entries": [ { "game": "plumber", "mode": "story",
                                "chapters": [ { "chapter": 1, "stars": [3, 2, 1] } ] } ] },
  "home": { "selectedGame": "plumber" } }
```
- `progress` è opzionale: le stelle migliori per livello, per gioco, modalità e capitolo.
- `home` è opzionale (ErNa-004): l'id dell'ultima scheda scelta; se manca o il gioco non esiste più si parte dalla prima.
- L'id stabile di un livello è (gioco, capitolo, indice nel capitolo): aggiungere livelli o capitoli non sposta le stelle già salvate.
- Sblocco (`StoryProgress`):
  - dentro un capitolo, un livello si sblocca risolvendo il precedente;
  - il capitolo successivo si sblocca risolvendo tutti i 9 livelli del precedente, con qualsiasi numero di stelle.
- ErNa-003: i progressi salvati con la vecchia lista piatta (`stars` per modalità) vengono ignorati, perché eravamo ancora in sviluppo.

## Idraulico
- Regole complete: `docs/idraulico-regole.md`. In breve: prima si collegano i tubi, poi si apre l'acqua. Il tentativo riesce solo se non ci sono perdite, i colori restano separati e ogni fontana riceve il suo colore.
- **Livelli della Storia**: 27 in 3 capitoli.
  - Sono generati una volta nell'Editor (*Idraulico > Genera livelli Storia*, seed fissi) e salvati in `plumber_story.json`, con `chapter` e `index`. Il telefono non li rigenera.
  - Tabella in `docs/idraulico-livelli.md`.
- **Risolutore esatto**: cerca la rete chiusa di costo minimo. Gestisce più fonti, più arrivi, i colori e le simmetrie dei pezzi. Usa IDA\* sulle estremità aperte, con un limite inferiore ammissibile.
- **Generatore**: tre schemi (percorso singolo, bivio con T, due colori).
  - Riempie il resto con pezzi casuali e ruota tutto a caso.
  - Scarta i livelli già risolti, quelli fuori gradino e, nel capitolo 1, quelli che usano una T.
- **Tocco**: la mossa vale subito nel modello; la rotazione visiva si accoda (0,12 s per quarto di giro).
- **"Apri l'acqua"**: le rotazioni in corso finiscono subito, poi l'acqua avanza cella dopo cella da tutte le fonti.
  - Riuscita: l'acqua scorre con un leggero movimento, schizza fuori dalle fontane e compare il pannello.
  - Errore: l'acqua si ferma al primo problema, parte l'effetto con il messaggio, poi il livello ricomincia.
- **Fonti e arrivi** (ErNa-004) sono disegni (4 per tipo) scelti dal seed del livello; non ruotano mai e un moncone di tubo sotto mostra l'apertura. La fonte ha le parti colorate nel colore della sua acqua (maschera), l'arrivo ha sotto un alone del suo colore (`PlumberConfig`). Dettagli in `docs/grafica.md`.
