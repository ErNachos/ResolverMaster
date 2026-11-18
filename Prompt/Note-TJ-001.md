# Note — Update TJ-001 (Traffic jam)

Unity era chiuso: tutto in batchmode direttamente sul progetto. I prompt `Update-ErNa-009…015.md` sono vuoti: non toccati.

## Fatto, parte per parte
- **A — Regole e modello** (`Games/TrafficJam/Logic/TrafficLayout.cs`, `TrafficPuzzle.cs`): griglie verticali 4x6, 5x7, 6x8, 7x9; auto (2) e camion (3) che scorrono solo nel loro verso; barriere fisse; auto rossa e (dal capitolo 5) blu, ognuna con la sua uscita colorata sulla sua linea; 1 mossa = uno scorrimento di quante caselle si vuole, rilascio sul posto = nessuna mossa, la spinta d'uscita conta; vittoria = tutte le auto obiettivo fuori; stelle del Core. Forma testuale dei livelli. Regole in `docs/trafficjam-regole.md`.
- **B — Risolutore e generatore** (`TrafficSolver.cs`, `TrafficGenerator.cs`): ricerca in ampiezza in avanti (tutte le posizioni) e all'indietro dalle posizioni risolte (tabella delle distanze), tetto 2 milioni (`maxStates`). Generatore deterministico: disposizione casuale del gradino, esplorazione, partenza più lontana entro il gradino; in più una salita (sposta un veicolo e tiene la modifica se non diventa più facile). Limite in tentativi. Verificato contro una ricerca ingenua indipendente su 197 livelli casuali: 0 differenze (minimo, distanze, mossa della lampadina, forma testuale).
- **C — Schermata di gioco** (`View/TrafficBoardView.cs`, `TrafficVehicleView.cs`, `TrafficSprites.cs`): trascinamento lungo il verso, stop contro veicoli/barriere/bordo, aggancio alla casella più vicina, uscita animata oltre il bordo con dissolvenza, tutto a tempo reale. Annulla come l'Idraulico. Grafica provvisoria dal codice (veicoli dall'alto con ruote, vetri, fari; camion con cabina e cassone; strisce bianche sulle auto obiettivo; barriere a strisce ambra/antracite; uscite = varco colorato nel bordo con frecce). Home: scheda attiva con Storia, Challenge, Sfida del giorno.
- **D — Storia**: 7 capitoli, 63 livelli (`Resources/Games/TrafficJam/trafficjam_story.json`, tabella in `docs/trafficjam-livelli.md`); strumento `Resolver Master/Traffic jam/Genera livelli Storia` (in parallelo, verifica le mosse minime col risolutore e la difficoltà nei capitoli). 5 temi nuovi nel Core; icone Material segnaposto per i capitoli.
- **E — Lampadina**: dopo l'annuncio, freccia sul veicolo della prossima mossa giusta (prima nell'ordine fisso), poi il veicolo scorre; mossa gratuita (non conta, si può annullare), al massimo 2 stelle; disattivata se risolto o senza annuncio; nascosta in Challenge.
- **F — Challenge e Sfida del giorno**: solo la tabella a gradini (16 gradini, da 4x6 a 7x9 con camion, barriere, due auto obiettivo, uscite laterali; dente di sega) e i margini in `TrafficJamConfig`; "Mosse finite" (Annulla / Termina, sconfitta senza annullamenti, poi Continua); salvataggio di mosse e posizioni (`x` = auto uscita); record/serie/salvataggi per gioco (già per id nel Core). Menu `Traffic jam/Prova Challenge` (`docs/trafficjam-challenge.md`) e `Traffic jam/Misura risolutore`.

