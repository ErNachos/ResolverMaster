# Update ErNa-007 — Pubblicità con premio (di prova), lampadina e Challenge

## Modalità di lavoro (l'utente non è al PC)
- **Non fermarti a fare domande.** Quando c'è una scelta aperta, prendi la soluzione più ragionevole e annotala.
- **Piccolo problema**: aggiralo, annotalo e vai avanti.
- **Problema grave** (il progetto non compila, test rotti che non riesci a riparare): riporta quella parte all'ultima versione funzionante, annota e passa alla parte successiva.
- Annota tutto in `Prompt/Note-ErNa-007.md` ("Scelte fatte", "Problemi aperti").
- **Commit e push dopo ogni parte completata** (A, B, C, D), con test verdi, validatore OK e 0 warning. Mai push --force, reset --hard o clean.
- Alla fine scrivi il riepilogo finale e fermati.

## Contesto
Resolver Master, minigioco Idraulico con 9 capitoli di Storia. AdMob non è ancora integrato. Nella schermata della Storia c'è un pulsante lampadina senza funzione.

## Parte A — Pubblicità con premio (Core)
- Interfaccia nel Core, ad esempio `IRewardedAdService`, con `IsAvailable` e `Show(onRewarded, onFailedOrClosed)`.
- Versione **di prova**, attiva in editor e nelle build di test: pannello "Annuncio di prova" con una breve attesa e due pulsanti (guardato / chiuso).
- Un unico punto per scegliere quale versione usare; quella AdMob arriverà più avanti senza toccare il resto.
- Senza annuncio disponibile, i pulsanti che lo usano sono **disattivati**.

## Parte B — Lampadina (solo Storia)
- Tocco ⇒ annuncio ⇒ se guardato, **un pezzo viene sistemato**.
- Scelta del pezzo: il risolutore cerca la soluzione con meno mosse **dalla situazione attuale** (rispettando bloccati e pezzi già sistemati). Tra i pezzi da ruotare sceglie quello sul percorso **più vicino a una fonte**.
- Il pezzo ruota con animazione e resta **fissato**, con un contorno luminoso diverso dai bulloni. Non si può più ruotare né annullare.
- Le rotazioni della lampadina **non contano** come mosse.
- Livello con lampadina usata ⇒ al massimo **2 stelle**.
- Al reset per errore dell'acqua i pezzi sistemati **restano** sistemati.
- La lampadina è disattivata se tutti i pezzi sono già giusti o se non c'è l'annuncio. **Nascosta in Challenge.**

## Parte C — Challenge: generazione
- **Deterministica**: stesso seed ⇒ stesso livello su qualsiasi telefono. Generatore casuale proprio nel Core (PCG o xorshift), non `UnityEngine.Random` né `System.Random`.
- Limite in **numero di tentativi**, non in tempo. Se si sfora, seed derivato fisso (seed + 1) e si riprova.
- Seed del livello = hash(seed della run, numero del livello, numero di cambi).
- **Tabella a gradini** nel file dei parametri dell'Idraulico (griglia, fonti, ponti/bloccati/croci, intervallo di mosse minime). Si parte da 3x4 / 4x5 con 1 fonte e si arriva a 7x9 con 4 fonti. Dente di sega quando entra una novità. Dopo l'ultimo gradino si resta al massimo. Si sale di gradino ogni 3 livelli all'inizio, ogni 5 più avanti (valori nel file).
- **Massimo di mosse** = minimo + margine. Il margine scende dal 60% al 10% del minimo, **mai sotto +1**.
- Stelle come in Storia.
- Generazione fuori dal thread principale, con una scorta dei prossimi 2 livelli.
- Menu `Resolver Master/Idraulico/Prova Challenge`: genera i primi 50 livelli di un seed e scrive gradino, griglia, fonti, minimo, massimo e tempo.

## Parte D — Challenge: la run
- **Corsa infinita.** Il seed della run è casuale.
- Sempre visibili: mosse fatte, **mosse minime** e **massimo consentito**. Il contatore cambia colore quando mancano 2 mosse (senza colori dell'acqua).
- Al massimo i pezzi **non ruotano più**: si può solo annullare o aprire l'acqua.
- **Si perde** all'errore dell'acqua (perdita, colori mescolati, colore sbagliato, fontana asciutta). Niente vite.
- **3 annullamenti per run**; annullare **toglie la mossa** dal contatore.
- **Punteggio** = somma delle stelle; a parità contano i livelli superati. Record in locale.
- Fine run: stelle, livelli, record (evidenziato se nuovo), **Nuova run**, **Home**. Lascia spazio per "Continua" (ErNa-008).
- **Pausa** (Riprendi / Home) e **salvataggio automatico a ogni mossa** e a ogni cambio di livello. Salva: seed, livello, numero di cambi, rotazioni, mosse, storico degli annullamenti, annullamenti rimasti, salvavita usato, stelle, livelli superati.
- Una sola run attiva per gioco; iniziarne una nuova chiede conferma.
- **Home**: nella scheda dell'Idraulico, accesso a Challenge ("Riprendi" se c'è una run in sospeso) e record. Icone segnaposto.
- Struttura generica nel Core: un gioco futuro deve poter avere la sua Challenge fornendo solo generatore, risolutore e tabella.

## Regole generali
- Nessun testo nel codice; testi nuovi in italiano e inglese.
- Test da aggiungere: la lampadina sistema un pezzo, non conta mosse, dà al massimo 2 stelle e i pezzi sistemati sopravvivono al reset; stesso seed ⇒ stesso livello e nessuna dipendenza dal tempo; massimo ≥ minimo + 1; si perde all'errore; rotazioni bloccate al massimo; annulla toglie la mossa e si ferma a 3 per run; somma delle stelle e record; salvataggio e ripresa identici; pulsanti disattivati senza annuncio.

## Riepilogo finale
In `Prompt/Note-ErNa-007.md` e come ultimo messaggio:
- cosa è stato fatto, parte per parte;
- tempi di generazione per gradino;
- scelte fatte e problemi aperti;
- cosa provare sul telefono.