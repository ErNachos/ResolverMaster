# Update-ErNa-005 — Grafica rivista: UI da Gemini, tubi, griglie rettangolari, temi dei capitoli

## Contesto
- **Prima di tutto:** `git pull --ff-only`. Controlla che `Prompt/` e `ArtSource/` siano nel repository: se non lo sono, aggiungili con un commit separato.
- Unity è **chiuso** durante questo Update.
- La prova sul telefono dell'Update 004 non ha convinto: tubi con contorni troppo spessi, riflesso dei tubi incoerente quando ruotano, pulsanti e scritte generati dal codice poco belli, molto spazio vuoto sopra e sotto la griglia, colori senza identità.
- Questo Update **non cambia le regole del gioco**: cambia l'aspetto e la forma delle griglie.
- **Regola del Core:** UI kit, temi, sfondi e griglia rettangolare vanno nel Core; nell'Idraulico restano solo tubi, fonti, arrivi e i loro parametri.
- **Lingue:** ogni testo nuovo va nelle tabelle IT/EN. Nessun testo dentro le immagini.

## NON toccare
- Gli originali in `ArtSource/`: gli script li leggono e basta.
- Le regole dell'Idraulico e il risolutore (va solo adattato alle griglie rettangolari, se serve).
- Le immagini di fonti e arrivi: restano le stesse 4 + 4, con la loro prospettiva.
- Nessun keystore e nessuna password nel repository; firma mai in `ProjectSettings.asset`.
- Git: **mai** `--force`, `reset --hard`, `clean`.

## Parti

### Parte 1 — Interfaccia da Gemini
Le immagini sono in `ArtSource/UI/`. Ogni immagine contiene più elementi separati da spazio bianco:

