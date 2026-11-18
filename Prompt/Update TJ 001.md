# Update TJ-001 — Traffic jam: il gioco completo (regole, Storia, lampadina, Challenge)

## Modalità di lavoro (l'utente non è al PC)
- **Non fermarti a fare domande.** Quando c'è una scelta aperta, prendi la soluzione più ragionevole e annotala.
- **Piccolo problema**: aggiralo, annotalo e vai avanti.
- **Problema grave** (il progetto non compila, test rotti che non riesci a riparare): riporta quella parte all'ultima versione funzionante, annota e passa alla parte successiva.
- Annota tutto in `Prompt/Note-TJ-001.md` ("Scelte fatte", "Problemi aperti").
- **Commit e push dopo ogni parte completata** (A–F), con test verdi, validatore OK e 0 warning. Mai push --force, reset --hard o clean.
- Se l'Editor di Unity è aperto, lavora su una copia in `Logs/BatchCopy/` come già fatto in passato. Alla fine riporta gli asset generati dopo averli controllati con un diff.
- Alla fine scrivi il riepilogo finale e fermati.

## Contesto
Resolver Master ha già un minigioco completo, l'Idraulico, con Storia, Challenge, sfida del giorno, stelle, annullamenti, lampadina e pubblicità con premio (di prova).
Ora nasce il secondo minigioco: **Traffic jam**. Nella Home c'è già la sua scheda ("Prossimamente").

**Regola del Core:** Traffic jam deve fornire solo le sue parti (regole, generatore, risolutore, tabella della difficoltà, grafica). Tutto il resto si riusa dal Core. Se qualcosa oggi è legato all'Idraulico e serve anche qui, **spostalo nel Core, non copiarlo**. Annota cosa hai spostato.

Prima di iniziare leggi il Core, la struttura dell'Idraulico (come esempio da seguire), il file dei parametri, il validatore, `Make Previews` e lo strumento `Idraulico/Genera livelli Storia`.

## Parte A — Regole e modello
- **Griglia rettangolare verticale** (più alta che larga). Griglie supportate: 4x6, 5x7, 6x8, 7x9.
- **Veicoli**: auto da 2 caselle e camion da 3. Ogni veicolo è orizzontale o verticale e si muove **solo nel suo verso**, avanti e indietro, senza ruotare.
- **Ostacoli fissi**: caselle occupate che non si muovono (barriere da cantiere).
- **Auto obiettivo**: l'auto rossa. **Uscita** su un lato della griglia, allineata alla riga o alla colonna dell'auto, che è sempre orientata verso la sua uscita. Di base l'uscita è **in alto**.
- **Due auto obiettivo** (capitoli avanzati): rossa e blu, ognuna con la sua uscita colorata. Ogni auto può uscire **solo** dalla sua uscita, e gli altri veicoli non escono. Quando un'auto è uscita sparisce e libera spazio.
- **1 mossa** = spostare un veicolo di quante caselle vuoi. Se lo rilasci dove era, non conta. L'ultima spinta che fa uscire l'auto conta come mossa.
- **Vittoria**: tutte le auto obiettivo sono uscite.
- **Stelle**: le stesse regole dell'Idraulico (dal Core).
- Colori: rosso e blu sono riservati alle auto obiettivo e alle loro uscite. Gli altri veicoli usano colori diversi e ben distinguibili (es. giallo, bianco, beige, grigio, viola chiaro). Mai rosso e verde insieme come colori con un significato.

## Parte B — Risolutore e generatore
- **Risolutore**: ricerca in ampiezza sugli stati della griglia, con **mosse minime esatte**.
  - Tutte le mosse si possono disfare, quindi da qualsiasi posizione raggiungibile esiste sempre una soluzione.
  - Calcola anche la **tabella delle distanze**: per ogni posizione raggiungibile, quante mosse mancano alla fine. Si ottiene con una ricerca in ampiezza all'indietro, partendo da tutte le posizioni risolte. Servirà alla lampadina.
  - Tetto sul numero di stati (nel file dei parametri, es. 2 milioni). I livelli che lo superano vengono scartati dal generatore.
