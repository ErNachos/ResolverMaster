# Update-ErNa-001 — Struttura base di Resolver Master

## Contesto
- **Resolver Master** è una nuova app Android: una raccolta di **minigiochi puzzle**, semplici, immediati e senza registrazione, adatti a tutti.
- Nella **Home** si sceglie il minigioco. Ogni minigioco ha due modalità:
  - **Storia:** livelli in capitoli da 10, con 1–3 stelle in base alle mosse fatte rispetto al minimo.
  - **Challenge:** livelli generati al momento, sempre più difficili, con un limite di mosse (minimo + margine).
- I primi due minigiochi (arriveranno negli Update successivi, **non in questo**):
  - **Idraulico:** si ruotano i pezzi di tubo per portare l'acqua dalle fonti agli arrivi.
  - **Traffic jam:** si spostano auto e camion per far uscire un veicolo dal parcheggio.
- **Regola principale dell'architettura:** tutto ciò che può servire a più minigiochi sta nel **Core** (griglia, livelli, mosse, stelle, Storia, Challenge, salvataggio, lingue, UI kit, icone, audio, pubblicità). Ogni minigioco contiene solo le sue regole, i suoi pezzi, il suo generatore/risolutore e la sua grafica. Se una meccanica nata per un gioco può servire ad altri, va spostata nel Core, **mai copiata**.
- Questo Update crea solo lo **scheletro**: progetto, documenti, Home, scelta della modalità (segnaposto), impostazioni, lingue, salvataggio, UI kit, menu dell'Editor e validatore vuoto.
- **Unity è chiuso** durante questo Update: lancia tu test, validatore e build in batchmode.
- Editor Unity: `6000.0.85f1` in `C:\Users\filip\UnityEditors\`.
- Repository GitHub: `https://github.com/ErNachos/ResolverMaster.git` (privato, vuoto: se non ha ancora commit, collega il progetto a questo remote e fai il primo commit)

## NON toccare
- Nessun file fuori dalla cartella di questo progetto.
- La cartella `ArtSource/` (originali di Gemini): mai modificata dal codice.
- Nessun keystore e nessuna password nel repository. **Non salvare la firma in `ProjectSettings.asset`**: la firma si legge solo dalle variabili d'ambiente al momento della build.
- Git: **mai** `--force`, `reset --hard`, `clean`.

## Parti

