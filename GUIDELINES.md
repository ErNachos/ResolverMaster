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
- Ogni generatore (livelli Challenge, ecc.) riceve un **seed** esplicito e usa un proprio `System.Random(seed)`; mai `UnityEngine.Random` globale nella logica. Stesso seed = stesso livello (riproducibile nei test e nei report dei bug).

## 8. Pooling
- Gli oggetti creati spesso durante il gioco (pezzi, celle, effetti) si riusano da un pool; niente `Instantiate`/`Destroy` per mossa. Le schermate sono create una volta all'avvio e poi attivate/disattivate.

## 9. UI
- uGUI, una sola scena `Main`, schermate come pannelli gestiti da `ScreenManager` (pila; tasto Indietro di Android = `Keyboard.escapeKey` del nuovo Input System; Indietro sulla Home chiude l'app).
- Canvas Scaler: riferimento 1080x1920, modalità *Expand* (da 9:16 a 9:21 la larghezza resta 1080 unità e l'altezza cresce fino a 2520).
- Ogni schermata ha sfondo a tutto schermo e contenuto dentro `SafeArea`.
- Gli elementi si costruiscono con `UiFactory` dallo stile. Gli sprite a 9 fette si rigenerano da *Resolver Master > UI Kit > Regenerate Sprites*.
- Un `LayoutElement` dentro un layout group ha `flexibleHeight = 0` (vedi `UiFactory.Layout`): un layout group annidato con *force expand* altrimenti si allunga fino a riempire lo schermo.
- Un'Image a 9 fette con layout group sullo stesso oggetto riporta come altezza preferita la somma dei bordi dello sprite: fissa l'altezza con un `LayoutElement`.
- Icone: Google Material Symbols (Apache 2.0) generate da `Tools/Art/make_icons.py` in `Assets/Art/Icons/` (maschere bianche colorate dalla UI). Per aggiungerne una: una riga in `ICONS`, rilancia lo script, poi *UI Kit > Rebuild Icon Library*.
- Font: Fredoka (OFL), `Assets/Art/Fonts/`. Ogni nuovo asset di terze parti va in `docs/crediti.md` con la licenza.

## 10. Validatore
- *Resolver Master > Validate*. Ogni classe che implementa `IValidationCheck` viene eseguita automaticamente: un gioco aggiunge i suoi controlli solo dichiarando una classe.
- Controlli attuali: lingue (stesse chiavi in tutte le lingue, chiavi del codice presenti, righe malformate/duplicate), minigiochi (id, nome e icona validi), UI kit (stile, font, sprite, icone del Core).

## 11. Anteprime e misure
- *Resolver Master > Make Previews*: Home e Impostazioni, IT ed EN, a 1080x1920 (9:16) e 1080x2520 (9:21), in `Previews/` (ignorata da git). La zona fuori dalla safe area simulata è tinta di rosso.
- `Previews/previews_report.txt` misura: margini della safe area, distanza minima del contenuto dai bordi sicuri, elementi fuori schermo o fuori safe area, testi tagliati, elementi sovrapposti, elementi che escono dal loro contenitore, Image senza sprite.
- Safe area simulata: 9:16 nessun ritaglio; 9:21 120 px in alto (foro/notch) e 72 px in basso (barra dei gesti).
- In Editor gli asset usati dalle anteprime si caricano **dopo** `NewScene(Single)`, che scarica gli asset non referenziati.

## 12. Build
- Android: IL2CPP, solo ARM64, target API 36, minimo API 26, solo portrait (capovolto disattivato), rapporto massimo 2.4. Le librerie native sono allineate a 16 KB dall'NDK r27c incluso in Unity 6000.0.85f1 (nessun plugin nativo nostro).
- Pacchetto `com.eebbproduction.resolvermaster`, prodotto *Resolver Master*, azienda `eebbproduction`. Impostazioni applicate da *Resolver Master > Setup > Apply Project Settings*.
- Firma **solo** da variabili d'ambiente: `RESOLVERMASTER_KEYSTORE` (percorso fuori dal progetto), `RESOLVERMASTER_KEYSTORE_PASS`, `RESOLVERMASTER_KEY_ALIAS`, `RESOLVERMASTER_KEY_PASS`. Se ne manca una la build si ferma e dice quale (mai le password). Dopo la build i campi di firma vengono svuotati.
- Output: `Builds/Release/ResolverMaster-<versione>-<code>.aab` e `...-test.apk` (ignorati da git). Prima di ogni caricamento su Google Play: *Bump Version Code*.

## Changelog
- ErNa-001: prima stesura (scheletro del progetto).
