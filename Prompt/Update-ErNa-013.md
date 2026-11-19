# Update ErNa-013 — AdMob: annunci con premio e consenso (UMP)

Ambito: **Core** (generale). Non lanciare insieme ad altri update che toccano il Core.

## Modalità di lavoro (l'utente non è al PC)
- Non fare domande: scegli la soluzione più ragionevole.
- I piccoli problemi si aggirano; se un problema è grave, riporta la parte interessata all'ultima versione funzionante.
- **Non creare file di note** (`Note-*.md` o simili). Scelte e problemi vanno solo nel riepilogo finale, come messaggio.
- Commit e push su `main` solo con test verdi, validatore OK e 0 warning. Mai `push --force`, `reset --hard`, `clean`.
- Se l'Editor di Unity è aperto, lavora su una copia in `Logs/BatchCopy/`.
- Il riepilogo finale è solo un messaggio.

## Obiettivo
Oggi gli annunci con premio sono simulati: l'APK di test mostra un pannello di prova ("Chiudi" dà sempre il premio) e l'AAB di rilascio non ha annunci. Integra **AdMob vero** nel Core, con consenso UMP, senza cambiare il comportamento di Idraulico, Traffic jam e Pino.

## Dati AdMob
- **ID app (Android):** `ca-app-pub-6347940602066252~5954105532`
- **ID unità con premio (una sola, per tutti gli usi):** `ca-app-pub-6347940602066252/9823574496`
- **ID di test di Google (da usare quando non è un build di rilascio):**
  - ID app di test: `ca-app-pub-3940256099942544~3347511713`
  - unità con premio di test: `ca-app-pub-3940256099942544/5224354917`

Gli ID sono in **un solo punto** del Core (una piccola classe o ScriptableObject di configurazione), non sparsi nel codice.

## Regola del Core
Tutto il codice degli annunci sta nel Core. Individua il gestore annunci esistente (quello che oggi mostra il pannello di prova) e **mantieni invariata la sua interfaccia verso i giochi**, così Continua, Cambia livello e Lampadina (e i tre giochi) non cambiano. Non copiare nulla nei singoli giochi.

---

## Parte 1 — SDK e impostazioni
1. Installa il plugin **Google Mobile Ads per Unity** (ultima versione compatibile con Unity 6000.0.85f1) con il suo gestore delle dipendenze Android (External Dependency Manager). Risolvi le dipendenze Android.
2. Imposta l'**ID app** nelle impostazioni del plugin (Android). Per i build non di rilascio usa l'ID app di test; per il build di rilascio (AAB) l'ID app vero. La scelta deve essere automatica in base al tipo di build (nessuna modifica a mano prima di costruire).
3. Verifica che nel manifest finale ci siano:
   - il meta-data dell'ID app;
   - il permesso `com.google.android.gms.permission.AD_ID` (di solito arriva dall'SDK; se non c'è, aggiungilo).
4. Compatibilità: IL2CPP, ARM64, minSdk 26 e target API 36 restano invariati. Se il plugin impone altro, scegli la soluzione più ragionevole e scrivila nel riepilogo.
5. Imposta `MobileAds.RaiseAdEventsOnUnityMainThread = true` prima dell'inizializzazione, così i callback arrivano sul thread principale.

## Parte 2 — Consenso (UMP)
1. All'avvio, **prima** di inizializzare gli annunci: richiedi l'aggiornamento dello stato del consenso e mostra il modulo se necessario (utenti UE/UK).
2. Inizializza l'SDK degli annunci solo quando il consenso lo permette (`CanRequestAds`). Se il consenso non è richiesto o è già dato, inizializza subito.
3. Se il modulo di consenso non si carica (rete assente o errore), non bloccare il gioco: gli annunci restano non disponibili finché il consenso non è ottenuto; riprova al prossimo avvio.
4. Aggiungi nelle Impostazioni un pulsante **"Opzioni privacy"** che apre il modulo delle scelte sulla privacy. Il pulsante è visibile **solo** quando l'SDK indica che è richiesto (`PrivacyOptionsRequirementStatus`).
5. Solo nei build di sviluppo/APK di test (mai nel rilascio): se è semplice, abilita la geografia di debug "SEE/EEA" per poter vedere il modulo di consenso anche in Italia. Se non è semplice, ometti e scrivilo nel riepilogo.
6. Testi nuovi nelle tabelle delle lingue del Core (niente testo nel codice):
   - IT: "Opzioni privacy" / "Annuncio non disponibile. Riprova tra poco."
   - EN: "Privacy options" / "Ad not available. Try again shortly."

