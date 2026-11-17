# Resolver Master — linee guida tecniche

## 1. La regola del Core
- Tutto ciò che può servire a più minigiochi sta nel **Core** (`Assets/Scripts/Core/`): griglia, livelli, mosse, stelle, Storia, Challenge, salvataggio, lingue, UI kit, icone, audio, pubblicità.
- Ogni minigioco (`Assets/Scripts/Games/<Nome>/`) contiene **solo** le sue regole, i suoi pezzi, il suo generatore/risolutore e la sua grafica.
- Se una meccanica nata per un gioco può servire ad altri, va **spostata** nel Core, **mai copiata**.
- Il Core non conosce i giochi: l'assembly `ResolverMaster.Core` non referenzia `ResolverMaster.Games`. Ogni gioco si registra con una sottoclasse di `MinigameModule` (trovata per reflection da `GameRegistry.Discover`; `Assets/link.xml` la protegge dallo stripping IL2CPP).

## 2. Assembly
| Assembly | Cartella | Note |
|---|---|---|
| `ResolverMaster.Core` | `Assets/Scripts/Core/` | runtime; riferimenti: Input System, uGUI |
| `ResolverMaster.Games` | `Assets/Scripts/Games/` | runtime; riferisce solo il Core |
| `ResolverMaster.Editor` | `Assets/Scripts/Editor/` | solo Editor: menu, setup, validatore, build, anteprime |
| `ResolverMaster.Tests.EditMode` | `Assets/Tests/EditMode/` | solo test di logica |

## 3. Logica in classi C# pure
- Le regole (lingua iniziale, tabelle di testo, registro, in futuro mosse/stelle/generatori) stanno in classi senza `MonoBehaviour` e, quando possibile, senza tipi Unity, così si testano in EditMode.
- I `MonoBehaviour` sono adattatori sottili (input, rendering, ciclo di vita).
- Eccezione accettata: il salvataggio usa `JsonUtility` (funziona anche in EditMode).

## 4. Parametri in un unico file per sistema
- Regole comuni dei rompicapi (stelle): **`PuzzleConfig`** (`Assets/Resources/ResolverMaster/PuzzleConfig.asset`). Temi dei capitoli (Core): **`ThemeConfig`** (`Assets/Resources/ResolverMaster/ThemeConfig.asset`). Idraulico: **`PlumberConfig`** (`Assets/Resources/Games/Plumber/PlumberConfig.asset`: colori dell'acqua e aloni degli arrivi, spessori, ombre, velocità dell'acqua, durate, generatore, capitoli con il loro tema, tabella di difficoltà). Gli asset mancanti si creano con *Setup > Ensure Config Assets* (valori predefiniti dalle classi; gli asset esistenti non vengono toccati).
- UI: **`UiKitStyle`** (`Assets/Resources/ResolverMaster/UiKitStyle.asset`): colori, misure, pressione dei pulsanti, Home, dimensioni e contorno dei testi, durate delle animazioni, font TMP, materiali, sprite di Gemini e generati. Nessun colore o misura sparso nelle schermate.
- Se un nuovo stile cambia molti valori predefiniti: `ConfigAssets.ResetVisualDefaultsBatch` riscrive `UiKitStyle`, `ThemeConfig` e `PlumberConfig` con i valori delle classi (controllare prima con git che non ci siano modifiche a mano).
- Ogni sistema futuro (Storia, Challenge, ogni minigioco) avrà il suo unico file di parametri.

## 5. Testi e lingue
- **Nessun testo nel codice**: solo chiavi. Le chiavi del Core sono costanti in `LocKeys`; i nomi dei giochi sono in `GameInfo.NameKey`.
- Tabelle: `Assets/Resources/Localization/<it|en>/*.txt`, formato `chiave = valore` (UTF-8, `#` commento, `\n` a capo). Il Core ha `core.txt`, ogni gioco il suo file.
- Chiave mancante: a schermo compare la chiave; il validatore segnala chiavi mancanti in una lingua o usate dal codice e non definite.
- Primo avvio: italiano se il telefono è in italiano, altrimenti inglese (`LanguagePicker`). La lingua si salva solo quando l'utente la sceglie nelle Impostazioni.

