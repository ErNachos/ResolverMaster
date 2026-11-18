# Update TJ-002 — Traffic jam: grafica dei veicoli e degli ostacoli

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
Resolver Master, minigioco Traffic jam. Oggi veicoli e ostacoli sono disegnati dal codice in modo provvisorio. L'utente ha creato la grafica definitiva con Gemini; gli originali sono in `ArtSource/TJ/`.

Prima di iniziare leggi come l'Idraulico prepara e usa la sua grafica (script in `Tools/Art/`, varianti scelte dal seed) e riusa lo stesso metodo.

## I file originali
Stile cartoon, contorni neri chiusi, sfondo bianco, veicoli visti dall'alto.
**Attenzione: in tutte le immagini il muso dei veicoli (fari e faccia) è rivolto verso il BASSO.**

| File | Contenuto, da sinistra a destra |
|---|---|
| `1` | Auto rossa preoccupata, auto rossa felice, auto blu preoccupata, auto blu felice (tutte con due strisce bianche) |
| `2` | Auto gialla arrabbiata, auto lilla annoiata, auto grigia assonnata, auto beige impaziente |
| `3` | Camion grigio scuro arrabbiato, camion giallo che sbuffa (con nuvoletta), camion lilla stanco |
| `4` | Tutti gli ostacoli, uniti in un'unica immagine dall'utente. **Da usare:** barriera quadrata a strisce diagonali gialle e nere (senza base); tombino aperto, masso, mucchio di immondizia (ognuno su una base quadrata). **Da scartare:** il gruppo di 4 coni (cerchi gialli e neri) e il blocco di cemento grigio con la fascia a strisce. |

## Parte A — Preparazione delle immagini
- Script in `Tools/Art/` (sul modello di quello dell'UI) che taglia ogni elemento, toglie lo sfondo bianco e salva gli sprite.
- Lo sfondo va tolto **solo partendo dai bordi** (riempimento dall'esterno): il bianco interno (occhi, strisce, nuvoletta, carta) deve restare.
- Contorni puliti, nessun alone bianco.
- Il coperchio del tombino esce un po' dalla sua base quadrata: tienilo intero e fai stare tutto dentro la casella.
- Salva gli sprite nella cartella della grafica di Traffic jam, con nomi chiari (es. `Target_Red_Worried`, `Car_Yellow`, `Truck_Grey`, `Obstacle_Manhole`).

## Parte B — Veicoli nel gioco
- **Orientamento:** gli originali hanno il muso in basso; ruota di conseguenza.
  - **Auto obiettivo:** muso sempre verso la sua uscita.
  - **Altri veicoli:** verso scelto dal seed del livello (su/giù se verticali, destra/sinistra se orizzontali), fisso per tutto il livello.
- **Proporzioni:**
  - le **auto** degli originali sono un po' più corte del doppio della larghezza. Preferisci la scala uniforme; se serve, allunga al massimo del 15–20% nella lunghezza. Scegli guardando le anteprime;
  - i **camion** sono lunghi circa il doppio della larghezza, ma devono occupare 3 caselle: **allunga solo il cassone** (la parte piatta dietro la cabina), lasciando cabina e faccia intatte.
- Lascia un piccolo margine tra veicoli vicini; specchietti e ruote non devono coprire il veicolo accanto in modo fastidioso.
- **Espressioni delle auto obiettivo:** preoccupata durante il livello, **felice** quando escono (cambio all'inizio dell'animazione di uscita).
- **Altri veicoli:** colore ed espressione scelti dal seed tra le 4 auto e i 3 camion.
- Le auto obiettivo restano l'unico rosso e blu del gioco (controllo del validatore già esistente).

## Parte C — Ostacoli
- 4 ostacoli: **barriera, tombino, masso, immondizia**.
- La scelta segue il **tema** del capitolo, con la variante scelta dal seed tra quelle ammesse. Tabella nel file dei parametri di Traffic jam:
  - Lavori in corso: barriera, tombino;
  - Mercato: immondizia;
  - Aeroporto: barriera;
  - Incrocio: tombino, immondizia;
  - Ora di punta: tutti;
  - altri temi e Challenge: tutti.
- Se la scelta "variante per tema e seed" è generica, mettila nel Core (le fonti e gli arrivi dell'Idraulico usano già varianti dal seed: riusa quel meccanismo).

## Parte D — Verifiche
- Anteprime con `Make Previews` su almeno un livello per capitolo, anche con i filtri per il daltonismo, a 4x6 e 7x9.
- Controlla la leggibilità delle facce a 7x9 (celle da circa 136 px).
- Togli il codice di disegno provvisorio dei veicoli solo se non serve più (es. come riserva in caso di sprite mancante, tienilo).

## Regole generali
- Nessun testo nel codice né nelle immagini.
- Test da aggiungere: ogni veicolo e ostacolo ha il suo sprite; l'auto obiettivo è orientata verso la sua uscita; stesso seed ⇒ stessi colori, espressioni, versi e ostacoli; l'espressione felice compare all'uscita.

## Riepilogo finale
Solo come ultimo messaggio (nessun file):
- cosa è stato fatto, parte per parte;
- come hai gestito le proporzioni di auto e camion;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.