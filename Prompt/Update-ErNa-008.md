# Update ErNa-008 — Challenge: Continua, Cambia livello e Sfida del giorno

## Modalità di lavoro (l'utente non è al PC)
- **Non fermarti a fare domande.** Quando c'è una scelta aperta, prendi la soluzione più ragionevole e annotala.
- **Piccolo problema**: aggiralo, annotalo e vai avanti.
- **Problema grave** (il progetto non compila, test rotti che non riesci a riparare): riporta quella parte all'ultima versione funzionante, annota e passa alla parte successiva.
- Annota tutto in `Prompt/Note-ErNa-008.md` ("Scelte fatte", "Problemi aperti").
- **Commit e push dopo ogni parte completata** (A, B, C), con test verdi, validatore OK e 0 warning. Mai push --force, reset --hard o clean.
- Alla fine scrivi il riepilogo finale e fermati.

## Contesto
Resolver Master. La run della Challenge funziona (ErNa-007) e la pubblicità con premio esiste in versione di prova.

## Parte A — Continua (salvavita)
- **Un solo salvavita per run.** A fine run, se non è stato usato, compare **Continua** con l'icona della pubblicità.
- Se l'annuncio viene guardato:
  - si riparte dallo **stesso livello**, dall'inizio, con mosse a 0;
  - gli **annullamenti tornano a 3**;
  - stelle e livelli superati restano.
- Il salvavita usato si salva con la run.

## Parte B — Cambia livello
- Pulsante **Cambia livello**, con l'icona della pubblicità, nella schermata della Challenge.
- Attivo **solo con 0 mosse** nel livello. Utilizzabile **tutte le volte che si vuole**, ogni volta con un annuncio.
- Il livello nuovo ha lo **stesso gradino**: seed = hash(seed della run, numero del livello, numero di cambi + 1). Il numero del livello nella run non cambia. Il numero di cambi si salva con la run.
- Dopo un Continua (mosse a 0) torna disponibile.

## Parte C — Sfida del giorno
- **5 livelli** al giorno. Il seed nasce dalla data locale del telefono (AAAAMMGG). Difficoltà in salita, da un gradino medio a uno alto (gradini nel file dei parametri).
- Regole della run: mosse minime e massimo visibili, si perde all'errore dell'acqua, **3 annullamenti**, **un salvavita** con la pubblicità.
- **Niente Cambia livello**: i livelli devono essere uguali per tutti.
- **Un tentativo al giorno.** Persa o completata, si chiude fino a domani.
- Pausa e salvataggio automatico. Una sfida in sospeso resta giocabile anche se cambia la data, ma vale per il suo giorno.
- Punteggio = stelle (massimo 15).
- **Serie di giorni**: +1 se completi tutti e 5 i livelli in un giorno consecutivo; si azzera se salti un giorno o perdi.
- In locale si salvano: risultato di oggi, serie attuale, serie migliore, miglior punteggio.
- **Home**: nella scheda dell'Idraulico, accesso alla Sfida del giorno con lo stato di oggi (da fare / in corso / fatta con N stelle) e la serie attuale. Icone segnaposto.

## Regole generali
- Salvavita, cambio livello e sfida del giorno sono generici: vanno nel Core.
- Nessun testo nel codice; testi nuovi in italiano e inglese.
- Test da aggiungere:
  - Continua una sola volta, con 3 annullamenti ripristinati e lo stesso livello;
  - Cambia livello solo a 0 mosse, con lo stesso gradino e livelli diversi a ogni cambio;
  - ripresa dopo il salvataggio con il livello cambiato;
  - stessa data ⇒ stessi 5 livelli; un solo tentativo; niente Cambia livello nella sfida del giorno;
  - serie che cresce, si azzera saltando un giorno o perdendo;
  - cambio di data durante una sfida in sospeso;
  - pulsanti disattivati senza annuncio.

## Riepilogo finale
In `Prompt/Note-ErNa-008.md` e come ultimo messaggio:
- cosa è stato fatto, parte per parte;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.