## 6. Salvataggio
- Un file JSON (`persistentDataPath/save.json`) con **`version`** e **blocchi opzionali** (`settings` ora; un blocco per i progressi di ogni gioco in futuro).
- Chiavi sconosciute ignorate, blocchi o campi mancanti con i valori predefiniti, file illeggibile = valori predefiniti (mai crash).
- Scrittura sicura: prima `save.json.tmp`, poi `File.Replace`.
- Nuovo campo = nuovo campo con valore predefinito, senza cambiare versione. Aumenta `SaveData.CurrentVersion` solo se i dati vecchi vanno convertiti, e aggiungi il passo in `SaveSerializer.Migrate`.
- Gli id dei giochi (`GameInfo.Id`) non si cambiano mai dopo l'uscita: sono le chiavi dei blocchi di salvataggio.

## 7. Casualità con seed
- Ogni generatore (livelli Challenge, ecc.) riceve un **seed** esplicito e usa un proprio **`SeededRandom(seed)`** del Core (PCG32, solo aritmetica intera: stesso risultato su ogni piattaforma); mai `UnityEngine.Random` né `System.Random` nella logica. Stesso seed = stesso livello (riproducibile nei test e nei report dei bug).
- I livelli della Storia si generano **una volta** nell'Editor e si salvano come dati; il telefono non li rigenera. Il validatore avvisa se il generatore non riproduce più un livello dal suo seed.

## 7b. Rompicapi
- Ogni gioco implementa le sue regole su `PuzzleBase<TLevel, TMove>` (carica, mossa, vittoria, annulla, ricomincia; mosse fatte = mosse sulla pila di annullamento) e la sua board su `PuzzleBoard`. Schermate, contatori, stelle e salvataggio sono del Core.
- Griglia: solo tramite `IGridTopology` (celle intere, direzioni in senso orario, `RotateMask`); oggi `SquareTopology`, con **colonne e righe diverse** (ErNa-005). Le board riportano `GridColumns` e `GridRows` e usano celle quadrate grandi il più possibile nell'area, centrate.
- Stelle: 3 al minimo, 2 fino a minimo + max(30% arrotondato per difetto, 2), 1 altrimenti (`StarRules`).
- Difficoltà: `DifficultyTable` di gradini (livelli coperti + intervalli min/max con chiavi scelte dal gioco). Ogni livello salvato deve rispettare il suo gradino (controllato dal validatore).
- Mosse minime: calcolate da un risolutore esatto del gioco, mai stimate; il validatore le ricalcola.
- **Capitoli (Core, `Core/Puzzle/Story.cs`)**:
  - La Storia di ogni gioco è uno `StoryBook` di capitoli da **9 livelli** (`StoryBook.LevelsPerChapter`), numerati di seguito (1, 2, … 27).
  - Ogni capitolo ha nome tradotto e tema (`ChapterTheme`: chiave, icona, colore) nel file di parametri del gioco.
  - Sblocco (`StoryProgress`): il livello successivo si sblocca risolvendo il precedente; il capitolo successivo si sblocca finendo tutti i 9 livelli del precedente, con qualsiasi numero di stelle.
  - Salvataggio per **id stabile** `LevelId` (gioco, capitolo, indice nel capitolo).
  - Difficoltà a dente di sega: sale dentro il capitolo e riparte più bassa quando arriva la novità; i primi 2 livelli di un capitolo nuovo sono facili.
- **Pulsante d'azione (Core, opzionale)**:
  - Una board che lo vuole dichiara `MainAction` (chiave del testo + icona) e riceve `RunMainAction()`. Esempio: "Apri l'acqua" dell'Idraulico.
  - Il pulsante sta sopra la barra in basso. Con `IsBusy` la board lo disattiva durante le animazioni, insieme ad annulla e ricomincia.
  - I messaggi brevi (`MessageRequested(chiave)`) compaiono al posto del pulsante. Durate in `UiKitStyle` (`toastHold`, `toastFade`).
