# Update-PG-002 — Grafica di Pino

## Modalità di lavoro (l'utente non è al PC)
- Non fare domande: scegli sempre la soluzione più ragionevole.
- Piccoli problemi: aggirali. Problemi gravi: riporta quella parte all'ultima versione funzionante e prosegui con le altre.
- Non creare file di note (`Note-*.md` o simili). Scelte e problemi vanno solo nel riepilogo finale.
- Commit e push solo con test verdi, validatore OK e 0 warning. Mai `push --force`, `reset --hard`, `clean`.
- Se l'Editor è aperto, lavora su una copia in `Logs/BatchCopy/`.
- Riepilogo finale solo come messaggio.

## Obiettivo
Sostituire la grafica provvisoria di Pino (disegnata dal codice) con gli sprite fatti dall'utente, aggiungere i cambi di vista per le espressioni e la scheda della Home.

## Immagini fornite
In `ArtSource/PG/`, ogni immagine ha gli elementi in riga, da sinistra a destra, su sfondo bianco:

| File | Elementi (da sinistra) |
|---|---|
| `1.png` | Pino arancione **visto di fronte**: normale, felice, spaventato, stordito |
| `2.png` | Pino viola a strisce **visto di fronte**: normale, felice, spaventato, stordito |
| `3.png` | **Visti dall'alto**, testa verso l'alto: arancione fermo, arancione che scivola sulla pancia, viola fermo, viola che scivola |
| `4.png` | Igloo: chiuso con bandierina arancione, aperto arancione, chiuso viola, aperto viola |
| `5.png` | Oggetti: pesce, blocco di ghiaccio, roccia, blocco affondato nell'acqua (tessera quadrata) |
| `6.png` | Tessere viste dall'alto: ghiaccio, neve, acqua, muro di neve |

Scheda della Home: `PG.png` nella cartella `cards` (cercala in `ArtSource/`; se è altrove, cercala nel progetto).

## Parte A — Ritaglio degli sprite
- Ritaglia ogni elemento, togli lo sfondo bianco (trasparenza con bordo pulito, senza alone bianco) e rifila i margini.
- Riusa lo strumento di ritaglio già fatto per Traffic jam (TJ-002). Se è specifico di Traffic jam, spostalo nel Core (regola del Core: mai copiarlo).
- Le immagini sono un po' sfocate: importa alla massima qualità, senza compressione che peggiori i contorni.
- Gli sprite della stessa famiglia devono avere la stessa scala e lo stesso punto di appoggio (pivot ai piedi per i pinguini di fronte, al centro per quelli dall'alto).
- Nomi chiari degli sprite, per esempio `Penguin_Orange_Front_Happy`, `Penguin_Purple_Top_Slide`, `Igloo_Orange_Open`, `Tile_Snow`.

## Parte B — Pinguini: vista dall'alto e "si gira verso di noi"
- **Durante il gioco** il pinguino si vede **dall'alto**:
  - fermo: sprite "fermo", ruotato verso l'ultima direzione di movimento (lo sprite ha la testa verso l'alto; ruota di 90° dal codice);
  - mentre scivola: sprite "scivola", ruotato nella direzione dello swipe.
- **Durante un evento** il pinguino **si gira verso di noi**: breve transizione (per esempio uno schiacciamento orizzontale che scambia lo sprite dall'alto con quello di fronte), poi mostra l'espressione:
  - **felice:** quando entra nel suo igloo (resta così fino alla fine del livello);
  - **spaventato:** quando cade in acqua, prima dell'effetto della caduta;
  - **stordito:** quando si ferma contro roccia, muro, blocco o l'altro pinguino dopo aver scivolato **almeno 3 caselle**; dura poco, poi torna alla vista dall'alto;
  - **normale:** all'inizio del livello, finché non arriva il primo swipe.
- Le espressioni non devono rallentare il gioco: uno swipe durante "stordito" o "normale" interrompe l'espressione e fa partire subito la mossa.
- Lo sprite di fronte è più alto di una casella: può sporgere verso l'alto. Disegna le righe più in basso sopra quelle più in alto.
- Con due pinguini ognuno ha i suoi sprite (arancione o viola a strisce).

## Parte C — Igloo, oggetti e terreni
- **Igloo:** chiuso finché mancano pesci, aperto quando sono tutti presi, con breve animazione di apertura. Ogni pinguino ha il suo igloo con la bandierina dello stesso colore e motivo.
- **Pesce:** piccola animazione quando viene preso (salta e sparisce).
- **Blocco di ghiaccio** e **roccia:** sprite al posto della grafica dal codice.
- **Blocco affondato:** quando un blocco riempie un buco, la casella d'acqua diventa la tessera "blocco affondato".
- **Tessere:** ghiaccio, neve e acqua come terreno della griglia. Per non farle sembrare tutte uguali, ruotale e specchiale in modo deterministico in base alla posizione della casella (stesso livello = stesso aspetto).
- **Muro di neve:** usato per i tratti di bordo a muro. Non va ruotato (ha il lato visibile in basso); sporge sotto la casella, quindi va disegnato sopra la riga sotto. I tratti di bordo d'acqua usano la tessera acqua.
- Gli sfondi dei 7 temi restano quelli attuali.

## Parte D — Scheda della Home
- Usa `PG.png` come immagine della scheda di Pino, con lo stesso trattamento delle schede degli altri giochi.

## Parte E — Pulizia e controlli
- Rimuovi la grafica provvisoria di Pino non più usata. Lascia `SdfArt` nel Core se serve ad altri giochi.
- Il validatore deve controllare che tutti gli sprite di Pino ci siano e siano collegati.
- Rigenera le anteprime con *Make Previews*, compresi i filtri per daltonismo. Verifica che i due pinguini, igloo chiuso e aperto, ghiaccio, neve e acqua si distinguano anche con i filtri.
- Aggiungi test per: scelta dello sprite in base allo stato (fermo, scivola, evento), rotazione in base alla direzione, rotazione e specchiatura deterministica delle tessere.

## Riepilogo finale (solo messaggio)
- Cosa è stato fatto, parte per parte.
- Scelte fatte e problemi aperti.
- Commit, test e validatore.
- Cosa provare sul telefono.