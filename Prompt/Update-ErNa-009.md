# Update ErNa-009 — Idraulico: Capitoli 10–13 e scorrimento tra i capitoli

## Modalità di lavoro (l'utente non è al PC)
- **Non fermarti a fare domande.** Quando c'è una scelta aperta, prendi la soluzione più ragionevole e annotala.
- **Piccolo problema**: aggiralo, annotalo e vai avanti.
- **Problema grave** (il progetto non compila, test rotti che non riesci a riparare): riporta quella parte all'ultima versione funzionante, annota e passa alla parte successiva.
- **Non creare file di note.** Scelte fatte e problemi aperti vanno solo nel riepilogo finale, come messaggio.
- Se in `Prompt/` esistono file `Note-*.md` creati dagli update precedenti, cancellali e includi la cancellazione nel commit.
- Commit e push a fine lavoro (o dopo ogni parte, se riesci a verificarla da sola), con test verdi, validatore OK e 0 warning. Mai push --force, reset --hard o clean.
- Se l'Editor di Unity è aperto, lavora su una copia in `Logs/BatchCopy/` come già fatto in passato.
- Alla fine scrivi il riepilogo finale e fermati.

## Contesto
Resolver Master, minigioco Idraulico: 9 capitoli di Storia (81 livelli), l'ultimo arriva a circa 21–22 mosse minime. L'utente li ha finiti tutti e vuole **capitoli più lunghi**, con più mosse.

C'è anche una meccanica che si è persa per strada: **una fonte che alimenta più arrivi** tramite le T. C'era nel Capitolo 2 e poi è sparita quasi del tutto. I capitoli nuovi la riportano al centro.

## Parte A — Scorrimento tra i capitoli (Core)
Suggerimento di un tester: dentro la schermata di un capitolo (la griglia 3x3 dei livelli) si deve poter passare al capitolo **precedente o successivo scorrendo con il dito a destra e a sinistra**.
- Stesso comportamento della Home a schede: scorrimento fluido, aggancio alla pagina, **pallini** che indicano il capitolo.
- I capitoli bloccati si vedono, con il lucchetto e i livelli non toccabili.
- Funziona per **tutti i giochi** (Idraulico e Traffic jam): va nel Core.
- Il pulsante Indietro e il ritorno da un livello portano al capitolo giusto.

## Parte B — La croce come divisore
- Fino a ora la croce era soprattutto una trappola. Nei capitoli nuovi diventa anche **utile**: una fonte che entra in una croce esce da tre lati e può alimentare **3 arrivi** dello stesso colore.
- Resta valida la regola: se nella croce entrano due colori diversi, si mescolano (errore).
- Controlla che generatore e risolutore gestiscano bene le croci usate come divisore.

## Parte C — Capitoli 10–13
Regola: una novità per capitolo, difficoltà a dente di sega (il primo livello di ogni capitolo più facile dell'ultimo del capitolo prima). Terreni e sfondi dal codice, senza colori vicini a quelli dell'acqua. Sblocco senza obbligo di stelle. Griglie fino a 7x9 (non andare oltre).

Per alzare le mosse minime: percorsi più lunghi che riempiono la griglia, più pezzi da ruotare, meno pezzi già giusti alla partenza. Evita le mosse "di riempimento" (tanti pezzi inutili fuori percorso): la difficoltà deve venire dal ragionamento.

| Cap. | Livelli | Tema | Novità | Mosse minime (circa) |
|---|---|---|---|---|
| 10 | 82–90 | Terme | **1 fonte → 3–4 arrivi**, con T e croce come divisore | da 15 a 25 |
| 11 | 91–99 | Stazione | **2 fonti, ognuna verso 2–3 arrivi**, con ponti | da 18 a 28 |
| 12 | 100–108 | Miniera | **3 fonti con diramazioni**, più tubi bloccati | da 20 a 32 |
| 13 | 109–117 | Reggia | Finale: **4 fonti con diramazioni**, tutto insieme | da 22 a 35 |

- In ogni livello almeno una fonte deve alimentare **2 o più arrivi**.
- L'ultimo livello è impegnativo ma non frustrante.
- Genera con `Idraulico/Genera livelli Storia`, verifica le mosse minime con il risolutore e controlla la crescita della difficoltà dentro ogni capitolo.
- I livelli 1–81 restano **identici**.

## Parte D — Verifiche
- Anteprime con `Make Previews`, anche con i filtri per il daltonismo, su almeno 2 livelli per capitolo nuovo.
- Misura i tempi del risolutore sui livelli più lunghi.
- Controlla che la lista dei capitoli scorra bene con 13 capitoli.

## Regole generali
- Nessun testo nel codice; nomi dei capitoli e testi nuovi in italiano e inglese.
- Test da aggiungere: scorrimento tra capitoli (precedente, successivo, bloccato, ritorno al capitolo giusto); croce come divisore verso 3 arrivi; croce con due colori = errore; 117 livelli caricati e numerati di seguito; livelli 1–81 invariati; ogni livello nuovo ha almeno una fonte con 2 o più arrivi; tutti i test di Traffic jam ancora verdi.

## Riepilogo finale
Solo come ultimo messaggio (nessun file):
- cosa è stato fatto, parte per parte;
- tabella compatta delle mosse minime dei livelli 82–117;
- tempi del risolutore;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.