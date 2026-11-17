# Update ErNa-006 — Idraulico: 4 fonti, ponte, tubi bloccati e Capitoli 4–9

## Modalità di lavoro (l'utente non è al PC)
- **Non fermarti a fare domande.** Quando c'è una scelta aperta, prendi la soluzione più ragionevole e annotala.
- **Piccolo problema** (un dettaglio che non riesci a fare, un test marginale, un caso limite): aggiralo, annotalo e vai avanti.
- **Problema grave** (il progetto non compila, test esistenti rotti che non riesci a riparare): non lasciare il repository rotto. Riporta quella parte all'ultima versione funzionante, annota e passa alla parte successiva.
- Annota tutto in `Prompt/Note-ErNa-006.md`, nelle sezioni "Scelte fatte" e "Problemi aperti".
- **Commit e push dopo ogni parte completata** (A, B, C, D), con test verdi, validatore OK e 0 warning. Mai push --force, reset --hard o clean.
- Alla fine scrivi il riepilogo finale (vedi in fondo) e fermati.

## Contesto
Resolver Master, minigioco Idraulico. Oggi ci sono 3 capitoli di Storia (27 livelli), al massimo 2 fonti (blu e arancione), griglie fino a 6x8.
Prima di iniziare leggi il codice: pezzi, flusso dell'acqua, disegno dei tubi, fonti/arrivi con maschera magenta, generatore, risolutore, file dei parametri, validatore, `Make Previews` e `Idraulico/Genera livelli Storia`.

## Parte A — Fino a 4 fonti
1. **Colori dell'acqua**, in ordine di comparsa: blu, arancione (già presenti, non cambiarli), **viola**, **giallo**. Prendi le tonalità dalla palette Okabe-Ito: un viola tendente al magenta e un giallo caldo, più chiaro dell'arancione. Tutti i colori in una sola lista nella configurazione dell'Idraulico. Solo colori, niente simboli; mai rosso e verde insieme.
2. **Validatore**: niente rosso + verde nella palette; differenza minima di luminosità tra ogni coppia (soglia in configurazione); l'UI e gli sfondi dei temi non usano colori vicini a quelli dell'acqua.
3. **Griglia 7x9** tra quelle supportate. Celle di circa 145–150 px su uno schermo largo 1080 px; nessuna cella fuori dallo schermo, anche a 9:21.
4. **Generatore e risolutore** con 1–4 fonti. Le mosse minime devono restare **esatte**. Misura i tempi su 7x9 con 4 fonti; obiettivo sotto 1 s a livello. Se serve ottimizza (potatura per costo minimo, prima i pezzi più vincolati), senza passare a una stima.
5. **Anteprime daltonismo**: `Make Previews` salva anche le versioni filtrate per deuteranopia, protanopia e tritanopia.

## Parte B — Ponte e croce
1. **Ponte**: collega nord–sud ed est–ovest come due tubi separati, che non si mescolano. Grafica dal codice: tubo sopra intero, con una leggera ombra su quello sotto; tubo sotto interrotto. I due flussi d'acqua scorrono indipendenti; quello sopra si disegna sopra.
2. Ponte e croce sono uguali in tutte le rotazioni: **toccarli non conta come mossa** (solo una piccola scossa). Correggi la croce se oggi conta. Ponte e croce devono essere molto diversi a colpo d'occhio.
3. La croce resta (collega le 4 aperture e mescola): serve a dividere un flusso o come trappola.

## Parte C — Tubi bloccati
1. Qualsiasi pezzo può essere **bloccato**: non ruota, al tocco fa la scossa e non conta mosse.
2. Grafica dal codice: piastra scura sotto il pezzo con 4 bulloni agli angoli, visibile su tutti i terreni.
3. Il risolutore li tratta come fissi. Il generatore li mette **sul percorso**, già giusti, come vincoli, oppure **fuori** come ostacoli o inganni.
4. Il formato dei livelli salva i pezzi bloccati; i livelli vecchi restano validi.
5. Il concetto di "elemento bloccato della griglia" va nel Core.

## Parte D — Capitoli 4–9
Regola: **una novità per capitolo**. Difficoltà a dente di sega: il primo livello di ogni capitolo è più facile dell'ultimo del capitolo prima. Terreni e sfondi generati dal codice, senza colori vicini a quelli dell'acqua.

| Cap. | Livelli | Tema | Contenuto | Griglie |
|---|---|---|---|---|
| 4 | 28–36 | Fabbrica (grigi, mattone) | 2 fonti; **ponte** necessario in ogni livello; croce trappola negli ultimi | 5x7 → 6x8 |
| 5 | 37–45 | Cantiere (terra battuta, sabbia scura) | 2 fonti; **tubi bloccati**; ponte negli ultimi 3 | 5x7 → 6x8 |
| 6 | 46–54 | Porto (banchina, legno, pietra) | **3 fonti**; niente bloccati, pochi ponti; primi 2 livelli ariosi | 6x8 |
| 7 | 55–63 | Montagna (roccia, prato alpino scuro) | Mix: 2–3 fonti, ponti, bloccati, croci trappola | 6x8, 7x9 negli ultimi 2 |
| 8 | 64–72 | Laboratorio (piastrelle chiare, grigio freddo) | **4 fonti**; primi 2 livelli con percorsi brevi e separati, poi ponti | 6x8 → 7x9 |
| 9 | 73–81 | Castello (pietra grigia calda, erba scura) | Mix finale: 3–4 fonti, tutto | 7x9 |

- Il mare del Porto **non** deve somigliare all'acqua blu: usa un verde acqua scuro o un grigio-azzurro spento.
- L'ultimo livello deve essere impegnativo ma **non frustrante**: mosse minime entro il massimo del Capitolo 8 più circa il 30%.
- Genera con lo strumento, verifica le mosse minime con il risolutore, fai le anteprime daltonismo su almeno 2 livelli dei capitoli 6, 7, 8 e 9.
- Lo sblocco dei capitoli resta senza obbligo di stelle.

## Regole generali
- Nessun testo nel codice; nomi dei capitoli e testi nuovi in italiano e inglese.
- Regola del Core: ciò che serve a più giochi sta nel Core.
- Test da aggiungere: palette valida; livelli con 3–4 fonti risolvibili; mosse minime esatte su casi piccoli; ponte senza mescolamento; ponte, croce e bloccati non contano mosse; compatibilità dei livelli vecchi; 81 livelli caricati e numerati di seguito.

## Riepilogo finale
In `Prompt/Note-ErNa-006.md` e come ultimo messaggio:
- cosa è stato fatto, parte per parte;
- tempi del risolutore;
- tabella compatta delle mosse minime di tutti gli 81 livelli;
- esito delle anteprime daltonismo;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.