### Parte 1 — Progetto e impostazioni
- Prodotto: **Resolver Master**, azienda `eebbproduction`, package `com.eebbproduction.resolvermaster`.
- Versione `1.0.0`, version code `1`.
- Android: IL2CPP, solo ARM64, target API 36, minimo API 26, librerie native allineate a 16 KB.
- **Solo portrait** (verticale e verticale capovolto disattivato). Riferimento 1080x1920, rapporti da 9:16 a 9:21.
- Nuovo **Input System** (vecchio disattivato).
- UI con **uGUI** (Canvas Scaler su 1080x1920).
- `.gitignore` per Unity (`Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, `Previews/`, file del keystore `*.keystore`, `*.jks`).

### Parte 2 — Cartelle e documenti
- Crea le cartelle: `Assets/Scripts/Core/`, `Assets/Scripts/Games/`, `Assets/Scripts/Editor/`, `Assets/Tests/EditMode/`, `Assets/Art/`, `ArtSource/`, `Tools/Art/`, `Previews/`, `docs/`, `Prompt/`, `Release/Site/`, `Release/PlayStore/`, `Builds/Release/`.
- Assembly definition separati per Core, Games, Editor e Tests.
- **`CLAUDE.md`**: Unity chiuso durante gli Update; pull `--ff-only` a inizio lavoro; commit e push a fine lavoro solo con test e validatore verdi; comandi git vietati; cosa non toccare; massimo circa 5 nuovi test per Update, solo logica; prima misurare e poi correggere i bug visivi; mai condividere `Editor.log`.
- **`GUIDELINES.md`**: la regola del Core; logica in classi C# pure; parametri di ogni sistema in un unico file; nessun testo nel codice (solo chiavi di lingua); salvataggio con versione; casualità con seed; pooling.
- **`PROGRESS.md`** in **UTF-8**, con la voce di questo Update.
- **`docs/architettura.md`**: struttura del Core e dei minigiochi, flusso delle schermate.
- **`docs/crediti.md`**: elenco di font, icone, audio e grafica con la licenza.

### Parte 3 — Schermate e navigazione
- Una sola scena `Main` con un gestore delle schermate (pannelli che si aprono e chiudono, con il tasto Indietro di Android gestito).
- **Home:** titolo dell'app, una scheda per ogni minigioco registrato, pulsante Impostazioni.
- **Registro dei minigiochi** nel Core: ogni minigioco si registra con id, chiave del nome, icona e disponibilità. Per ora due voci: Idraulico e Traffic jam, entrambe segnate "Prossimamente".
- Toccando una scheda si apre la **scelta della modalità** (Storia / Challenge), per ora con un messaggio segnaposto.
- **Impostazioni:** lingua (Italiano / English), suono sì/no, vibrazione sì/no.
- **Safe area** gestita da un componente riutilizzabile su tutte le schermate.

### Parte 4 — Lingue
- Italiano e inglese, con tabelle di testi in file (nessun testo scritto nel codice).
- Al **primo avvio**: italiano se il telefono è in italiano, altrimenti inglese. Poi vale la lingua scelta nelle Impostazioni.
- Chiave mancante: mostra la chiave e segnala l'errore nel validatore.
- Logica in una classe C# pura, separata da Unity, così si testa facilmente.

### Parte 5 — Salvataggio
- File JSON con **numero di versione** e **blocchi opzionali** (impostazioni ora; progressi dei giochi in futuro).
- Le chiavi sconosciute vengono ignorate, i blocchi mancanti prendono i valori predefiniti.
- Scrittura sicura: prima su file temporaneo, poi sostituzione.

### Parte 6 — UI kit e icone
- Un unico file di stile (`UiKitStyle`): colori, raggi, spessori, dimensioni del testo, durate delle animazioni.
- Pulsanti e pannelli a **9 fette generati da script**, con un menu dell'Editor per rigenerarli.
- Icone da **Google Material Symbols** (licenza Apache 2.0), salvate in `Assets/Art/Icons/`, con il credito in `docs/crediti.md`. Per ora: indietro, impostazioni, annulla, ricomincia, aiuto, stella, lucchetto, suono, vibrazione, lingua. Se non riesci a scaricarle, crea segnaposto semplici e scrivilo nel report.
- Font con licenza libera per uso commerciale (Google Fonts), con il credito.

### Parte 7 — Menu dell'Editor
Menu `Resolver Master/`:
- **Validate:** lancia il validatore e riporta gli errori.
- **Build Release AAB** e **Build Release APK (test)**: output in `Builds/Release/`. La firma si legge dalle variabili d'ambiente `RESOLVERMASTER_KEYSTORE`, `RESOLVERMASTER_KEYSTORE_PASS`, `RESOLVERMASTER_KEY_ALIAS`, `RESOLVERMASTER_KEY_PASS`. Se ne manca una, la build si ferma con un messaggio chiaro che dice quale manca (senza mai stampare le password).
- **Bump Version Code:** aumenta il version code di 1.
- **Make Previews:** genera le anteprime della Home e delle Impostazioni a 1080x1920 (9:16) e 1080x2520 (9:21) in `Previews/`.
- Ogni voce deve funzionare anche in **batchmode**.

### Parte 8 — Validatore
- Struttura a controlli registrabili, così ogni minigioco aggiungerà i suoi.
- Per ora controlla: chiavi di lingua presenti in entrambe le lingue, minigiochi registrati con nome e icona validi.

## Test
Al massimo 5 test EditMode, solo sulla logica:
1. Lingua iniziale: telefono in italiano → italiano; altre lingue → inglese.
2. Lingua salvata: dopo la scelta, vale quella e non quella del telefono.
3. Salvataggio: scrittura e lettura restituiscono gli stessi valori.
4. Salvataggio: chiavi sconosciute ignorate e blocchi mancanti con i valori predefiniti.
5. Bump Version Code: il numero aumenta di 1.

Nessun test su layout o grafica, nessun test che dipende dai fotogrammi dell'Editor.

## Verifiche e anteprime
- Lancia **Make Previews** e riporta i percorsi delle immagini.
- Misura e riporta: margini della safe area nelle due risoluzioni, eventuali elementi tagliati o sovrapposti.
- Lancia **Validate** e riporta il risultato.

## Criterio di completamento
- Il progetto si apre senza errori né warning di compilazione.
- Test e validatore verdi.
- Anteprime generate in `Previews/`.
- Se le variabili d'ambiente della firma sono presenti: **APK di test** e **AAB** generati in `Builds/Release/`. Se mancano: scrivilo nel report, non è un errore di questo Update.
- `CLAUDE.md`, `GUIDELINES.md`, `PROGRESS.md`, `docs/architettura.md`, `docs/crediti.md` creati.

## A fine lavoro
- Aggiorna `PROGRESS.md` (UTF-8).
- Commit e push con test e validatore verdi.
- Report breve: cosa è stato fatto, risultati di test e validatore, percorsi delle anteprime, misure della safe area, stato delle build, eventuali problemi o cose rimaste in sospeso.