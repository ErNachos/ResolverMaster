# Architettura di Resolver Master

## Struttura delle cartelle
```
Assets/
  Scripts/
    Core/                      ResolverMaster.Core (tutto ciò che è condiviso)
      App/                     AppBootstrap (avvio), AppServices (servizi), AppUi (costruzione UI)
      Games/                   GameInfo (+ immagine e colore della scheda, HomeSelection), MinigameModule (hook Storia + board), GameRegistry (moduli)
      Ads/                     IRewardedAdService, TestRewardedAdService, UnavailableRewardedAdService, RewardedAds (scelta), AdConfig (ErNa-007)
      Challenge/               ChallengeGeneration (IChallengeSource, ChallengeRules, ChallengeQueue), ChallengeRun (stato, record, blocco del salvataggio) (ErNa-007)
                               DailyChallenge (Sfida del giorno: regole pure, serie, DailyStats) (ErNa-008)
      Puzzle/                  basi comuni dei rompicapi (C# puro): Grid/ (IGridTopology, SquareTopology, DirectionMask),
                               SeededRandom (PCG32), PuzzleBase/IPuzzle/IPuzzleSession, UndoStack, StarRating/StarRules,
                               DifficultyTable, BucketQueue, GameMode/LevelRequest, PuzzleConfig (asset delle stelle),
                               Story (LevelId, StoryChapter, StoryBook, StoryProgress: capitoli e sblocco), ChapterTheme,
                               ArtVariants (disegno scelto dal seed del livello), Grid/CellLocks (elementi bloccati o fissati, ErNa-006)
      Localization/            Language, LanguagePicker, LocalizationTable, Localizer, LocKeys, LocalizationLoader, LocalizedText
      Save/                    SaveData (+SettingsBlock, ProgressBlock, HomeBlock), SaveSerializer, SaveStore
      UI/                      UiKitStyle, IconLibrary (libreria di sprite: icone, schede, disegni dei giochi), UiFactory (TextMeshPro),
                               PressableButton (scende e si scurisce; disattivato = desaturato), VertexOffset, SlicedFit (pillole a 9 fette),
                               CardCarousel (logica delle schede della Home), SafeArea, UiScreen, ScreenManager,
                               PuzzleBoard (board di un gioco + MainActionInfo, GridRows, Theme), ComponentPool, ModalOverlay, Decoration,
                               ColorVision (Lab, delta E, simulazione del daltonismo), ShakeMotion (scossa di un elemento bloccato)
        Themes/                VisualTheme + ThemeCatalog (tema di un capitolo, C# puro), ThemeConfig (asset dei temi), ThemeArt (terreni e sfondi generati)
        Screens/               Home (schede a scorrimento), ModeSelect, Settings, ChapterSelect (capitoli), LevelSelect (griglia 3x3 di un capitolo),
                               Game (+ pulsante d'azione, messaggio breve, pannello di vittoria), ScreenLayout, ScreenIds
    Games/                     ResolverMaster.Games (un sotto-cartella per gioco)
      Plumber/                 PlumberModule, PlumberConfig, PlumberKeys (Idraulico)
        Logic/                 PipePiece, PipeGrid, WaterFlow, WaterTiming + WaterMotion (tempi dell'acqua continua), PlumberPuzzle,
                               PlumberSolver, PlumberGenerator, PlumberDifficulty, PlumberLevel(+Set), PlumberLevelCodec,
                               PipeNodes (ponte = due nodi), PlumberPalette (regole dei colori dell'acqua)
        View/                  PlumberBoardView (: PuzzleBoard), PipeCellView, PipeSprites (tubi generati), WaterGraphic (acqua nei tubi),
                               PlumberArt (fonti e arrivi)
      TrafficJam/              TrafficJamModule (Traffic jam)
    Editor/                    ResolverMaster.Editor: ProjectSetup, MainSceneBuilder, UiKitGenerator, UiKitAssets, UiArtSetup (TMP, sprite di Gemini),
                               ArtImportRules, ReleaseBuild, VersionTools, PreviewMaker, ConfigAssets, Validation/,
                               Plumber/PlumberLevelTools (menu Idraulico > Genera livelli Storia)
  Tests/EditMode/              ResolverMaster.Tests.EditMode
  Resources/ResolverMaster/    UiKitStyle.asset, IconLibrary.asset, CardLibrary.asset, PuzzleConfig.asset, ThemeConfig.asset
  Resources/Games/Plumber/     PlumberConfig.asset, PlumberArt.asset (libreria dei disegni), plumber_story.json (livelli della Storia)
  Resources/Localization/      it/*.txt, en/*.txt (core.txt + un file per gioco)
  Art/UI/                      interfaccia di Gemini (Tools/Art/make_ui_art.py) + Generated/ (cornice delle schede, pillole)
  Art/Icons/                   icone Material segnaposto ancora usate
  Art/Fonts/                   Fredoka TTF + font asset SDF di TextMesh Pro e materiale con contorno
  Art/Shaders/                 UiDesaturate (pulsanti disattivati)
  Art/Plumber/                 fonti e arrivi preparati da Tools/Art/make_plumber_art.py
  Art/Cards/                   illustrazioni delle schede della Home (Gemini, Tools/Art/make_ui_art.py)
  TextMesh Pro/                TMP Essential Resources
  Scenes/Main.unity            l'unica scena
ArtSource/                     originali di Gemini (mai modificati dal codice)
Tools/Art/                     script di grafica (make_ui_art.py, make_icons.py, make_plumber_art.py); vedi docs/grafica.md
Previews/                      anteprime e report delle misure (ignorata da git)
Builds/Release/                AAB e APK (ignorata da git)
Release/Site, Release/PlayStore  materiale di pubblicazione
```