## Spostato nel Core
- `StoryBook.Build` (capitoli costruiti dai livelli; prima in `PlumberModule.BuildBook`, che ora lo usa).
- `BoardFrame` (finestra della griglia, terreno del tema, adattamento all'area, cornice sottile per griglie grandi; prima dentro `PlumberBoardView`, che ora lo usa: misure delle anteprime dell'Idraulico identiche).
- `PuzzleBase.ApplyFreeMove` (mosse che non contano: la lampadina che sposta un pezzo).
- "Mosse finite" nella Challenge: `PuzzleBoard.LosesWhenOutOfMoves`, `ChallengeRunState.IsOutOfMoves` / `OutOfMovesIsLost`, pannello nella `GameScreen`, testi `challenge.out_of_moves*`.
- `StateIndex` (indice degli stati per ricerche esaustive).
- Temi Parcheggio, Mercato, Aeroporto, Incrocio, Ora di punta in `ThemeConfig` (aggiunti all'asset senza toccare quelli esistenti, `ConfigAssets.AddMissingThemes`).

## Tempi (Editor, Mono; sul telefono IL2CPP dovrebbe essere simile o più veloce)
- Risolutore esatto sui 13 livelli 7x9 della Storia (3.414–19.280 posizioni): media 12,4 ms, massimo 22,5 ms.
- Lampadina, prima volta su un 7x9 (tabella + mossa): media 12,8 ms, massimo 24,6 ms → immediata.
- Generatore Challenge: 4x6 1–2 ms; 5x7 17–135 ms; 6x8 79–389 ms (massimo 923 ms); 7x9 una auto obiettivo 507 ms (massimo 990); **7x9 con due auto obiettivo 0,9–1,5 s in media, massimo circa 3 s** (sopra l'obiettivo di 1 s). Non blocca il gioco: il Core prepara i 2 livelli successivi in sottofondo; il primo livello della Sfida del giorno è un 6x8 (ch-6).
- Storia: 63 livelli in 102 s in parallelo (16 thread). Il validatore li rigenera dal seed in parallelo.

## Mosse minime dei 63 livelli
| Cap. | Livelli | Griglie | Mosse minime |
|---|---|---|---|
| 1 Parcheggio | 1–9 | 4x6 → 5x7 | 2, 2, 4, 4, 5, 5, 7, 8, 8 |
| 2 Al mercato | 10–18 | 5x7 | 4, 5, 6, 7, 8, 10, 11, 11, 12 |
| 3 Centro città | 19–27 | 6x8 | 8, 8, 9, 10, 12, 13, 14, 15, 17 |
| 4 Lavori in corso | 28–36 | 6x8 | 10, 11, 11, 13, 14, 15, 16, 17, 19 |
| 5 Aeroporto | 37–45 | 6x8 | 10, 12, 12, 13, 15, 15, 16, 17, 19 |
| 6 Incrocio | 46–54 | 6x8 → 7x9 | 11, 12, 14, 15, 16, 16, 17, 18, 19 |
| 7 Ora di punta | 55–63 | 7x9 | 14, 15, 16, 16, 18, 18, 19, 20, 21 |

## Scelte fatte
- **Un solo commit** invece di uno per parte: le parti condividono gli stessi file (regole, risolutore, generatore, vista, Core) e sono state sviluppate insieme; spezzarle avrebbe dato commit non verificati con test e validatore. Commit unico solo a tutto verde.
- Generatore con **salita** oltre a "disposizione casuale + partenza più lontana": la sola disposizione casuale dava livelli troppo facili e spazi enormi sulle griglie grandi. Griglie piene (molti veicoli, camion e barriere) = spazi piccoli e livelli più difficili.
- Stato impacchettato con 4 bit per le auto obiettivo e 3 per gli altri veicoli: fino a 21 veicoli in 64 bit.
- Tetti di posizioni diversi: 2 milioni per risolutore/lampadina/validatore; 20.000 per le disposizioni provate dal generatore della Storia; 4.000 per quello della Challenge (veloce sul telefono).
- Livello 1 della Storia: 2 mosse (il gradino ammette 1–2; il generatore sceglie la partenza più lontana).
- La Challenge sulla 7x9 ha minimi 9–13 (più bassi dei gradini 6x8 più duri): la novità (griglia grande, due auto, uscite laterali) fa da dente di sega e il margine stretto (+10%) dà la difficoltà; minimi più alti costavano diversi secondi.
- Storia: se un seed non dà un livello si prova il seed fisso + 1000 (salvato nel livello).
- "Mosse finite": pannello con **Annulla** (verde, principale) e **Termina**; senza annullamenti la run è persa subito; il messaggio è stato accorciato dopo la misura delle anteprime (testo uscito dal riquadro).
- La lampadina fa una mossa gratuita che si può annullare (altrimenti la pila di annullamento non sarebbe più coerente); il livello resta comunque a 2 stelle.
- Rosso #E02B20 e blu #2F7FE8 delle auto obiettivo: ΔE ≥ 20 da ogni colore di UI, temi, capitoli, veicoli e barriere (nuovo controllo `TrafficJamPalette`); le auto obiettivo hanno anche due strisce bianche (forma, non solo colore). Altri veicoli: giallo, bianco, beige, grigio, viola chiaro.
- Test: 5 nuovi metodi che coprono tutti i punti della lista del prompt (regole, mosse minime e seed, lampadina, Mosse finite + salvataggio, 63 livelli); tutti i test dell'Idraulico verdi.

## Problemi aperti
- Generazione Challenge 7x9 con due auto obiettivo: media 0,9–1,5 s, punte di 3 s (obiettivo < 1 s non raggiunto; avviene in sottofondo).
- Grafica e icone dei capitoli provvisorie (dal codice / Material) in attesa di Gemini.
- Su 9:21 le griglie restano limitate dalla larghezza (spazio libero sopra e sotto 333–360 px).

## Da provare sul telefono
- Trascinamento: fluidità, precisione su 7x9 (celle 136 px), dita veloci, rilascio a metà casella, rilascio sul posto (nessuna mossa).
- Uscita: tirare l'auto oltre il varco, uscita della rossa e della blu, uscite laterali e in basso; Annulla dopo un'uscita.
- Lampadina: freccia, scorrimento, stelle massime 2.
- Challenge: contatore rosso, "Mosse finite" (Annulla / Termina), run persa senza annullamenti e Continua; chiudere l'app a metà livello (con un'auto già uscita) e riaprire; tempi di caricamento dei livelli 7x9; Sfida del giorno.
- Home: scheda Traffic jam attiva, record e Sfida del giorno.
