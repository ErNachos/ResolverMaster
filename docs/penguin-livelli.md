# Pingù — livelli della Storia

File generato da *Resolver Master > Pingù > Genera livelli Storia* (non modificare a mano). Regole: `docs/penguin-regole.md`.
Dati del gioco: `Assets/Resources/Games/Penguin/penguin_story.json`. Seed del livello n = 5101000 + n (se quel seed non dà un livello: + 1000, + 2000...). Generazione: 2 s in parallelo. Mosse minime esatte (ricerca in ampiezza su tutti gli stati).

| Cap. | Livelli | Griglie | Mosse minime |
|---|---|---|---|
| 1 `penguin.chapter1.name` | 1–9 | 4x6 → 5x7 | 1, 2, 3, 4, 4, 4, 5, 5, 7 |
| 2 `penguin.chapter2.name` | 10–18 | 5x7 | 2, 3, 4, 5, 5, 6, 6, 8, 8 |
| 3 `penguin.chapter3.name` | 19–27 | 5x7 → 6x8 | 3, 4, 5, 5, 6, 6, 7, 8, 9 |
| 4 `penguin.chapter4.name` | 28–36 | 6x8 | 4, 4, 5, 5, 7, 7, 8, 9, 11 |
| 5 `penguin.chapter5.name` | 37–45 | 6x8 | 3, 5, 5, 6, 8, 9, 10, 10, 11 |
| 6 `penguin.chapter6.name` | 46–54 | 6x8 → 7x9 | 3, 5, 5, 6, 7, 8, 8, 10, 12 |
| 7 `penguin.chapter7.name` | 55–63 | 7x9 | 7, 9, 9, 9, 10, 11, 13, 13, 15 |

Controllo: la difficoltà non scende mai dentro un capitolo e ogni capitolo parte più facile della fine del precedente.

## Livelli

