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

## Problemi aperti
- (nessuno per ora)
