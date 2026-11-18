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

## Update ErNa-005 — Grafica rivista: UI da Gemini, tubi, griglie rettangolari, temi dei capitoli (2026-11-17)

**Fatto**
- **Git:** `git pull --ff-only` già aggiornato. `Prompt/` (003–005) e `ArtSource/` (Cards, Plumber, UI) aggiunti con un commit separato ("Aggiunti Prompt/ e ArtSource/"). Il problema aperto dell'ErNa-004 è risolto.
- **Script `Tools/Art/make_ui_art.py`**: separa gli 8 fogli di `ArtSource/UI/` in 30 elementi (4/3/3/4/4/4/4/4, come la tabella), toglie il bianco esterno e i buchi chiusi dal contorno (stella vuota, lucchetto, ingranaggio, trofeo), ritaglia senza scalare, scrive `Assets/Art/UI/ui_art.json` con i bordi a 9 fette; prepara anche le schede della Home (`card_plumber` 978x932, `card_trafficjam` 972x653).
- **Interfaccia (Core)**:
  - `UiKitStyle` ricreato: sprite di Gemini (pillole verde e crema, rotondi crema e giallo, pannello, nastro, banner, caselle, stelle), font TMP, materiale con contorno, materiale dei pulsanti disattivati.
  - **TextMesh Pro** ovunque: font asset Fredoka SDF (statici, 149 caratteri), contorno SDF continuo; eliminato il vecchio contorno fatto con copie del testo. TMP Essential Resources importate in `Assets/TextMesh Pro/`.
  - `PressableButton`: premuto scende di 8 unità e si scurisce; disattivato = sprite desaturati (shader `ResolverMaster/UI Desaturate`). `SlicedFit`: pillole a 9 fette senza deformare le estremità.
  - Schermate: Home (ingranaggio su rotondo giallo, GIOCA! verde, illustrazioni nelle schede, "Prossimamente" con lucchetto), scelta modalità (illustrazione del gioco, finestre crema), Impostazioni (globo, pillole delle lingue, suono sì/no, vibrazione attenuata con X disegnata dal codice), capitoli (banner giallo con anteprima del tema), livelli (caselle crema/verde/grigia, stelle piene e vuote), gioco (icone di Gemini, "Apri l'acqua" con il rubinetto), vittoria (pannello, nastro con il titolo, stelle).
  - Titolo della vittoria accorciato per stare nel nastro senza ingrandirlo: "Completato!" / "Well done!".
  - Rimossi gli sprite generati non più usati (`button*`, `card_pressed`, `outline`, `round*`, `soft`; restano `card` e `circle` in `Assets/Art/UI/Generated/`) e 12 icone Material (restano story, icone dei giochi e dei capitoli).