| Livello | Cap. | Gradino | Griglia | Rocce | Neve | Buchi | Tratti d'acqua | Pesci | Blocchi | Pinguini | Mosse minime | Stati | Seed | Tentativi |
|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 1 | c1-1 | 4x6 | 3 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 6 | 5101001 | 1 |
| 2 | 1 | c1-2 | 4x6 | 3 | 0 | 0 | 0 | 0 | 0 | 1 | 2 | 8 | 5101002 | 1 |
| 3 | 1 | c1-3 | 4x6 | 3 | 0 | 0 | 0 | 0 | 0 | 1 | 3 | 9 | 5101003 | 7 |
| 4 | 1 | c1-4 | 4x6 | 5 | 0 | 0 | 0 | 0 | 0 | 1 | 4 | 13 | 5101004 | 2 |
| 5 | 1 | c1-5 | 4x6 | 4 | 0 | 0 | 0 | 0 | 0 | 1 | 4 | 9 | 5101005 | 9 |
| 6 | 1 | c1-6 | 5x7 | 5 | 0 | 0 | 0 | 0 | 0 | 1 | 4 | 18 | 5101006 | 5 |
| 7 | 1 | c1-7 | 5x7 | 5 | 0 | 0 | 0 | 0 | 0 | 1 | 5 | 16 | 5101007 | 27 |
| 8 | 1 | c1-8 | 5x7 | 5 | 0 | 0 | 0 | 0 | 0 | 1 | 5 | 16 | 5101008 | 2 |
| 9 | 1 | c1-9 | 5x7 | 6 | 0 | 0 | 0 | 0 | 0 | 1 | 7 | 15 | 5101009 | 23 |
| 10 | 2 | c2-1 | 5x7 | 3 | 0 | 0 | 0 | 1 | 0 | 1 | 2 | 19 | 5101010 | 2 |
| 11 | 2 | c2-2 | 5x7 | 4 | 0 | 0 | 0 | 1 | 0 | 1 | 3 | 11 | 5101011 | 4 |
| 12 | 2 | c2-3 | 5x7 | 4 | 0 | 0 | 0 | 2 | 0 | 1 | 4 | 13 | 5101012 | 8 |
| 13 | 2 | c2-4 | 5x7 | 4 | 0 | 0 | 0 | 2 | 0 | 1 | 5 | 32 | 5101013 | 6 |
| 14 | 2 | c2-5 | 5x7 | 5 | 0 | 0 | 0 | 2 | 0 | 1 | 5 | 24 | 5101014 | 21 |
| 15 | 2 | c2-6 | 5x7 | 6 | 0 | 0 | 0 | 2 | 0 | 1 | 6 | 42 | 5101015 | 15 |
| 16 | 2 | c2-7 | 5x7 | 7 | 0 | 0 | 0 | 3 | 0 | 1 | 6 | 35 | 5101016 | 6 |
| 17 | 2 | c2-8 | 5x7 | 6 | 0 | 0 | 0 | 3 | 0 | 1 | 8 | 57 | 5101017 | 1 |
| 18 | 2 | c2-9 | 5x7 | 5 | 0 | 0 | 0 | 3 | 0 | 1 | 8 | 58 | 5101018 | 15 |
| 19 | 3 | c3-1 | 5x7 | 4 | 1 | 0 | 0 | 0 | 0 | 1 | 3 | 12 | 5101019 | 94 |
| 20 | 3 | c3-2 | 5x7 | 3 | 1 | 0 | 0 | 0 | 0 | 1 | 4 | 15 | 5101020 | 11 |
| 21 | 3 | c3-3 | 5x7 | 5 | 2 | 0 | 0 | 0 | 0 | 1 | 5 | 15 | 5101021 | 20 |
| 22 | 3 | c3-4 | 6x8 | 5 | 3 | 0 | 0 | 0 | 0 | 1 | 5 | 24 | 5101022 | 86 |
| 23 | 3 | c3-5 | 6x8 | 7 | 2 | 0 | 0 | 1 | 0 | 1 | 6 | 35 | 5101023 | 55 |
| 24 | 3 | c3-6 | 6x8 | 6 | 2 | 0 | 0 | 1 | 0 | 1 | 6 | 36 | 5101024 | 11 |
| 25 | 3 | c3-7 | 6x8 | 8 | 4 | 0 | 0 | 2 | 0 | 1 | 7 | 70 | 5101025 | 9 |
| 26 | 3 | c3-8 | 6x8 | 5 | 3 | 0 | 0 | 2 | 0 | 1 | 8 | 45 | 5101026 | 27 |
| 27 | 3 | c3-9 | 6x8 | 6 | 3 | 0 | 0 | 2 | 0 | 1 | 9 | 53 | 5101027 | 20 |
| 28 | 4 | c4-1 | 6x8 | 4 | 0 | 1 | 1 | 0 | 0 | 1 | 4 | 7 | 5101028 | 55 |
| 29 | 4 | c4-2 | 6x8 | 6 | 0 | 1 | 1 | 0 | 0 | 1 | 4 | 14 | 5101029 | 10 |
| 30 | 4 | c4-3 | 6x8 | 5 | 1 | 2 | 1 | 0 | 0 | 1 | 5 | 16 | 5101030 | 65 |
| 31 | 4 | c4-4 | 6x8 | 6 | 1 | 2 | 1 | 0 | 0 | 1 | 5 | 19 | 5101031 | 198 |
| 32 | 4 | c4-5 | 6x8 | 6 | 1 | 2 | 1 | 1 | 0 | 1 | 7 | 27 | 5101032 | 321 |
| 33 | 4 | c4-6 | 6x8 | 6 | 1 | 3 | 1 | 1 | 0 | 1 | 7 | 31 | 5101033 | 109 |
| 34 | 4 | c4-7 | 6x8 | 5 | 1 | 3 | 1 | 1 | 0 | 1 | 8 | 21 | 5101034 | 103 |
| 35 | 4 | c4-8 | 6x8 | 7 | 1 | 4 | 2 | 1 | 0 | 1 | 9 | 24 | 5101035 | 105 |
| 36 | 4 | c4-9 | 6x8 | 6 | 2 | 4 | 2 | 2 | 0 | 1 | 11 | 31 | 5101036 | 825 |
| 37 | 5 | c5-1 | 6x8 | 4 | 0 | 0 | 0 | 0 | 1 | 1 | 3 | 45 | 5101037 | 18 |
| 38 | 5 | c5-2 | 6x8 | 6 | 0 | 0 | 0 | 0 | 1 | 1 | 5 | 76 | 5101038 | 223 |
| 39 | 5 | c5-3 | 6x8 | 6 | 0 | 1 | 0 | 0 | 1 | 1 | 5 | 43 | 5102039 | 3037 |
| 40 | 5 | c5-4 | 6x8 | 5 | 1 | 2 | 0 | 0 | 1 | 1 | 6 | 66 | 5101040 | 7 |
| 41 | 5 | c5-5 | 6x8 | 6 | 0 | 2 | 1 | 1 | 1 | 1 | 8 | 37 | 5101041 | 280 |
| 42 | 5 | c5-6 | 6x8 | 7 | 1 | 2 | 1 | 1 | 2 | 1 | 9 | 77 | 5101042 | 6 |
| 43 | 5 | c5-7 | 6x8 | 6 | 1 | 2 | 0 | 1 | 2 | 1 | 10 | 233 | 5101043 | 13 |
| 44 | 5 | c5-8 | 6x8 | 8 | 1 | 2 | 1 | 1 | 2 | 1 | 10 | 45 | 5101044 | 445 |
| 45 | 5 | c5-9 | 6x8 | 6 | 2 | 2 | 1 | 1 | 2 | 1 | 11 | 65 | 5101045 | 511 |
| 46 | 6 | c6-1 | 6x8 | 6 | 0 | 0 | 0 | 0 | 0 | 2 | 3 | 25 | 5101046 | 1 |
| 47 | 6 | c6-2 | 6x8 | 5 | 0 | 0 | 0 | 0 | 0 | 2 | 5 | 42 | 5101047 | 2 |
| 48 | 6 | c6-3 | 6x8 | 5 | 1 | 0 | 0 | 0 | 0 | 2 | 5 | 66 | 5101048 | 4 |
| 49 | 6 | c6-4 | 6x8 | 6 | 1 | 1 | 0 | 1 | 0 | 2 | 6 | 137 | 5101049 | 10 |
| 50 | 6 | c6-5 | 6x8 | 8 | 0 | 0 | 0 | 1 | 0 | 2 | 7 | 170 | 5101050 | 16 |
| 51 | 6 | c6-6 | 7x9 | 8 | 2 | 2 | 1 | 1 | 0 | 2 | 8 | 154 | 5101051 | 44 |
| 52 | 6 | c6-7 | 7x9 | 8 | 1 | 1 | 1 | 1 | 1 | 2 | 8 | 532 | 5101052 | 6 |
| 53 | 6 | c6-8 | 7x9 | 6 | 2 | 2 | 1 | 2 | 1 | 2 | 10 | 34 | 5101053 | 42 |
| 54 | 6 | c6-9 | 7x9 | 7 | 2 | 2 | 2 | 2 | 1 | 2 | 12 | 174 | 5101054 | 14 |
| 55 | 7 | c7-1 | 7x9 | 9 | 2 | 1 | 1 | 1 | 1 | 1 | 7 | 120 | 5101055 | 5 |
| 56 | 7 | c7-2 | 7x9 | 7 | 2 | 2 | 2 | 1 | 1 | 1 | 9 | 84 | 5101056 | 42 |
| 57 | 7 | c7-3 | 7x9 | 9 | 2 | 2 | 1 | 2 | 0 | 2 | 9 | 226 | 5101057 | 27 |
| 58 | 7 | c7-4 | 7x9 | 9 | 3 | 3 | 1 | 2 | 1 | 1 | 9 | 76 | 5101058 | 145 |
| 59 | 7 | c7-5 | 7x9 | 7 | 3 | 1 | 2 | 1 | 1 | 2 | 10 | 902 | 5101059 | 8 |
| 60 | 7 | c7-6 | 7x9 | 8 | 1 | 2 | 1 | 2 | 2 | 1 | 11 | 251 | 5101060 | 24 |
| 61 | 7 | c7-7 | 7x9 | 7 | 1 | 2 | 1 | 1 | 1 | 2 | 13 | 219 | 5101061 | 34 |
| 62 | 7 | c7-8 | 7x9 | 8 | 1 | 2 | 2 | 3 | 2 | 1 | 13 | 156 | 5101062 | 20 |
| 63 | 7 | c7-9 | 7x9 | 10 | 3 | 2 | 2 | 2 | 1 | 2 | 15 | 921 | 5101063 | 156 |

