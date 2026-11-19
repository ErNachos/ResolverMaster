# Update-ErNa-011 — Lampadina come mossa, icona dell'app, niente intro di Unity

## Modalità di lavoro (l'utente non è al PC)
- Non fare domande: scegli sempre la soluzione più ragionevole.
- Piccoli problemi: aggirali. Problemi gravi: riporta quella parte all'ultima versione funzionante e prosegui con le altre.
- Non creare file di note (`Note-*.md` o simili). Scelte e problemi vanno solo nel riepilogo finale.
- Commit e push solo con test verdi, validatore OK e 0 warning. Mai `push --force`, `reset --hard`, `clean`.
- Se l'Editor è aperto, lavora su una copia in `Logs/BatchCopy/`.
- Riepilogo finale solo come messaggio.

## Parte A — La lampadina conta come mossa (Core, tutti i giochi)
- Regola nuova: le mosse fatte dalla lampadina **contano come mosse**, esattamente come se le avesse fatte il giocatore. Il contatore sale e il totale finale non può mai scendere sotto il minimo.
- Si conta quello che la lampadina fa davvero:
  - **Idraulico:** sistemare un pezzo conta quante rotazioni di 90° servono per portarlo nella posizione giusta, partendo da com'è (da 1 a 3);
  - **Traffic jam:** la mossa fatta conta 1;
  - **Pino:** la mossa fatta conta 1; quando la lampadina annulla fino a una posizione risolvibile, si comporta come gli annullamenti normali.
- Resta la regola: livello in cui si è usata la lampadina = **massimo 2 stelle**.
- La lampadina resta nascosta nella Challenge e nella sfida del giorno.
- Il caso "mosse finite" deve funzionare anche se l'ultima mossa l'ha fatta la lampadina.
- La regola sta nel Core; i giochi dicono solo quante mosse vale l'aiuto.
- Aggiorna i testi che spiegano la lampadina (italiano e inglese), se dicono che non conta.
- Test: per ogni gioco, un livello risolto usando la lampadina più volte ha mosse ≥ minimo e al massimo 2 stelle.

## Parte B — Icona dell'app
Immagine: `ArtSource/Icon/Icon.png`. È un disco con sfondo blu a raggi, tre carte a ventaglio e il nastro "RM"; gli angoli fuori dal disco sono bianchi e le carte sporgono un po' dal disco.
- **Primo piano (adaptive foreground):** rendi trasparente il bianco fuori dal disegno (fuori dal disco e intorno alle carte che sporgono), senza alone bianco. Ridimensiona tutto il disegno, carte comprese, perché stia dentro la zona sicura dell'icona adattiva (cerchio centrale di 66 dp su 108 dp), con un piccolo margine.
- **Sfondo (adaptive background):** tinta unita blu notte, presa dal colore del bordo del disco.
- **Icone legacy e round:** stessa composizione, disco su sfondo blu notte.
- **Icona per il Play Store:** crea `ArtSource/Icon/Icon_PlayStore_512.png`, 512x512, PNG a 32 bit, quadrata e piena fino ai bordi (disco su blu notte, nessun bianco), sotto 1 MB.
- Imposta tutte le icone Android nelle Player Settings (adaptive, round, legacy), per tutte le densità.
- Lo script di preparazione delle icone va in `Tools/Art/`, così si può rifare se cambia l'immagine.
- Il validatore controlla che le icone Android ci siano e che nessuna abbia angoli bianchi.

## Parte C — Niente intro di Unity
- Nelle Player Settings disattiva la schermata iniziale di Unity: niente logo "Made with Unity" e niente splash screen di Unity (in Unity 6 si può anche con la licenza Personal).
- Lo splash di sistema di Android 12+ mostra l'icona dell'app: imposta il colore di sfondo blu notte, uguale allo sfondo dell'icona.
- Il validatore controlla che lo splash di Unity sia disattivato.

## Riepilogo finale (solo messaggio)
- Cosa è stato fatto, parte per parte.
- Scelte fatte e problemi aperti.
- Commit, test e validatore.
- Cosa provare sul telefono.