## Core e minigiochi
- **Core**: servizi comuni e UI dell'app.
  - Lingue, salvataggio (impostazioni + stelle per livello con id stabile + ultima scheda della Home), registro dei minigiochi, UI kit (stile cartoon, pulsanti che scendono, librerie di sprite).
  - Schermate: Home, Modalità, Impostazioni, Capitoli, Livelli del capitolo, Gioco (con pulsante d'azione, messaggio breve e pannello di vittoria).
  - Basi dei rompicapi: griglia astratta (colonne e righe diverse), casualità con seed, contratto del puzzle, annulla, stelle, tabella di difficoltà, capitoli e sblocco, coda di ricerca, pool.
  - Temi dei capitoli (ErNa-005): `ThemeConfig` (Giardino, Fattoria, Città), terreno e sfondo generati da `ThemeArt`; la `GameScreen` e la griglia dei livelli mostrano lo sfondo del tema, la board riceve il tema in `PuzzleBoard.Theme`.
  - Prossimi: Challenge, aiuto, audio, pubblicità.
- **Cosa resta nell'Idraulico**:
  - i pezzi e le loro aperture;
  - il calcolo dell'acqua con i suoi errori (`WaterFlow`);
  - le regole della mossa e dell'apertura (`PlumberPuzzle : PuzzleBase`);
  - il risolutore e il generatore;
  - i valori del suo gradino (`cols`, `rows`, `minMoves`, `path`, `tees`, `crosses`, `sinks`, `colors`);
  - nomi, icone e id del tema dei suoi capitoli (in `PlumberConfig`);
  - i tempi dell'acqua continua (`WaterTiming`: distanza lungo i tubi di ogni punto, dove avviene ogni problema);
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
                              └──(Challenge: riprende la run o ne inizia una; "Nuova run" con conferma)──> Gioco (Challenge) ──(vinto)──> Avanti
                                                                                    └──(errore)──> fine run: Continua (pubblicità, 1 per run) / Nuova run / Home
  Home ──(pulsante Challenge / Riprendi sulla scheda)──> Gioco (Challenge)   ·   Indietro nella Challenge = pausa (Riprendi / Home)
  Home ──(riga "Sfida del giorno" sulla scheda)──> Gioco (5 livelli del giorno) ──> fine sfida: Continua / Home
  Gioco (Challenge, 0 mosse) ──(Cambia livello, pubblicità)──> nuovo livello dello stesso gradino
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
- **Livelli della Storia**: 81 in 9 capitoli (ErNa-006), su griglie rettangolari 3x4, 4x5, 5x7, 6x8, 7x9 (colonne x righe). Fino a 4 colori, ponti, pezzi bloccati.
  - Sono generati una volta nell'Editor (*Idraulico > Genera livelli Storia*, seed fissi) e salvati in `plumber_story.json`, con `chapter` e `index`. Il telefono non li rigenera.
  - Tabella in `docs/idraulico-livelli.md`.
- **Risolutore esatto**: cerca la rete chiusa di costo minimo. Lavora sui nodi di `PipeNodes` (una cella = un nodo; un ponte = due nodi separati).
- **Grandi griglie** (7x9): sotto i 150 px di cella la cornice si disegna a metà spessore e usa un po' del margine (`compactCellBelow`, `compactFrameScale`, `compactOverflow`): celle da 145–146 px su 1080. Gestisce più fonti, più arrivi, i colori e le simmetrie dei pezzi. Usa IDA\* sulle estremità aperte, con un limite inferiore ammissibile.
- **Generatore**: tre schemi (percorso singolo, bivio con T, un percorso per colore con 2–4 colori). ErNa-006: un percorso può attraversare ad angolo retto un dritto di un altro percorso (ponte); pezzi bloccati sul percorso (già giusti) o fuori.
  - Riempie il resto con pezzi casuali e ruota tutto a caso.
  - Scarta i livelli già risolti, quelli fuori gradino e, nel capitolo 1, quelli che usano una T.
- **Tocco**: la mossa vale subito nel modello; la rotazione visiva si accoda (0,12 s per quarto di giro).
- **"Apri l'acqua"**: le rotazioni in corso finiscono subito, poi l'acqua **scorre in modo continuo** da tutte le fonti insieme, alla stessa velocità, seguendo la forma di ogni tubo (ErNa-005).
  - Logica (`WaterTiming`, C# puro): i tubi diventano un grafo (centri delle celle e metà dei lati aperti); la distanza dalla fonte più vicina lungo i tubi (bracci 0,5, curve pi/4) è il momento di arrivo. Dopo una T i due rami partono insieme. `WaterMotion` converte distanza e tempo (velocità costante, leggera accelerazione iniziale).
  - Le regole restano quelle di `WaterFlow`: `WaterTiming` dice solo dove, lungo i tubi, avviene ogni problema; si mostra quello che arriva per primo.
  - Riuscita: ogni fontana si riempie quando l'acqua arriva, poi gli spruzzi e il pannello.
  - Errore: l'acqua avanza fluida fino al punto sbagliato, parte l'effetto con il messaggio, poi il livello ricomincia.
- **Fonti e arrivi** (ErNa-004) sono disegni (4 per tipo) scelti dal seed del livello; non ruotano mai, hanno un'ombra morbida sotto e il moncone di tubo verso l'apertura entra sotto l'oggetto. La fonte ha le parti colorate nel colore della sua acqua (maschera), l'arrivo ha sotto un alone del suo colore (`PlumberConfig`). Dettagli in `docs/grafica.md`.
