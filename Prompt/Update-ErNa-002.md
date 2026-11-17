# Update-ErNa-002 — Idraulico: primo giocabile

## Contesto
- **Prima di tutto:** il commit di ErNa-001 è rimasto in locale. Fai `git push -u origin main` verso `https://github.com/ErNachos/ResolverMaster.git`. Se fallisce, esegui `gh auth status` e `gh auth setup-git` e riprova. Poi il solito `git pull --ff-only`.
- Unity è **chiuso** durante questo Update.
- Questo Update rende **giocabile l'Idraulico in modalità Storia**, con i primi 10 livelli (capitolo 1).
- **Regola del Core (vedi `GUIDELINES.md`):** tutto ciò che servirà anche ad altri minigiochi (griglia, mosse, stelle, annulla, schermata di gioco, scelta del livello, pannello di vittoria, casualità con seed, generazione dei livelli) va nel **Core**. Nella cartella dell'Idraulico vanno solo le sue regole, i pezzi, il generatore, il risolutore e la grafica dei tubi.
- La Challenge **non** fa parte di questo Update, ma le scelte fatte qui devono già permetterla (livelli generati da seed, difficoltà da tabella).

### Regole dell'Idraulico
- Griglia quadrata. In ogni cella c'è un pezzo:
  - **dritto**, **curva**, **T**, **croce**: ruotabili;
  - **fonte** e **arrivo**: fissi, con una sola apertura.
