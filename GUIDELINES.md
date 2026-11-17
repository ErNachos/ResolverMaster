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
- Regole comuni dei rompicapi (stelle): **`PuzzleConfig`** (`Assets/Resources/ResolverMaster/PuzzleConfig.asset`). Idraulico: **`PlumberConfig`** (`Assets/Resources/Games/Plumber/PlumberConfig.asset`: colori, spessori, durate, generatore, tabella di difficoltà). Gli asset mancanti si creano con *Setup > Ensure Config Assets* (valori predefiniti dalle classi; gli asset esistenti non vengono toccati).
- UI: **`UiKitStyle`** (`Assets/Resources/ResolverMaster/UiKitStyle.asset`): colori, raggi, spessori, misure, dimensioni del testo, durate delle animazioni, font, sprite generati. Nessun colore o misura sparso nelle schermate.
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
- Griglia: solo tramite `IGridTopology` (celle intere, direzioni in senso orario, `RotateMask`); oggi `SquareTopology`.
- Stelle: 3 al minimo, 2 fino a minimo + max(30% arrotondato per difetto, 2), 1 altrimenti (`StarRules`).
- Difficoltà: `DifficultyTable` di gradini (livelli coperti + intervalli min/max con chiavi scelte dal gioco). Ogni livello salvato deve rispettare il suo gradino (controllato dal validatore).
- Mosse minime: calcolate da un risolutore esatto del gioco, mai stimate; il validatore le ricalcola.

## 8. Pooling
- Gli oggetti creati spesso durante il gioco (pezzi, celle, effetti) si riusano da un pool (`ComponentPool<T>` del Core); niente `Instantiate`/`Destroy` per mossa. Le schermate sono create una volta all'avvio e poi attivate/disattivate.

## 9. UI
- uGUI, una sola scena `Main`, schermate come pannelli gestiti da `ScreenManager` (pila; tasto Indietro di Android = `Keyboard.escapeKey` del nuovo Input System; Indietro sulla Home chiude l'app).
- Canvas Scaler: riferimento 1080x1920, modalità *Expand* (da 9:16 a 9:21 la larghezza resta 1080 unità e l'altezza cresce fino a 2520).
- Ogni schermata ha sfondo a tutto schermo e contenuto dentro `SafeArea`.
- Gli elementi si costruiscono con `UiFactory` dallo stile. Gli sprite a 9 fette si rigenerano da *Resolver Master > UI Kit > Regenerate Sprites*.
- Un `LayoutElement` dentro un layout group ha `flexibleHeight = 0` (vedi `UiFactory.Layout`): un layout group annidato con *force expand* altrimenti si allunga fino a riempire lo schermo.
- Un'Image a 9 fette con layout group sullo stesso oggetto riporta come altezza preferita la somma dei bordi dello sprite: fissa l'altezza con un `LayoutElement`.
- Icone: Google Material Symbols (Apache 2.0) generate da `Tools/Art/make_icons.py` in `Assets/Art/Icons/` (maschere bianche colorate dalla UI). Per aggiungerne una: una riga in `ICONS`, rilancia lo script, poi *UI Kit > Rebuild Icon Library*.
- I popup a tutto schermo (pannello di vittoria) stanno sotto un livello scuro con `ModalOverlay`: le misure delle anteprime considerano solo il popup.
- Nessuna animazione deve dipendere da `Awake`/`Start`/coroutine per lo stato finale: ogni board offre `SnapVisuals()` (le anteprime girano in edit mode, dove `Update` non avanza).
- Font: Fredoka (OFL), `Assets/Art/Fonts/`. Ogni nuovo asset di terze parti va in `docs/crediti.md` con la licenza.

## 10. Validatore
- *Resolver Master > Validate*. Ogni classe che implementa `IValidationCheck` viene eseguita automaticamente: un gioco aggiunge i suoi controlli solo dichiarando una classe.
- Controlli attuali: lingue (stesse chiavi in tutte le lingue, chiavi del codice presenti, righe malformate/duplicate), minigiochi (id, nome e icona validi), UI kit (stile, font, sprite, icone del Core), PuzzleConfig, Idraulico (ogni livello della Storia leggibile, risolvibile, non già risolto, mosse minime salvate = risolutore, valori nel gradino; avviso se il seed non lo riproduce più).

## 11. Anteprime e misure
- *Resolver Master > Make Previews*: Home, Impostazioni, Scelta livello, Gioco (livello 1 all'inizio, livello 8 con acqua parziale, pannello di vittoria), IT ed EN, a 1080x1920 (9:16) e 1080x2520 (9:21), in `Previews/` (ignorata da git). La zona fuori dalla safe area simulata è tinta di rosso.
- `Previews/previews_report.txt` misura: margini della safe area, distanza minima del contenuto dai bordi sicuri, elementi fuori schermo o fuori safe area, testi tagliati, elementi sovrapposti, elementi che escono dal loro contenitore, Image senza sprite; nella schermata di gioco dimensione delle celle e distanza della griglia da bordi e barre.
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
