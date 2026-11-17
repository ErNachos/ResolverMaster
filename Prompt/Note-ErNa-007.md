# Note ErNa-007 — Pubblicità con premio (di prova), lampadina e Challenge

## Scelte fatte
- **Prompt/Update-ErNa-008.md è identico a Update-ErNa-007.md** (stesso contenuto byte per byte, anche il titolo "Update ErNa-007"). Probabilmente il file dell'ErNa-008 è stato salvato con il testo sbagliato: l'ho eseguito una sola volta come ErNa-007 e non ho inventato un ErNa-008. Da verificare con l'utente.
- Come per l'ErNa-006, l'Editor di Unity era aperto sul progetto: test, validatore e anteprime girano in batchmode sulla copia `Logs/BatchCopy/` (vedi Note-ErNa-006.md).
- **Parte A — annunci con premio (Core, `Core/Ads/`)**: interfaccia `IRewardedAdService` (`IsAvailable`, `Show(onRewarded, onFailedOrClosed)`); `TestRewardedAdService` (pannello `TestAdPanel`: "Annuncio di prova", attesa di `AdConfig.testAdSeconds` = 1,5 s con il conto alla rovescia, poi "Guardato" / "Chiudi"); `UnavailableRewardedAdService` (nessun annuncio). Unico punto di scelta: `RewardedAds.Create` (versione di prova nell'Editor e nelle build di sviluppo, se `AdConfig.testAdsInTestBuilds`; altrimenti nessun annuncio finché non arriva AdMob). Parametri in `Assets/Resources/ResolverMaster/AdConfig.asset`.
- I pulsanti che usano un annuncio si abilitano con `RewardedAds.CanOffer(ads, azionePossibile)`: disattivati senza annuncio, mentre un annuncio è già aperto o quando l'azione non serve.
- Le stelle con un aiuto usato: regola del Core `StarRules.maxStarsWithHint` = 2 (`StarRating.Compute(..., hintUsed)`).
- **Parte B — lampadina** (`PlumberPuzzle.TryFindHint` / `ApplyHint`): il risolutore riparte dallo stato attuale (bloccati e pezzi già sistemati restano fissi); tra i pezzi da ruotare sceglie quello del percorso che l'acqua raggiunge per primo (il più vicino a una fonte). Il pezzo ruota con l'animazione normale, non conta come mossa e diventa `Pinned` nel `CellLockSet` del Core (contorno bianco luminoso, `hintGlowColor`, diverso dalla piastra con bulloni). Non ruota più; un "annulla" di una mossa fatta prima su quel pezzo toglie la mossa dal contatore ma il pezzo resta fermo.
- I pezzi sistemati sopravvivono al reset per errore dell'acqua (e a Ricomincia); un nuovo caricamento del livello (anche "Ripeti" dal pannello di vittoria) li dimentica: nuovo hook del Core `PuzzleBase.OnFreshLoad`.
- Lampadina disattivata se tutti i pezzi sono già giusti, se non c'è un annuncio, durante le animazioni o a livello risolto (`RewardedAds.CanOffer`).
- Il risultato del risolutore per la lampadina è tenuto in memoria finché lo stato non cambia (nessun calcolo a ogni fotogramma).
- **Parte C — generazione della Challenge** (Core `Core/Challenge/ChallengeGeneration.cs`): un gioco fornisce solo `IChallengeSource` (tabella a gradini, margini, generatore con risolutore esatto); il Core fa il resto. Seed del livello = hash SplitMix64 di (seed della run, numero del livello, numero di cambi): niente orologio, niente `UnityEngine.Random`/`System.Random`; il generatore usa il `SeededRandom` del Core (PCG32). Limite in **tentativi** (`PlumberConfig.challengeMaxAttempts` = 4000 per seed); se non basta, seed + 1, + 2, … fino a `challenge.maxSeedRetries` (30).
- Massimo di mosse = minimo + margine; il margine scende in modo uniforme dal 60% (primo gradino) al 10% (ultimo), mai sotto +1. Stelle come nella Storia.
- Tabella a gradini (`PlumberDifficulty.Challenge`, copiata in `PlumberConfig.challengeDifficulty`): 16 gradini, di 3 livelli fino al 15 e di 5 dopo; da 3x4 con 1 fonte a 7x9 con 4 fonti; dente di sega all'arrivo di ogni novità (bivio, secondo colore, ponte, bloccati, terzo e quarto colore). Dopo il livello 70 si resta sull'ultimo gradino.
- Scorta: `ChallengeQueue` prepara il livello corrente e i 2 successivi con `Task.Run` (fuori dal thread principale); la sorgente dell'Idraulico (`PlumberChallenge`) è pura e legge solo dati copiati.
- `Resolver Master/Idraulico/Prova Challenge` scrive i primi 50 livelli di un seed (gradino, griglia, fonti, minimo, massimo, tempo) in `docs/idraulico-challenge.md` e nel log, più i tempi medi di tutti i 16 gradini (livelli 1–70).
- "Numero di cambi": il prompt lo nomina senza descriverlo. L'ho trattato come il contatore delle volte in cui il livello corrente viene sostituito (es. un futuro "cambia livello" o il salvavita dell'ErNa-008): entra nel seed ed è salvato; oggi resta 0.
- **Parte D — la run** (Core: `ChallengeRun.cs`, `ChallengeLauncher`, `DialogPanel`, `GameScreen` in modalità Challenge): seed della run casuale (`Environment.TickCount` + `Guid`, solo il seed della run; i livelli ne seguono in modo deterministico). Il livello generato si salva nella run (dati, minimo, massimo): una run ripresa non dipende dalla versione del generatore.
- Salvataggio a ogni mossa, annullamento e cambio di livello: seed, livello, cambi, livello generato, rotazioni (una cifra per cella), mosse, storico degli annullamenti, annullamenti rimasti, salvavita usato, stelle, livelli superati. Ripresa = ricarica il livello e rigioca lo storico (`PuzzleBase.Replay`); il test controlla che rotazioni e contatori siano identici.
- Nella Challenge: niente Ricomincia (sarebbe un annullamento illimitato) e niente lampadina; il pulsante Annulla ha un badge con gli annullamenti rimasti. Indietro (pulsante o tasto) = pausa; dopo la fine della run = Home. Un livello vinto mostra il pannello di vittoria con solo "Avanti"; i livelli cambiano tema a ogni gradino.
- Errore dell'acqua nella Challenge: effetto e messaggio come nella Storia, poi la board si ferma (`PuzzleBoard.RestartOnFailure = false`, evento `Failed`) e compare la fine della run. Il punteggio entra nel record (stelle, a parità livelli), la run viene tolta dal salvataggio.
- Contatore rosso (`UiKitStyle.offMark`) quando mancano 2 mosse o meno: non è un colore dell'acqua.
- Home: sulla scheda dell'Idraulico la coppa con il record (stelle) e il pulsante "Challenge" / "Riprendi"; l'illustrazione si accorcia per lasciare spazio (come per "Prossimamente"). Scelta della modalità: Challenge riprende o inizia; sotto, la run in corso o il record, e "Nuova run" con conferma.
- Tasto Indietro con l'annuncio di prova aperto: ignorato (si risponde con i pulsanti dell'annuncio).

