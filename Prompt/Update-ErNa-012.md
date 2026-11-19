# Update-ErNa-012 — Effetti sonori generati, musiche, Pino senza stordito

## Modalità di lavoro (l'utente non è al PC)
- Non fare domande: scegli sempre la soluzione più ragionevole.
- Piccoli problemi: aggirali. Problemi gravi: riporta quella parte all'ultima versione funzionante e prosegui con le altre.
- Non creare file di note (`Note-*.md` o simili). Scelte e problemi vanno solo nel riepilogo finale.
- Commit e push solo con test verdi, validatore OK e 0 warning. Mai `push --force`, `reset --hard`, `clean`.
- Se l'Editor è aperto, lavora su una copia in `Logs/BatchCopy/`.
- Riepilogo finale solo come messaggio.

## Parte A — Pino: niente più "stordito"
- Togli l'espressione "stordito": quando il pinguino si ferma contro roccia, muro, blocco o l'altro pinguino, resta nella vista dall'alto, senza girarsi e senza stelline.
- Restano le altre espressioni: normale all'inizio, spaventato quando cade, felice nell'igloo.
- Rimuovi lo sprite stordito dalla libreria e dai controlli del validatore, se non è più usato. Adatta test e anteprime.

## Parte B — Effetti sonori generati dal codice
I pacchetti Kenney non si possono scaricare. Al loro posto, **genera tutti gli effetti con uno script Python**.
- Script: `Tools/Audio/make_sfx.py`. Sintesi con onde, rumore filtrato, inviluppi e piccoli accordi. Usa solo librerie Python comuni (per esempio numpy e scipy) e installale se mancano.
- **Deterministico:** stesso script = stessi file. Ogni suono è descritto da parametri leggibili in cima allo script, così si possono ritoccare e rigenerare.
- **Stile:** morbido e "cozy", adatto a un puzzle cartoon. Niente suoni aspri, acuti fastidiosi o troppo forti. Tutti gli effetti hanno lo stesso carattere.
- **Copertura:** un suono per ognuno dei 34 eventi già agganciati (UI, Idraulico, Traffic jam, Pino, parti comuni). Eventi simili possono condividere lo stesso suono con piccole variazioni di altezza. Indicazioni:
  - UI: click leggero, apertura e chiusura di pannelli, cambio scheda;
  - Idraulico: rotazione del pezzo (tic meccanico), acqua che scorre, arrivo dell'acqua;
  - Traffic jam: presa e rilascio del veicolo, auto obiettivo che esce (piccolo "vroom" stilizzato);
  - Pino: scivolata sul ghiaccio (fruscio), stop sulla neve (tonfo soffice), urto contro un ostacolo (tonfo breve), pesce preso (pop brillante), igloo che si apre, caduta in acqua (splash stilizzato), blocco che scivola e che affonda;
  - comuni: stella guadagnata (una nota per stella, che sale), vittoria (jingle breve), errore e sconfitta (jingle discendente, gentile), lampadina, annulla, "Sei bloccato".
- **Formato:** file brevi (in genere sotto 1 s, jingle sotto 2,5 s), in `Assets/Audio/Sfx/`, con la stessa intensità percepita (normalizzazione), senza click all'inizio e alla fine.
- Sostituisci la tabella `AudioCatalog.KenneyFiles` con quella dei suoni generati. Togli i riferimenti a Kenney che non servono più.
- **Ascolto in Editor:** aggiungi il menu *Resolver Master > Audio > Ascolta effetti*, una finestra con la lista degli eventi e un pulsante per sentire ciascun suono.
- Il validatore controlla che ogni evento abbia il suo file e che nessun file sia vuoto o in saturazione.

## Parte C — Musiche
- Se in `ArtSource/Audio/Music/` ci sono `Music_Home`, `Music_Plumber`, `Music_TrafficJam`, `Music_Penguin`, importale con *Resolver Master > Audio > Importa suoni* e normalizza i volumi tra loro e rispetto agli effetti (la musica più bassa degli effetti).
- Se mancano, lasciale mancanti senza errori e dillo nel riepilogo.

## Riepilogo finale (solo messaggio)
- Cosa è stato fatto, parte per parte.
- Elenco evento → suono, in breve.
- Scelte fatte e problemi aperti.
- Commit, test e validatore.
- Cosa provare sul telefono.