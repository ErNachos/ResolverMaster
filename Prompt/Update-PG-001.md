# Update PG-001 — Pingù: il gioco completo (regole, Storia, lampadina, Challenge)

## Modalità di lavoro (l'utente non è al PC)
- **Non fermarti a fare domande.** Quando c'è una scelta aperta, prendi la soluzione più ragionevole e ricordala per il riepilogo.
- **Piccolo problema**: aggiralo e vai avanti.
- **Problema grave** (il progetto non compila, test rotti che non riesci a riparare): riporta quella parte all'ultima versione funzionante e passa alla parte successiva.
- **Non creare file di note.** Scelte fatte e problemi aperti vanno solo nel riepilogo finale, come messaggio.
- Commit e push a fine lavoro (o dopo ogni parte, se riesci a verificarla da sola), con test verdi, validatore OK e 0 warning. Mai push --force, reset --hard o clean.
- Se l'Editor di Unity è aperto, lavora su una copia in `Logs/BatchCopy/` come già fatto in passato.
- Alla fine scrivi il riepilogo finale e fermati.

## Contesto
Resolver Master ha già due minigiochi completi, Idraulico e Traffic jam, con Storia, Challenge, sfida del giorno, stelle, annullamenti, lampadina, pubblicità con premio (di prova), scorrimento tra i capitoli.
Ora nasce il terzo: **Pingù**, un pinguino che scivola sul ghiaccio.

**Regola del Core:** Pingù fornisce solo le sue parti (regole, generatore, risolutore, tabella della difficoltà, grafica). Tutto il resto si riusa dal Core. Se qualcosa oggi è legato a un altro gioco e serve anche qui, **spostalo nel Core, non copiarlo**.

**Nome:** "Pingù" è uguale in tutte le lingue, ma va comunque nei file delle lingue (una sola voce), così si può cambiare in un punto. Nel codice, nelle cartelle e nelle classi usa un nome tecnico neutro, ad esempio `Penguin`.

