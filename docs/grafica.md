# Grafica di Resolver Master

## Stile
- **Cartoon con contorni neri spessi**, forme arrotondate e semplici, colori pieni, ombre morbide.
- Pulsanti, schede e pannelli: contorno nero, angoli molto arrotondati, una **fascia più scura in basso** (effetto "da premere").
  - Premuto: il pulsante scende di qualche pixel e la fascia si riduce (`PressableButton`).
  - Disattivato: colori spenti (sfondo verso il grigio, testo e icona attenuati).
- Colori dell'interfaccia (tutti in `UiKitStyle`):
  - sfondo lavanda chiaro, pannelli e schede crema, testo e icone scuri;
  - **pulsante principale** (GIOCA!, Apri l'acqua, Avanti): verde vivo, testo bianco con contorno scuro;
  - pulsanti secondari: crema; pulsanti rotondi per le icone;
  - titoli: testo bianco con contorno scuro (font Fredoka).
- **Mai blu né arancione** come colori dell'interfaccia: sono i colori dell'acqua dell'Idraulico e devono restare riconoscibili (vale anche per i colori dei capitoli e delle schede).
- Pulsanti toccabili di almeno **130 px** di lato (`minTouchSize`, misurato dalle anteprime).
- Nessun testo dentro le immagini: tutti i testi vengono dalle tabelle delle lingue.

## Cartelle
| Cartella | Contenuto | Chi la scrive |
|---|---|---|
| `ArtSource/` | originali di Gemini (`Plumber/Fonte_N.png`, `Plumber/Arrivo_N.png`; in futuro `Plumber/Sources/`, `Plumber/Targets/`, `Reference/`) | l'utente; **il codice li legge e basta** |
| `Assets/Art/UI/` | sprite del UI kit (pulsanti, schede, pannelli, cerchi; a riposo e premuti) | `UiKitGenerator` (*UI Kit > Regenerate Sprites*) |
| `Assets/Art/Icons/` | icone Material Symbols (segnaposto) | `Tools/Art/make_icons.py` |
| `Assets/Art/Plumber/Sources/` | `fonte_N.png` + `fonte_N_mask.png` | `Tools/Art/make_plumber_art.py` |
| `Assets/Art/Plumber/Targets/` | `arrivo_N.png` | `Tools/Art/make_plumber_art.py` |
| `Assets/Art/Cards/` | illustrazioni delle schede della Home (`card_<id>.png`) | in arrivo da Gemini |

I tubi dell'Idraulico non sono immagini: li disegna il codice (`PipeSprites`) con contorno nero, ombreggiatura tonda e un riflesso chiaro separato.

## Script `Tools/Art/make_plumber_art.py`
Uso, dalla root del progetto: `python Tools/Art/make_plumber_art.py` (Python 3 con Pillow, numpy, scipy).
1. Legge `ArtSource/Plumber/Sources/` e `Targets/` (oppure `Fonte_*.png` / `Arrivo_*.png` in `ArtSource/Plumber/`).
2. Toglie lo sfondo bianco partendo dai bordi: il bianco dentro l'oggetto resta, perché i contorni neri sono chiusi.
3. Ritaglia l'oggetto, lo centra in un quadrato con un margine del 4% e lo riduce a 512 px (mai ingrandito).
4. Solo per le fonti: le parti **magenta** (anche ombreggiate) diventano una **maschera** in scala di grigi che conserva le ombre; nell'immagine base diventano grigio chiaro neutro. Il gioco colora la maschera con il colore dell'acqua.
5. Scrive in modo sicuro (file temporaneo, poi sostituzione) e stampa, per ogni fonte, la percentuale di magenta: se è sotto il 2% si ferma con un errore (maschera vuota o sbagliata).

Regole per gli originali delle fonti: le parti che devono prendere il colore dell'acqua vanno disegnate in **magenta puro** (#FF00FF, con le sue ombre); nessun altro magenta nel disegno.

## Come aggiungere o cambiare un'immagine
- **Fonte o arrivo dell'Idraulico**:
  1. metti l'originale in `ArtSource/Plumber/` (o in `Sources/` / `Targets/`);
  2. `python Tools/Art/make_plumber_art.py`;
  3. apri Unity (o batchmode) e lancia *Resolver Master > UI Kit > Rebuild Sprite Libraries*;
  4. se cambia il numero di disegni, aggiorna `PlumberConfig.artVariants`;
  5. *Validate* e *Make Previews*.
- La variante di ogni fonte e di ogni arrivo si sceglie dal **seed del livello** (`ArtVariants`): lo stesso livello ha sempre gli stessi disegni; in un livello con più fonti (o più arrivi) i disegni sono diversi finché ce ne sono.
- Dimensione del disegno rispetto alla cella: `PlumberConfig.endpointSize` (0,8). Il disegno non ruota mai; un moncone di tubo sotto di lui indica il lato dell'apertura.

## Come aggiungere una scheda della Home
1. Il gioco dichiara nel suo `MinigameModule` il nome dell'immagine (`GameInfo.CardImage`, es. `card_plumber`) e il colore del riquadro (`GameInfo.CardColor`, non blu né arancione).
2. Metti `card_<id>.png` in `Assets/Art/Cards/` e lancia *UI Kit > Rebuild Sprite Libraries*.
3. Senza immagine la Home disegna un segnaposto (l'icona del gioco) e il validatore mostra un **warning**.
4. Il nome sulla scheda viene dalla tabella delle lingue (`GameInfo.NameKey`).

## Cambiare lo stile
- Valori in `UiKitStyle` (colori, raggi, spessore del contorno, profondità dei pulsanti, misure della Home); poi *UI Kit > Regenerate Sprites*.
- Per riportare `UiKitStyle` e `PlumberConfig` ai valori scritti nelle classi: `-executeMethod ResolverMaster.Editor.ConfigAssets.ResetVisualDefaultsBatch` (le modifiche a mano a quei due file si perdono: controllare prima con git).