## Parte 3 — Annunci con premio nel gestore del Core
1. **Precarica** l'annuncio con premio dopo l'inizializzazione. Dopo ogni visualizzazione (chiusura o errore) ricarica subito il successivo.
2. Quando il giocatore chiede un annuncio (Continua, Cambia livello, Lampadina):
   - se l'annuncio è pronto: mostralo;
   - **il premio si dà solo quando l'SDK segnala che l'utente ha guadagnato la ricompensa**, non alla semplice chiusura;
   - se l'utente chiude prima della fine: nessun premio;
   - se l'annuncio non è pronto o fallisce: mostra il messaggio "Annuncio non disponibile…" (nella lingua corrente) e **nessun premio**; avvia un nuovo caricamento.
3. Durante l'annuncio: metti in pausa musica ed effetti e rimettili alla chiusura; blocca l'input del gioco; gestisci correttamente la pausa/ripresa dell'app (focus). Non lasciare la partita in uno stato inconsistente se l'app va in background durante l'annuncio.
4. Conformità alle norme AdMob: gli annunci con premio si mostrano **solo su richiesta del giocatore**, mai da soli, né all'avvio né a fine livello. Il pannello che spiega "guarda un annuncio per…" resta com'è.
5. **Pannello di prova:** resta solo come sostituto nell'Editor di Unity e nei test automatici (tramite l'interfaccia del gestore). Sul telefono si usano sempre annunci veri: di test nell'APK di test, veri nell'AAB.
6. Nel build di sviluppo/APK di test usa **solo** l'ID unità di test. Nell'AAB di rilascio usa **solo** l'ID unità vero. Mai l'ID vero in un build di test (rischio sospensione dell'account).

## Parte 4 — Validatore, test, build
1. **Validatore:** aggiungi i controlli:
   - ID app e ID unità vero presenti e nel formato corretto;
   - nel rilascio non compaiono gli ID di test; nei build di test non compare l'ID vero;
   - il permesso `AD_ID` e il meta-data dell'ID app sono nel manifest.
2. **Test:** aggiungi test per la logica dei premi (premio solo con ricompensa guadagnata; nessun premio se non pronto, chiuso prima o in errore; ricarico dopo ogni annuncio) usando un fornitore finto. I test esistenti (66) devono restare verdi. 0 warning.
3. **Build:** costruisci l'**AAB** e l'**APK di test** (`Resolver Master > Build Release AAB` e `Build Release APK (test)`).
   - **Non cambiare il version code:** l'AAB 1.0.0 (1) non è ancora stato caricato su Play Console.
   - Controlla, con gli strumenti disponibili (per esempio `bundletool` o `aapt2`), che nel manifest dell'AAB ci siano `AD_ID` e l'ID app **vero**. Se non puoi verificarlo, scrivilo nel riepilogo.
4. Aggiorna il riepilogo del progetto solo nel messaggio finale (nessun file di note).
5. Commit e push (messaggio tipo `ErNa-013: AdMob annunci con premio e consenso UMP`).

## Da non toccare
Regole di gioco, stelle, generazione dei livelli, grafica, salvataggi, Storia, Challenge e sfida del giorno. Questo update cambia solo come il Core mostra gli annunci e raccoglie il consenso.

## Riepilogo finale (solo messaggio)
- Versione del plugin installata e come sono stati risolti gli ID di test/rilascio.
- Se il manifest dell'AAB contiene `AD_ID` e l'ID app vero.
- Scelte fatte e problemi incontrati.
- Risultato dei test (numero), del validatore (numero di controlli) e hash del commit.
- Cosa deve provare l'utente sul telefono con l'APK di test: annuncio di test che si apre da Continua, Cambia livello e Lampadina; premio solo a fine annuncio; musica che riparte; modulo di consenso (se visibile) e pulsante "Opzioni privacy".