| File | Contenuto (in ordine di lettura: da sinistra a destra, dall'alto in basso) |
|---|---|
| `ui_1_pulsanti.png` | pulsante rettangolare verde, pulsante rettangolare crema, pulsante rotondo crema, pulsante rotondo giallo |
| `ui_2_pannelli.png` | pannello finestra crema, nastro titolo verde, banner capitolo giallo |
| `ui_3_caselle.png` | casella livello crema (normale), verde (completato), grigia (bloccato) |
| `ui_4_icone_navigazione.png` | freccia indietro, freccia avanti, casa, X |
| `ui_5_icone_gioco.png` | annulla, ricomincia, lampadina (aiuto), spunta |
| `ui_6_icone_stelle.png` | stella piena, stella vuota, lucchetto, ingranaggio |
| `ui_7_icone_impostazioni.png` | suono sì, suono no, vibrazione, globo (lingua) |
| `ui_8_icone_speciali.png` | rubinetto (apri l'acqua), trofeo, fulmine, calendario |

- **Script `Tools/Art/make_ui_art.py`:** separa gli elementi di ogni immagine (regioni separate dallo sfondo bianco), toglie il bianco esterno, ritaglia, dà a ogni elemento un nome chiaro (`btn_green`, `btn_cream`, `btn_round_cream`, `btn_round_yellow`, `panel`, `ribbon`, `banner_chapter`, `tile_level`, `tile_done`, `tile_locked`, `icon_back`, `icon_next`, …) e salva in `Assets/Art/UI/`. Mai ingrandire. Rieseguibile, con scrittura sicura. Riporta nel report quanti elementi ha trovato in ogni immagine e la dimensione di ognuno: se il numero non corrisponde alla tabella, fermati e segnalalo.
- **Bordi a 9 fette** per pulsanti rettangolari, pannello, banner e caselle: gli angoli restano intatti, si allunga solo il centro. Il **nastro titolo** non si allunga a 9 fette (le pieghe si deformerebbero): si scala in modo uniforme.
- **Pulsanti:** premuto = scende di qualche pixel e si scurisce leggermente; disattivato = desaturato. Tutto dal codice.
- **Icone:** sopra i pulsanti rotondi crema; GIOCA! e "Apri l'acqua" usano il pulsante verde (con l'icona rubinetto su "Apri l'acqua"). Impostazioni nella Home: pulsante rotondo giallo con ingranaggio.
- **Impostazioni:** suono con icona sì/no; vibrazione con la sua icona, attenuata e con una piccola X dal codice quando è spenta; lingua con il globo.
- **Stelle:** piena e vuota da Gemini, ovunque (caselle, capitoli, vittoria).
- **Scritte con TextMeshPro** e contorno SDF pulito e continuo, nel font già in uso. Elimina il vecchio contorno fatto con copie del testo.
- **Schede della Home:** prepara anche le illustrazioni in `ArtSource/Cards/` (`idraulico.png`, `traffic.png`): togli lo sfondo bianco, ritaglia, salva in `Assets/Art/Cards/`. Il riquadro colorato resta del codice.
- Rimuovi i vecchi sprite generati dal codice che non servono più e le icone Material Symbols non più usate (aggiorna `docs/crediti.md`).

### Parte 2 — Tubi
- **Contorno nero circa la metà** di quello attuale.
- **Nessun riflesso.** Il tubo è a colore pieno. Prepara in `PlumberConfig` un parametro, **disattivato**, per una sfumatura **simmetrica** (più chiara al centro, più scura sui due bordi), che resta corretta in ogni posizione anche durante la rotazione.
- Spessore del tubo in percentuale della cella, in `PlumberConfig`.

### Parte 2b — Acqua fluida
- Oggi l'acqua avanza **a scatti**, una cella alla volta. Deve invece scorrere in modo **continuo**: il fronte dell'acqua avanza dentro ogni tubo seguendone la forma (dritto, curva ad arco, T, croce) e passa alla cella successiva senza pause.
- **Velocità costante** in tutto il percorso, in celle al secondo, in `PlumberConfig`. Solo una leggera accelerazione all'inizio, quando si apre l'acqua.
- **T:** dopo l'incrocio l'acqua prosegue nei due rami **contemporaneamente**, alla stessa velocità.
- **Più fonti:** tutti i flussi partono insieme e avanzano con la stessa velocità.
- **Arrivo:** quando l'acqua entra, la fontana si riempie gradualmente, poi gli spruzzi.
- **Errore:** il fronte avanza fluido fino al punto sbagliato, poi parte l'effetto (schizzi per la perdita, evidenziazione per i colori).
- Il fronte dell'acqua ha il **bordo arrotondato**, non tagliato dritto.
- Animazione **indipendente dai fotogrammi**: stessa durata a 30 e a 60 fps.
- La logica del percorso (in che ordine e in quanto tempo l'acqua raggiunge ogni punto) resta nella logica C# pura già esistente. La parte visiva usa quei tempi.

### Parte 3 — Fonti e arrivi
- Una **piccola ombra morbida** sotto ogni oggetto, così sembra appoggiato sulla cella.
- Il pezzetto di tubo verso l'apertura **entra da sotto l'oggetto**, non di lato.

### Parte 4 — Griglie rettangolari
- La griglia del Core supporta **colonne e righe diverse**. Il risolutore e il generatore dell'Idraulico vanno adattati, se serve.
- Nuove dimensioni (colonne x righe):

| Prima | Ora |
|---|---|
| 3x3 | 3 x 4 |
| 4x4 | 4 x 5 |
| 5x5 | 5 x 7 |
| 6x6 | 6 x 8 |

- Aggiorna la tabella di difficoltà in `PlumberConfig` e **rigenera i 27 livelli** con il menu. Mantieni l'idea dei primi livelli del capitolo 1: 1, 2 e 3 mosse minime. Aggiorna `docs/idraulico-livelli.md`.
- La griglia usa al massimo lo spazio tra la barra in alto e "Apri l'acqua", con margini costanti, e resta centrata.

### Parte 5 — Temi dei capitoli (Core + parametri per gioco)
- Ogni capitolo ha un **tema**, in un unico file di parametri:

| Cap. | Tema (IT / EN) | Terreno delle celle | Sfondo dello schermo |
|---|---|---|---|
| 1 | Giardino / Garden | erba con lastre di pietra chiara | verde prato, con cielo chiaro in alto |
| 2 | Fattoria / Farm | terra battuta con un po' di paglia | campi gialli e marroni caldi |
| 3 | Città / City | marciapiede grigio chiaro | grigio azzurrino con sagome di palazzi semplici |

- Per ora terreno e sfondo sono **generati dal codice** con colori, motivi semplici e casualità con seed (per esempio ciuffi d'erba e sassolini nel giardino). Più avanti arriveranno immagini da Gemini: prepara il punto dove andranno.
- Il terreno deve far risaltare **tubi grigi** e **acqua blu e arancione**: niente blu né arancione forti nei terreni.
- Il tema si applica a griglia di gioco, schermata del capitolo (griglia dei livelli) e banner del capitolo nella lista.
- Fonti e arrivi per ora restano le stesse varianti per tutti i temi.

### Parte 6 — Validatore
- UI: tutti gli elementi attesi esistono in `Assets/Art/UI/`.
- Ogni capitolo ha un tema valido.
- Controlli dei livelli invariati, sulle nuove griglie.

## Test
Al massimo 5 nuovi test EditMode, solo sulla logica:
1. Griglia rettangolare: vicini corretti sui bordi e negli angoli di una griglia 3x4.
2. Generatore: su griglia 5x7, lo stesso seed dà lo stesso livello, ed è risolvibile.
3. Temi: ogni capitolo restituisce il suo tema, e un capitolo senza tema usa quello predefinito.
4. Tempi dell'acqua: su un livello scritto a mano con una T, i due rami vengono raggiunti nello stesso istante, e una cella più lontana viene raggiunta più tardi in proporzione alla distanza.

## Verifiche e anteprime
- **Make Previews**, nelle due risoluzioni (1080x1920 e 1080x2520):
  - Home (IT ed EN), scelta della modalità, Impostazioni;
  - lista dei capitoli, griglia dei livelli di ogni capitolo;
  - un livello per capitolo prima dell'apertura (3x4, 5x7, 6x8);
  - un'apertura riuscita a due colori;
  - il pannello di vittoria.
- Misura e riporta: dimensione delle celle per ogni griglia, spazio libero sopra e sotto la griglia, dimensione minima dei pulsanti, testi tagliati, elementi sovrapposti, e se qualche elemento UI viene **ingrandito** oltre la sua risoluzione.
- Guarda anche le immagini, non solo le misure. Prima misura e riporta, poi correggi.

## Criterio di completamento
- Compilazione senza errori né warning.
- Test e validatore verdi.
- Tutte le schermate usano pulsanti, icone e pannelli di Gemini e le scritte TextMeshPro.
- Tubi senza riflesso e con contorno più sottile; ombre sotto fonti e arrivi.
- Acqua che scorre fluida, senza scatti tra una cella e l'altra.
- 27 livelli rigenerati su griglie rettangolari, giocabili.
- Tre temi visibili.
- Anteprime generate.

## A fine lavoro
- Aggiorna `PROGRESS.md` (UTF-8), `docs/architettura.md`, `docs/grafica.md` (UI da Gemini, script, temi) e `docs/crediti.md`.
- Commit e push con test e validatore verdi.
- Report breve: cosa è stato fatto, elementi trovati dallo script, risultati di test e validatore, tabella dei livelli, misure, problemi o cose in sospeso.