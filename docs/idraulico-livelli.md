# Idraulico — livelli della Storia

File generato da *Resolver Master > Idraulico > Genera livelli Storia* (non modificare a mano).
Dati del gioco: `Assets/Resources/Games/Plumber/plumber_story.json`. Seed del livello n = 2611000 + n.

## Capitolo 1

| Livello | Gradino | Griglia | Dritti | Curve | T | Croci | Mosse minime | Percorso | Seed | Tentativi |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | c1-1 | 3x3 | 3 | 4 | 0 | 0 | 1 | 3 | 2611001 | 14 |
| 2 | c1-2 | 3x3 | 3 | 4 | 0 | 0 | 2 | 3 | 2611002 | 7 |
| 3 | c1-3 | 3x3 | 3 | 4 | 0 | 0 | 3 | 3 | 2611003 | 1 |
| 4 | c1-4 | 4x4 | 7 | 7 | 0 | 0 | 3 | 4 | 2611004 | 2 |
| 5 | c1-4 | 4x4 | 8 | 6 | 0 | 0 | 3 | 6 | 2611005 | 6 |
| 6 | c1-5 | 4x4 | 6 | 8 | 0 | 0 | 5 | 5 | 2611006 | 3 |
| 7 | c1-5 | 4x4 | 7 | 7 | 0 | 0 | 6 | 7 | 2611007 | 2 |
| 8 | c1-6 | 5x5 | 10 | 11 | 2 | 0 | 7 | 11 | 2611008 | 22 |
| 9 | c1-7 | 5x5 | 9 | 12 | 2 | 0 | 8 | 9 | 2611009 | 2 |
| 10 | c1-8 | 5x5 | 11 | 9 | 3 | 0 | 9 | 10 | 2611010 | 7 |

## Gradini di difficoltà (PlumberConfig)

| Gradino | Livelli | Valori |
|---|---|---|
| c1-1 | 1–1 | size 3, minMoves 1, path 1-3, tees 0, crosses 0 |
| c1-2 | 2–2 | size 3, minMoves 2, path 2-4, tees 0, crosses 0 |
| c1-3 | 3–3 | size 3, minMoves 3, path 3-5, tees 0, crosses 0 |
| c1-4 | 4–5 | size 4, minMoves 3-4, path 4-7, tees 0, crosses 0 |
| c1-5 | 6–7 | size 4, minMoves 5-6, path 5-9, tees 0, crosses 0 |
| c1-6 | 8–8 | size 5, minMoves 7-8, path 9-15, tees 1-3, crosses 0 |
| c1-7 | 9–9 | size 5, minMoves 8-9, path 9-15, tees 1-3, crosses 0 |
| c1-8 | 10–10 | size 5, minMoves 9-10, path 10-15, tees 1-3, crosses 0 |

## Griglie iniziali

Formato di `PlumberLevelCodec`: righe dall'alto separate da `/`; `I` dritto, `C` curva, `T`, `X` croce, `S` fonte, `E` arrivo; la cifra è il numero di rotazioni orarie dalla forma base.

- 1: `C1 E3 C0/C3 C2 I1/I2 S0 I0`
- 2: `E2 I3 C3/I3 C3 C1/C0 I2 S3`
- 3: `C1 S2 I3/E2 I2 C0/C1 C3 I1`
- 4: `I3 I3 C3 C0/I0 C1 C3 I1/C3 I3 E3 I3/C3 I3 S3 C3`
- 5: `I3 C2 I1 I3/C1 I3 C0 I3/C3 C2 I0 I2/C0 E0 S0 I2`
- 6: `C0 I2 I0 C1/C3 S3 C3 E0/I0 C1 C2 C1/C3 I2 I3 I2`
- 7: `I2 I2 I2 I0/C3 E1 C0 S2/C0 C0 C3 I1/I0 C3 I3 C2`
- 8: `C0 I0 C2 I1 C1/I3 C2 C2 C3 I2/T0 E0 C0 T0 C0/C1 I1 S3 I2 I0/C3 I0 I2 C3 I1`
- 9: `C0 I0 I3 I2 C2/S0 C3 C1 I3 I1/I0 C0 C1 C0 T0/T3 C3 C2 C0 C0/I0 I1 C2 E0 I3`
- 10: `T1 I3 C3 I1 I3/C1 C2 I3 I0 I0/C1 C3 I3 I3 C2/S0 I3 T3 C0 C2/I3 I3 E0 T1 C2`

## Tempo del generatore

generator 5x5 (tier c1-8), 30 seeds: average 0,4 ms (min 0,0, max 1,5), average attempts 17,2 (Editor, misurato all'ultima generazione).
