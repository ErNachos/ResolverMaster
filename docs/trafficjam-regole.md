# Traffic jam — regole (TJ-001)

## Griglia e veicoli
- Griglie verticali (più alte che larghe): 4x6, 5x7, 6x8, 7x9. Casella = (colonna, riga), riga 0 in alto.
- **Auto** da 2 caselle e **camion** da 3. Ogni veicolo è orizzontale o verticale e si muove **solo nel suo verso**, avanti e indietro, senza ruotare.
- **Barriere** (ostacoli fissi): caselle occupate che non si muovono mai.
- **Auto obiettivo**: la rossa, e nei capitoli avanzati anche la blu. Ognuna ha la sua **uscita**, un varco nel bordo colorato come lei,
  sulla sua riga o colonna; l'auto è sempre orientata verso la sua uscita. Di base l'uscita è in alto (capitolo 6 in poi anche a destra,
  in basso, a sinistra).
- Sulla riga (o colonna) di un'auto obiettivo non ci sono barriere né altri veicoli nel suo stesso verso (chiuderebbero l'uscita per sempre):
  la attraversano solo veicoli di traverso.

## Mosse
- **1 mossa** = spostare un veicolo lungo la sua linea di quante caselle si vuole, fermandosi contro veicoli, barriere e bordo.
- Rilasciato dov'era: nessuna mossa.
- Un'auto obiettivo con la strada libera fino alla sua uscita può essere spinta fuori: **la spinta che la fa uscire conta come mossa**.
  Uscita, sparisce e libera spazio. Gli altri veicoli non escono mai; ogni auto obiettivo esce solo dalla sua uscita.
- **Vittoria**: tutte le auto obiettivo sono uscite.
- **Stelle**: come l'Idraulico (Core `StarRules`): 3 al minimo, 2 fino a minimo + max(30%, 2), 1 altrimenti; con la lampadina al massimo 2.

## Risolutore (esatto)
- Stato = posizione di ogni veicolo lungo la sua linea, in un numero da 64 bit (4 bit per un'auto obiettivo, che può essere "uscita";
  3 bit per gli altri): fino a 21 veicoli.
- Ricerca in ampiezza **in avanti** dalla partenza: tutte le posizioni raggiungibili (dopo la vittoria non si muove più nulla).
- Ricerca in ampiezza **all'indietro** da tutte le posizioni risolte: la **tabella delle distanze** (mosse che mancano da ogni posizione).
  Gli scorrimenti si possono sempre disfare; un'uscita si disfa rimettendo l'auto su una casella della sua linea con la strada libera.
- Tetto: `TrafficJamConfig.maxStates` (2 milioni) posizioni; oltre, il livello è scartato.
- Lampadina: dalla posizione attuale la prima mossa (ordine fisso: veicoli in ordine, posizioni crescenti, uscita per ultima) che porta a
  "una mossa in meno". È una mossa gratuita (non conta, si può annullare).

## Generatore (deterministico, `SeededRandom` del Core)
1. Disposizione casuale del gradino: auto obiettivo sulle loro linee, poi barriere, camion e auto in posti liberi.
2. Esplorazione di tutte le posizioni (con un tetto più basso per le disposizioni provate: `storyMaxStates`, `challengeMaxStates`).
3. Partenza = la posizione più lontana dalla soluzione con tutte le auto obiettivo dentro, entro il massimo del gradino (a parità: scelta dal seed).
4. Se è più facile del minimo del gradino, un veicolo (o una barriera) si sposta in un altro posto libero e si riesplora; la modifica resta se
   il livello non diventa più facile (salita). Dopo `climbSteps` modifiche si riparte da una disposizione nuova.
- Ogni esplorazione è un tentativo; il limite è in tentativi, mai in tempo. Mosse minime al massimo `maxMinMoves` (40).

## Challenge
- La tabella a gradini e i margini sono in `TrafficJamConfig` (massimo = minimo + margine, dal 60% al 10%, almeno +1).
- **Sconfitta**: arrivati al massimo senza aver risolto, i veicoli si bloccano e compare **"Mosse finite"**: *Annulla* (se restano
  annullamenti) libera una mossa, *Termina* chiude la run; senza annullamenti la run è persa. Dopo la sconfitta vale il Continua.
- Salvataggio: le mosse fatte (veicolo, da, a) e la posizione di ogni veicolo (`x` = auto obiettivo già uscita).