## Problemi aperti
- "Numero di cambi" e "salvavita usato" sono nel salvataggio ma oggi non cambiano mai (0 / no): li userà "Continua" dell'ErNa-008.
- AdMob non è integrato: nelle build di rilascio non ci sono annunci e la lampadina resta disattivata (voluto).
- Le icone della Challenge (fulmine, coppa) sono quelle di Gemini già presenti; nessuna icona nuova.

## Riepilogo finale
**Fatto**
- A: annunci con premio nel Core (interfaccia, versione di prova con pannello, scelta in un solo punto, pulsanti disattivati senza annuncio).
- B: lampadina nella Storia (pezzo più vicino a una fonte, fissato con contorno luminoso, non è una mossa, massimo 2 stelle, sopravvive al reset, nascosta nella Challenge).
- C: generazione della Challenge deterministica (Core generico + Idraulico), 16 gradini, massimo = minimo + margine, scorta in background, menu Prova Challenge.
- D: la run completa (contatori, massimo, 3 annullamenti, perdita all'errore, punteggio e record, fine run, pausa, salvataggio a ogni mossa, una run per gioco con conferma, accesso dalla Home e dalla scelta della modalità).
- Test 31/31 (5 nuovi), validatore OK con 0 errori e 0 avvisi, 0 warning, 176 anteprime senza problemi. Commit A, B, C, D su GitHub.

**Tempi di generazione per gradino** (seed 20261117, livelli 1–70, ms medi / massimi): ch-1 0,0/0,1 · ch-2 0,1/0,1 · ch-3 0,1/0,1 · ch-4 0,0/0,1 · ch-5 0,2/0,3 · ch-6 0,8/3,2 · ch-7 0,5/0,9 · ch-8 0,6/1,2 · ch-9 0,3/0,6 · ch-10 0,3/1,1 · ch-11 0,5/1,8 · ch-12 0,1/0,3 · ch-13 1,3/3,1 · ch-14 0,7/1,7 · ch-15 0,6/1,2 · ch-16 0,9/1,4.

**Da provare sul telefono**: annuncio di prova e lampadina; Challenge (contatore, blocco al massimo, annullamenti, perdita, fine run, record); chiusura e riapertura dell'app a metà livello; pausa con il tasto Indietro; "Nuova run" con conferma; scheda della Home.