- **Generatore**, deterministico con il generatore casuale del Core:
  1. dispone i veicoli a caso, secondo il gradino di difficoltà (griglia, numero di auto e camion, ostacoli, numero di auto obiettivo, lato dell'uscita);
  2. esplora tutte le posizioni raggiungibili;
  3. come partenza sceglie la posizione **più lontana dalla soluzione**, oppure una con le mosse minime nell'intervallo richiesto.
  - Tetto di circa 40 mosse minime.
  - Limite in **numero di tentativi**, non in tempo (come l'Idraulico).
- Misura i tempi su 7x9 e scrivili nel riepilogo. Obiettivo: lampadina immediata e generazione sotto 1 s.

## Parte C — Schermata di gioco
- **Trascinamento** con il dito, lungo il verso del veicolo, fermandosi contro gli altri veicoli e gli ostacoli. Al rilascio il veicolo si aggancia alla casella più vicina. Movimento fluido e indipendente dai fps.
- L'auto obiettivo esce con una breve animazione oltre il bordo.
- **Annulla**: come nell'Idraulico (illimitato in Storia, 3 per run in Challenge).
- **Grafica dal codice, provvisoria**: stile cartoon con contorni neri e forme arrotondate. Veicoli visti **dall'alto** (rettangoli arrotondati con vetri e ruote), camion più lunghi, ostacoli come barriere a strisce, uscite come varco nel bordo colorato come la sua auto. La grafica definitiva arriverà dopo con Gemini.
- Nell'UI non usare il rosso e il blu delle auto obiettivo.
- Nella Home la scheda di Traffic jam diventa attiva (non più "Prossimamente"), con gli stessi accessi dell'Idraulico: Storia, Challenge, Sfida del giorno.

## Parte D — Storia: 7 capitoli (63 livelli)
Una novità per capitolo, difficoltà a dente di sega (il primo livello di ogni capitolo è più facile dell'ultimo del capitolo prima). Sblocco dei capitoli senza obbligo di stelle. Terreni e sfondi dal codice, senza usare rosso e blu.

| Cap. | Tema | Novità | Griglie |
|---|---|---|---|
| 1 | Parcheggio | Solo auto, uscita in alto | 4x6 → 5x7 |
| 2 | Mercato | **Camion** | 5x7 |
| 3 | Centro città | **Griglia più grande** | 6x8 |
| 4 | Lavori in corso | **Ostacoli fissi** | 6x8 |
| 5 | Aeroporto | **Due auto obiettivo** (rossa e blu, uscite colorate) | 6x8 |
| 6 | Incrocio | **Uscite su lati diversi** (non solo in alto) | 6x8 → 7x9 |
| 7 | Ora di punta | Mix di tutto | 7x9 |

- Livello 1: pochissimi veicoli, si risolve in 1–2 mosse.
- L'ultimo livello è impegnativo ma non frustrante.
- Crea lo strumento `Resolver Master/Traffic jam/Genera livelli Storia`, sul modello di quello dell'Idraulico. Genera i livelli, verifica le mosse minime con il risolutore e controlla che la difficoltà cresca dentro ogni capitolo.

## Parte E — Lampadina (solo Storia)
- Dopo l'annuncio con premio la lampadina **fa la prossima mossa giusta**: dalla tabella delle distanze prende un veicolo e una posizione che portano a "una mossa in meno". Una freccia mostra il veicolo e il verso, poi il veicolo si sposta con animazione.
- Se le mosse giuste sono più di una, scegline una in modo fisso (deterministico).
- La mossa della lampadina **non conta**. Livello con lampadina usata ⇒ al massimo **2 stelle**.
- Disattivata se il livello è già risolto o se non c'è un annuncio. Nascosta in Challenge.
- Riusa dal Core la logica generica dell'aiuto e il tetto di stelle.

## Parte F — Challenge e Sfida del giorno
- Riusa dal Core run, punteggio, record, pausa, salvataggio, Continua, Cambia livello e sfida del giorno. Traffic jam fornisce solo la **tabella a gradini** nel suo file dei parametri: si parte da 4x6 con poche auto e si arriva a 7x9 con camion, ostacoli, due auto obiettivo e uscite su lati diversi. Dente di sega quando entra una novità.
- Massimo di mosse = minimo + margine, come nell'Idraulico (valori nel file dei parametri).
- **Sconfitta**: in Traffic jam non c'è il "controllo" dell'acqua.
  - Quando le mosse arrivano al massimo senza aver risolto, i veicoli si bloccano e compare "Mosse finite".
  - Se restano annullamenti, il giocatore può annullare per liberare mosse oppure premere **Termina**; se non ne restano, la run è persa.
  - Dopo la sconfitta vale il Continua con la pubblicità, come nell'Idraulico.
- Il salvataggio deve memorizzare le posizioni dei veicoli e quali auto obiettivo sono già uscite.
- Record, serie e salvataggi separati per gioco.
- Menu `Resolver Master/Traffic jam/Prova Challenge`, sul modello di quello dell'Idraulico.

## Regole generali
- Nessun testo nel codice; tutti i testi nuovi in italiano e inglese. Nessun testo nelle immagini.
- Nei prompt e nei commenti non si cita nessun altro gioco.
- Test da aggiungere:
  - movimento solo nel verso del veicolo, stop contro veicoli e ostacoli;
  - rilascio nella posizione di partenza = 0 mosse;
  - uscita solo per l'auto giusta dalla sua uscita;
  - mosse minime esatte su casi piccoli verificati a mano;
  - stesso seed ⇒ stesso livello;
  - la lampadina riduce sempre di 1 le mosse mancanti, non conta e dà al massimo 2 stelle;
  - "Mosse finite" e sconfitta in Challenge;
  - salvataggio e ripresa identici;
  - 63 livelli caricati e numerati di seguito;
  - l'Idraulico continua a funzionare come prima (tutti i suoi test verdi).
- Anteprime con `Make Previews` (anche con i filtri per il daltonismo) su almeno 2 livelli per capitolo.

## Riepilogo finale
In `Prompt/Note-TJ-001.md` e come ultimo messaggio:
- cosa è stato fatto, parte per parte;
- cosa è stato spostato nel Core;
- tempi di risolutore e generatore;
- tabella compatta delle mosse minime dei 63 livelli;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.