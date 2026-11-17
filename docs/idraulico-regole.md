# Idraulico — regole del gioco

Valide da ErNa-003. Codice: `Assets/Scripts/Games/Plumber/Logic/` (`WaterFlow`, `PlumberPuzzle`, `PlumberSolver`, `PlumberGenerator`).

## Scopo
Ruotare i tubi per collegare ogni **fonte** alle **fontane** (arrivi) del suo colore, poi premere **"Apri l'acqua"**.

## Pezzi
| Pezzo | Aperture | Rotazione |
|---|---|---|
| Dritto | 2 opposte | sì (2 posizioni diverse) |
| Curva | 2 vicine | sì (4 posizioni) |
| T | 3 | sì (4 posizioni). Divide un flusso verso due uscite |
| Croce | 4, tutte collegate tra loro | non serve (1 posizione) |
| Fonte | 1 | no. Colorata come la sua acqua |
| Fontana (arrivo) | 1 | no. Ha sotto un alone del suo colore |

- Un tocco = un quarto di giro in senso orario = **una mossa**. Annulla e Ricomincia come prima.
- Colori: blu e arancione (`PlumberConfig.waterColors`). Aloni delle fontane: `sinkHaloColors`, dimensione `sinkHaloSize`.
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
I capitoli sono di 9 livelli, numerati di seguito (1–27). Lo sblocco segue le regole del Core.

| Cap. | Nome | Contenuto | Griglie |
|---|---|---|---|
| 1 | Prima goccia / First Drop | 1 fonte → 1 fontana, dritti e curve. La T compare solo come inganno: non serve mai, e il validatore lo verifica | 3x4 → 5x7 (Giardino) |
| 2 | Il bivio / The Fork | 1 fonte → 2 fontane dello stesso colore, una T sul percorso | 4x5 → 5x7 (Fattoria) |
| 3 | Due colori / Two Colors | 2 fonti → 2 fontane, una blu e una arancione | 5x7 → 6x8 (Città) |

- La difficoltà sale dentro il capitolo e riparte più bassa all'inizio del successivo: i primi 2 livelli di ogni capitolo nuovo sono facili.
- I livelli 1, 2 e 3 richiedono 1, 2 e 3 mosse.
- Tabella completa dei livelli: `docs/idraulico-livelli.md`.
