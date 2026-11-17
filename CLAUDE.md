# Resolver Master — regole di lavoro

Progetto: app Android in Unity 6 (`6000.0.85f1`, in `C:\Users\filip\UnityEditors\`), raccolta di minigiochi puzzle.
Leggi anche [GUIDELINES.md](GUIDELINES.md) (regole tecniche), [PROGRESS.md](PROGRESS.md) (stato) e [docs/architettura.md](docs/architettura.md).

## Inizio sessione
1. Leggi GUIDELINES.md e PROGRESS.md.
2. Leggi il prompt più recente in `Prompt/` (`Update-ErNa-NNN.md`, numero più alto) e verifica in PROGRESS.md cosa è già fatto.
3. `git fetch` e poi `git pull --ff-only`. Se il pull non è fast-forward (storie divergenti, conflitti): **fermati e chiedi**.

## Durante il lavoro
- **Unity è chiuso** durante gli Update: test, validatore, anteprime e build si lanciano in batchmode (comandi sotto).
- Un solo Update alla volta. I file in `Prompt/` sono dell'utente: non modificarli.
- Rispondi in italiano; codice e commenti in inglese.
- Bug visivi: **prima misurare, poi correggere** (Make Previews + report delle misure), mai correzioni "a occhio".
- Se qualcosa è ambiguo, chiedi.

## Test
- Al massimo circa **5 nuovi test per Update**, solo sulla **logica** (EditMode). Nessun test su layout, grafica o fotogrammi dell'Editor: per quelli bastano le anteprime in `Previews/`.

## Fine lavoro
- Aggiorna PROGRESS.md (UTF-8). Se cambia una regola o una decisione tecnica, aggiorna GUIDELINES.md.
- Commit e push **solo con test e validatore verdi**. Messaggio: `Update ErNa-NNN: titolo breve`. Controlla che i `.meta` nuovi siano inclusi.
- Update fallito o a metà: niente commit, riporta il problema.

## Git
- **Vietati**: `git push --force`, `git reset --hard`, `git clean`, cancellare lavoro locale o remoto.
- Se `fetch`/`push` chiedono autenticazione: fermati, l'utente si autentica da sé. Mai token o password in file, chat o comandi.

## Non toccare
- Nessun file fuori dalla cartella del progetto.
- `ArtSource/` (originali di Gemini): mai modificata dal codice.
- Nessun keystore né password nel repository; la firma **non** va in `ProjectSettings.asset` (solo variabili d'ambiente al momento della build).
- **Mai condividere `Editor.log`** (contiene percorsi e dati della macchina): riporta solo le righe utili.

## Comandi batchmode (dalla root del progetto)
```
U="C:/Users/filip/UnityEditors/6000.0.85f1/Editor/Unity.exe"; P="<percorso del progetto>"
"$U" -batchmode -projectPath "$P" -buildTarget Android -runTests -testPlatform EditMode -testResults "$P/Logs/tests.xml" -logFile "$P/Logs/tests.log"
"$U" -batchmode -projectPath "$P" -buildTarget Android -executeMethod ResolverMaster.Editor.Validation.Validator.RunBatch -logFile "$P/Logs/validate.log"   # exit 0 OK, 3 errori
"$U" -batchmode -projectPath "$P" -buildTarget Android -executeMethod ResolverMaster.Editor.PreviewMaker.MakePreviewsBatch -logFile "$P/Logs/previews.log"
"$U" -batchmode -projectPath "$P" -buildTarget Android -executeMethod ResolverMaster.Editor.ProjectSetup.ApplyAllBatch -logFile "$P/Logs/setup.log"
"$U" -batchmode -projectPath "$P" -buildTarget Android -executeMethod ResolverMaster.Editor.ReleaseBuild.BuildApkBatch -logFile "$P/Logs/build.log"   # o BuildAabBatch
"$U" -batchmode -projectPath "$P" -buildTarget Android -executeMethod ResolverMaster.Editor.VersionTools.BumpVersionCodeBatch -logFile "$P/Logs/bump.log"
```