## Gradini di difficoltà (PenguinConfig)

| Gradino | Livello | Valori |
|---|---|---|
| c1-1 | 1 | cols 4, rows 6, minMoves 1-2, rocks 2-3, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-2 | 2 | cols 4, rows 6, minMoves 2-3, rocks 2-4, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-3 | 3 | cols 4, rows 6, minMoves 3, rocks 3-4, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-4 | 4 | cols 4, rows 6, minMoves 3-4, rocks 3-5, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-5 | 5 | cols 4, rows 6, minMoves 4, rocks 3-5, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-6 | 6 | cols 5, rows 7, minMoves 4-5, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-7 | 7 | cols 5, rows 7, minMoves 5, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-8 | 8 | cols 5, rows 7, minMoves 5-6, rocks 5-7, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c1-9 | 9 | cols 5, rows 7, minMoves 6-7, rocks 5-7, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c2-1 | 10 | cols 5, rows 7, minMoves 2-3, rocks 3-5, snow 0, holes 0, waterEdges 0, fish 1, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 1, useless 2 |
| c2-2 | 11 | cols 5, rows 7, minMoves 3, rocks 3-5, snow 0, holes 0, waterEdges 0, fish 1, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 1, useless 2 |
| c2-3 | 12 | cols 5, rows 7, minMoves 3-4, rocks 3-5, snow 0, holes 0, waterEdges 0, fish 2, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 1, useless 2 |
| c2-4 | 13 | cols 5, rows 7, minMoves 4-5, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 1-2, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c2-5 | 14 | cols 5, rows 7, minMoves 5, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 2, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c2-6 | 15 | cols 5, rows 7, minMoves 5-6, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 2, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c2-7 | 16 | cols 5, rows 7, minMoves 6-7, rocks 4-7, snow 0, holes 0, waterEdges 0, fish 2-3, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c2-8 | 17 | cols 5, rows 7, minMoves 7-8, rocks 5-7, snow 0, holes 0, waterEdges 0, fish 2-3, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c2-9 | 18 | cols 5, rows 7, minMoves 8-9, rocks 5-7, snow 0, holes 0, waterEdges 0, fish 3, blocks 0, penguins 1, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-1 | 19 | cols 5, rows 7, minMoves 3-4, rocks 3-5, snow 1, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-2 | 20 | cols 5, rows 7, minMoves 4, rocks 3-5, snow 1-2, holes 0, waterEdges 0, fish 0, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-3 | 21 | cols 5, rows 7, minMoves 4-5, rocks 3-5, snow 2, holes 0, waterEdges 0, fish 0-1, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-4 | 22 | cols 6, rows 8, minMoves 5-6, rocks 4-6, snow 2-3, holes 0, waterEdges 0, fish 0-1, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-5 | 23 | cols 6, rows 8, minMoves 6, rocks 4-7, snow 2-3, holes 0, waterEdges 0, fish 1, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-6 | 24 | cols 6, rows 8, minMoves 6-7, rocks 5-7, snow 2-3, holes 0, waterEdges 0, fish 1-2, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-7 | 25 | cols 6, rows 8, minMoves 7-8, rocks 5-8, snow 3-4, holes 0, waterEdges 0, fish 1-2, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-8 | 26 | cols 6, rows 8, minMoves 8-9, rocks 5-8, snow 3-4, holes 0, waterEdges 0, fish 1-2, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c3-9 | 27 | cols 6, rows 8, minMoves 9-10, rocks 6-8, snow 3-4, holes 0, waterEdges 0, fish 2, blocks 0, penguins 1, needSnow 1, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-1 | 28 | cols 6, rows 8, minMoves 3-4, rocks 4-6, snow 0, holes 1-2, waterEdges 0-1, fish 0, blocks 0, penguins 1, needSnow 0, danger 1, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-2 | 29 | cols 6, rows 8, minMoves 4-5, rocks 4-6, snow 0, holes 1-2, waterEdges 1, fish 0, blocks 0, penguins 1, needSnow 0, danger 1, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-3 | 30 | cols 6, rows 8, minMoves 5, rocks 4-6, snow 0-1, holes 2-3, waterEdges 1, fish 0-1, blocks 0, penguins 1, needSnow 0, danger 1, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-4 | 31 | cols 6, rows 8, minMoves 5-6, rocks 4-7, snow 0-1, holes 2-3, waterEdges 1-2, fish 0-1, blocks 0, penguins 1, needSnow 0, danger 1, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-5 | 32 | cols 6, rows 8, minMoves 6-7, rocks 4-7, snow 0-1, holes 2-3, waterEdges 1-2, fish 1, blocks 0, penguins 1, needSnow 0, danger 2, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-6 | 33 | cols 6, rows 8, minMoves 7-8, rocks 5-7, snow 0-2, holes 2-4, waterEdges 1-2, fish 1-2, blocks 0, penguins 1, needSnow 0, danger 2, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-7 | 34 | cols 6, rows 8, minMoves 8-9, rocks 5-8, snow 0-2, holes 3-4, waterEdges 1-2, fish 1-2, blocks 0, penguins 1, needSnow 0, danger 2, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-8 | 35 | cols 6, rows 8, minMoves 9-10, rocks 5-8, snow 1-2, holes 3-4, waterEdges 2, fish 1-2, blocks 0, penguins 1, needSnow 0, danger 2, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c4-9 | 36 | cols 6, rows 8, minMoves 10-11, rocks 5-8, snow 1-2, holes 3-4, waterEdges 2-3, fish 2, blocks 0, penguins 1, needSnow 0, danger 2, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c5-1 | 37 | cols 6, rows 8, minMoves 3-4, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 0, blocks 1, penguins 1, needSnow 0, danger 0, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c5-2 | 38 | cols 6, rows 8, minMoves 4-5, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 0, blocks 1, penguins 1, needSnow 0, danger 0, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c5-3 | 39 | cols 6, rows 8, minMoves 5-6, rocks 4-6, snow 0, holes 1, waterEdges 0, fish 0, blocks 1, penguins 1, needSnow 0, danger 0, needPush 1, needFill 1, fishOnPath 0, useless 2 |
| c5-4 | 40 | cols 6, rows 8, minMoves 6-7, rocks 4-7, snow 0-1, holes 1-2, waterEdges 0-1, fish 0-1, blocks 1, penguins 1, needSnow 0, danger 0, needPush 1, needFill 1, fishOnPath 0, useless 2 |
| c5-5 | 41 | cols 6, rows 8, minMoves 7-8, rocks 4-7, snow 0-1, holes 1-2, waterEdges 0-1, fish 0-1, blocks 1, penguins 1, needSnow 0, danger 0, needPush 1, needFill 1, fishOnPath 0, useless 2 |
| c5-6 | 42 | cols 6, rows 8, minMoves 8-9, rocks 5-7, snow 0-1, holes 1-2, waterEdges 0-1, fish 1, blocks 2, penguins 1, needSnow 0, danger 0, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c5-7 | 43 | cols 6, rows 8, minMoves 9-10, rocks 5-8, snow 0-2, holes 1-2, waterEdges 0-1, fish 1-2, blocks 2, penguins 1, needSnow 0, danger 0, needPush 1, needFill 1, fishOnPath 0, useless 2 |
| c5-8 | 44 | cols 6, rows 8, minMoves 10-11, rocks 5-8, snow 0-2, holes 1-3, waterEdges 1, fish 1-2, blocks 2, penguins 1, needSnow 0, danger 0, needPush 1, needFill 1, fishOnPath 0, useless 2 |
| c5-9 | 45 | cols 6, rows 8, minMoves 11-12, rocks 5-8, snow 0-2, holes 2-3, waterEdges 1-2, fish 1-2, blocks 2, penguins 1, needSnow 0, danger 0, needPush 1, needFill 1, fishOnPath 0, useless 2 |
| c6-1 | 46 | cols 6, rows 8, minMoves 3-4, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-2 | 47 | cols 6, rows 8, minMoves 4-5, rocks 4-6, snow 0, holes 0, waterEdges 0, fish 0, blocks 0, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-3 | 48 | cols 6, rows 8, minMoves 5-6, rocks 4-7, snow 0-1, holes 0, waterEdges 0, fish 0-1, blocks 0, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-4 | 49 | cols 6, rows 8, minMoves 6-7, rocks 5-7, snow 0-1, holes 0-1, waterEdges 0-1, fish 0-1, blocks 0, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-5 | 50 | cols 6, rows 8, minMoves 7, rocks 5-8, snow 0-1, holes 0-1, waterEdges 0-1, fish 1, blocks 0, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-6 | 51 | cols 7, rows 9, minMoves 7-8, rocks 6-9, snow 0-2, holes 0-2, waterEdges 0-1, fish 1-2, blocks 0, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-7 | 52 | cols 7, rows 9, minMoves 8-9, rocks 6-9, snow 0-2, holes 0-2, waterEdges 1, fish 1-2, blocks 1, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-8 | 53 | cols 7, rows 9, minMoves 9-10, rocks 6-9, snow 1-2, holes 1-2, waterEdges 1, fish 1-2, blocks 1, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c6-9 | 54 | cols 7, rows 9, minMoves 10-12, rocks 6-9, snow 1-2, holes 1-2, waterEdges 1-2, fish 2, blocks 1, penguins 2, needSnow 0, danger 0, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c7-1 | 55 | cols 7, rows 9, minMoves 7-8, rocks 6-9, snow 1-2, holes 1-2, waterEdges 1, fish 1-2, blocks 1, penguins 1, needSnow 0, danger 1, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c7-2 | 56 | cols 7, rows 9, minMoves 8-9, rocks 6-9, snow 1-2, holes 1-2, waterEdges 1-2, fish 1-2, blocks 1, penguins 1, needSnow 0, danger 1, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c7-3 | 57 | cols 7, rows 9, minMoves 9, rocks 6-9, snow 1-2, holes 1-2, waterEdges 1, fish 1-2, blocks 0, penguins 2, needSnow 0, danger 1, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c7-4 | 58 | cols 7, rows 9, minMoves 9-10, rocks 6-9, snow 1-3, holes 1-3, waterEdges 1-2, fish 1-2, blocks 1, penguins 1, needSnow 0, danger 1, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c7-5 | 59 | cols 7, rows 9, minMoves 10-11, rocks 6-10, snow 1-3, holes 1-3, waterEdges 1-2, fish 1-2, blocks 1, penguins 2, needSnow 0, danger 1, needPush 0, needFill 0, fishOnPath 0, useless 2 |
| c7-6 | 60 | cols 7, rows 9, minMoves 11-12, rocks 6-10, snow 1-3, holes 1-3, waterEdges 1-2, fish 2, blocks 2, penguins 1, needSnow 0, danger 1, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c7-7 | 61 | cols 7, rows 9, minMoves 12-13, rocks 6-10, snow 1-3, holes 1-3, waterEdges 1-2, fish 1-2, blocks 1, penguins 2, needSnow 0, danger 1, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c7-8 | 62 | cols 7, rows 9, minMoves 13-14, rocks 6-10, snow 1-3, holes 2-3, waterEdges 1-2, fish 2-3, blocks 2, penguins 1, needSnow 0, danger 1, needPush 1, needFill 0, fishOnPath 0, useless 2 |
| c7-9 | 63 | cols 7, rows 9, minMoves 14-16, rocks 6-10, snow 1-3, holes 2-3, waterEdges 1-2, fish 2-3, blocks 1, penguins 2, needSnow 0, danger 1, needPush 1, needFill 0, fishOnPath 0, useless 2 |

