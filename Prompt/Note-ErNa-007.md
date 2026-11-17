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

## Problemi aperti
- Il tasto Indietro di Android con il pannello dell'annuncio aperto agisce sulla schermata sotto (il pannello resta aperto).