Prima di iniziare leggi il Core e la struttura di Traffic jam (l'esempio più recente da seguire).

## Parte A — Regole e modello
- **Griglia rettangolare verticale.** Griglie supportate: 4x6, 5x7, 6x8, 7x9.
- **1 mossa = 1 swipe** (su, giù, sinistra, destra). Il pinguino scivola finché non viene fermato.
- **Lo swipe vale solo quando tutto è fermo.** Gli swipe durante un'animazione vengono ignorati.
- **Swipe a vuoto** (il pinguino non può muoversi in quella direzione): non conta come mossa.
- **Caselle:**
  - **ghiaccio**: si scivola;
  - **roccia**: blocca, il pinguino si ferma nella casella prima;
  - **neve**: il pinguino ci entra e **si ferma lì**;
  - **acqua**: il pinguino ci cade ⇒ **livello fallito**.
- **Bordo della griglia**, per ogni lato o tratto di lato: **muro di neve** (ferma il pinguino come una roccia) o **acqua** (ci cade). Nei primi capitoli tutto muro; più avanti tratti di muro e tratti d'acqua.
- **Igloo:** il pinguino **ci entra appena ci passa sopra**, non serve fermarsi lì.
- **Pesci:** il pinguino li raccoglie passandoci sopra. Finché non li ha presi tutti, l'igloo è **chiuso** e il pinguino ci scivola sopra come sul ghiaccio. Quando li ha presi tutti l'igloo si apre (piccola animazione).
- **Blocchi di ghiaccio:**
  - il pinguino che sbatte contro un blocco si ferma nella casella prima e il blocco **scivola** nella stessa direzione, finché non viene fermato (roccia, muro, altro blocco, pinguino, neve: il blocco si ferma sulla neve come il pinguino);
  - se il blocco non può muoversi, fa da roccia;
  - **blocco nell'acqua** (buco o tratto di bordo interno alla griglia): il blocco **affonda e riempie il buco**, che diventa ghiaccio. I blocchi non cadono fuori dalla griglia: il bordo d'acqua li ferma come un muro (annotalo se scegli diversamente).
- **Due pinguini** (capitoli avanzati):
  - lo swipe muove **entrambi insieme**, nella stessa direzione;
  - si fermano anche **l'uno contro l'altro**;
  - ognuno ha **il suo igloo**; il primo che arriva entra e sparisce, l'altro continua;
  - se uno cade in acqua, il livello è fallito;
  - per distinguerli: sciarpe di colore e motivo diversi (es. arancione a tinta unita e viola a strisce), con la bandierina del suo igloo uguale. Mai solo il colore.
  - i pesci sono raccolti da chi ci passa sopra e valgono per tutti e due.
- **Vittoria:** tutti i pinguini sono nel loro igloo.
- **Fallimento** (caduta in acqua): effetto e messaggio, poi il livello ricomincia con mosse a 0, come l'errore dell'acqua dell'Idraulico. Riusa la logica del Core.
- **Stelle:** le regole del Core.

## Parte B — "Sei bloccato"
In Pingù non tutte le mosse si possono disfare: il pinguino può finire in una posizione da cui non c'è più soluzione.
- Dopo ogni mossa il risolutore controlla se c'è ancora una soluzione. Se non c'è, compare **"Sei bloccato"** con **Annulla** e **Ricomincia** (in Storia).
- In Challenge: Annulla (se restano annullamenti) oppure **Termina** ⇒ sconfitta. Riusa il meccanismo "Mosse finite" del Core se è adatto.

## Parte C — Risolutore e generatore
- **Risolutore**: ricerca in ampiezza sugli stati (posizioni dei pinguini, pesci raccolti, posizioni dei blocchi, buchi riempiti, pinguini già entrati), con **mosse minime esatte**. Tetto sul numero di stati nel file dei parametri.
- **Generatore** deterministico con il generatore casuale del Core, limite in **numero di tentativi** (non in tempo):
  - dispone rocce, neve, acqua, pesci, blocchi, igloo e pinguini secondo il gradino di difficoltà;
  - tiene solo livelli con mosse minime nell'intervallo richiesto;
  - il livello deve **usare la novità del capitolo**: ad esempio nel Capitolo 5 la soluzione deve spingere almeno un blocco, e nei livelli con buchi almeno uno deve servire a riempirne uno;
  - scarta livelli con troppi elementi inutili.
- **Primi livelli di ogni capitolo "che insegnano":** la novità si capisce da sola. Esempio: nel Capitolo 2 i primi pesci stanno **sul percorso obbligatorio** verso l'igloo, così il giocatore li raccoglie senza pensarci e capisce la regola. Applica lo stesso principio alle altre novità.
- Misura i tempi su 7x9 (anche con due pinguini e blocchi) e scrivili nel riepilogo. Obiettivo: controllo "Sei bloccato" e lampadina immediati, generazione sotto 1 s.

## Parte D — Schermata di gioco
- Swipe su tutta l'area della griglia, con una soglia minima per non scambiare un tocco per uno swipe.
- Scivolata a velocità costante, fluida e indipendente dai fps; piccola animazione di urto quando si ferma.
- Animazioni: raccolta del pesce, apertura dell'igloo, ingresso nell'igloo, caduta in acqua, blocco che scivola e che affonda riempiendo il buco.
- **Annulla**: come negli altri giochi (illimitato in Storia, 3 per run in Challenge).
- **Grafica dal codice, provvisoria**, vista dall'alto, stile cartoon con contorni neri: pinguino (con espressioni: normale, felice all'igloo, spaventato quando cade, stordito dopo un urto), igloo, pesce, roccia, neve, acqua, blocco. La grafica definitiva arriverà dopo con Gemini.
- **Home:** nuova scheda "Pingù", con gli stessi accessi degli altri giochi (Storia, Challenge, Sfida del giorno). Immagine della scheda provvisoria.

