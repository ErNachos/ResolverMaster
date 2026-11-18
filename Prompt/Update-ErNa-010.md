# Update ErNa-010 — Nome Pino, annuncio di prova, sfondo della Home e audio

## Modalità di lavoro (l'utente non è al PC)
- **Non fermarti a fare domande.** Quando c'è una scelta aperta, prendi la soluzione più ragionevole e ricordala per il riepilogo.
- **Piccolo problema**: aggiralo e vai avanti.
- **Problema grave** (il progetto non compila, test rotti che non riesci a riparare): riporta quella parte all'ultima versione funzionante e passa alla parte successiva.
- **Non creare file di note.** Scelte fatte e problemi aperti vanno solo nel riepilogo finale, come messaggio.
- Commit e push a fine lavoro (o dopo ogni parte, se riesci a verificarla da sola), con test verdi, validatore OK e 0 warning. Mai push --force, reset --hard o clean.
- Se l'Editor di Unity è aperto, lavora su una copia in `Logs/BatchCopy/` come già fatto in passato.
- Alla fine scrivi il riepilogo finale e fermati.

## Contesto
Resolver Master ha tre minigiochi: Idraulico, Traffic jam e il gioco del pinguino (appena fatto con l'update PG-001).

## Parte A — Pulizia e nome
1. Il gioco del pinguino e il pinguino si chiamano **"Pino"** (uguale in tutte le lingue). Se nei testi, nei menu o nei commenti c'è ancora un altro nome, sostituiscilo con "Pino". Nel codice resta il nome tecnico neutro (`Penguin`).
2. Cancella i file vuoti `Prompt/Update-ErNa-011.md` … `Prompt/Update-ErNa-015.md` (sono residui senza contenuto). Non toccare gli altri file di `Prompt/`.

## Parte B — Annuncio di prova sul telefono
L'utente prova il gioco con l'APK di `Resolver Master/Build Release APK (test)` e **non ha mai visto la lampadina attiva**: oggi l'annuncio di prova funziona solo in editor, quindi sul telefono lampadina, Continua e Cambia livello restano disattivati.
- L'**APK di test** usa l'annuncio di prova. L'**AAB di rilascio** resta senza annunci finché non arriva AdMob. La scelta dipende dal tipo di build, non da una modifica manuale; aggiungi un test o un controllo del validatore.
- Semplifica il pannello "Annuncio di prova": dopo la breve attesa c'è **un solo pulsante "Chiudi"**. Alla chiusura il premio viene **sempre** dato: l'utente chiude il pannello e vede subito il suggerimento della lampadina (o il Continua, o il Cambia livello).
- Il caso "annuncio non guardato" resta provabile solo in editor, con un'opzione nella configurazione.

## Parte C — Home: sfondo e navigazione
Oggi lo sfondo della Home è un lilla piatto e i pallini sopra GIOCA! non piacciono (sono piatti, fuori stile, e quello attivo sembra una macchia verde).
- **Sfondo:**
  - leggera **sfumatura** verticale;
  - sopra, una **trama di piccole sagome** in un tono appena più scuro dello sfondo, che scorre lentamente in diagonale. Le sagome prendono spunto dai giochi: goccia, pezzo di tubo, auto, pesce, fiocco di neve. Ottienile dalla grafica esistente come silhouette a un colore. Densità bassa, movimento lento: non deve distrarre;
  - **il colore cambia con la scheda attiva**, con un passaggio morbido mentre si scorre: lilla per l'Idraulico, giallo tenue per Traffic jam, azzurro ghiaccio tenue per Pino. Il validatore deve continuare a controllare la distanza dai colori dell'acqua e delle auto obiettivo.
- **Pallini → frecce:** togli i pallini e metti **due pulsanti tondi con una freccia**, ai lati della scheda, nello stesso stile dei pulsanti tondi esistenti (contorno nero, volume). La freccia scompare (o si spegne) alla prima e all'ultima scheda. Lo scorrimento con il dito resta.
- Usa lo stesso componente (frecce e sfondo) anche nella schermata dei capitoli, con il colore del gioco al posto dei pallini.
- Tutto generico, nel Core.

## Parte D — Audio
Oggi l'app è muta: ci sono solo le impostazioni salvate di audio e vibrazione.

**1. Sistema audio (Core)**
- Un gestore audio con due canali: **musica** (un brano alla volta, dissolvenza di circa 1 s quando cambia) ed **effetti** (più suoni insieme, con un limite per non sovrapporre lo stesso suono troppe volte).
- Volumi separati per musica ed effetti, letti dalle impostazioni già salvate. Se le impostazioni hanno solo acceso/spento, aggiungi i due interruttori separati (Musica, Effetti), con testi in italiano e inglese.
- La musica si ferma quando l'app va in pausa e riprende al ritorno.
- **Vibrazione** breve sugli eventi importanti (vittoria, errore, sconfitta), solo se attiva nelle impostazioni.
- Ogni suono è legato a un **nome di evento** (es. `ui_click`, `plumber_rotate`), con una tabella evento → file. Se un file manca, nessun errore: silenzio.

**2. Effetti sonori**
- Scarica dal sito kenney.nl questi pacchetti, tutti con licenza **CC0** (pubblico dominio, uso commerciale libero): **Interface Sounds**, **Impact Sounds**, **UI Audio**, **Music Jingles**. Salva gli originali e la licenza in `ArtSource/Audio/Kenney/`.
- Se non riesci a scaricarli, salta questo punto: prepara solo la tabella degli eventi e scrivilo nel riepilogo, così l'utente li scarica a mano.
- Scegli i suoni per questi eventi (morbidi, allegri, adatti a tutti; niente suoni aggressivi):
  - **UI:** tocco di un pulsante, scorrimento delle schede, apertura e chiusura dei pannelli, stella ottenuta (una per stella, in salita), capitolo sbloccato, nuovo record;
  - **Idraulico:** rotazione di un pezzo, tocco su pezzo che non ruota, apertura dell'acqua, acqua che arriva alla fontana, livello riuscito, perdita/errore;
  - **Traffic jam:** presa del veicolo, aggancio alla casella, urto contro un ostacolo, uscita dell'auto obiettivo, livello riuscito;
  - **Pino:** inizio scivolata, urto contro roccia o muro, arrivo sulla neve, pesce raccolto, igloo che si apre, ingresso nell'igloo, caduta in acqua, blocco spinto, blocco che affonda;
  - **Comuni:** lampadina, annulla, mosse finite / sei bloccato, sconfitta in Challenge, Continua.
- Per i momenti di vittoria usa i **jingle brevi** di Music Jingles.
- Se per un evento non trovi un suono adatto nei pacchetti, puoi crearne uno semplice dal codice (sintesi) oppure lasciarlo vuoto: annotalo nel riepilogo.
- Normalizza i volumi, così nessun suono è molto più forte degli altri. Importazione in Unity adatta al telefono (compressione e caricamento corretti per suoni brevi e lunghi).

**3. Musiche**
- 4 brani in loop: **Home**, **Idraulico**, **Traffic jam**, **Pino**. I file li sceglie e li scarica l'utente in `ArtSource/Audio/Music/`, con i nomi `Music_Home`, `Music_Plumber`, `Music_TrafficJam`, `Music_Penguin` (estensione .ogg, .mp3 o .wav).
- Se i file ci sono, importali e collegali (la Home suona la sua musica, ogni gioco la sua, anche in Storia e Challenge). Se mancano, lascia il collegamento pronto e silenzio.
- Volume della musica più basso degli effetti (proposta: 60%).

## Regole generali
- Nessun testo nel codice; testi nuovi in italiano e inglese.
- Test da aggiungere: tipo di build ⇒ annunci di prova sì/no; il pannello di prova dà sempre il premio alla chiusura; colore dello sfondo che segue la scheda; frecce spente alla prima e all'ultima scheda; ogni evento audio ha un file o è vuoto senza errori; musica giusta per ogni schermata; volumi presi dalle impostazioni; tutti i test dei tre giochi verdi.

## Riepilogo finale
Solo come ultimo messaggio (nessun file):
- cosa è stato fatto, parte per parte;
- pacchetti scaricati e tabella eventi → suoni (compatta);
- eventi rimasti senza suono;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.