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

## Update ErNa-003 — Idraulico: "Apri l'acqua", colori e capitoli (2026-11-17)

**Fatto**
- **Git:** all'inizio i commit di ErNa-001 ed ErNa-002 erano già su GitHub (0 commit avanti, 0 indietro). Il problema aperto di ErNa-002 è risolto.
- **Core, capitoli** (`Core/Puzzle/Story.cs`, C# puro):
  - `LevelId` (gioco, capitolo, indice), `StoryChapter`, `StoryBook` (capitoli da 9, livelli numerati di seguito), `StoryProgress` (sblocco e stelle del capitolo), `ChapterTheme` (nome, icona, colore).
  - Salvataggio: `progress` ora è diviso per capitolo, con id stabili. I progressi salvati con i vecchi livelli vengono ignorati, perché siamo ancora in sviluppo.
  - Schermate nuove: **lista dei capitoli** (nome, stelle su 27, lucchetto) e **griglia 3x3 dei livelli** di un capitolo, che sostituisce la vecchia scelta del livello.
  - "Avanti" passa al capitolo successivo solo quando è sbloccato.
- **Core, pulsante d'azione** (opzionale):
  - `PuzzleBoard.MainAction`, `RunMainAction`, `IsBusy`, `MessageRequested`.
  - Nella `GameScreen` il pulsante sta sopra la barra in basso ed è disattivato durante le animazioni (anche annulla e ricomincia).
  - Il messaggio breve compare al suo posto. Senza `MainAction` (Traffic jam) il pulsante non c'è e l'area del puzzle si allunga.
  - Nuovi anche: `PuzzleBase.RefreshSolved()` (vittoria che non nasce da una mossa) e `Decoration` (grafiche sovrapposte di proposito, escluse da quelle misure).
- **Idraulico, regole** (`docs/idraulico-regole.md`):
  - Prima si collega, poi "Apri l'acqua", che non conta come mossa. L'acqua parte da tutte le fonti insieme.
  - `WaterFlow` riconosce perdita, colori mescolati, colore sbagliato e fontana asciutta, con cella, direzione e momento dell'errore.
  - Un tentativo sbagliato fa ripartire il livello: mosse a 0, annulla svuotato.
- **Idraulico, risolutore esatto** (rete chiusa con più fonti, più arrivi e colori, simmetrie; IDA\*): confrontato con una ricerca che prova tutte le rotazioni su 32.132 griglie piccole casuali (487 risolvibili): 0 differenze.
- **Idraulico, generatore**: percorso singolo (cap. 1, T solo come inganno), bivio con T (cap. 2), due percorsi di colori diversi (cap. 3).
- **Idraulico, livelli e parametri**:
  - Tabella a dente di sega dei 27 livelli in `PlumberConfig.storyDifficulty`.
  - Capitoli in `PlumberConfig.chapters`: "Prima goccia", "Il bivio", "Due colori".
  - Colori blu e arancione. Aloni degli arrivi: `sinkHaloColors`, `sinkHaloSize`.
  - `PlumberConfig.asset` è stato ricreato con i nuovi valori predefiniti; non c'erano modifiche a mano.
- **Idraulico, grafica**:
  - Tubi grigi durante il gioco; fonte colorata come la sua acqua; arrivo con alone colorato sotto.
  - Su richiesta dell'utente niente simboli su fonti e arrivi, solo colori.
  - L'acqua avanza cella per cella; le fontane si riempiono, poi compare il pannello.
  - Errori: schizzi per la perdita, celle che lampeggiano per i colori, fontana che lampeggia se asciutta; messaggio "Perdita!", "Colori mescolati!", "Colore sbagliato!" o "Fontana asciutta!", poi ripartenza.
- **Icone nuove** (Material Symbols): `water_drop`, `fork`, `palette` per i capitoli; `faucet` per il pulsante.
- **Validatore Idraulico**:
  - Ogni livello è risolvibile con le nuove regole, ha le mosse minime uguali a quelle del risolutore, non è già risolto all'apertura e rispetta il suo gradino.
  - Ogni capitolo ha 9 livelli.
  - Nel capitolo 1 la soluzione non usa T e non ne ha bisogno.
  - Chiavi di testo, colori e icone presenti.
- **Anteprime nuove**: capitoli, griglia 3x3, un livello per capitolo, apertura riuscita a due colori, apertura sbagliata per perdita. Le misure includono il pulsante d'azione.
- **Test**:
  - 5 nuovi: perdita, colori, T, risolutore a due colori, sblocco dei capitoli.
  - 3 aggiornati alle nuove regole, non contano nel limite: `Water_HandMadeLevel`, `Solver_HandMadeLevels`, `Generator_SameSeedSameLevel`.

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 15/15 verdi. Validatore: OK (5 controlli, 0 errori, 0 warning).
- **Livelli** (mosse minime): cap. 1 → 1, 2, 3, 4, 5, 5, 7, 7, 9 (da 3x3 a 5x5); cap. 2 → 2, 4, 5, 5, 7, 8, 8, 10, 12 (da 4x4 a 5x5); cap. 3 → 2, 4, 6, 7, 7, 9, 10, 11, 11 (da 5x5 a 6x6). Tabella completa in `docs/idraulico-livelli.md`.
- **Generatore** 6x6 a due colori: 0,40 ms in media su 30 seed (max 2,0 ms), 8,9 tentativi in media.
- **Anteprime**: 40 immagini, nessun problema misurato. Le misure hanno trovato e fatto correggere un testo tagliato ("22/27" nella lista dei capitoli: campo da 116 px portato a 154 px).
- **Misure**:
  - Celle: 158,7 px (6x6), 190,4 px (5x5), 238 px (4x4), 317,3 px (3x3).
  - Griglia a 64 px dai bordi dello schermo.
  - "Apri l'acqua" (760x152 px), centrato sulla griglia: 9:16 → 216 px sotto la griglia e 24 px sopra la barra in basso; 9:21 → 420 px e 24 px.

**Da provare a mano sul telefono**
- I 27 livelli con le nuove regole: stelle salvate, sblocco nel capitolo, capitolo 2 sbloccato dopo il livello 9, "Avanti" dal livello 9 al livello 10.
- Gli effetti di errore e la ripartenza: tempi (`failEffect` 1,0 s + `failPause` 0,45 s) e leggibilità del messaggio.
- Tocchi rapidi seguiti subito da "Apri l'acqua": le rotazioni in corso si completano subito.

**Problemi aperti / prossimi passi**
- La croce resta con 4 aperture collegate; i flussi che si incrociano senza mescolarsi arriveranno con il capitolo 4.
- Senza simboli, fonti e arrivi si distinguono solo dal colore (scelta dell'utente). Da rivalutare per chi distingue male i colori.
- `ProjectSettings/ProjectSettings.asset` (formato di compressione delle texture Android, cambiato dall'editor) e la cartella `New Unity Project/` non sono state toccate né incluse nel commit.

## Update ErNa-004 — Grafica: fonti e arrivi veri, stile dei pulsanti, Home a schede (2026-11-17)

**Fatto**
- **Git:** `git pull --ff-only` già aggiornato. `Prompt/` e `ArtSource/` vanno nel repository con un commit separato (vedi "Problemi aperti").
- **Script `Tools/Art/make_plumber_art.py`** (rieseguibile, scrittura sicura): toglie lo sfondo bianco dai bordi, ritaglia e centra a 512 px, separa il magenta delle fonti in una maschera con le ombre.
  - Gli originali non erano in `Sources/`/`Targets/` ma in `ArtSource/Plumber/Fonte_1..4.png` e `Arrivo_1..4.png`: lo script legge entrambe le disposizioni, `ArtSource/` non è stata toccata.
  - Magenta trovato: fonte 1 45,6%, fonte 2 35,4%, fonte 3 17,3% (barile, solo le fasce), fonte 4 44,3% dell'oggetto. Nessun residuo rosa nell'immagine base (0 pixel).
- **Core, stile** (`UiKitStyle` ricreato con i nuovi valori, `UiKitGenerator`):
  - sprite con contorno nero spesso e fascia d'ombra in basso, versioni "premuto", pulsante rotondo; `PressableButton` (scende quando lo premi, colori spenti da disattivato);
  - `UiFactory`: `PrimaryButton` (verde, testo bianco con contorno), `SecondaryButton` (crema), `IconButton` (rotondo, icona scura), `CardButton`, `Panel`, `Title` (testo con contorno scuro);
  - palette senza blu né arancione (sfondo lavanda, crema, verde, lampone); anche i colori dei capitoli 1 e 3 (erano blu e arancione) ora sono giallo e viola;
  - applicato a Home, scelta modalità, Impostazioni, capitoli, livelli, gioco, pannello di vittoria, messaggi di errore.
- **Core, Home a schede**: una scheda per gioco (riquadro colorato, illustrazione o segnaposto, nome tradotto), scorrimento con il dito e aggancio al centro (`CardCarousel`, C# puro), schede laterali più piccole e trasparenti, pallini, GIOCA!/PLAY!, Impostazioni in basso a sinistra. Traffic jam: scheda attenuata con lucchetto e "Prossimamente", GIOCA! disattivato. Ultima scheda salvata nel nuovo blocco opzionale `home`.
- **Core, immagini**: `IconLibrary` è ora la libreria di sprite generica (icone, `CardLibrary` delle schede, `PlumberArt` dell'Idraulico); `GameInfo.CardImage`/`CardColor`; `ArtVariants` (disegno scelto dal seed del livello).
- **Idraulico**: fonti e arrivi disegnati (variante dal seed), maschera della fonte nel colore dell'acqua, alone sotto l'arrivo, moncone di tubo verso l'apertura, gocce alla vittoria; tubi con contorno nero, ombreggiatura e riflesso; acqua che scorre con un leggero movimento. Regole, risolutore, generatore e livelli **non toccati**.
- **Validatore**: 6 controlli. Nuovi: disegni dell'Idraulico (4 fonti con maschera non vuota, 4 arrivi), colore della scheda (errore) e immagine della scheda (warning), sprite e misure minime dei pulsanti nello stile.
- **Anteprime**: Home con Idraulico e con Traffic jam al centro, scelta modalità, Impostazioni, capitoli, livelli, livello 1 (3x3), livello 27 (6x6), apertura riuscita, perdita, vittoria. Nuove misure: disegni di fonti/arrivi, pulsante più piccolo.
- **Test**: 3 nuovi (varianti dal seed, aggancio delle schede, ultima scheda salvata/ripristinata).
- Documenti: `docs/grafica.md` (nuovo), `docs/architettura.md`, `docs/crediti.md`, `GUIDELINES.md`.

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 18/18 verdi. Validatore: OK (6 controlli, 0 errori, 2 warning: immagini delle schede mancanti, ammessi).
- Anteprime: 44 immagini, nessun problema misurato.
- **Misure**:
  - fonti e arrivi: 253,9 px nelle celle 3x3 (317,3 px), 126,9 px nelle 6x6 (158,7 px), cioè l'80% della cella;
  - pulsante più piccolo: 136x136 px (rotondi); bottoni della barra in basso 144 px;
  - "Apri l'acqua" → barra in basso: 48 px (9:16 e 9:21); dalla griglia 210 px (9:16) e 414 px (9:21);
  - nessun testo tagliato né sovrapposto, anche in inglese.
- Bug trovati dalle misure e dalle immagini e corretti: icona segnaposto di Traffic jam sotto lucchetto e scritta (2 sovrapposizioni misurate); cerchi disegnati come quadrati arrotondati (raggio dello sprite 64 < metà di 168–220 px → 112); GIOCA! disattivato verde scuro invece che spento; gocce della vittoria piccole e in fila; nome degli asset svuotato dal ripristino dei valori predefiniti.

**Da provare a mano sul telefono**
- Scorrimento delle schede: trascinamento, lancio veloce (al massimo una scheda), tocco su una scheda laterale, scheda ripresa al riavvio.
- Pulsanti che scendono quando li premi; tocco lungo e trascinamento fuori dal pulsante.
- Leggibilità di fonti e arrivi nei livelli 6x6 e movimento dell'acqua; effetto di vittoria.

**Problemi aperti / prossimi passi**
- Il commit separato di `Prompt/` e `ArtSource/` è stato bloccato dai permessi della sessione: da rifare (vedi report).
- `ArtSource/Reference/fontana_riferimento.png` non c'è nel progetto.
- Icone e illustrazioni delle schede: segnaposto, arriveranno da Gemini (cartelle e nomi già pronti).
- Il riflesso dei tubi ruota con il tubo (luce non sempre dall'alto a sinistra).