- **Temi dei capitoli (Core)**: `ThemeConfig` (un file) con Giardino, Fattoria e Città; `ThemeCatalog` (tema del capitolo, predefinito se manca); `ThemeArt` genera con il seed lastre delle celle, terreno tra le celle e sfondo; posti già pronti per le immagini di Gemini. Applicati a griglia di gioco, schermata dei livelli e banner dei capitoli. `PlumberConfig.chapters[i].theme`: garden, farm, city.
- **Tubi**: contorno 0,022 della cella (metà), nessun riflesso, colore pieno; spessore `pipeWidthPercent` (34%); `pipeShading` (sfumatura simmetrica) pronto ma spento; sprite a 384 px.
- **Acqua fluida**: `WaterTiming` + `WaterMotion` (logica pura) calcolano dove e quando l'acqua raggiunge ogni punto (bracci 0,5, curve pi/4, rami della T insieme, tutte le fonti insieme, velocità costante 4 celle/s con accelerazione di 0,35 s); `WaterGraphic` disegna l'acqua dentro i tubi con il fronte arrotondato, con il tempo reale. Le fontane si riempiono quando l'acqua arriva, poi gli spruzzi; in caso di errore l'acqua arriva fluida fino al punto sbagliato, poi l'effetto. Regole (`WaterFlow`) invariate.
- **Fonti e arrivi**: ombra morbida sotto ogni oggetto; disegno sollevato (8% della cella) così il moncone entra sotto l'oggetto.
- **Griglie rettangolari**: la tabella usa `cols`/`rows` (3x4, 4x5, 5x7, 6x8); generatore adattato (risolutore già generico); la griglia usa al massimo lo spazio tra barra in alto e "Apri l'acqua" con margini costanti, centrata. **27 livelli rigenerati** (`docs/idraulico-livelli.md`).
- **Validatore**: 7 controlli; nuovi: elementi di Gemini, risorse e font TMP, materiali, temi (`ThemeCheck`), tema valido per ogni capitolo.
- **Anteprime**: livelli di ogni capitolo, un livello per capitolo (3x4, 5x7, 6x8), acqua a metà percorso; nuove misure: celle per griglia, spazio sopra/sotto la griglia, testi TMP tagliati, sprite ingranditi.
- **Test**: 4 nuovi (vicini sulla griglia 3x4, generatore 5x7, temi, tempi dell'acqua con la T).

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 22/22 verdi. Validatore: OK (7 controlli, 0 errori, 0 warning).
- **Livelli** (griglia, mosse minime): cap. 1 → 3x4: 1, 2, 3; 4x5: 4, 5, 5; 5x7: 6, 7, 9. Cap. 2 → 4x5: 2, 4, 4, 5; 5x7: 6, 7, 9, 10, 10. Cap. 3 → 5x7: 2, 3, 6, 7; 6x8: 7, 8, 9, 10, 12. Generatore 6x8 a due colori: 0,55 ms in media su 30 seed (max 3,0 ms).
- **Anteprime**: 60 immagini, nessun problema misurato.
- **Misure**:
  - celle: 3x4 → 311 px (9:16) / 321 px (9:21); 5x7 → 177 / 192 px; 6x8 → 155 / 160 px;
  - spazio libero sopra e sotto la griglia (uguale, griglia centrata): 9:16 → 58–61 px; 9:21 → 212–244 px (lì le griglie sono limitate dalla larghezza);
  - fonti e arrivi al 78% della cella (121–251 px);
  - pulsante più piccolo 136x136 px (rotondi); nessun testo tagliato né sovrapposto, IT ed EN;
  - nessuno sprite di progetto ingrandito (scala massima x1,00); solo gli sfondi generati dei temi sono disegnati a x2 (previsto).
- Bug trovati da misure e immagini e corretti: numero della casella sovrapposto di 2 px alla stella centrale; lucchetto e badge sopra l'illustrazione di Traffic jam; ombre nascoste sotto i disegni; bordi seghettati negli sfondi (ora 2x2 campioni); acqua non disegnata nelle anteprime (al `WaterGraphic` mancavano `[ExecuteAlways]` e il `CanvasRenderer`).

**Da provare a mano sul telefono**
- Scorrimento dell'acqua (velocità, accelerazione, T, due colori) a 30 e 60 fps; riempimento delle fontane e spruzzi.
- Pulsanti premuti (scendono e si scuriscono) e disattivati (desaturati); leggibilità dei testi con contorno.
- Leggibilità di tubi e acqua sui tre terreni; livelli 6x8 (celle da 155 px).

**Problemi aperti / prossimi passi**
- Su schermi 9:21 le griglie sono limitate dalla larghezza: restano 212–244 px liberi sopra e sotto.
- Icone ancora segnaposto (Material): Storia, icone dei giochi e dei capitoli.
- Le TMP Essential Resources in batch si importano con `-importPackage` (vedi `docs/grafica.md`).
- `.claudesettings.json` (locale) non incluso nel commit.

## Update ErNa-006 — Idraulico: 4 fonti, ponte, tubi bloccati e Capitoli 4–9 (2026-11-17)

**Fatto** (dettagli e scelte in `Prompt/Note-ErNa-006.md`)
- **Nota di lavoro**: l'Editor di Unity era aperto sul progetto e la sessione non poteva chiuderlo. Test, validatore, anteprime e generazione sono girati in batchmode su una copia in `Logs/BatchCopy/` (ignorata da git); i risultati sono stati riportati nel progetto dopo un `diff`.
- **A — fino a 4 fonti**: colori viola `#8E3A80` e giallo `#F0E442` (tonalità Okabe-Ito), aloni nuovi; `PlumberPalette` e `PlumberPaletteCheck` (niente rosso + verde, L* distanti ≥ 10, nessun colore di UI/temi/capitoli entro ΔE 20). Colore del Capitolo 1 da giallo a rosa. Generatore con 1 percorso per colore (2–4). Griglia 7x9 con cornice compatta. `ColorVision` (Core): Lab, ΔE, simulazione del daltonismo; anteprime filtrate in `Previews/colorblind/`. Menu *Idraulico > Misura risolutore*.
- **B — ponte e croce**: pezzo `Bridge` (`B`), due canali separati (`PipeNodes`) in risolutore, acqua e tempi; grafica dal codice (tubo nord–sud sopra con flange e ombra, flusso sopra; est–ovest sotto). Croce e ponte non ruotano: tocco = scossa (`ShakeMotion`, Core), nessuna mossa.
- **C — tubi bloccati**: `CellLockSet` nel Core (`Locked` / `Pinned`); piastra scura con 4 bulloni; generatore con bloccati sul percorso (già giusti) o fuori; formato `#` (i livelli vecchi restano validi).
- **D — capitoli 4–9**: 54 livelli nuovi (81 in tutto), 6 temi nuovi (Fabbrica, Cantiere, Porto, Montagna, Laboratorio, Castello) con 3 sfondi nuovi generati (porto con mare verde acqua scuro, montagna, castello), 6 icone Material nuove, nomi IT/EN. Ponti nel generatore (un percorso attraversa un dritto di un altro colore). Validatore: ponte necessario, bloccati e ponti nel gradino, ultimo livello entro il massimo del Capitolo 8 + 30%. Lista dei capitoli scorrevole (`UiFactory.ScrollList`). I 27 livelli dei capitoli 1–3 sono identici a prima.
- **Test**: 5 nuovi (palette; 3–4 colori risolvibili e minimo uguale alla ricerca esaustiva; ponte senza mescolamento; croce/ponte/bloccati non contano mosse e livelli vecchi compatibili; 81 livelli numerati con minimo esatto).

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 27/27 verdi. Validatore: OK (8 controlli, 0 errori, 0 warning).
- Risolutore (30 livelli casuali per riga): 7x9 con 4 colori media 0,2–0,5 ms, massimo 2,9 ms; generatore 7x9 a 4 colori (gradino c9-9) 2,4 ms in media, massimo 9,8 ms.
- Anteprime: 144 immagini, nessun problema misurato; 7x9 → celle 145 px (9:16) e 146 px (9:21), griglia a 33/29 px dai bordi.

| Cap. | Livelli | Griglie | Mosse minime |
|---|---|---|---|
| 1 | 1–9 | 3x4 → 4x5 → 5x7 | 1, 2, 3, 4, 5, 5, 6, 7, 9 |
| 2 | 10–18 | 4x5 → 5x7 | 2, 4, 4, 5, 6, 7, 9, 10, 10 |
| 3 | 19–27 | 5x7 → 6x8 | 2, 3, 6, 7, 7, 8, 9, 10, 12 |
| 4 | 28–36 | 5x7 → 6x8 | 4, 7, 7, 8, 10, 11, 10, 13, 15 |
| 5 | 37–45 | 5x7 → 6x8 | 7, 8, 7, 10, 11, 11, 13, 13, 14 |
| 6 | 46–54 | 6x8 | 4, 5, 7, 10, 9, 12, 12, 14, 13 |
| 7 | 55–63 | 6x8 → 7x9 | 8, 8, 10, 12, 14, 15, 16, 15, 17 |
| 8 | 64–72 | 6x8 → 7x9 | 8, 9, 11, 10, 12, 13, 17, 18, 18 |
| 9 | 73–81 | 7x9 | 11, 11, 13, 16, 17, 18, 19, 22, 21 |

**Da provare a mano sul telefono**
- Ponte: tocco (scossa, mosse invariate), acqua che passa sopra e sotto; croce: scossa. Pezzi bloccati su tutti i terreni.
- Griglie 7x9 (celle da 145 px): precisione dei tocchi.
- Lista dei capitoli: scorrimento, tocco su un capitolo dopo lo scorrimento.
- Leggibilità dei 4 colori, soprattutto arancione e giallo.

**Problemi aperti / prossimi passi**
- Con deuteranopia/protanopia arancione e giallo si distinguono soprattutto per luminosità (ΔE 17 simulato): accettabile, ma senza simboli (scelta dell'utente) resta il caso più debole.
- Il livello 80 (22 mosse) è più lungo del livello 81 (21).

## Update ErNa-007 — Pubblicità con premio (di prova), lampadina e Challenge (2026-11-17)

**Fatto** (dettagli e scelte in `Prompt/Note-ErNa-007.md`)
- **Nota**: `Prompt/Update-ErNa-008.md` è identico a `Update-ErNa-007.md` (anche il titolo): l'ErNa-007 è stato eseguito una sola volta, nessun ErNa-008 inventato. Come per l'ErNa-006, batchmode sulla copia `Logs/BatchCopy/` perché l'Editor era aperto.
- **A — annunci con premio (Core)**: `IRewardedAdService`, versione di prova (`TestAdPanel`: "Annuncio di prova", 1,5 s di attesa, Guardato / Chiudi), nessun annuncio in rilascio; un solo punto di scelta (`RewardedAds.Create`); pulsanti disattivati senza annuncio (`RewardedAds.CanOffer`). `AdConfig.asset`.
- **B — lampadina (Storia)**: annuncio → il risolutore riparte dallo stato attuale e sistema il pezzo del percorso più vicino a una fonte, con l'animazione; il pezzo resta fissato (contorno bianco luminoso), non conta come mossa, non si annulla, sopravvive al reset per errore; al massimo 2 stelle. Disattivata se tutto è già giusto o senza annuncio; nascosta nella Challenge.
- **C — Challenge, generazione (Core + Idraulico)**: seed del livello = hash(seed della run, livello, cambi); PCG32 del Core; limite in tentativi, poi seed + 1; 16 gradini (3x4 con 1 fonte → 7x9 con 4 fonti, gradini di 3 e poi di 5 livelli, dente di sega a ogni novità); massimo = minimo + margine dal 60% al 10% (almeno +1); scorta dei prossimi 2 livelli fuori dal thread principale; menu *Idraulico > Prova Challenge* (`docs/idraulico-challenge.md`).
- **D — Challenge, la run**: corsa infinita con seed casuale; mosse / minimo / massimo sempre visibili (rosso a 2 mosse dal massimo); al massimo i pezzi non ruotano; si perde al primo errore dell'acqua; 3 annullamenti per run (badge sul pulsante) che tolgono la mossa; punteggio = stelle (a parità, livelli); record in locale; fine run (stelle, livelli, record evidenziato, Nuova run, Home, spazio per "Continua"); pausa (Riprendi / Home) anche con il tasto Indietro; salvataggio a ogni mossa e a ogni livello (blocco `challenge`); una run per gioco, "Nuova run" con conferma; scheda della Home con record e pulsante Challenge / Riprendi. Struttura generica nel Core.
- **Test**: 5 nuovi (annunci e pulsanti disattivati; lampadina; generazione deterministica anche su un altro thread, massimo ≥ minimo + 1, ultimo gradino; regole della run, record, salvataggio e ripresa identici).

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 31/31 verdi. Validatore: OK (8 controlli, 0 errori, 0 warning).
- Generazione della Challenge per gradino (livelli 1–70 del seed 20261117): media 0,0–1,3 ms, massimo 3,2 ms (tabella in `docs/idraulico-challenge.md`).
- Anteprime: 176 immagini, nessun problema misurato (nuove: annuncio di prova, lampadina, Challenge, pausa, fine run, scelta modalità con run in corso, conferma, Home con record).

**Da provare a mano sul telefono**
- Lampadina: annuncio di prova (attesa, Guardato / Chiudi), pezzo che ruota e si illumina, stelle massime 2, reset dopo un errore.
- Challenge: contatore rosso, blocco al massimo (scossa), 3 annullamenti, perdita all'errore, fine run e record; chiudere l'app a metà livello e riaprire (riprende identica); pausa con il tasto Indietro; "Nuova run" con conferma; scheda della Home.

**Problemi aperti / prossimi passi**
- "Numero di cambi" e "salvavita usato" sono salvati ma oggi restano 0 / no: serviranno con "Continua" (ErNa-008).
- AdMob non integrato: in una build di rilascio la lampadina resta disattivata.

## Update ErNa-008 — Challenge: Continua, Cambia livello e Sfida del giorno (2026-11-17)

**Fatto** (dettagli e scelte in `Prompt/Note-ErNa-008.md`)
- **Nota**: l'Editor di Unity era chiuso: batchmode direttamente sul progetto. Il prompt 008 ora ha il suo contenuto (non più la copia del 007).
- **A — Continua (Core)**: un salvavita per run con un annuncio. Al primo errore la run resta salvata in attesa (`lost`); Continua = stesso livello dall'inizio, mosse 0, annullamenti di nuovo a 3, stelle e livelli restano. Il record si aggiorna solo quando la run finisce davvero. `DialogPanel` con pulsante extra (icona della pubblicità) che allarga la finestra solo quando c'è.
- **B — Cambia livello (Core)**: pulsante nella barra in basso con il badge della pubblicità, solo a 0 mosse, quante volte si vuole; stesso gradino e stesso numero, seed con un cambio in più (`changes`, del livello corrente, salvato). `ChallengeQueue` per (livello, cambi), con il prossimo cambio già pronto.
- **C — Sfida del giorno (Core, `DailyChallenge`)**: 5 livelli dalla data locale (AAAAMMGG), gradini ch-6 → ch-15 in `PlumberConfig.challenge.dailyTiers`, niente Cambia livello, 3 annullamenti, un salvavita, un tentativo al giorno, sfida in sospeso valida per il suo giorno, punteggio su 15, serie (attuale e migliore) e miglior punteggio nel blocco `challenge.daily`. Riga sulla scheda della Home (stato di oggi e serie), pannello di fine sfida. Data da `AppServices.Clock`.
- Icone segnaposto Material: `ad_video`, `change_level`, `streak`. Validatore: nuovo controllo `DailyChallenge`. Anteprime nuove: Continua, Cambia livello, Home con la sfida, sfida in gioco, sfida persa con Continua, sfida completata.
- **Test**: 4 nuovi (Continua; Cambia livello e ripresa; stessa data = stessi livelli e un tentativo; serie e cambio di data). Adattati 2 test esistenti (sconfitta che aspetta il Continua, `ChallengeQueue.Reset`).

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 35/35 verdi. Validatore: OK (9 controlli, 0 errori, 0 warning).
- Anteprime: 200 immagini, nessun problema misurato. Le misure hanno trovato e fatto correggere la riga della sfida sulla Home (numero della serie largo 8 px, fiamma sovrapposta ai testi).

**Da provare a mano sul telefono**
- Continua dopo un errore (anche chiudendo l'app sul pannello), seconda sconfitta senza Continua.
- Cambia livello solo a 0 mosse; riapertura dell'app dopo un cambio.
- Sfida del giorno: 5 livelli, fine sfida, riga della Home "Fatta", giorno dopo e serie; sfida lasciata a metà oltre la mezzanotte.

**Problemi aperti / prossimi passi**
- AdMob non integrato: in rilascio Continua, Cambia livello e lampadina restano disattivati.
- Icone della pubblicità, del cambio e della serie: segnaposto.
- La data è quella del telefono: cambiandola si può anticipare la sfida.

## Update TJ-001 — Traffic jam: il gioco completo (2026-11-18)

**Fatto** (dettagli, scelte e tempi in `Prompt/Note-TJ-001.md`)
- **A — Regole**: griglie verticali 4x6–7x9, auto e camion che scorrono solo nel loro verso, barriere, auto obiettivo rossa (e blu) con la sua uscita colorata; 1 mossa = uno scorrimento, rilascio sul posto = nessuna mossa, la spinta d'uscita conta (`docs/trafficjam-regole.md`).
- **B — Risolutore esatto** (ricerca in ampiezza avanti + indietro, tabella delle distanze, tetto 2 milioni) e **generatore** deterministico (disposizione casuale + salita, partenza più lontana, limite in tentativi).
- **C — Gioco**: trascinamento a tempo reale con aggancio, uscita animata, Annulla; grafica provvisoria dal codice; scheda della Home attiva.
- **D — Storia**: 7 capitoli, 63 livelli (`docs/trafficjam-livelli.md`), menu *Traffic jam > Genera livelli Storia* (in parallelo); 5 temi nuovi nel Core.
- **E — Lampadina**: freccia + mossa gratuita, al massimo 2 stelle.
- **F — Challenge e Sfida del giorno**: 16 gradini in `TrafficJamConfig`, "Mosse finite" (Annulla / Termina, sconfitta senza annullamenti, poi Continua), menu *Traffic jam > Prova Challenge* (`docs/trafficjam-challenge.md`).
- **Spostato nel Core**: `StoryBook.Build`, `BoardFrame`, `PuzzleBase.ApplyFreeMove`, "Mosse finite" (`LosesWhenOutOfMoves`, `IsOutOfMoves`), `StateIndex`, temi nuovi (`ConfigAssets.AddMissingThemes`).
- Validatore: controlli `TrafficJam` e `TrafficJamPalette` (rosso e blu riservati). Anteprime Traffic jam (anche daltonismo).
- **Test**: 5 nuovi (regole e uscite; mosse minime esatte e stesso seed; lampadina; Mosse finite, salvataggio e ripresa; 63 livelli).

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 40/40 verdi. Validatore: OK (11 controlli, 0 errori, 0 warning).
- Anteprime: 312 immagini, nessun problema misurato (le misure hanno fatto accorciare il testo di "Mosse finite"). Celle: 4x6 213 px, 6x8 157 px, 7x9 136 px. Misure dell'Idraulico invariate.
- Tempi: risolutore 7x9 12 ms in media; lampadina 13 ms; generatore Challenge da 1 ms (4x6) a 1,5 s in media (7x9 con due auto obiettivo, in sottofondo).

| Cap. | Livelli | Griglie | Mosse minime |
|---|---|---|---|
| 1 | 1–9 | 4x6 → 5x7 | 2, 2, 4, 4, 5, 5, 7, 8, 8 |
| 2 | 10–18 | 5x7 | 4, 5, 6, 7, 8, 10, 11, 11, 12 |
| 3 | 19–27 | 6x8 | 8, 8, 9, 10, 12, 13, 14, 15, 17 |
| 4 | 28–36 | 6x8 | 10, 11, 11, 13, 14, 15, 16, 17, 19 |
| 5 | 37–45 | 6x8 | 10, 12, 12, 13, 15, 15, 16, 17, 19 |
| 6 | 46–54 | 6x8 → 7x9 | 11, 12, 14, 15, 16, 16, 17, 18, 19 |
| 7 | 55–63 | 7x9 | 14, 15, 16, 16, 18, 18, 19, 20, 21 |

**Da provare a mano sul telefono**
- Trascinamento e uscite (anche laterali e in basso), lampadina, "Mosse finite", ripresa di una run chiusa a metà, tempi dei livelli 7x9 della Challenge.

**Problemi aperti / prossimi passi**
- Generazione Challenge 7x9 con due auto obiettivo sopra 1 s (in sottofondo). Grafica e icone provvisorie.

## Update TJ-002 — Traffic jam: grafica dei veicoli e degli ostacoli (2026-11-18)

**Fatto**
- **Nota**: l'Editor di Unity era chiuso: batchmode direttamente sul progetto. Cancellati i vecchi `Prompt/Note-*.md` (come chiede il prompt); niente file di note nuovi.
- **A — Preparazione** (`Tools/Art/make_trafficjam_art.py`): 15 sprite in `Assets/Art/TrafficJam/` dai 4 fogli di `ArtSource/TJ/` (non toccati): `Target_Red/Blue_Worried/Happy`, `Car_Yellow/Lilac/Grey/Beige`, `Truck_Grey/Yellow/Lilac`, `Obstacle_Barrier/Manhole/Rock/Trash`. Sfondo tolto solo dai bordi (bianchi interni intatti, nessun alone), ritaglio senza scalare, veicoli girati col muso in alto. Libreria `Resources/Games/TrafficJam/TrafficJamArt.asset`.
- **B — Veicoli**: auto obiettivo col muso verso l'uscita, preoccupata, felice dall'inizio dell'uscita (torna preoccupata con Annulla); gli altri: una delle 4 auto o dei 3 camion e il verso dal seed (`TrafficLook`, logica pura). Proporzioni: auto allungate del 18% ripetendo righe del tetto, camion allungati dell'89% ripetendo righe del cassone (cabina e faccia intatte); nel gioco stessa scala sui due assi in (lunghezza − 0,12) x 0,88 celle. Gli strati disegnati dal codice restano come riserva se manca uno sprite.
- **C — Ostacoli**: 4 disegni, scelti dal seed tra quelli ammessi dal tema (`TrafficJamConfig.obstaclesByTheme`, `ThemeVariants` nuovo nel Core); Challenge: tutti.
- `targetColors` e `vehicleColors` = vernice dei disegni. `UiKitStyle.offMark` da #D9404A a #B8324A: era a ΔE 19,6 dal rosso vero dei disegni (validatore).
- Validatore: nuovo controllo `TrafficJamArt`. Anteprime: auto obiettivo a metà uscita (4x6 e 7x9); la scala degli sprite ora si misura anche per immagini ruotate; misura della larghezza dei disegni.
- **Test**: 4 nuovi (ogni disegno nel file e nella libreria; auto obiettivo verso l'uscita; stesso seed = stessi disegni, versi e ostacoli; faccia felice all'uscita).

**Risultati**
- Compilazione: 0 errori, 0 warning. Test: 44/44 verdi. Validatore: OK (12 controlli, 0 errori, 0 warning).
- Anteprime: 320 schermate (578 immagini con il daltonismo), nessun problema misurato, nessuno sprite ingrandito. Veicoli larghi l'88% della cella: 187 px a 4x6, 153 px a 5x7, 138 px a 6x8, 120 px a 7x9.

**Da provare a mano sul telefono**
- Leggibilità delle facce a 7x9; faccia felice all'uscita (trascinamento e lampadina); Annulla dopo un'uscita.

**Problemi aperti / prossimi passi**
- Con il muso in alto o di lato la faccia appare capovolta o girata (vista dall'alto).
- Uscite colorate e freccia della lampadina restano disegnate dal codice.
