# Grafica di Resolver Master

## Stile
- **Cartoon con contorni neri**, forme arrotondate, colori pieni. Dall'ErNa-005 pulsanti, pannelli, caselle, stelle e icone sono **disegni di Gemini** (`Assets/Art/UI/`).
- Pulsanti (`PressableButton`):
  - premuto: il pulsante scende di `pressDrop` (8 unità) e si scurisce (`pressDarken` 0,86);
  - disattivato: gli sprite sono **desaturati** (shader `ResolverMaster/UI Desaturate`, materiale `UiKitStyle.disabledMaterial`) e i testi sbiaditi.
- Pulsanti a pillola (verde e crema): a 9 fette, le estremità tonde restano intatte e si allunga solo il centro (`SlicedFit` tiene l'altezza: **mai più alti di 159 px**, l'altezza del disegno).
- Pulsanti rotondi (crema; giallo per l'ingranaggio della Home) con l'icona sopra; GIOCA! e "Apri l'acqua" usano la pillola verde (con il rubinetto su "Apri l'acqua").
- Il **nastro verde** non si allunga mai (le pieghe si deformerebbero): si disegna alla sua misura (titolo del pannello di vittoria).
- **Testi con TextMesh Pro**: font Fredoka SDF (atlante statico con i caratteri latini e italiani), contorno SDF continuo dal materiale `Fredoka-SemiBold SDF Outline` (`UiKitStyle.textOutline`, `textOutlineDilate`).
- **Mai blu né arancione** come colori dell'interfaccia e dei terreni: sono i colori dell'acqua dell'Idraulico (il validatore controlla i terreni dei temi).
- Pulsanti toccabili di almeno **130 px** di lato (`minTouchSize`, misurato dalle anteprime). Nessun testo dentro le immagini.

## Cartelle
| Cartella | Contenuto | Chi la scrive |
|---|---|---|
| `ArtSource/` | originali di Gemini (`UI/ui_1..8_*.png`, `Cards/*.png`, `Plumber/Fonte_N.png`, `Arrivo_N.png`, `TJ/1..4.png`, `PG/1..6.png`) | l'utente; **il codice li legge e basta** |
| `Assets/Art/UI/` | 30 elementi di Gemini (`btn_*`, `panel`, `ribbon`, `banner_chapter`, `tile_*`, `star_*`, `icon_*`) + `ui_art.json` (misure e bordi a 9 fette) | `Tools/Art/make_ui_art.py` |
| `Assets/Art/UI/Generated/` | cornice delle schede della Home (`card`) e pillola con contorno sottile (`circle`: pallini, badge) | `UiKitGenerator` (*UI Kit > Regenerate Sprites*) |
| `Assets/Art/Icons/` | icone Material segnaposto ancora usate (story, icone dei giochi e dei capitoli) | `Tools/Art/make_icons.py` |
| `Assets/Art/Cards/` | illustrazioni delle schede della Home (`card_<id>.png`) | `Tools/Art/make_ui_art.py` |
| `Assets/Art/Plumber/Sources/`, `Targets/` | fonti (`fonte_N` + `fonte_N_mask`) e arrivi (`arrivo_N`) | `Tools/Art/make_plumber_art.py` |
| `Assets/Art/TrafficJam/` | auto obiettivo (`Target_<Red|Blue>_<Worried|Happy>`), auto (`Car_*`), camion (`Truck_*`), ostacoli (`Obstacle_*`), tutti col muso in alto | `Tools/Art/make_trafficjam_art.py` |
| `Assets/Art/Penguin/` | Pino (PG-002): pinguini di fronte `Penguin_<Orange|Purple>_Front_<Normal|Happy|Scared>` (una tela comune, piedi sul bordo basso; lo stordito dei fogli non si scrive più ma misura ancora la tela, ErNa-012), dall'alto `Penguin_<colore>_Top_<Idle|Slide>` (una tela, corpo al centro, testa in alto), igloo `Igloo_<colore>_<Closed|Open>` (una tela, base in basso), `Fish`, `IceBlock`, `Rock`, tessere `Tile_<Ice|Snow|Water|SunkenBlock|SnowWall>`; importati **senza compressione** (`ArtImportRules.CrispIllustrationDirs`) | `Tools/Art/make_penguin_art.py` |
| `Assets/Art/AppIcon/` | icone Android (ErNa-011): `AppIcon_Foreground_N` / `AppIcon_Background_N` (adattive, 81–432 px), `AppIcon_Round_N` e `AppIcon_Legacy_N` (36–192 px), una per densità; importate senza compressione né ridimensionamento; da `ArtSource/Icon/Icon.png`, che dà anche `ArtSource/Icon/Icon_PlayStore_512.png` | `Tools/Art/make_app_icon.py` |
| `Assets/Art/Fonts/` | Fredoka TTF + font asset SDF e materiale con contorno | `UiArtSetup` (Setup > Apply All) |
| `Assets/Art/Shaders/` | shader e materiale dei pulsanti disattivati | a mano / `UiArtSetup` |
| `Assets/TextMesh Pro/` | TMP Essential Resources | importate una volta (vedi sotto) |

## Script `Tools/Art/make_ui_art.py`
Uso, dalla root del progetto: `python Tools/Art/make_ui_art.py` (Python 3 con Pillow, numpy, scipy).
1. Per ogni foglio di `ArtSource/UI/` toglie il bianco partendo dai bordi; i buchi bianchi chiusi dal contorno nero (dentro la stella vuota, l'ingranaggio, il lucchetto, il trofeo) diventano trasparenti, i riflessi chiari restano.
2. Separa gli elementi (gruppi di pixel separati da spazio bianco; pezzi a meno di 26 px sono lo stesso elemento: onde del suono, X del "suono no") e li nomina in ordine di lettura secondo la tabella `SHEETS`.
3. Se il numero di elementi di un foglio non corrisponde, **si ferma senza scrivere nulla**.
4. Ritaglia con 3 px di margine, **mai ingrandito né rimpicciolito**, scrittura sicura; scrive `ui_art.json` con misure e bordi a 9 fette (pillole, pannello, banner, caselle; il nastro e le icone non si affettano).
5. Prepara le schede della Home da `ArtSource/Cards/` (nomi riconosciuti dall'inizio: `idraul…` → `card_plumber`, `traf…` → `card_trafficjam`, `pg…` → `card_penguin`), al massimo 1024 px. Solo le schede: `python Tools/Art/make_ui_art.py --cards`.

## Ritaglio comune dei disegni dei giochi (`Tools/Art/sprite_cut.py`, PG-002)
Fogli con oggetti in riga su sfondo bianco: sfondo tolto dai bordi (bianchi interni intatti, frangia scura: niente alone), oggetti separati dalle colonne vuote, ritaglio senza scalare, scrittura sicura. Lo usano `make_trafficjam_art.py` e `make_penguin_art.py` (regola del Core: mai copiarlo).

Dopo lo script: *Resolver Master > Setup > Apply All* (o `ConfigAssets.ResetVisualDefaultsBatch`) applica i bordi, riempie lo stile e ricostruisce le librerie.

## TextMesh Pro
- Le TMP Essential Resources si importano **una volta**: nell'Editor *Resolver Master > Setup > Import TMP Essentials*; in batch `Unity -batchmode -quit -projectPath P -importPackage "P/Library/PackageCache/com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage"` (in batch `AssetDatabase.ImportPackage` finirebbe dopo l'uscita).
- `UiArtSetup.EnsureFonts` crea i font asset (campionamento 90, padding 12, atlante 1024 statico) e il materiale con contorno. Nuovi caratteri: aggiungerli a `UiArtSetup.Charset` e ricreare i font (`EnsureFonts(rebuild: true)`).

## Temi dei capitoli (Core)
- Un unico file: `Assets/Resources/ResolverMaster/ThemeConfig.asset` (`ThemeConfig`, Core). Temi: **Giardino** (erba con lastre di pietra chiara; prato con cielo chiaro), **Fattoria** (terra battuta con paglia; campi gialli e marroni), **Città** (marciapiede grigio chiaro; cielo grigio azzurrino con palazzi).
- Ogni capitolo di un gioco nomina il suo tema (`PlumberConfig.chapters[i].theme`); senza tema o con un id sconosciuto si usa `defaultTheme`.
- La grafica è **generata dal codice** (`ThemeArt`) con i colori e il seed del tema: lastra della cella (4 varianti, 336 px), terreno tra le celle (256 px, ripetuto senza giunture), sfondo dello schermo (540x1260, disegnato a 2x, ancorato in basso). Stesso tema = stesse immagini.
- **Immagini di Gemini in futuro**: `VisualTheme.slabImage`, `groundImage`, `backdropImage`; se impostate sostituiscono quelle generate.
- Dove si vede: griglia di gioco (lastre e terreno), schermata dei livelli del capitolo (sfondo), banner del capitolo nella lista (lastra del tema con l'icona).

## Idraulico
- **Tubi** (`PipeSprites`): colore pieno, contorno nero `pipeOutline` 0,022 della cella (metà dell'ErNa-004), spessore `pipeWidthPercent` 34% della cella, **nessun riflesso**. `pipeShading` (0 = spento) prepara una sfumatura simmetrica (chiara al centro, scura sui due bordi) che resta giusta anche durante la rotazione.
- **Acqua** (`WaterGraphic`): una striscia dentro il tubo (`waterWidth` 78% dell'interno) disegnata fino al fronte, con il **fronte arrotondato**; segue i dritti, gli archi delle curve, T e croci. I tempi vengono dalla logica pura (`WaterTiming`, `WaterMotion`): velocità costante `waterSpeed` (4 celle/s) con una leggera accelerazione iniziale (`waterAccelTime` 0,35 s, partenza al 35%); tutto con il tempo reale (uguale a 30 e 60 fps).
- **Fonti e arrivi**: disegni (4 + 4) scelti dal seed, alti `endpointSize` 78% della cella e sollevati di `endpointLift` 0,08: il moncone di tubo entra **sotto** l'oggetto. Sotto ogni oggetto una **ombra morbida** (`shadowWidth`, `shadowHeight`, `shadowAlpha`, `shadowOffset`). La fontana si riempie (`fountainFill`) quando l'acqua arriva, poi gli spruzzi (`winSpray`).

## Traffic jam (TJ-002: disegni di Gemini)
- **Script `Tools/Art/make_trafficjam_art.py`**: separa gli oggetti di ogni foglio di `ArtSource/TJ/` (colonne vuote tra un oggetto e l'altro), toglie il bianco **solo dai bordi** (occhi, strisce, nuvoletta e carta restano), ritaglia senza scalare (specchietti, ruote e coperchio del tombino interi), gira i veicoli di 180° (negli originali il muso è in basso, nel gioco ogni disegno ha il muso in alto) e li **allunga ripetendo le righe di una fascia piatta**: auto sul tetto, al massimo +18%; camion sul cassone fino a 3 celle (+89%). Cabina, faccia e contorno restano come disegnati; nessun ingrandimento. Stampa il colore della carrozzeria di ogni veicolo (da copiare in `TrafficJamConfig.vehicleColors`).
- **Veicoli**: il disegno sta in (lunghezza − 2·`vehicleGap`) x (1 − 2·`vehicleGap`) celle (`vehicleGap` 0,06), con la stessa scala sui due assi, centrato: specchietti compresi, i vicini non si toccano. Auto e camion all'88% della cella in larghezza (a 7x9: 120 px). Con il muso in alto la faccia appare capovolta: è la vista dall'alto.
- **Scelte dal seed** (`TrafficLook`, logica pura): auto obiettivo rossa/blu col muso verso l'uscita, preoccupata, **felice dal primo fotogramma dell'uscita** (anche con la lampadina; torna preoccupata con Annulla); gli altri veicoli prendono una delle 4 auto o dei 3 camion e un verso lungo la loro linea.
- **Ostacoli**: barriera, tombino, masso, immondizia, nella casella con margine `obstacleInset`. Quali in ogni tema: `TrafficJamConfig.obstaclesByTheme` (`ThemeVariants` del Core): Cantiere barriera e tombino, Mercato immondizia, Aeroporto barriera, Incrocio tombino e immondizia, Ora di punta e altri temi tutti; nella Challenge tutti.
- **Riserva**: se manca un disegno si usano gli strati provvisori disegnati dal codice (`TrafficSprites`, TJ-001): ruote, carrozzeria tinta (`vehicleColors`), strisce, vetri; barriera a strisce. Restano dal codice anche le uscite colorate nel bordo e la freccia della lampadina.
- Colori in `TrafficJamConfig`: rosso e blu riservati alle auto obiettivo (`targetColors` = vernice dei disegni, #ED3331 e #357DC5); `vehicleColors` = vernice degli altri disegni (validatore: ΔE ≥ 20 dal rosso e dal blu).
- Temi dei capitoli (Core): Parcheggio, Mercato, Città, Cantiere, Aeroporto, Incrocio, Ora di punta; icone Material segnaposto in `Assets/Art/Icons` (`make_icons.py`).

## Come aggiungere o cambiare un'immagine
- **Elemento dell'interfaccia**: cambia il foglio in `ArtSource/UI/` mantenendo ordine e numero degli elementi, poi `python Tools/Art/make_ui_art.py` e *Setup > Apply All*. Un elemento nuovo: una riga in `SHEETS` (nome e tipo di taglio) e in `UiArtSetup.Elements`.
- **Fonte o arrivo**: originale in `ArtSource/Plumber/`, `python Tools/Art/make_plumber_art.py`, *UI Kit > Rebuild Sprite Libraries*; se cambia il numero di disegni, `PlumberConfig.artVariants`.
- **Veicolo o ostacolo di Traffic jam**: originale in `ArtSource/TJ/` (stesso ordine), `python Tools/Art/make_trafficjam_art.py`, *Rebuild Sprite Libraries*; un disegno nuovo: un nome in `NAMES` dello script e in `TrafficLook`.
- **Scheda della Home**: originale in `ArtSource/Cards/`, una riga in `CARDS` di `make_ui_art.py`, lo script, *Rebuild Sprite Libraries*; il gioco dichiara `GameInfo.CardImage` e `CardColor` (non blu né arancione).
- **Icona dell'app** (ErNa-011): sostituisci `ArtSource/Icon/Icon.png` (disegno su bianco), `python Tools/Art/make_app_icon.py`, poi *Setup > Apply Project Settings* (imposta tutte le icone Android e il colore dello splash). Lo script toglie il bianco fuori dal disegno (anche le sacche bianche fuori dal disco) senza alone, prende il blu notte dal bordo del disco e mette il disegno, carte comprese, entro la zona sicura dell'icona adattiva (0,285 del lato contro 0,306).
- Poi sempre *Validate* e *Make Previews*.

## Cambiare lo stile
- Valori in `UiKitStyle` (colori, misure, testi, contorno dei testi, pressione dei pulsanti); dopo un cambio del contorno dei testi o dei raggi: *Setup > Apply All*.
- Per riportare `UiKitStyle`, `ThemeConfig` e `PlumberConfig` ai valori scritti nelle classi: `-executeMethod ResolverMaster.Editor.ConfigAssets.ResetVisualDefaultsBatch` (le modifiche a mano a quei file si perdono: controllare prima con git).