- Una vittoria che non nasce da una mossa (es. aprire l'acqua) usa `PuzzleBase.RefreshSolved()`; l'azione non conta come mossa.
- **Temi dei capitoli (Core, ErNa-005)**: ogni capitolo nomina un tema di `ThemeConfig` (`ChapterTheme.theme`); senza tema o con id sconosciuto vale il tema predefinito (`ThemeCatalog`). La grafica del tema è generata da `ThemeArt` (seed del tema) finché non arrivano le immagini (`VisualTheme.*Image`). Nei terreni niente blu né arancione forti.
- **Animazioni che seguono un percorso** (es. l'acqua): i tempi (in che ordine e quando si raggiunge ogni punto) si calcolano nella logica pura; la vista disegna da quei tempi con il tempo reale (`Time.unscaledDeltaTime`), mai a passi per fotogramma.

## 8. Pooling
- Gli oggetti creati spesso durante il gioco (pezzi, celle, effetti) si riusano da un pool (`ComponentPool<T>` del Core); niente `Instantiate`/`Destroy` per mossa. Le schermate sono create una volta all'avvio e poi attivate/disattivate.

## 9. UI
- uGUI, una sola scena `Main`, schermate come pannelli gestiti da `ScreenManager` (pila; tasto Indietro di Android = `Keyboard.escapeKey` del nuovo Input System; Indietro sulla Home chiude l'app).
- Canvas Scaler: riferimento 1080x1920, modalità *Expand* (da 9:16 a 9:21 la larghezza resta 1080 unità e l'altezza cresce fino a 2520).
- Ogni schermata ha sfondo a tutto schermo e contenuto dentro `SafeArea`.
- Gli elementi si costruiscono con `UiFactory` dallo stile. Gli sprite a 9 fette si rigenerano da *Resolver Master > UI Kit > Regenerate Sprites*.
- **Stile (ErNa-005)**: pulsanti, pannelli, caselle, stelle e icone sono disegni di Gemini (`Assets/Art/UI`, `Tools/Art/make_ui_art.py`); dal codice restano solo la cornice colorata delle schede della Home e le pillole sottili (`UiKitGenerator`). Pulsanti: `PrimaryButton` (pillola verde, testo bianco con contorno), `SecondaryButton` (pillola crema), `IconButton` (rotondo crema o giallo con icona di Gemini), `PanelButton`, `CardButton` (scheda della Home), `Panel` (finestra crema), `Title`. Ogni pulsante è un `PressableButton`: premuto scende di `pressDrop` e si scurisce; disattivato con `UiFactory.SetInteractable` (sprite desaturati, testi sbiaditi).
- **Testi solo TextMesh Pro** (`TextMeshProUGUI`, Fredoka SDF statico); il contorno è il materiale SDF `fontBoldOutlined`, mai copie del testo. Un carattere nuovo va aggiunto a `UiArtSetup.Charset`.
- **Mai ingrandire la grafica di progetto**: le pillole non sono mai più alte del disegno (159 px; `SlicedFit` tiene la proporzione verticale), il nastro si disegna alla sua misura. Le anteprime segnalano ogni sprite di `Assets/` disegnato più grande della sua risoluzione.
- Un `Graphic` scritto da noi (es. `WaterGraphic`) ha `[ExecuteAlways]` (non si eredita) e un `CanvasRenderer` aggiunto prima del componente, altrimenti non si disegna nelle anteprime.
- **Mai blu né arancione** come colori dell'interfaccia e dei terreni (sono i colori dell'acqua). Pulsanti toccabili di almeno `minTouchSize` (130 px): le anteprime lo misurano.
- Grafica, cartelle e script: `docs/grafica.md`.
- Un `LayoutElement` dentro un layout group ha `flexibleHeight = 0` (vedi `UiFactory.Layout`): un layout group annidato con *force expand* altrimenti si allunga fino a riempire lo schermo.
- Un'Image a 9 fette con layout group sullo stesso oggetto riporta come altezza preferita la somma dei bordi dello sprite: fissa l'altezza con un `LayoutElement`.
- Icone: quelle di Gemini (`icon_*`, a colori, `UiFactory.Icon`); le poche ancora segnaposto sono maschere Material in `Assets/Art/Icons/` (`UiFactory.MaskIcon`, colore `UiKitStyle.icon`). La `IconLibrary` le contiene tutte (*UI Kit > Rebuild Sprite Libraries*).
- Librerie di sprite (`IconLibrary`, Core): icone, schede della Home (`Assets/Art/Cards`, `CardLibrary`) e disegni dei giochi (es. `PlumberArt`). Una nuova cartella di disegni = una riga in `UiKitAssets.Libraries` (e in `ArtImportRules.IllustrationDirs`).
- Home: una scheda per gioco (`GameInfo.CardImage`, `GameInfo.CardColor`); immagine mancante = segnaposto e warning del validatore. Logica di scorrimento in `CardCarousel` (C# puro).
- I popup a tutto schermo (pannello di vittoria) stanno sotto un livello scuro con `ModalOverlay`: le misure delle anteprime considerano solo il popup.
- Le grafiche disegnate apposta sopra altre (alone sotto un arrivo, fontana sul suo moncone, riflessi, schizzi) hanno il componente `Decoration`. Le misure non segnalano le loro sovrapposizioni né l'uscita dal contenitore; restano controllati la posizione sullo schermo e lo sprite. Con `mayLeaveScreen` (schede laterali della Home) non si controlla neanche lo schermo. Una `Decoration` disattivata non conta.
- Nessuna animazione deve dipendere da `Awake`/`Start`/coroutine per lo stato finale: ogni board offre `SnapVisuals()` (le anteprime girano in edit mode, dove `Update` non avanza).
- Font: Fredoka (OFL), `Assets/Art/Fonts/`. Ogni nuovo asset di terze parti va in `docs/crediti.md` con la licenza.

## 10. Validatore
- *Resolver Master > Validate*. Ogni classe che implementa `IValidationCheck` viene eseguita automaticamente: un gioco aggiunge i suoi controlli solo dichiarando una classe.
- Controlli attuali:
  - lingue: stesse chiavi in tutte le lingue, chiavi del codice presenti, righe malformate o duplicate;
  - minigiochi: id, nome e icona validi;
  - UI kit: tutti gli elementi di Gemini in `Assets/Art/UI`, risorse TMP, font e materiali, sprite dello stile, icone del Core;
  - temi: `ThemeConfig` presente, id unici, tema predefinito valido, nomi tradotti, niente blu/arancione forti nei terreni;
  - PuzzleConfig;
  - schede della Home: colore impostato (errore), immagine presente (solo warning);
  - disegni dell'Idraulico: 4 fonti con maschera non vuota e 4 arrivi, nei file e nella libreria;
  - Idraulico, sui parametri: colori e aloni, icone, nomi e **tema valido** dei capitoli, chiavi di `PlumberKeys`;
  - Idraulico, su ogni livello della Storia: leggibile e risolvibile con le regole ErNa-003; mosse minime salvate = risolutore; aprendo l'acqua all'inizio non è già risolto; valori nel gradino; ogni capitolo ha esattamente 9 livelli; nel capitolo 1 la soluzione non usa T e non ne ha bisogno; avviso se il seed non lo riproduce più.

## 11. Anteprime e misure
- *Resolver Master > Make Previews*, in IT ed EN, a 1080x1920 (9:16) e 1080x2520 (9:21), in `Previews/` (ignorata da git). La zona fuori dalla safe area simulata è tinta di rosso. Schermate:
  - Home con la scheda Idraulico e con la scheda Traffic jam al centro, scelta della modalità, Impostazioni, lista dei capitoli, griglia dei livelli di ogni capitolo (con il suo tema);
  - Gioco: un livello per capitolo prima dell'apertura (1: 3x4, 14: 5x7, 27: 6x8), acqua a metà percorso, apertura riuscita a due colori, apertura sbagliata per perdita nel momento dell'effetto, pannello di vittoria.
- `Previews/previews_report.txt` misura: margini della safe area, distanza minima del contenuto dai bordi sicuri, elementi fuori schermo o fuori safe area, testi tagliati, elementi sovrapposti, elementi che escono dal loro contenitore, Image senza sprite.
- Su ogni schermata misura anche il pulsante più piccolo (errore sotto `minTouchSize`), i testi TMP che escono dal loro rettangolo e la scala massima degli sprite (errore se uno sprite di `Assets/` è ingrandito; quelli generati sono solo riportati). Nella schermata di gioco: celle (colonne x righe e lato), spazio libero sopra e sotto la griglia, disegni di fonti e arrivi, distanze da bordi e barre, posizione del pulsante d'azione.
- Safe area simulata: 9:16 nessun ritaglio; 9:21 120 px in alto (foro/notch) e 72 px in basso (barra dei gesti).
- In Editor gli asset usati dalle anteprime si caricano **dopo** `NewScene(Single)`, che scarica gli asset non referenziati.

## 12. Build
- Android: IL2CPP, solo ARM64, target API 36, minimo API 26, solo portrait (capovolto disattivato), rapporto massimo 2.4. Le librerie native sono allineate a 16 KB dall'NDK r27c incluso in Unity 6000.0.85f1 (nessun plugin nativo nostro).
- Pacchetto `com.eebbproduction.resolvermaster`, prodotto *Resolver Master*, azienda `eebbproduction`. Impostazioni applicate da *Resolver Master > Setup > Apply Project Settings*.
- Firma **solo** da variabili d'ambiente: `RESOLVERMASTER_KEYSTORE` (percorso fuori dal progetto), `RESOLVERMASTER_KEYSTORE_PASS`, `RESOLVERMASTER_KEY_ALIAS`, `RESOLVERMASTER_KEY_PASS`. Se ne manca una la build si ferma e dice quale (mai le password). Dopo la build i campi di firma vengono svuotati.
- Output: `Builds/Release/ResolverMaster-<versione>-<code>.aab` e `...-test.apk` (ignorati da git). Prima di ogni caricamento su Google Play: *Bump Version Code*.

## Changelog
- ErNa-001: prima stesura (scheletro del progetto).
- ErNa-002: basi dei rompicapi nel Core (§7b), `SeededRandom` al posto di `System.Random` (§7), file di parametri `PuzzleConfig`/`PlumberConfig` (§4), livelli Storia generati e salvati, popup con `ModalOverlay`, `SnapVisuals` per le anteprime.
- ErNa-004: stile cartoon del UI kit (sprite con contorno e profondità, `PressableButton`, niente blu/arancione), Home a schede (`CardCarousel`, blocco `home` del salvataggio), librerie di sprite generiche, fonti e arrivi disegnati scelti dal seed (`ArtVariants`), nuovi controlli e misure (§9, §10, §11).
- ErNa-005: interfaccia di Gemini e TextMesh Pro (§9), pulsanti premuti/disattivati, temi dei capitoli nel Core (§4, §7b), griglie rettangolari (§7b), acqua continua con tempi nella logica (§7b), nuovi controlli e misure (§10, §11).
- ErNa-003: capitoli da 9 livelli, sblocco e id stabili nel Core (§7b); pulsante d'azione opzionale e messaggi brevi nella schermata di gioco (§7b); `Decoration` (§9); nuove regole e controlli dell'Idraulico (§10, `docs/idraulico-regole.md`); anteprime aggiornate (§11).