## Parte E — Storia: 7 capitoli (63 livelli)
Una novità per capitolo, difficoltà a dente di sega (il primo livello di ogni capitolo più facile dell'ultimo del capitolo prima). Sblocco senza obbligo di stelle. Terreni e sfondi dal codice.

| Cap. | Tema | Novità | Griglie |
|---|---|---|---|
| 1 | Lago ghiacciato | Solo ghiaccio, rocce e igloo; bordo tutto muro | 4x6 → 5x7 |
| 2 | Banchisa | **Pesci** da raccogliere | 5x7 |
| 3 | Bufera | **Neve** dove ci si ferma | 5x7 → 6x8 |
| 4 | Disgelo | **Acqua**: buchi nella griglia e tratti di bordo d'acqua | 6x8 |
| 5 | Ghiacciaio | **Blocchi di ghiaccio** da spingere, anche per riempire i buchi | 6x8 |
| 6 | Polo | **Due pinguini** | 6x8 → 7x9 |
| 7 | Aurora | Mix di tutto | 7x9 |

- Livello 1: si risolve in 1–2 mosse.
- L'ultimo livello è impegnativo ma non frustrante.
- Strumento `Resolver Master/Pingù/Genera livelli Storia`, sul modello degli altri giochi.

## Parte F — Lampadina (solo Storia)
- Dopo l'annuncio con premio:
  - se c'è ancora una soluzione, **fa la prossima mossa giusta** (freccia nella direzione, poi la mossa si fa da sola);
  - se il giocatore è bloccato, **annulla automaticamente** fino all'ultima posizione da cui c'è una soluzione (senza consumare annullamenti).
- La mossa della lampadina non conta. Livello con lampadina usata ⇒ al massimo **2 stelle**.
- Disattivata senza annuncio. Nascosta in Challenge.

## Parte G — Challenge e Sfida del giorno
- Riusa dal Core run, punteggio, record, pausa, salvataggio, Continua, Cambia livello e sfida del giorno. Pingù fornisce solo la **tabella a gradini** nel suo file dei parametri: da 4x6 con sola roccia fino a 7x9 con acqua, blocchi e due pinguini. Dente di sega quando entra una novità.
- Massimo di mosse = minimo + margine, come negli altri giochi.
- **Sconfitta**: caduta in acqua, oppure "Sei bloccato" / mosse finite senza annullamenti (o con Termina). Poi vale il Continua con la pubblicità.
- Il salvataggio memorizza tutto lo stato (pinguini, pesci, blocchi, buchi riempiti, pinguini entrati).
- Record, serie e salvataggi separati per gioco.
- Menu `Resolver Master/Pingù/Prova Challenge`.

## Regole generali
- Nessun testo nel codice né nelle immagini; testi nuovi in italiano e inglese.
- Nei prompt e nei commenti non si cita nessun altro gioco esterno all'app.
- Test da aggiungere:
  - scivolata su ghiaccio, stop su roccia, muro e neve, caduta in acqua;
  - swipe a vuoto = 0 mosse, swipe durante un'animazione ignorato;
  - igloo chiuso finché mancano pesci, ingresso passandoci sopra;
  - blocco spinto, blocco che riempie un buco;
  - due pinguini mossi insieme, stop reciproco, ingresso separato;
  - "Sei bloccato" rilevato correttamente;
  - mosse minime esatte su casi piccoli verificati a mano;
  - stesso seed ⇒ stesso livello;
  - lampadina (prossima mossa e uscita dal blocco), non conta e dà al massimo 2 stelle;
  - salvataggio e ripresa identici;
  - 63 livelli caricati e numerati di seguito;
  - Idraulico e Traffic jam continuano a funzionare (tutti i loro test verdi).
- Anteprime con `Make Previews` (anche con i filtri per il daltonismo) su almeno 2 livelli per capitolo.

## Riepilogo finale
Solo come ultimo messaggio (nessun file):
- cosa è stato fatto, parte per parte;
- cosa è stato spostato nel Core;
- tempi di risolutore e generatore;
- tabella compatta delle mosse minime dei 63 livelli;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.