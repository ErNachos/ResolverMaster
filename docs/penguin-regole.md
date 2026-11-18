# Pingù — regole (PG-001)

Nome tecnico nel codice: `Penguin` (`Assets/Scripts/Games/Penguin/`). Il nome mostrato, "Pingù", sta nei file delle lingue (`game.penguin.name`).

## Il campo
- Griglie verticali: 4x6, 5x7, 6x8, 7x9 (colonne x righe).
- Caselle: **ghiaccio** (si scivola), **roccia** (ferma nella casella prima), **neve** (chi ci entra si ferma lì), **acqua** (il pinguino ci cade: livello fallito).
- **Bordo**: un tratto per ogni lato di casella sul bordo, **muro di neve** (ferma come una roccia) o **acqua** (il pinguino ci cade; un blocco si ferma come contro un muro).
- **Igloo**: uno per pinguino. Chiuso finché non sono stati presi **tutti** i pesci (ci si scivola sopra come sul ghiaccio); aperto, il pinguino **ci entra appena ci passa sopra** e sparisce. Un pinguino fermo sul suo igloo quando l'ultimo pesce viene preso entra alla fine della mossa.
- **Pesci**: raccolti da chi ci passa sopra; valgono per tutti i pinguini.
- **Blocchi di ghiaccio**: il pinguino che li urta si ferma nella casella prima e il blocco scivola nella stessa direzione finché non lo ferma una roccia, il bordo (muro o acqua), un altro blocco, un pinguino, un pesce non ancora preso o un igloo (scelta: i blocchi non coprono pesci e igloo); si ferma **nella** neve; in un buco d'acqua **affonda** e il buco diventa ghiaccio. Un blocco che non può muoversi fa da roccia. I pinguini spingono i blocchi, i blocchi non spingono altri blocchi.

## La mossa
- 1 mossa = 1 swipe (su, giù, destra, sinistra) su tutta la griglia; sotto la soglia (`swipeThreshold` della cella, almeno `swipeMinUnits`) è un tocco, non uno swipe.
- Lo swipe vale solo quando tutto è fermo; quelli durante un'animazione sono ignorati (`PenguinPlay`).
- Swipe a vuoto (nulla si muove): nessuna mossa.
- Tutti i pinguini scivolano insieme, alla stessa velocità, una casella per passo; in ogni passo si muove prima chi è più avanti nella direzione: due pinguini in fila restano distanziati e si fermano l'uno contro l'altro.
- **Vittoria**: tutti i pinguini nel loro igloo.
- **Caduta in acqua**: effetto, messaggio "Splash! In acqua!", poi il livello ricomincia con mosse a 0 (Storia, come l'errore dell'acqua dell'Idraulico); nella Challenge la run è persa (poi Continua).
- Annulla rimette lo stato prima dello swipe (in Storia illimitato, nella Challenge 3 per run).

## "Sei bloccato"
Dopo ogni mossa la tabella di tutti gli stati raggiungibili (`PenguinSpace`, calcolata una volta per livello) dice se c'è ancora una soluzione. Se non c'è: **Sei bloccato** (Storia: Annulla / Ricomincia; Challenge: Annulla / Termina, sconfitta subito se non restano annullamenti). Il pannello è del Core (`PuzzleBoard.IsStuck`).

## Lampadina (solo Storia)
Dopo l'annuncio: se c'è una soluzione, freccia sui pinguini e la prossima mossa giusta (non conta); se si è bloccati, annulla fino all'ultimo stato con una soluzione. Al massimo 2 stelle.

## Risolutore e generatore
- Ricerca in ampiezza su tutti gli stati (posizioni dei pinguini, pesci presi, blocchi, buchi riempiti, pinguini entrati, impacchettati in 64 bit), poi distanze esatte dalla fine all'indietro. Tetto `maxStates` in `PenguinConfig`.
- Generatore deterministico (`SeededRandom`), limite in tentativi: bozza casuale → salita (si sposta un elemento alla volta) → controlli: mosse minime nel gradino, novità usata (neve necessaria, acqua pericolosa lungo la soluzione, spinta necessaria, buco da riempire necessario), primi pesci sul percorso (livelli che insegnano), al massimo `useless` elementi mai toccati.
- Livelli: `docs/penguin-livelli.md`; Challenge: `docs/penguin-challenge.md`.
