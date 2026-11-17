# Note ErNa-008 — Challenge: Continua, Cambia livello e Sfida del giorno

## Scelte fatte
- L'Editor di Unity era chiuso: test, validatore e anteprime sono girati in batchmode direttamente sul progetto (nessuna copia).
- Icone segnaposto nuove (Material Symbols, `Tools/Art/make_icons.py`): `ad_video` (smart_display) sui pulsanti con la pubblicità, `change_level` (shuffle) per Cambia livello, `streak` (local_fire_department) per la serie. Aggiunte a `IconLibrary.CoreIcons` (controllate dal validatore) insieme a `icon_calendar` e `icon_trophy`.

### Parte A — Continua (salvavita)
- `ChallengeRunState`: nuovo campo salvato `lost` (la run ha perso e aspetta la scelta). `OnFailed()` mette `lost`; se il salvavita è già stato usato la run finisce subito (`over`). `CanContinue` = persa, non finita, salvavita non usato. `Continue()` = salvavita usato, stesso livello dall'inizio (stesso `levelData`, mosse 0, storico vuoto), annullamenti di nuovo a 3; stelle e livelli superati restano. `Finish()` chiude la run.
- Una run persa in attesa resta la run attiva nel salvataggio: se si chiude l'app sul pannello di fine run, "Riprendi" riapre il livello con il pannello e il Continua ancora disponibile. Il punteggio va nel record solo quando la run finisce davvero (Home, Nuova run, tasto Indietro, o seconda sconfitta).
- `DialogPanel` (Core): pulsante extra opzionale, largo come la riga, con icona a sinistra; la finestra si allunga solo quando è visibile (`SetExtra`). Sul pannello di fine run è "Continua" con l'icona della pubblicità; disattivato senza annuncio (`RewardedAds.CanOffer`) e aggiornato quando la disponibilità cambia.
- Un test esistente (`Run_MaxBlocksTurns_...`) è stato adattato: dopo il primo errore la run aspetta il Continua (`lost`), finisce con `Finish()`.

### Parte B — Cambia livello
- `ChallengeRunState.CanChangeLevel`: run che lo permette (`allowChange`, falso nella Sfida del giorno), non persa né finita, livello presente, **0 mosse** e pila di annullamento vuota (annullando fino a 0 mosse torna disponibile). `ChangeLevel()`: cambi + 1, il livello va rigenerato; il numero del livello non cambia.
- Seed = `ChallengeRules.LevelSeed(seed della run, livello, cambi)`, cioè con "cambi + 1" a ogni cambio; stesso gradino (dipende solo dal numero del livello). **Scelta**: il contatore dei cambi è del livello corrente e torna a 0 al livello successivo (così i livelli successivi già preparati restano validi); è salvato con la run.
- `ChallengeQueue`: i livelli si identificano con (numero, cambi); oltre al livello corrente e ai 2 successivi prepara in background anche il prossimo cambio, così il cambio è immediato. `Reset(runSeed)` non dipende più dai cambi.
- Pulsante: rotondo nella barra in basso al posto della lampadina (che nella Challenge è nascosta), icona segnaposto `change_level` e badge verde con l'icona della pubblicità sull'angolo. Attivo solo con 0 mosse e un annuncio disponibile; dopo un Continua (mosse a 0) torna attivo.
- Test esistente adattato: `ChallengeQueue.Reset(777)` (prima `Reset(777, 0)`).

