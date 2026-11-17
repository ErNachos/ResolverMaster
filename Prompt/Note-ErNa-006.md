# Note ErNa-006 — Idraulico: 4 fonti, ponte, tubi bloccati e Capitoli 4–9

## Scelte fatte
- **Unity aperto sul progetto** all'inizio (Editor avviato alle 16:22). La chiusura dell'Editor è stata negata dai permessi della sessione: test, validatore, anteprime e generazione girano in batchmode su una **copia** del progetto in `Logs/BatchCopy/` (cartella già ignorata da git, dentro il progetto). Prima di ogni esecuzione la copia viene allineata (`Assets`, `Packages`, `ProjectSettings`, `docs`); i `.meta` nuovi e i file generati tornano nel progetto vero. Le modifiche agli asset generati dalla copia sono state controllate con `diff` prima di riportarle.
- **Prompt/Update-ErNa-008.md** è identico byte per byte a `Update-ErNa-007.md` (anche il titolo dice ErNa-007): l'ho trattato come un duplicato (vedi Note-ErNa-007.md).
- **Parte A — colori**: viola `#8E3A80` (tonalità del "reddish purple" di Okabe-Ito `#CC79A7`, reso più scuro: con la luminosità originale, L* 61, era a soli 3,6 punti dal blu, L* 57) e giallo `#F0E442` (il giallo di Okabe-Ito, il più chiaro). Luminosità L*: viola 38,4 · blu 57,4 · arancione 69,1 · giallo 89,1 → scarto minimo 11,7 (blu–arancione); soglia del validatore 10 (`PlumberConfig.minWaterLightnessGap`).
- Aloni degli arrivi nuovi: viola chiaro `#D9B0D2`, giallo chiaro `#F8F2A8`.
- Validatore (`PlumberPaletteCheck`): niente rosso + verde; scarto di luminosità; nessun colore opaco di `UiKitStyle`, dei temi (`ThemeConfig`), dei capitoli o dei tubi entro ΔE 20 (CIE76, `minUiWaterDeltaE`) da un colore dell'acqua. Unico colore trovato troppo vicino: il colore del Capitolo 1 (giallo `#FFCD3C`, ΔE 18 dal nuovo giallo) → ora rosa `#F28DB2`. Gli sprite di Gemini (es. il pulsante rotondo giallo) sono immagini e non sono controllati.
- **Griglia 7x9**: con i margini attuali le celle sarebbero ~137 px. Per le griglie grandi (cella sotto 150, `compactCellBelow`) la cornice crema si disegna a metà spessore (`compactFrameScale` 0,5: sprite a 9 fette rimpicciolito, mai ingrandito), il bordo interno scende da 30 a 15 e la cornice può usare 16 px del margine (`compactOverflow`). Calcolo: 1080 → celle ~146 px (9:16 limitata anche in altezza: ~145 px). Misura reale nelle anteprime della Parte D.
- **Generatore**: layout `Colors` con 1 percorso per colore (2–4). Per 2 colori resta la sequenza casuale dell'ErNa-003, così i 27 livelli salvati non cambiano (validatore: 0 avvisi "il seed non riproduce più il livello").
- **Parte B — ponte**: nella logica un ponte è due nodi separati (`PipeNodes`: canale nord–sud = indice della cella, canale est–ovest = numero di celle + indice). Risolutore, `WaterFlow` e `WaterTiming` lavorano sui nodi, quindi i due flussi non si toccano mai. Codice nel formato dei livelli: `B`. Un ponte non ruota mai: si disegna sempre con il tubo nord–sud sopra (intero, un po' più largo, con due flange e un'ombra morbida sul tubo sotto) e la sua acqua sopra; il tubo est–ovest sotto, con la sua acqua coperta dal tubo sopra.
- "Ponte necessario" = il livello diventa impossibile se ogni ponte si risolve come una croce (`PlumberSolver.Options.BridgesAsCrosses`).
- **Croce**: da ErNa-006 non ruota più (è uguale in tutte le rotazioni): tocco = piccola scossa, nessuna mossa. Prima la rotazione contava come mossa senza cambiare nulla. Il risolutore la tratta come fissa (era già così, una sola orientazione distinta).
- Scossa: logica pura nel Core (`ShakeMotion`), parametri in `PlumberConfig` (`shakeDuration` 0,28 s, `shakeAmplitude` 6% della cella, `shakeCycles` 3); la usano croce, ponte e pezzi bloccati.
- Anteprime nuove con un livello fatto a mano (ponte, croce trappola, due pezzi bloccati): `game_bridge`, `game_bridge_flowing`, `game_bridge_ok`. La board ora carica anche un livello qualsiasi (`PlumberBoardView.LoadLevel`, servirà per la Challenge).
- **Parte C — bloccati**: concetto nel Core `CellLockSet` (`Core/Puzzle/Grid/CellLocks.cs`): per cella `None`, `Locked` (dal livello) o `Pinned` (fissato durante il gioco: servirà alla lampadina dell'ErNa-007). `PlumberPuzzle.Locks` lo riempie dai pezzi con `#`; un tocco su una cella che non può cambiare non è una mossa (scossa). Il risolutore tiene i bloccati come sono.
- Generatore: nuovo valore del gradino `locked`; ogni pezzo bloccato va, a caso, **sul percorso** già nella posizione giusta (vincolo) o **fuori** in posizione casuale (ostacolo/inganno). I numeri casuali in più si estraggono solo se il gradino chiede bloccati: i livelli dei capitoli 1–3 non cambiano.
- Grafica: piastra scura (`lockPlateColor` #3E424C) grande come la lastra, bordo nero e 4 bulloni chiari con taglio; il tubo resta sopra. Controllata su Giardino, Fattoria e Città (anteprime `game_locked_c1`, `game_locked_c2`, `game_bridge`). Ponte ritoccato dopo la prima anteprima: flange più grandi e ombra più scura (`bridgeShadowAlpha` 0,45), per distinguerlo meglio dalla croce.
- Anteprime daltonismo: `Previews/colorblind/<schermata>_it_<risoluzione>_{deut,prot,trit}.png` (Machado 2009, gravità piena) per le schermate di gioco in italiano; il report riporta la differenza minima tra i colori dell'acqua per ogni tipo.

- **Parte D**: gradini 28–81 nella tabella (`PlumberDifficulty.Story`, copiati in `PlumberConfig.asset` con `ConfigAssets.ResetGameDefaultsBatch`: il diff ha mostrato solo righe aggiunte). Il livello 81 era uscito a 24 mosse (limite 18 × 1,3 = 23,4): gradino ristretto a 21–23 → 21.
- Ponti nel generatore: un percorso (dal secondo in poi) può attraversare ad angolo retto un dritto di un percorso di altro colore (probabilità 0,7 quando servono ponti); "necessario" verificato con il risolutore.
- Temi: Fabbrica (terreno a mattoni, lastre grigie, sfondo città), Cantiere (terra battuta e sabbia scura, campi), Porto (banchina in pietra, lastre di legno, **mare verde acqua scuro** `#4E7D74`), Montagna (prato alpino scuro, roccia, cime innevate), Laboratorio (piastrelle chiare, grigio freddo), Castello (pietra grigia calda, erba scura, mura merlate). Tutti passano il controllo ΔE ≥ 20 dall'acqua.
- Nomi dei capitoli: 4 Ponti di ferro / Iron Bridges, 5 Cantiere bloccato / Locked Site, 6 Tre colori / Three Colors, 7 Alta quota / High Up, 8 Quattro colori / Four Colors, 9 Il gran finale / The Grand Finale. Icone (Material Symbols, segnaposto): factory, construction, anchor, landscape, science, castle.
- **Lista dei capitoli scorrevole**: con 9 capitoli le anteprime misuravano i capitoli 7–9 fuori dallo schermo → `UiFactory.ScrollList` (Core). Le misure ora controllano la lista e contano gli elementi raggiungibili scorrendo.

## Problemi aperti
- Daltonismo: con deuteranopia e protanopia arancione e giallo si distinguono soprattutto per luminosità (ΔE simulato 17,4 deut, 29,7 prot); il viola diventa blu notte, distinto dal blu per luminosità. Tritanopia: tutti ben distinti (minimo ΔE 37). Accettabile, ma è il caso più debole senza simboli.
- Il livello 80 (22 mosse) è un po' più lungo del livello 81 (21), che resta entro il limite richiesto.
- Le icone dei capitoli 4–9 sono segnaposto Material, come quelle dei capitoli 1–3.

## Tempi del risolutore (Parte A)
Misurati con *Idraulico > Misura risolutore* (30 livelli casuali per riga, risolutore esatto da solo):

| Griglia | Colori | Tubi del percorso | Media | Massimo | Nodi (media / max) |
|---|---|---|---|---|---|
| 6x8 | 2 | 12–18 | 0,2 ms | 3,6 ms | 180 / 526 |
| 6x8 | 3 | 14–22 | 0,1 ms | 0,4 ms | 148 / 258 |
| 7x9 | 3 | 18–26 | 0,2 ms | 1,7 ms | 347 / 769 |
| 7x9 | 4 | 18–28 | 0,1 ms | 0,2 ms | 236 / 471 |
| 7x9 | 4 | 26–34 | 0,3 ms | 0,5 ms | 511 / 952 |

Ben sotto 1 s: nessuna ottimizzazione necessaria.

## Riepilogo finale
**Fatto**
- A: 4 colori (blu, arancione, viola, giallo) con controlli di palette e distanza da interfaccia/temi; griglia 7x9 (celle 145–146 px); generatore e risolutore con 1–4 fonti, mosse minime esatte; anteprime daltonismo.
- B: ponte a due canali indipendenti, disegnato dal codice e molto diverso dalla croce; croce e ponte non contano mosse (scossa).
- C: pezzi bloccati (concetto nel Core `CellLockSet`), piastra con bulloni, risolutore e generatore, formato compatibile.
- D: capitoli 4–9, 54 livelli nuovi, 6 temi, lista dei capitoli scorrevole.
- Test 27/27 (5 nuovi), validatore OK con 0 errori e 0 avvisi, 0 warning di compilazione, 144 anteprime senza problemi. Commit A, B, C e D su GitHub.

**Tempi del risolutore**: 7x9 con 4 colori in media 0,2–0,5 ms, massimo 2,9 ms; nessun livello vicino a 1 s.

**Mosse minime**

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

**Anteprime daltonismo** (livelli 47, 53, 58, 63, 66, 72, 75, 81 e gli altri di gioco, in `Previews/colorblind/`): i 4 colori restano distinguibili; il caso più debole è arancione/giallo per deuteranopia e protanopia (diversi soprattutto in luminosità).

**Da provare sul telefono**: ponte e croce (scossa, nessuna mossa), pezzi bloccati, griglie 7x9, scorrimento della lista dei capitoli, leggibilità di arancione e giallo.
