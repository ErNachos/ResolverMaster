# Idraulico — regole del gioco

Valide da ErNa-003; ponte, pezzi bloccati e 4 colori da ErNa-006. Codice: `Assets/Scripts/Games/Plumber/Logic/` (`WaterFlow`, `PlumberPuzzle`, `PlumberSolver`, `PlumberGenerator`).

## Scopo
Ruotare i tubi per collegare ogni **fonte** alle **fontane** (arrivi) del suo colore, poi premere **"Apri l'acqua"**.

## Pezzi
| Pezzo | Aperture | Rotazione |
|---|---|---|
| Dritto | 2 opposte | sì (2 posizioni diverse) |
| Curva | 2 vicine | sì (4 posizioni) |
| T | 3 | sì (4 posizioni). Divide un flusso verso due uscite |
| Croce | 4, tutte collegate tra loro: **mescola** i flussi di colori diversi; dal Capitolo 10 anche **divisore**: un flusso che entra esce dagli altri 3 lati (fino a 3 fontane dello stesso colore) | no (uguale in ogni rotazione): il tocco fa solo una scossa e non conta |
| Ponte (ErNa-006) | 4, ma come **due tubi separati**: nord–sud ed est–ovest, che non si mescolano | no (uguale in ogni rotazione): scossa, nessuna mossa |
| Fonte | 1 | no. Colorata come la sua acqua |
| Fontana (arrivo) | 1 | no. Ha sotto un alone del suo colore |

- Un tocco = un quarto di giro in senso orario = **una mossa**. Annulla e Ricomincia come prima.
- **Pezzi bloccati** (ErNa-006, `#` nel formato): qualsiasi tubo può esserlo. Ha sotto una piastra scura con 4 bulloni, non ruota; il tocco fa una scossa e non conta. Il risolutore lo tiene com'è. Il generatore li mette sul percorso già giusti (vincoli) o fuori (ostacoli, inganni). Nel Core: `CellLockSet`.
- Ponte: il tubo nord–sud è disegnato sopra (intero, con flange e ombra) e il suo flusso sopra; quello est–ovest sotto. Per il risolutore è due celle separate (`PipeNodes`).
- Colori, in ordine di comparsa: blu, arancione, viola, giallo (`PlumberConfig.waterColors`; mai rosso con verde, luminosità distanti, controllati dal validatore). Aloni delle fontane: `sinkHaloColors`, dimensione `sinkHaloSize`.
- Mentre si ruota, l'acqua **non** si vede: i tubi restano grigi.

## Aprire l'acqua
- Il pulsante "Apri l'acqua" **non** conta come mossa.
- L'acqua parte da tutte le fonti nello stesso momento e avanza una cella alla volta. Passa da una cella alla vicina solo se le due aperture si guardano.
- Durante l'animazione i tocchi sui tubi sono bloccati, come Annulla, Ricomincia e il pulsante stesso.

### Tentativo riuscito
Valgono tutte queste condizioni:
1. **nessuna perdita**: ogni apertura bagnata finisce in un tubo collegato o in una fontana;
2. ogni fontana riceve acqua **del suo colore**;
3. due colori diversi **non si toccano** mai.

Le celle che l'acqua non raggiunge non contano. Le fontane si riempiono, poi compare il pannello di vittoria.

### Tentativo sbagliato
| Errore | Quando | Effetto | Messaggio |
|---|---|---|---|
| Perdita | l'acqua esce da un'apertura verso il bordo o verso un lato chiuso | schizzi dall'apertura | "Perdita!" |
| Colori mescolati | due colori si incontrano | le due celle lampeggiano | "Colori mescolati!" |
| Colore sbagliato | una fontana riceve l'altro colore | la fontana si riempie del colore sbagliato e lampeggia | "Colore sbagliato!" |
| Fontana asciutta | una fontana non riceve acqua | la fontana lampeggia | "Fontana asciutta!" |

- L'acqua scorre fino al **primo** errore, in ordine di tempo; a parità di tempo vale l'ordine della tabella.
- Poi parte l'effetto e il messaggio compare al posto del pulsante. Infine il **livello ricomincia da capo**: mosse a 0 e annulla svuotato.
- Durate e colori sono in `PlumberConfig`: `waterStep`, `sinkFill`, `winPause`, `failEffect`, `failPause`, `failBlinks`, `errorColor`.