### Parte C — Sfida del giorno
- Core `Core/Challenge/DailyChallenge.cs` (regole pure, nessun orologio): `DayKey` (AAAAMMGG), `DayIndex` (giorni consecutivi anche tra mesi e anni), `RunSeed` = la data stessa (stessi livelli su ogni telefono), `Begin` (un tentativo al giorno: `lastStartedDay`), `Finish` (risultato, miglior punteggio, serie), `CurrentStreak`, `Status` (da fare / in corso / fatta con N stelle).
- La data locale arriva da `AppServices.Clock` (`DateTime.Now`; le anteprime usano il 17/11/2026).
- La run giornaliera è una `ChallengeRunState` con `dayKey`, `fixedSteps` (5 gradini) e `allowChange = false`; è salvata nello stesso blocco `challenge.runs`, separata dalla run infinita (`ActiveRun` / `ActiveDaily`). I risultati sono in `challenge.daily` (`DailyStats`: ultimo giorno iniziato, risultato di oggi, serie attuale, serie migliore, miglior punteggio). Il punteggio non entra nel record della Challenge.
- Gradini: `PlumberConfig.challenge.dailyTiers` = ch-6, ch-9, ch-12, ch-13, ch-15 (da 5x7 a 2 colori fino a 7x9 a 4 colori con ponti e bloccati). `ChallengeRules.Generate` accetta un gradino fisso; `ChallengeQueue.Reset(seed, gradini)` non prepara nulla dopo il 5° livello. Nella `GameScreen` ci sono due scorte per gioco (infinita e giornaliera).
- **Scelte**:
  - "Un tentativo al giorno": la sfida di oggi si chiude appena iniziata e poi persa o completata. Se l'orologio del telefono torna indietro, non si può giocare un giorno già passato (si gioca solo un giorno successivo all'ultimo iniziato).
  - Sfida in sospeso: finché non è chiusa, la sfida di oggi non può iniziare (la scheda mostra "In corso"); chiusa quella vecchia, oggi torna "Da fare". Vale per il suo giorno: risultato e serie usano il suo `dayKey`.
  - Serie: +1 se il giorno della sfida completata è il giorno dopo l'ultimo completato, altrimenti riparte da 1; persa = 0. Quella mostrata è 0 se l'ultimo giorno completato è prima di ieri.
  - Il salvavita funziona come nella Challenge; dopo l'ultimo livello vinto il risultato si salva subito, "Avanti" apre il pannello di fine sfida.
  - Fine sfida: pannello "Sfida del giorno" con Completata! / Non completata, Stelle N/15, Serie e migliore, Continua (se possibile) e Home.
  - Titolo in gioco: "Sfida del giorno · N/5"; pausa: "Livello N/5 · S stelle".
- Home: sulla scheda dell'Idraulico una riga crema (calendario di Gemini, "Sfida del giorno", stato di oggi, fiamma segnaposto rossa `offMark` con la serie attuale) sopra record e pulsante Challenge; l'illustrazione si accorcia. Disattivata quando la sfida di oggi è finita. Le misure delle anteprime hanno trovato un errore nella prima versione (numero della serie largo 8 px, fiamma sovrapposta ai testi di 26 px): corretto con misure esplicite, poi 0 problemi.
- Validatore: nuovo controllo `DailyChallenge` (5 gradini esistenti, mai in discesa, i 5 livelli di un giorno di prova generati).

### Test
- 4 nuovi (35 in tutto): Continua; Cambia livello e ripresa dal salvataggio; Sfida del giorno (stessa data = stessi 5 livelli, un tentativo, niente Cambia livello); serie e sfida in sospeso con cambio di data. I pulsanti disattivati senza annuncio sono controllati nei test di Continua e Cambia livello (`RewardedAds.CanOffer`).

## Problemi aperti
- AdMob non è ancora integrato: in una build di rilascio Continua e Cambia livello restano disattivati (come la lampadina).
- Icone segnaposto (Material): pubblicità, Cambia livello, fiamma della serie.
- La Sfida del giorno usa la data locale del telefono: cambiando l'ora del telefono si può anticipare la sfida di domani (non c'è un orologio di rete).
- La Scelta della modalità non mostra la Sfida del giorno (solo la Home, come chiesto).

## Riepilogo finale
**Fatto**
- A — Continua: un salvavita per run, con l'annuncio; stesso livello dall'inizio, mosse 0, 3 annullamenti, stelle e livelli restano; salvato con la run, anche se si chiude l'app sul pannello.
- B — Cambia livello: pulsante con il badge della pubblicità al posto della lampadina; solo a 0 mosse, quante volte si vuole; stesso gradino e stesso numero di livello, seed con un cambio in più; cambi salvati con la run; il cambio successivo è già preparato in background.
- C — Sfida del giorno: 5 livelli dalla data, gradini in salita dal file dei parametri, 3 annullamenti, un salvavita, niente Cambia livello, un tentativo al giorno, pausa e salvataggio automatico, sfida in sospeso valida per il suo giorno, punteggio su 15, serie attuale e migliore, miglior punteggio; riga sulla scheda della Home.
- Test 35/35 (4 nuovi), validatore OK (9 controlli, 0 errori, 0 avvisi), 0 warning di compilazione, 200 anteprime senza problemi. Commit A, B, C su GitHub.

**Da provare sul telefono**
- Challenge: perdere un livello → Continua (annuncio di prova) → stesso livello con 3 annullamenti; perdere di nuovo → fine run senza Continua. Chiudere l'app sul pannello di fine run e riaprire (Riprendi → pannello con Continua).
- Cambia livello: attivo solo a 0 mosse (anche dopo aver annullato fino a 0), nuovo livello con lo stesso numero; chiudere e riaprire l'app dopo un cambio.
- Sfida del giorno dalla Home: 5 livelli, titolo N/5, pausa, fine sfida; la riga diventa "Fatta · N stelle" e si disattiva; il giorno dopo torna "Da fare" e la serie sale. Lasciare una sfida a metà prima di mezzanotte e finirla il giorno dopo.