- **Un tocco** su un pezzo lo ruota di 90° in senso orario = **1 mossa**.
- L'acqua parte dalle fonti e scorre nei pezzi collegati (aperture combacianti). Dopo ogni mossa si ricalcola e i tubi pieni si colorano.
- **Vittoria:** tutti gli arrivi ricevono l'acqua. Le perdite (aperture verso il vuoto) sono permesse e i pezzi in più possono restare scollegati.
- Il modello dei dati prevede già un **colore** per ogni coppia fonte/arrivo (in futuro: più fonti, l'acqua di un colore deve arrivare solo al suo arrivo). In questo Update si usa **un solo colore**.
- Il modello prevede anche i **pezzi bloccati** (non ruotabili), da usare in capitoli futuri.

### Mosse e stelle
- **Mosse minime:** il numero minimo di rotazioni per risolvere il livello, calcolato dal risolutore tenendo conto delle simmetrie (il dritto ha 2 posizioni, la croce 1) e di tutte le soluzioni possibili.
- **Stelle** (parametri nel file di configurazione del Core):
  - 3 stelle: mosse = minimo;
  - 2 stelle: fino a minimo + 30% (almeno +2);
  - 1 stella: livello risolto con più mosse.
- **Annulla** toglie l'ultima mossa e riduce il contatore. **Ricomincia** riporta il livello all'inizio.

## NON toccare
- `ArtSource/`.
- Nessun keystore e nessuna password nel repository; firma mai in `ProjectSettings.asset`.
- Git: **mai** `--force`, `reset --hard`, `clean`.
- Le parti di ErNa-001 che funzionano: modificale solo se servono al Core, e scrivilo nel report.

## Parti

### Parte 1 — Core: basi comuni dei puzzle
- **Griglia astratta:** celle, direzioni e vicini, con la topologia quadrata come prima implementazione. Deve essere possibile aggiungere esagoni e triangoli in futuro senza cambiare il resto.
- **Generatore casuale con seed** proprio (non `UnityEngine.Random` né `System.Random`), con lo stesso risultato su ogni piattaforma.
- **Contratto del puzzle:** carica livello, applica mossa, controlla vittoria, annulla, ricomincia, mosse fatte, mosse minime.
- **Pila di annullamento** generica.
- **Calcolo delle stelle** dai parametri di configurazione.
- **Tabella di difficoltà** generica: gradini con valori minimi e massimi, letta da un file di parametri per ogni gioco.
- Tutto in classi C# pure, testabili.

### Parte 2 — Core: schermate di gioco comuni
- **Scelta del livello:** griglia di pulsanti con numero, stelle ottenute e lucchetto. Il livello successivo si sblocca risolvendo il precedente.
- **Schermata di gioco:** barra in alto (indietro, numero del livello, mosse fatte / minime), area del puzzle adattata allo schermo e alla safe area, barra in basso (annulla, ricomincia, aiuto disattivato per ora).
- **Pannello di vittoria:** stelle ottenute con una piccola animazione, mosse fatte e minime, pulsanti *Ripeti* e *Avanti*.
- **Salvataggio:** nuovo blocco opzionale con le stelle di ogni livello, per ogni minigioco e modalità.
- Dalla Home: Idraulico → Storia apre la scelta del livello. Idraulico non è più "Prossimamente". Challenge resta segnaposto.

### Parte 3 — Idraulico: logica
- Pezzi, rotazioni, aperture e calcolo dell'acqua.
- **Risolutore:** trova le mosse minime e dice se il livello è risolvibile.
- **Generatore:** crea un percorso dalla fonte all'arrivo, mette i pezzi giusti lungo il percorso, riempie le altre celle con pezzi casuali, poi ruota tutto a caso. Scarta i livelli già risolti all'inizio o fuori dai valori del gradino.

### Parte 4 — Idraulico: capitolo 1
- Tabella di difficoltà del capitolo 1 nel file di parametri dell'Idraulico. Indicazioni:
  - livelli 1–3: griglia 3x3, solo dritti e curve, mosse minime 1, 2, 3;
  - livelli 4–7: griglia 4x4, solo dritti e curve, mosse minime in crescita lenta;
  - livelli 8–10: griglia 5x5, compaiono i pezzi a T, percorso più tortuoso.
- Menu dell'Editor `Resolver Master/Idraulico/Genera livelli Storia`: genera i 10 livelli da seed fissi, li salva come dati del gioco e scrive la tabella in `docs/idraulico-livelli.md` (livello, griglia, pezzi, mosse minime, seed).
- I livelli della Storia sono **generati una volta e salvati**, non rigenerati sul telefono.

### Parte 5 — Idraulico: grafica e animazioni
- Grafica **generata dal codice** per ora (Gemini arriverà dopo): tubi arrotondati grigi da vuoti e blu da pieni, fonte e arrivo riconoscibili, celle della griglia con sfondo leggero. Colori e spessori nel file di parametri dell'Idraulico.
- Rotazione animata (circa 0,12 s). Il tocco durante l'animazione si accoda, non si perde.
- L'acqua si propaga **cella dopo cella** con una breve animazione.
- Alla vittoria: l'arrivo si riempie con un piccolo effetto, poi il pannello di vittoria.
- Pooling delle celle.

### Parte 6 — Validatore
Aggiungi i controlli dell'Idraulico:
- ogni livello della Storia è risolvibile;
- le mosse minime salvate corrispondono a quelle calcolate dal risolutore;
- nessun livello è già risolto all'inizio;
- ogni livello rispetta i valori del suo gradino.

## Test
Al massimo 5 test EditMode, solo sulla logica:
1. Rotazione: aperture corrette dopo 1, 2, 3, 4 rotazioni per ogni tipo di pezzo.
2. Acqua: su un livello scritto a mano, raggiunge le celle giuste.
3. Risolutore: su un livello scritto a mano, trova il numero di mosse minime atteso, considerando le simmetrie.
4. Generatore: lo stesso seed produce lo stesso livello, e il livello è risolvibile.
5. Stelle: 3, 2 e 1 stella ai valori limite.

## Verifiche e anteprime
- **Make Previews:** aggiungi scelta del livello, livello 1 all'inizio, un livello a metà con l'acqua parziale, pannello di vittoria, nelle due risoluzioni (1080x1920 e 1080x2520).
- Misura e riporta: dimensione delle celle in px nei livelli 3x3 e 5x5, distanza della griglia dai bordi e dalle barre, elementi tagliati o sovrapposti.
- Riporta la tabella dei 10 livelli (griglia, mosse minime).
- Riporta quanto impiega il generatore per un livello 5x5 (tempo medio).

## Criterio di completamento
- Il push di ErNa-001 è andato a buon fine.
- Compilazione senza errori né warning.
- Test e validatore verdi.
- I 10 livelli del capitolo 1 si giocano dall'inizio alla fine, con stelle salvate e sblocco progressivo.
- Anteprime generate.

## A fine lavoro
- Aggiorna `PROGRESS.md` (UTF-8) e `docs/architettura.md` (cosa è nel Core e cosa nell'Idraulico).
- Commit e push con test e validatore verdi.
- Report breve: cosa è stato fatto, cosa è finito nel Core, risultati di test e validatore, tabella dei livelli, misure, tempi del generatore, problemi o cose in sospeso.