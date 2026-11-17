# Update-ErNa-004 — Grafica: fonti e arrivi veri, stile dei pulsanti, Home a schede

## Contesto
- **Prima di tutto:** `git pull --ff-only`. Poi aggiungi al repository la cartella `Prompt/` (tutti i file degli Update) e la cartella `ArtSource/` (originali di Gemini), con un commit separato.
- Unity è **chiuso** durante questo Update.
- Lo stile grafico scelto è **cartoon con contorni neri spessi**, forme arrotondate e semplici, colori pieni con ombre morbide. Il riferimento è `ArtSource/Reference/fontana_riferimento.png`.
- Questo Update porta nel gioco la grafica già pronta e rifà lo stile dell'interfaccia. Le **icone** e le **illustrazioni delle schede** arriveranno da Gemini in un Update successivo: per ora usa dei segnaposto, ma prepara già dove andranno.
- **Lingue (regola già attiva, da rispettare in tutto il nuovo lavoro):** il gioco è in **italiano e inglese**. Al primo avvio parte in italiano se il telefono è in italiano, altrimenti in inglese; poi vale sempre la lingua scelta nelle Impostazioni, che resta salvata. **Nessun testo scritto nel codice né dentro le immagini**: tutti i testi nuovi (nomi dei giochi, GIOCA!/PLAY!, Prossimamente…) vanno nelle tabelle delle lingue.
- **Regola del Core:** stile dei pulsanti, schede della Home e caricamento delle immagini dei giochi vanno nel Core. Nell'Idraulico restano solo i suoi sprite e il loro uso.

## NON toccare
- Gli originali in `ArtSource/`: lo script li legge e basta, non li modifica mai.
- Le regole dell'Idraulico, il risolutore, il generatore e i livelli.
- Nessun keystore e nessuna password nel repository; firma mai in `ProjectSettings.asset`.
- Git: **mai** `--force`, `reset --hard`, `clean`.

## Parti

