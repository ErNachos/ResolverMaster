# Update-ErNa-003 — Idraulico: "Apri l'acqua", colori e capitoli

## Contesto
- **Prima di tutto:** controlla che i commit di ErNa-001 e ErNa-002 siano su GitHub. Se `main` locale è avanti rispetto a `origin/main`, fai `git push`. Poi il solito `git pull --ff-only`.
- Unity è **chiuso** durante questo Update.
- La prima prova sul telefono è andata bene: il gioco è intuitivo e veloce. Questo Update cambia le regole dell'Idraulico e introduce i **capitoli**.
- **Regola del Core (vedi `GUIDELINES.md`):** i capitoli, la loro schermata e lo sblocco vanno nel **Core**, perché serviranno identici al Traffic jam. Il pulsante d'azione principale va nel Core come elemento **opzionale** della schermata di gioco (il Traffic jam non lo userà).

### Nuove regole dell'Idraulico
- **Prima colleghi, poi apri.** Mentre il giocatore ruota i tubi, l'acqua **non** si vede: i tubi restano grigi.
- Un grande pulsante **"Apri l'acqua"** avvia l'acqua da tutte le fonti contemporaneamente. Premerlo **non** conta come mossa.
- **Tentativo riuscito** se valgono tutte queste condizioni:
  - ogni estremità bagnata finisce in un tubo collegato o in un arrivo (**nessuna perdita**);
  - ogni arrivo riceve acqua **del suo colore**;
  - due colori diversi **non si toccano** mai.
- **Tentativo sbagliato** in tutti gli altri casi:
  - **perdita:** l'acqua esce da un'apertura verso il vuoto o verso un pezzo non combaciante;
  - **colore sbagliato:** due colori si incontrano, o un arrivo riceve il colore sbagliato;
  - **fontana asciutta:** un arrivo non riceve acqua.
- Con un tentativo sbagliato: l'acqua scorre fino all'errore, il punto viene evidenziato con un breve effetto (schizzi per la perdita, evidenziazione per i colori, fontana che lampeggia se asciutta), compare un breve messaggio, poi il **livello ricomincia da capo** con mosse a 0 e annulla svuotato.
- Le celle che l'acqua non raggiunge non contano.
- **Colori:** blu e arancione, in `PlumberConfig`. Fonte e arrivo dello stesso colore hanno anche lo **stesso simbolo** (per esempio goccia e foglia, da Material Symbols), così il gioco funziona anche per chi distingue male i colori.
- **Pezzi:**
  - **T:** divide un flusso verso due uscite. Siccome perde se un ramo resta aperto, si usa sul percorso solo dove servono due arrivi.
  - **Croce:** ha 4 aperture tutte collegate tra loro (resta così in questo Update; i flussi che si incrociano senza mescolarsi arriveranno con il capitolo 4).
- Durante l'animazione dell'acqua i tocchi sui tubi sono bloccati.

### Stelle
- Invariate: contano le **rotazioni del tentativo riuscito**, confrontate con le mosse minime.

### Capitoli (nel Core)
- I livelli sono **numerati di seguito** (Livello 1, 2, … 27) e raggruppati in **capitoli da 9**, ognuno con un nome tradotto e un tema.
- **Storia → lista dei capitoli:** nome, stelle ottenute su 27, lucchetto.
- **Capitolo → griglia 3x3 dei livelli:** numero, stelle, lucchetto.
- **Sblocco:**
  - dentro un capitolo, il livello successivo si sblocca risolvendo il precedente;
  - il capitolo successivo si sblocca **finendo tutti i 9 livelli** del precedente, **senza** obbligo di stelle.
- Identificativo di ogni livello stabile (gioco, capitolo, indice), usato nel salvataggio.
- La difficoltà sale "a dente di sega": cresce dentro il capitolo e scende un po' all'inizio del successivo, quando arriva la novità.

### Capitoli dell'Idraulico in questo Update
| Cap. | Nome (IT / EN) | Contenuto | Griglie indicative |
|---|---|---|---|
| 1 | Prima goccia / First Drop | 1 fonte → 1 fontana, dritti e curve. La T può comparire **solo come pezzo inganno**, mai sul percorso | da 3x3 a 5x5 |
| 2 | Il bivio / The Fork | 1 fonte → 2 fontane dello stesso colore, la T sul percorso | da 4x4 a 5x5 |
| 3 | Due colori / Two Colors | 2 fonti → 2 fontane, una blu e una arancione | da 5x5 a 6x6 |

- I primi livelli del capitolo 1 seguono ancora l'idea "livello 1 = 1 mossa, livello 2 = 2 mosse, livello 3 = 3 mosse".
- I primi 2 livelli di ogni capitolo nuovo sono **facili**, per far capire la novità.

## NON toccare
- `ArtSource/`.
- Nessun keystore e nessuna password nel repository; firma mai in `ProjectSettings.asset`.
- Git: **mai** `--force`, `reset --hard`, `clean`.
- La grafica resta generata dal codice: niente Gemini in questo Update.

