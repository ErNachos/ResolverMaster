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
- Anteprime daltonismo: `Previews/colorblind/<schermata>_it_<risoluzione>_{deut,prot,trit}.png` (Machado 2009, gravità piena) per le schermate di gioco in italiano; il report riporta la differenza minima tra i colori dell'acqua per ogni tipo.

## Problemi aperti
- Nessuno per ora.

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