### Parte 1 — Script per fonti e arrivi (`Tools/Art/`)
- Input: `ArtSource/Plumber/Sources/` (fonti) e `ArtSource/Plumber/Targets/` (arrivi), 4 immagini per cartella. Elenca nel report i nomi dei file trovati.
- Per ogni immagine:
  - toglie lo sfondo bianco partendo dai bordi (i contorni neri sono chiusi, quindi il bianco *dentro* l'oggetto resta);
  - ritaglia l'oggetto, lo centra in un quadrato con un piccolo margine e lo riduce a 512 px (mai ingrandire);
  - **solo per le fonti:** separa la parte **magenta** (anche le sfumature più scure del magenta) in una **maschera** in scala di grigi che conserva le ombre, e nell'immagine base la sostituisce con un grigio chiaro neutro. Il codice colorerà la maschera con il colore dell'acqua.
- Output in `Assets/Art/Plumber/Sources/` e `Assets/Art/Plumber/Targets/` con nomi coerenti (`fonte_1`, `fonte_1_mask`, `arrivo_1`…).
- Rieseguibile, con scrittura sicura (file temporaneo e poi sostituzione). Riporta per ogni fonte la percentuale di pixel magenta trovati, per accorgersi se una maschera è vuota o sbagliata.

### Parte 2 — Fonti e arrivi nel gioco
- Al posto della grafica provvisoria, ogni fonte e ogni arrivo usa uno dei 4 disegni. La variante si sceglie dal **seed del livello**: lo stesso livello ha sempre gli stessi disegni.
- **Fonte:** immagine base più la maschera colorata con il colore della sua acqua.
- **Arrivo:** immagine neutra con l'**alone colorato** sotto (`sinkHaloColors`, `sinkHaloSize`).
- Un **pezzetto di tubo** generato dal codice esce da sotto l'oggetto verso il lato dell'apertura, così si vede sempre da che parte si collega. L'oggetto non ruota mai.
- Alla vittoria, sopra gli arrivi compare un effetto d'acqua generato dal codice (spruzzi e gocce del colore giusto).
- Dimensione dell'oggetto rispetto alla cella in `PlumberConfig`.
- **I tubi** restano generati dal codice, ma prendono lo stile nuovo: contorno nero spesso, riflesso chiaro, acqua con un leggero movimento quando scorre.

### Parte 3 — Stile dei pulsanti e dei pannelli (Core)
- Aggiorna `UiKitStyle` e gli sprite a 9 fette generati da script:
  - contorno nero spesso, angoli molto arrotondati, colori pieni, una fascia d'ombra più scura in basso (effetto "da premere");
  - **quando lo premi** il pulsante scende di qualche pixel e l'ombra si riduce; **disattivato**: colori spenti;
  - **pulsante principale** (GIOCA!, Apri l'acqua, Avanti): verde vivo con testo bianco e contorno scuro;
  - **pulsanti secondari:** chiari (crema o bianco);
  - **pulsanti rotondi** per le icone (indietro, impostazioni, annulla, ricomincia, aiuto);
  - pannelli e schede color crema con contorno nero.
- Evita il **blu** e l'**arancione** come colori principali dell'interfaccia: sono i colori dell'acqua e devono restare riconoscibili.
- Titoli con contorno scuro, nel font già in uso.
- Applica lo stile a **tutte** le schermate: Home, scelta della modalità, Impostazioni, capitoli, livelli, gioco, pannello di vittoria, messaggi di errore.
- Pulsanti toccabili di almeno **130 px** di lato. Tra "Apri l'acqua" e la barra in basso almeno **48 px**.
- Le icone restano quelle attuali come segnaposto, ma in un colore scuro uniforme.

### Parte 4 — Home a schede (Core)
- Una **scheda grande** per ogni minigioco registrato, con riquadro colorato arrotondato, illustrazione e nome (dalle tabelle delle lingue).
- Le schede si **scorrono con il dito** e si fermano centrate; quelle a lato si vedono in parte, più piccole e più trasparenti. Sotto ci sono i **pallini** della posizione.
- Sotto i pallini c'è un grande pulsante **GIOCA! / PLAY!**, che apre la scelta della modalità del gioco al centro.
- In basso a sinistra c'è il pulsante rotondo delle **Impostazioni**. In alto il titolo **Resolver Master**.
- Un gioco "Prossimamente" (Traffic jam) ha la scheda attenuata con un lucchetto e la scritta "Prossimamente", e il pulsante GIOCA! è disattivato.
- L'**ultima scheda scelta** viene salvata e riproposta all'avvio.
- Ogni minigioco indica nel registro l'immagine della sua scheda e il colore del riquadro. Le immagini andranno in `Assets/Art/Cards/`: se l'immagine manca, si usa un segnaposto generato dal codice e il validatore mostra un **warning**, non un errore.
- Logica di scorrimento e scelta (indice, aggancio, limiti) in una classe C# pura.

### Parte 5 — Validatore
- Fonti e arrivi: per l'Idraulico esistono 4 fonti con maschera non vuota e 4 arrivi.
- Ogni minigioco registrato ha nome tradotto in IT ed EN e colore della scheda. Immagine della scheda: warning se manca.
- Tutte le chiavi di lingua nuove esistono in IT ed EN.

## Test
Al massimo 5 nuovi test EditMode, solo sulla logica:
1. Variante di fonte e arrivo: lo stesso seed sceglie sempre gli stessi disegni.
2. Scorrimento delle schede: l'aggancio sceglie la scheda più vicina e non va oltre la prima e l'ultima.
3. Ultima scheda scelta: salvata e riletta; se il gioco salvato non esiste più si torna alla prima scheda.

## Verifiche e anteprime
- **Make Previews**, nelle due risoluzioni (1080x1920 e 1080x2520) e in **italiano e inglese** per la Home:
  - Home con la scheda Idraulico al centro e con la scheda Traffic jam al centro;
  - scelta della modalità, Impostazioni, lista dei capitoli, griglia dei livelli;
  - un livello del capitolo 1 (3x3) e uno del capitolo 3 (6x6) prima dell'apertura;
  - un'apertura riuscita a due colori e il pannello di vittoria.
- Misura e riporta: dimensione in px di fonti e arrivi nelle celle 3x3 e 6x6, dimensione minima dei pulsanti, distanza tra "Apri l'acqua" e la barra in basso, testi tagliati o che escono dai pulsanti (anche in inglese), elementi sovrapposti.
- Guarda anche le immagini, non solo le misure. Prima misura e riporta, poi correggi.

## Criterio di completamento
- `Prompt/` e `ArtSource/` nel repository.
- Compilazione senza errori né warning.
- Test e validatore verdi (i warning per le immagini delle schede mancanti sono ammessi).
- Fonti e arrivi veri visibili in tutti i 27 livelli, con colori corretti.
- Stile nuovo applicato a tutte le schermate. Home a schede funzionante in IT ed EN.
- Anteprime generate.

## A fine lavoro
- Aggiorna `PROGRESS.md` (UTF-8), `docs/architettura.md` e crea `docs/grafica.md` (stile, cartelle, script, come aggiungere una nuova immagine o una scheda).
- Aggiorna `docs/crediti.md` (grafica generata con Gemini).
- Commit e push con test e validatore verdi.
- Report breve: cosa è stato fatto, cosa è finito nel Core, output dello script (file e percentuali di magenta), risultati di test e validatore, misure, problemi o cose in sospeso.