## Parti

### Parte 1 — Core: capitoli
- Modello dei capitoli, sblocco e conteggio delle stelle, in classi C# pure.
- Schermata **lista dei capitoli** e griglia **3x3 dei livelli**, che sostituisce la vecchia scelta del livello.
- Il salvataggio usa gli identificativi stabili. I progressi salvati con i vecchi livelli possono essere ignorati: siamo ancora in sviluppo, scrivilo nel report.

### Parte 2 — Core: pulsante d'azione principale
- Elemento opzionale della schermata di gioco: grande pulsante sopra la barra in basso, con testo e icona forniti dal minigioco.
- Disattivato durante le animazioni.

### Parte 3 — Idraulico: logica
- Calcolo dell'acqua con più fonti e colori, rilevamento di perdite, colori mescolati, arrivo sbagliato e fontana asciutta. Il risultato deve dire **dove** è avvenuto l'errore, per l'animazione.
- **Risolutore** aggiornato alle nuove regole: più fonti, più arrivi, colori, nessuna perdita. Trova le mosse minime considerando simmetrie e tutte le soluzioni.
- **Generatore** aggiornato:
  - capitolo 1: percorso singolo;
  - capitolo 2: percorso che si divide con una T verso due fontane;
  - capitolo 3: due percorsi separati di colori diversi.
  Riempie le altre celle con pezzi casuali, ruota a caso e scarta i livelli già risolti all'inizio o fuori dal gradino.

### Parte 4 — Idraulico: livelli
- Tabella di difficoltà dei 3 capitoli in `PlumberConfig`.
- Rigenera il capitolo 1 e genera i capitoli 2 e 3 con il menu `Resolver Master/Idraulico/Genera livelli Storia`.
- Aggiorna `docs/idraulico-livelli.md`: capitolo, livello, griglia, numero di fonti e arrivi, pezzi, mosse minime, seed.

### Parte 5 — Idraulico: grafica e animazioni
- Durante il gioco: tubi grigi, fonti e arrivi già colorati con il loro simbolo.
- **Apertura riuscita:** l'acqua parte da tutte le fonti insieme e si propaga cella dopo cella. Le fontane si riempiono con un piccolo effetto, poi compare il pannello di vittoria.
- **Apertura sbagliata:** propagazione fino all'errore, effetto sul punto sbagliato, breve messaggio tradotto ("Perdita!", "Colori mescolati!", "Fontana asciutta!"), poi reset.
- Durate e colori in `PlumberConfig`.

### Parte 6 — Validatore
Controlli dell'Idraulico aggiornati:
- ogni livello è risolvibile con le nuove regole;
- le mosse minime salvate corrispondono al risolutore;
- aprendo l'acqua all'inizio il livello **non** è già risolto;
- ogni capitolo ha esattamente 9 livelli;
- ogni livello rispetta il suo gradino;
- nel capitolo 1 nessuna T sul percorso della soluzione.

## Test
Al massimo 5 **nuovi** test EditMode, solo sulla logica. I test esistenti che non valgono più con le nuove regole vanno aggiornati e non contano nel limite: scrivilo nel report.
1. Perdita: un tubo bagnato aperto verso il vuoto fa fallire il tentativo e indica la cella giusta.
2. Colori: due flussi che si toccano, o un arrivo del colore sbagliato, fanno fallire il tentativo.
3. T: una fonte che raggiunge due fontane senza perdite dà un tentativo riuscito.
4. Risolutore: su un livello scritto a mano con due colori, trova le mosse minime attese.
5. Capitoli: il capitolo 2 si sblocca solo dopo aver finito tutti i 9 livelli del capitolo 1, con qualsiasi numero di stelle.

## Verifiche e anteprime
- **Make Previews**, nelle due risoluzioni (1080x1920 e 1080x2520):
  - lista dei capitoli;
  - griglia 3x3 dei livelli;
  - un livello per capitolo prima dell'apertura;
  - un'apertura riuscita a due colori;
  - un'apertura sbagliata per perdita, nel momento dell'effetto.
- Misura e riporta: dimensione delle celle nei 6x6, posizione del pulsante "Apri l'acqua" rispetto alla griglia e alla barra in basso, elementi tagliati o sovrapposti.
- Riporta la tabella dei 27 livelli e il tempo medio del generatore per un 6x6 a due colori.

## Criterio di completamento
- Commit precedenti su GitHub.
- Compilazione senza errori né warning.
- Test e validatore verdi.
- I 27 livelli si giocano con le nuove regole, con capitoli, stelle e sblocchi salvati.
- Anteprime generate.

## A fine lavoro
- Aggiorna `PROGRESS.md` (UTF-8), `docs/architettura.md` e crea `docs/idraulico-regole.md` con le regole del gioco.
- Commit e push con test e validatore verdi.
- Report breve: cosa è stato fatto, cosa è finito nel Core, test aggiornati, risultati di test e validatore, tabella dei livelli, misure, tempi del generatore, problemi o cose in sospeso.