## Stelle
Contano le **rotazioni del tentativo riuscito**, confrontate con le mosse minime. Le soglie sono quelle del Core (`StarRules`): 3 stelle al minimo, 2 stelle fino a minimo + max(30%, 2), altrimenti 1.

## Mosse minime (risolutore)
- Una soluzione è una rete chiusa di celle bagnate. Ogni apertura di una cella bagnata guarda una cella bagnata che apre verso di lei. Ogni parte collegata della rete ha un solo colore. Ogni parte che contiene una fontana contiene anche una fonte.
- Le celle fuori dalla rete restano come sono e costano 0.
- Il costo di una cella è il numero minimo di tocchi orari per raggiungere l'orientamento voluto. Le posizioni simmetriche contano come una sola.
- La ricerca è esatta e copre tutte le soluzioni (IDA\* sulle "estremità aperte" della rete).
- Il risolutore è stato confrontato con una ricerca che prova tutte le rotazioni, su 32.132 griglie piccole casuali: nessuna differenza.

## Capitoli della Storia
I capitoli sono di 9 livelli, numerati di seguito (1–117). Una novità per capitolo. Lo sblocco segue le regole del Core.

| Cap. | Nome | Contenuto | Griglie |
|---|---|---|---|
| 1 | Prima goccia / First Drop | 1 fonte → 1 fontana, dritti e curve. La T compare solo come inganno: non serve mai, e il validatore lo verifica | 3x4 → 5x7 (Giardino) |
| 2 | Il bivio / The Fork | 1 fonte → 2 fontane dello stesso colore, una T sul percorso | 4x5 → 5x7 (Fattoria) |
| 3 | Due colori / Two Colors | 2 fonti → 2 fontane, una blu e una arancione | 5x7 → 6x8 (Città) |
| 4 | Ponti di ferro / Iron Bridges | 2 colori; un **ponte** necessario in ogni livello; croci trappola negli ultimi | 5x7 → 6x8 (Fabbrica) |
| 5 | Cantiere bloccato / Locked Site | 2 colori; **pezzi bloccati**; un ponte negli ultimi 3 | 5x7 → 6x8 (Cantiere) |
| 6 | Tre colori / Three Colors | **3 colori**; niente bloccati, pochi ponti; i primi 2 livelli ariosi | 6x8 (Porto) |
| 7 | Alta quota / High Up | mix: 2–3 colori, ponti, bloccati, croci trappola | 6x8, 7x9 negli ultimi 2 (Montagna) |
| 8 | Quattro colori / Four Colors | **4 colori**; i primi 2 con percorsi brevi e separati, poi ponti | 6x8 → 7x9 (Laboratorio) |
| 9 | Il gran finale / The Grand Finale | mix finale: 3–4 colori, tutto | 7x9 (Castello) |
| 10 | Mille fontane / A Thousand Fountains | **1 fonte → 3–4 fontane**, con T e **croce come divisore** | 6x8 → 7x9 (Terme) |
| 11 | Binari incrociati / Crossing Tracks | **2 fonti, ognuna verso 2–3 fontane**, con ponti | 6x8 → 7x9 (Stazione) |
| 12 | Gallerie profonde / Deep Tunnels | **3 fonti con diramazioni**, più tubi bloccati | 6x8 → 7x9 (Miniera) |
| 13 | La grande reggia / The Great Palace | finale: **4 fonti con diramazioni**, tutto insieme | 7x9 (Reggia) |

- La difficoltà sale dentro il capitolo e riparte più bassa all'inizio del successivo: i primi 2 livelli di ogni capitolo nuovo sono facili.
- I livelli 1, 2 e 3 richiedono 1, 2 e 3 mosse.
- "Ponte necessario": con croci al posto dei ponti il livello diventa impossibile (validatore).
- L'ultimo livello resta entro il massimo del capitolo precedente + 30% (`PlumberConfig.finalLevelMaxRatio`, validatore).
- ErNa-009, capitoli 10–13: ogni livello ha almeno una fonte che alimenta 2 o più fontane; la difficoltà non scende mai dentro il capitolo; i livelli 1–81 restano identici (il generatore si ferma senza salvare se cambierebbero). Il generatore costruisce un albero per colore (`Layout.Tree`): dalla fonte alla prima fontana, poi un ramo per ogni fontana in più da un tubo dello stesso albero (diventa una T) o da una T (diventa una croce, il divisore).
- Tabella completa dei livelli: `docs/idraulico-livelli.md`.
