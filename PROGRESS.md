# PROGRESS — Resolver Master

## Update ErNa-001 — Struttura base (2026-11-17)

**Fatto**
- Progetto Unity 6000.0.85f1 creato da zero: Resolver Master / eebbproduction / `com.eebbproduction.resolvermaster`, v1.0.0 (code 1); IL2CPP, solo ARM64, API 26–36, solo portrait, rapporto massimo 2.4, solo nuovo Input System, uGUI. Impostazioni applicate da codice (`ProjectSetup`).
- Cartelle, 4 assembly (Core, Games, Editor, Tests), `.gitignore`, `link.xml`.
- Core: lingue (IT/EN, tabelle in file, classe pura `Localizer` + `LanguagePicker`), salvataggio JSON versionato a blocchi con scrittura sicura, registro dei minigiochi per reflection (Idraulico e Traffic jam, "Prossimamente"), UI kit (`UiKitStyle`, sprite a 9 fette generati, `IconLibrary`, `UiFactory`, `SafeArea`), `ScreenManager` con tasto Indietro di Android.
- Schermate: Home (titolo, schede dei giochi, Impostazioni), Scelta modalità (Storia/Challenge con messaggio segnaposto), Impostazioni (lingua, suono, vibrazione; salvate subito).
- Icone Material Symbols (Apache 2.0) generate da `Tools/Art/make_icons.py`; font Fredoka (OFL).
- Menu `Resolver Master/`: Validate, Build Release AAB, Build Release APK (test), Bump Version Code, Make Previews, Setup/*, UI Kit/*; ognuno con variante batch.
- Validatore a controlli registrabili (lingue, minigiochi, UI kit).
- 5 test EditMode (lingua iniziale, lingua salvata, salvataggio andata/ritorno, chiavi sconosciute/blocchi mancanti, bump del version code).
- Documenti: CLAUDE.md, GUIDELINES.md, docs/architettura.md, docs/crediti.md.

**Risultati**
- Test: 5/5 verdi. Validatore: OK (3 controlli, 0 errori, 0 warning). Compilazione: 0 errori, 0 warning.
- Anteprime: `Previews/{home,settings}_{it,en}_{1080x1920,1080x2520}.png` + `previews_report.txt`, nessun problema misurato.
- Safe area simulata: 9:16 margini 0; 9:21 120 px in alto e 72 px in basso. Distanza minima del contenuto dal bordo sicuro: 48 px ai lati (Home), 76/88 px (Impostazioni).
- Bug visivi trovati e corretti con le misure: icone senza sprite nelle anteprime (asset caricati prima di `NewScene`), badge "Prossimamente" fuori dalla scheda (altezza preferita dello sprite a 9 fette), pulsanti lingua allungati (flexibleHeight implicito). Le misure ora controllano anche Image senza sprite ed elementi fuori dal contenitore.
- Build: non eseguite, mancano le variabili d'ambiente della firma (la build si ferma con il messaggio corretto, verificato in batch).

**Da provare a mano sul telefono**
- Tasto Indietro di Android (Impostazioni/Modalità → Home; Home → chiude l'app).
- Lingua al primo avvio con il telefono in italiano e in un'altra lingua; cambio lingua dalle Impostazioni e riavvio.
- Safe area su un telefono con notch/foro.

**Problemi aperti / prossimi passi**
- Repository GitHub `ErNachos/ResolverMaster` non trovato dall'account autenticato: commit locale fatto, push non eseguito (ripreso in ErNa-002).
- Suono e vibrazione sono solo impostazioni salvate (nessun audio/vibrazione ancora).
- Prossimi Update: primo minigioco (Idraulico) con griglia/livelli/mosse/stelle nel Core.

## Update ErNa-002 — Idraulico: primo giocabile (2026-11-17)

**Fatto**
- Core, basi dei rompicapi (C# puro, in `Core/Puzzle/`): griglia astratta `IGridTopology` + `SquareTopology`, `SeededRandom` (PCG32, uguale su ogni piattaforma), contratto `IPuzzle`/`PuzzleBase` (carica, mossa, vittoria, annulla, ricomincia, mosse fatte/minime), `UndoStack`, `StarRating` + `StarRules` (in `PuzzleConfig.asset`), `DifficultyTable` generica, `BucketQueue` per le ricerche, `GameMode`/`LevelRequest` (pronto per la Challenge).
- Core, schermate: Scelta livello (numero, stelle, lucchetto, sblocco progressivo), Gioco (barra in alto con indietro / livello / mosse-minimo; area del puzzle adattata a schermo e safe area; barra in basso annulla / ricomincia / aiuto disattivato), pannello di vittoria (stelle animate, mosse e minimo, Ripeti / Avanti). `PuzzleBoard` (base della board di un gioco), `ComponentPool`, `ModalOverlay`.
- Salvataggio: nuovo blocco opzionale `progress` (stelle migliori per gioco, modalità e livello).
- Idraulico: pezzi (dritto, curva, T, croce, fonte, arrivo; colore e blocco già nel modello), acqua, `PlumberPuzzle`, risolutore esatto (A*), generatore da seed, codec testuale dei livelli, `PlumberConfig.asset` (colori, spessori, durate, generatore, tabella del capitolo 1), grafica generata da codice, rotazione animata con tocchi accodati, acqua cella per cella, riempimento dell'arrivo alla vittoria, celle in pool.
- Capitolo 1: 10 livelli generati da seed fissi (`Assets/Resources/Games/Plumber/plumber_story.json`), tabella in `docs/idraulico-livelli.md`. Menu `Resolver Master/Idraulico/Genera livelli Storia` e `Setup/Ensure Config Assets` (con varianti batch).
- Validatore: controlli `PuzzleConfig` e `Plumber` (risolvibile, mosse minime = risolutore, non già risolto, valori del gradino, avviso se il seed non riproduce il livello).
- Anteprime: aggiunte scelta livello, livello 1 all'inizio, livello 8 con acqua parziale, pannello di vittoria; misure della griglia nel report.
- 5 nuovi test EditMode (rotazioni, acqua, risolutore con simmetrie, generatore, stelle).

**Modifiche a parti di ErNa-001 (servivano al Core)**
- `MinigameModule`: nuovi hook `StoryLevelCount` e `CreateBoard`. `GameRegistry` tiene i moduli (`FindModule`). Idraulico ora "Disponibile".
- `ModeSelectScreen`: Storia apre la scelta del livello se il gioco ha livelli (Challenge resta segnaposto). `ScreenLayout.Create` ha il parametro `bodyColumn`. `UiKitStyle`: misure, colori e durate delle nuove schermate. `AppServices.Puzzle`. `PreviewMaker`: scatti generalizzati, popup misurati da soli. `ProjectSetup.ApplyAll` crea anche gli asset di configurazione.

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 10/10 verdi. Validatore: OK (5 controlli, 0 errori, 0 warning).
- Livelli (griglia, mosse minime): 1–3 3x3 → 1, 2, 3; 4–7 4x4 → 3, 3, 5, 6; 8–10 5x5 con T → 7, 8, 9.
- Generatore 5x5: 0,4 ms in media su 30 seed (max 1,5 ms), 17 tentativi in media.
- Anteprime: 24 immagini, nessun problema misurato. Celle: 317,3 px (3x3), 190,4 px (5x5). Griglia a 64 px dai bordi dello schermo (e della safe area); dalle barre 292 px (9:16) e 496 px (9:21).

**Da provare a mano sul telefono**
- I 10 livelli dall'inizio alla fine: stelle salvate, sblocco, Avanti dopo il livello 10 torna alla scelta del livello.
- Tocchi rapidi durante la rotazione (si accodano), Annulla durante l'animazione, Indietro Android dalla schermata di gioco.

**Problemi aperti / prossimi passi**
- Push di ErNa-001 e di questo Update non eseguito da Claude: il comando `git push` è stato bloccato dai permessi della sessione (vedi report).
- Risolutore: una sola coppia fonte/arrivo; i colori multipli richiedono un'estensione quando arriveranno i loro livelli.
- Aiuto, Challenge, suoni e vibrazione: prossimi Update.