## Posizioni iniziali

Formato di `PenguinLayout`: `LxA:righe[:bordo]`; `.` ghiaccio, `#` roccia, `s` neve, `w` acqua, `f` pesce, `c` blocco di ghiaccio, `P`/`Q` pinguini, `I`/`J` i loro igloo; bordo (se c'è acqua): un carattere per lato di casella, `m` muro di neve, `w` acqua (in alto da sinistra, a destra dall'alto, in basso da sinistra, a sinistra dall'alto).

- 1: `4x6:#.#./...I/..../.#../...P/....`
- 2: `4x6:..../...#/..P./..#./.#../I...`
- 3: `4x6:..../..../..P./..#./.#.#/..I.`
- 4: `4x6:.P../..#./...#/#.I#/..#./....`
- 5: `4x6:.P../..#./.#../#..#/..../..I.`
- 6: `5x7:I#.../...../...../#...P/#..../..##./.....`
- 7: `5x7:...../...../#..../...#I/.#.../P.#../...#.`
- 8: `5x7:..##./...../...I./#..../.#..P/....#/.....`
- 9: `5x7:.P.../...../.#.../...##/....I/.#.../.#..#`
- 10: `5x7:..##./..P../....f/..#../...../...../....I`
- 11: `5x7:#..../#..I./...f./...../...../#.P../....#`
- 12: `5x7:...I./#..f#/...../...../P..../.#..f/..#..`
- 13: `5x7:...f./..#../...../f...#/...P./.I#../..#..`
- 14: `5x7:...../...#./..#../...../.#.#./#f.../I..Pf`
- 15: `5x7:#...P/...../....#/#.#.f/#..#./...../.If..`
- 16: `5x7:..ff#/I###./.#.../...../...#./....P/f.#..`
- 17: `5x7:...../##.#./#..../...If/#P.../..ff./..#..`
- 18: `5x7:....#/f.f../....P/#.f../..##./...../...#I`
- 19: `5x7:P.#.#/..#../...../s..../...../....I/..#..`
- 20: `5x7:...../...../...../.#.P#/..#../sI.../.....`
- 21: `5x7:...../...../.#.#./#..../.sI../..#.P/..#s.`
- 22: `6x8:.ss#../....#./...#../....../....s./P#..I./....../..#...`
- 23: `6x8:.##.../....#./..#I../...#../....../#..f.s/..P..s/.#....`
- 24: `6x8:....../....../..#..P/ss...#/.I..#./....#./....../.f.##.`
- 25: `6x8:..s.../I..fss/s..#../#.P.../..#.#./.#..../##.#../....f.`
- 26: `6x8:#.P.../s.#.../#...../....#./....../..f..s/.f..#./....sI`
- 27: `6x8:s....#/.#...f/.f..s./s...../....#I/.#..../....../P..#.#`
- 28: `6x8:.P..../....../....../.....w/....../#.#.../.#...I/.#....:mmmmmmmmmmmmmmmmmmwwmmmmmmmm`
- 29: `6x8:...w#./.P...#/..#.../....../....#./....../..#.I./.....#:mmmmmmmmmmmmmmmwwwmmmmmmmmmm`
- 30: `6x8:#.#.../....s./....#I/...#../....w./....../...P../...#.w:mmmmmmmmmmwwwmmmmmmmmmmmmmmm`
- 31: `6x8:#....#/....#./#...../...P../.w..../w.I..#/....../...#s.:mmmmmmmmmmmmmmmmmmmmmmmmwwmm`
- 32: `6x8:...#../.....#/w...#./..I#../fs..../#..P../...w../....#.:mmmmmmmmmmmmmmmmmwwwmmmmmmmm`
- 33: `6x8:..#..#/..#w.#/.w..../..Pf../.s...#/..w.I./....../..#...:mmmmmmmmmmmmmmmmmmmmmmmmmmmw`
- 34: `6x8:.Is.../.P..w#/..##../....../...f../.....#/...w../w..#..:mmmmmmmmmmmmmmmmwwwmmmmmmmmm`
- 35: `6x8:##.#.s/..#.../I...../#...ww/..#w../....P./....../...f#w:mmwwwmmmmmmmmmmmmmmmmmmwmmmm`
- 36: `6x8:w.wP../#.I.../.....#/w...../w#.#../f..s../.s.#../#..f..:mmmwwwmmmmmmmmmmmmmmmwmmmmmm`
- 37: `6x8:....../...I../....c./....P./.#.#../....../....../...#.#`
- 38: `6x8:.#..../....../..c.../.I..../...P../#.#.../....##/.....#`
- 39: `6x8:....../.#.#../..Pc../....../#...#./....../.....w/.##..I`
- 40: `6x8:....../....#./.I..../.##ww./....c./...P../...#../.s...#`
- 41: `6x8:...#../....../..I#../.c...#/w..P.#/f...../.w...#/...#..:mmmmmmmmmmwwwmmmmmmmmmmmmmmm`
- 42: `6x8:.s...#/I..#../..cw#./#..w#./....../....../.#..../#P.fc.:mmmmmmmmmmmmmmmmmmmmmmmmwwwm`
- 43: `6x8:..c.../c....w/.P#.#./s.I.##/w...../....../.#..../.f..#.`
- 44: `6x8:I#..#./....../..P.#./.#..../...c../.##sw./w..c../#..f.#:mmwwwmmmmmmmmmmmmmmmmmmmmmmm`
- 45: `6x8:#...../#...##/....../.w..../P..Isf/...cw./c.s.../.#...#:mmmmmmmmmmwwwmmmmmmmmmmmmmmm`
- 46: `6x8:....../#....P/#.J..Q/.#..../.#.#../.....I/....../.....#`
- 47: `6x8:....../.....J/....I./....Q./.....P/...#.#/.#..../##....`
- 48: `6x8:....##/.P.J#./....../....../.....#/....../...s../.QI#..`
- 49: `6x8:...#../#...../#..P../....../..#.../.#..J./.I.f.s/...#wQ`
- 50: `6x8:.#.#../f...../....#./..P.Q#/.#J.../....../.#..#./.I.#..`
- 51: `7x9:.f...##/s....../.P...#./#...#.I/#....../Q.#Jw.s/......w/......./.#.....:mmmmmmmmmmmmmmmmmmmmwwmmmmmmmmmm`
- 52: `7x9:Q..s.../cI...../..J..../......P/......./#.wf.##/.#...../.....##/#.....#:mmmwwmmmmmmmmmmmmmmmmmmmmmmmmmmm`
- 53: `7x9:...w.../......f/.#.f.Q./......./..P#.../#.I#..s/......#/..w.#../...s.cJ:mmmmmmmmmmmmwwmmmmmmmmmmmmmmmmmm`
- 54: `7x9:.#.cfs#/......./..#.f../.#Q.J../P....../wIw..../#....#./.s...../#......:mmmmmmmmmmmmmmmmmwmmmmmmmmmmmwww`
- 55: `7x9:......#/..P..../.#...../...c###/..I..../...s..#/#....f#/......./.sw.#..:mmmmmmmmmmmmmmmmmwmmmmmmmmmmmmmm`
- 56: `7x9:#....s./......./.#.w.#./#I.##../.f.c.../..w#.../....s../......./...P...:mmmmmmmmmmmmmmmmmmmmwmmmmmwwwmmm`
- 57: `7x9:.##wI../...Q.../...#..s/f..#.../.#..P../.....J./.f#..../......./##w#.s.:mmmmmmmmmmwwmmmmmmmmmmmmmmmmmmmm`
- 58: `7x9:#.P..#./w#.#.../s....../##...../.#..s../..s.w.#/.....#./.Icffw./.......:mmmmmmmmmmwwwmmmmmmmmmmmmmmmmmmm`
- 59: `7x9:s....s./......I/....##P/...#.w./...J.../......./..sc.#./#.f...#/.Q..#..:mmmmmmmmwwmmmmmmmmmmmmmmmmmmwwmm`
- 60: `7x9:...c.../..f.#Pw/......./...####/c#...../.#...f./...w.../......./..I.#.s:mmmmmmmmmwwwmmmmmmmmmmmmmmmmmmmm`
- 61: `7x9:......./....#.c/QP...f./......./.....#./ww.#.../..#..#./.#.J..#/I.s....:mmmmmmmmmmmmmmmmmmmmmmmmmmmmmwww`
- 62: `7x9:...#f../.....#./s....#w/w.....I/.#..c.c/..#..../....#../..#P#f./.f.....:mmmmmmmmmmmwwmmmmmmmmmmwwwmmmmmm`
- 63: `7x9:..sJ##./#wI..../...#.../.#..sw./sQ..#../..#fP../#.#.#../...c.../.....f.:mmmmmmmmmmwwwmmmmmmmmwwmmmmmmmmm`

## Tempi

```
Pingù timing (Editor, Mono; exact minimum, no estimate)
  solver (table of every state), 13 Story levels 7x9 (34-921 states): average 0,4 ms, max 2,0 ms
  "You are stuck" + light bulb with the table: average 0,1 µs, max 1,0 µs
  Challenge generator ch-13 (7x9, 2 penguin(s), blocks 0), 20 seeds: average 6 ms, max 27 ms, minimum moves 7-10
  Challenge generator ch-14 (7x9, 1 penguin(s), blocks 1), 20 seeds: average 5 ms, max 12 ms, minimum moves 8-11
  Challenge generator ch-15 (7x9, 2 penguin(s), blocks 1), 20 seeds: average 7 ms, max 21 ms, minimum moves 8-11
  Challenge generator ch-16 (7x9, 2 penguin(s), blocks 1), 20 seeds: average 6 ms, max 26 ms, minimum moves 9-12
```
