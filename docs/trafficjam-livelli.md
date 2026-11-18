# Traffic jam — livelli della Storia

File generato da *Resolver Master > Traffic jam > Genera livelli Storia* (non modificare a mano). Regole: `docs/trafficjam-regole.md`.
Dati del gioco: `Assets/Resources/Games/TrafficJam/trafficjam_story.json`. Seed del livello n = 3611000 + n (se quel seed non dà un livello: + 1000, + 2000...). Generazione: 103 s in parallelo. Mosse minime esatte (ricerca in ampiezza su tutte le posizioni).

| Cap. | Livelli | Griglie | Mosse minime |
|---|---|---|---|
| 1 `trafficjam.chapter1.name` | 1–9 | 4x6 → 5x7 | 2, 2, 4, 4, 5, 5, 7, 8, 8 |
| 2 `trafficjam.chapter2.name` | 10–18 | 5x7 | 4, 5, 6, 7, 8, 10, 11, 11, 12 |
| 3 `trafficjam.chapter3.name` | 19–27 | 6x8 | 8, 8, 9, 10, 12, 13, 14, 15, 17 |
| 4 `trafficjam.chapter4.name` | 28–36 | 6x8 | 10, 11, 11, 13, 14, 15, 16, 17, 19 |
| 5 `trafficjam.chapter5.name` | 37–45 | 6x8 | 10, 12, 12, 13, 15, 15, 16, 17, 19 |
| 6 `trafficjam.chapter6.name` | 46–54 | 6x8 → 7x9 | 11, 12, 14, 15, 16, 16, 17, 18, 19 |
| 7 `trafficjam.chapter7.name` | 55–63 | 7x9 | 14, 15, 16, 16, 18, 18, 19, 20, 21 |

Controllo: la difficoltà non scende mai dentro un capitolo e ogni capitolo parte più facile della fine del precedente.

## Livelli

| Livello | Cap. | Gradino | Griglia | Auto | Camion | Barriere | Auto obiettivo | Uscite laterali | Mosse minime | Posizioni | Seed | Tentativi |
|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 1 | c1-1 | 4x6 | 2 | 0 | 0 | 1 | 0 | 2 | 66 | 3611001 | 1 |
| 2 | 1 | c1-2 | 4x6 | 3 | 0 | 0 | 1 | 0 | 2 | 106 | 3611002 | 1 |
| 3 | 1 | c1-3 | 4x6 | 4 | 0 | 0 | 1 | 0 | 4 | 297 | 3611003 | 1 |
| 4 | 1 | c1-4 | 4x6 | 4 | 0 | 0 | 1 | 0 | 4 | 179 | 3611004 | 14 |
| 5 | 1 | c1-5 | 4x6 | 6 | 0 | 0 | 1 | 0 | 5 | 1602 | 3611005 | 14 |
| 6 | 1 | c1-6 | 5x7 | 7 | 0 | 0 | 1 | 0 | 5 | 7535 | 3611006 | 5 |
| 7 | 1 | c1-7 | 5x7 | 8 | 0 | 0 | 1 | 0 | 7 | 1402 | 3611007 | 1 |
| 8 | 1 | c1-8 | 5x7 | 8 | 0 | 0 | 1 | 0 | 8 | 10627 | 3611008 | 11 |
| 9 | 1 | c1-9 | 5x7 | 9 | 0 | 0 | 1 | 0 | 8 | 7464 | 3611009 | 3 |
| 10 | 2 | c2-1 | 5x7 | 4 | 1 | 0 | 1 | 0 | 4 | 4610 | 3611010 | 2 |
| 11 | 2 | c2-2 | 5x7 | 6 | 1 | 0 | 1 | 0 | 5 | 2176 | 3611011 | 62 |
| 12 | 2 | c2-3 | 5x7 | 7 | 1 | 0 | 1 | 0 | 6 | 11120 | 3611012 | 138 |
| 13 | 2 | c2-4 | 5x7 | 6 | 2 | 0 | 1 | 0 | 7 | 10064 | 3611013 | 36 |
| 14 | 2 | c2-5 | 5x7 | 6 | 2 | 0 | 1 | 0 | 8 | 10941 | 3611014 | 2 |
| 15 | 2 | c2-6 | 5x7 | 8 | 2 | 0 | 1 | 0 | 10 | 19639 | 3611015 | 1 |
| 16 | 2 | c2-7 | 5x7 | 8 | 3 | 0 | 1 | 0 | 11 | 1572 | 3611016 | 41 |
| 17 | 2 | c2-8 | 5x7 | 8 | 2 | 0 | 1 | 0 | 11 | 18188 | 3611017 | 65 |
| 18 | 2 | c2-9 | 5x7 | 9 | 2 | 0 | 1 | 0 | 12 | 603 | 3611018 | 21 |
| 19 | 3 | c3-1 | 6x8 | 10 | 2 | 0 | 1 | 0 | 8 | 18768 | 3611019 | 126 |
| 20 | 3 | c3-2 | 6x8 | 10 | 3 | 0 | 1 | 0 | 8 | 8976 | 3611020 | 214 |
| 21 | 3 | c3-3 | 6x8 | 11 | 3 | 0 | 1 | 0 | 9 | 19582 | 3611021 | 26 |
| 22 | 3 | c3-4 | 6x8 | 11 | 3 | 0 | 1 | 0 | 10 | 7632 | 3611022 | 431 |
| 23 | 3 | c3-5 | 6x8 | 11 | 3 | 0 | 1 | 0 | 12 | 7280 | 3611023 | 138 |
| 24 | 3 | c3-6 | 6x8 | 11 | 3 | 0 | 1 | 0 | 13 | 18914 | 3611024 | 520 |
| 25 | 3 | c3-7 | 6x8 | 11 | 3 | 0 | 1 | 0 | 14 | 5014 | 3611025 | 351 |
| 26 | 3 | c3-8 | 6x8 | 12 | 4 | 0 | 1 | 0 | 15 | 17894 | 3611026 | 162 |
| 27 | 3 | c3-9 | 6x8 | 13 | 4 | 0 | 1 | 0 | 17 | 12714 | 3611027 | 120 |
| 28 | 4 | c4-1 | 6x8 | 8 | 3 | 2 | 1 | 0 | 10 | 10316 | 3611028 | 84 |
| 29 | 4 | c4-2 | 6x8 | 10 | 3 | 2 | 1 | 0 | 11 | 10843 | 3611029 | 31 |
| 30 | 4 | c4-3 | 6x8 | 9 | 3 | 3 | 1 | 0 | 11 | 11085 | 3611030 | 7 |
| 31 | 4 | c4-4 | 6x8 | 11 | 3 | 3 | 1 | 0 | 13 | 11710 | 3611031 | 24 |
| 32 | 4 | c4-5 | 6x8 | 10 | 3 | 3 | 1 | 0 | 14 | 9794 | 3611032 | 94 |
| 33 | 4 | c4-6 | 6x8 | 10 | 3 | 3 | 1 | 0 | 15 | 15582 | 3611033 | 272 |
| 34 | 4 | c4-7 | 6x8 | 10 | 4 | 5 | 1 | 0 | 16 | 3014 | 3611034 | 32 |
| 35 | 4 | c4-8 | 6x8 | 10 | 3 | 4 | 1 | 0 | 17 | 10040 | 3611035 | 190 |
| 36 | 4 | c4-9 | 6x8 | 10 | 3 | 5 | 1 | 0 | 19 | 1362 | 3611036 | 106 |
| 37 | 5 | c5-1 | 6x8 | 8 | 3 | 1 | 2 | 0 | 10 | 11277 | 3611037 | 189 |
| 38 | 5 | c5-2 | 6x8 | 9 | 3 | 0 | 2 | 0 | 12 | 2449 | 3611038 | 215 |
| 39 | 5 | c5-3 | 6x8 | 9 | 3 | 2 | 2 | 0 | 12 | 14273 | 3611039 | 11 |
| 40 | 5 | c5-4 | 6x8 | 10 | 3 | 1 | 2 | 0 | 13 | 11868 | 3611040 | 1093 |
| 41 | 5 | c5-5 | 6x8 | 10 | 3 | 2 | 2 | 0 | 15 | 17938 | 3611041 | 35 |
| 42 | 5 | c5-6 | 6x8 | 10 | 3 | 2 | 2 | 0 | 15 | 14235 | 3611042 | 738 |
| 43 | 5 | c5-7 | 6x8 | 11 | 3 | 2 | 2 | 0 | 16 | 5734 | 3611043 | 223 |
| 44 | 5 | c5-8 | 6x8 | 10 | 4 | 2 | 2 | 0 | 17 | 18276 | 3611044 | 546 |
| 45 | 5 | c5-9 | 6x8 | 12 | 3 | 3 | 2 | 0 | 19 | 2409 | 3611045 | 121 |
| 46 | 6 | c6-1 | 6x8 | 8 | 3 | 1 | 1 | 1 | 11 | 12986 | 3611046 | 36 |
| 47 | 6 | c6-2 | 6x8 | 8 | 2 | 1 | 1 | 1 | 12 | 19104 | 3611047 | 207 |
| 48 | 6 | c6-3 | 6x8 | 11 | 3 | 2 | 2 | 1 | 14 | 11996 | 3611048 | 240 |
| 49 | 6 | c6-4 | 6x8 | 9 | 3 | 2 | 2 | 2 | 15 | 8931 | 3611049 | 561 |
| 50 | 6 | c6-5 | 6x8 | 10 | 4 | 0 | 2 | 1 | 16 | 13832 | 3611050 | 318 |
| 51 | 6 | c6-6 | 7x9 | 13 | 4 | 5 | 1 | 1 | 16 | 15844 | 3611051 | 634 |
| 52 | 6 | c6-7 | 7x9 | 11 | 5 | 5 | 2 | 1 | 17 | 18387 | 3611052 | 3398 |
| 53 | 6 | c6-8 | 7x9 | 11 | 6 | 5 | 2 | 2 | 18 | 15000 | 3611053 | 2898 |
| 54 | 6 | c6-9 | 7x9 | 10 | 6 | 7 | 2 | 2 | 19 | 11814 | 3612054 | 478 |
| 55 | 7 | c7-1 | 7x9 | 13 | 4 | 6 | 1 | 1 | 14 | 15724 | 3611055 | 879 |
| 56 | 7 | c7-2 | 7x9 | 10 | 5 | 6 | 2 | 0 | 15 | 18317 | 3611056 | 36 |
| 57 | 7 | c7-3 | 7x9 | 12 | 5 | 7 | 1 | 1 | 16 | 19280 | 3611057 | 44 |
| 58 | 7 | c7-4 | 7x9 | 11 | 6 | 7 | 2 | 1 | 16 | 15342 | 3611058 | 143 |
| 59 | 7 | c7-5 | 7x9 | 10 | 6 | 7 | 2 | 2 | 18 | 19280 | 3611059 | 217 |
| 60 | 7 | c7-6 | 7x9 | 13 | 5 | 6 | 1 | 1 | 18 | 3414 | 3611060 | 204 |
| 61 | 7 | c7-7 | 7x9 | 10 | 6 | 7 | 2 | 1 | 19 | 9292 | 3612061 | 3319 |
| 62 | 7 | c7-8 | 7x9 | 11 | 5 | 8 | 2 | 1 | 20 | 14833 | 3611062 | 1667 |
| 63 | 7 | c7-9 | 7x9 | 11 | 5 | 7 | 2 | 2 | 21 | 14613 | 3611063 | 970 |

## Gradini di difficoltà (TrafficJamConfig)

| Gradino | Livello | Valori |
|---|---|---|
| c1-1 | 1 | cols 4, rows 6, minMoves 1-2, cars 2, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-2 | 2 | cols 4, rows 6, minMoves 2-3, cars 3, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-3 | 3 | cols 4, rows 6, minMoves 3-4, cars 4, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-4 | 4 | cols 4, rows 6, minMoves 4-5, cars 4-5, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-5 | 5 | cols 4, rows 6, minMoves 5-6, cars 5-6, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-6 | 6 | cols 5, rows 7, minMoves 5-6, cars 6-7, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-7 | 7 | cols 5, rows 7, minMoves 6-7, cars 7-8, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-8 | 8 | cols 5, rows 7, minMoves 7-8, cars 8-9, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c1-9 | 9 | cols 5, rows 7, minMoves 8-10, cars 9-10, trucks 0, obstacles 0, targets 1, sideExits 0 |
| c2-1 | 10 | cols 5, rows 7, minMoves 4-5, cars 4-6, trucks 1, obstacles 0, targets 1, sideExits 0 |
| c2-2 | 11 | cols 5, rows 7, minMoves 5-6, cars 5-6, trucks 1, obstacles 0, targets 1, sideExits 0 |
| c2-3 | 12 | cols 5, rows 7, minMoves 6-7, cars 5-7, trucks 1-2, obstacles 0, targets 1, sideExits 0 |
| c2-4 | 13 | cols 5, rows 7, minMoves 7-8, cars 6-7, trucks 1-2, obstacles 0, targets 1, sideExits 0 |
| c2-5 | 14 | cols 5, rows 7, minMoves 8-9, cars 6-8, trucks 2, obstacles 0, targets 1, sideExits 0 |
| c2-6 | 15 | cols 5, rows 7, minMoves 9-10, cars 6-8, trucks 2, obstacles 0, targets 1, sideExits 0 |
| c2-7 | 16 | cols 5, rows 7, minMoves 10-11, cars 7-8, trucks 2-3, obstacles 0, targets 1, sideExits 0 |
| c2-8 | 17 | cols 5, rows 7, minMoves 11-12, cars 7-9, trucks 2-3, obstacles 0, targets 1, sideExits 0 |
| c2-9 | 18 | cols 5, rows 7, minMoves 12-14, cars 7-9, trucks 2-3, obstacles 0, targets 1, sideExits 0 |
| c3-1 | 19 | cols 6, rows 8, minMoves 7-8, cars 8-10, trucks 2-3, obstacles 0, targets 1, sideExits 0 |
| c3-2 | 20 | cols 6, rows 8, minMoves 8-9, cars 8-10, trucks 2-3, obstacles 0, targets 1, sideExits 0 |
| c3-3 | 21 | cols 6, rows 8, minMoves 9-10, cars 9-11, trucks 2-3, obstacles 0, targets 1, sideExits 0 |
| c3-4 | 22 | cols 6, rows 8, minMoves 10-11, cars 9-11, trucks 3, obstacles 0, targets 1, sideExits 0 |
| c3-5 | 23 | cols 6, rows 8, minMoves 11-12, cars 10-12, trucks 3, obstacles 0, targets 1, sideExits 0 |
| c3-6 | 24 | cols 6, rows 8, minMoves 12-13, cars 10-12, trucks 3-4, obstacles 0, targets 1, sideExits 0 |
| c3-7 | 25 | cols 6, rows 8, minMoves 13-14, cars 11-12, trucks 3-4, obstacles 0, targets 1, sideExits 0 |
| c3-8 | 26 | cols 6, rows 8, minMoves 14-15, cars 11-13, trucks 3-4, obstacles 0, targets 1, sideExits 0 |
| c3-9 | 27 | cols 6, rows 8, minMoves 15-17, cars 11-13, trucks 3-4, obstacles 0, targets 1, sideExits 0 |
| c4-1 | 28 | cols 6, rows 8, minMoves 9-10, cars 8-10, trucks 2-3, obstacles 2, targets 1, sideExits 0 |
| c4-2 | 29 | cols 6, rows 8, minMoves 10-11, cars 8-10, trucks 2-3, obstacles 2-3, targets 1, sideExits 0 |
| c4-3 | 30 | cols 6, rows 8, minMoves 11-12, cars 9-11, trucks 2-3, obstacles 2-3, targets 1, sideExits 0 |
| c4-4 | 31 | cols 6, rows 8, minMoves 12-13, cars 9-11, trucks 3, obstacles 3-4, targets 1, sideExits 0 |
| c4-5 | 32 | cols 6, rows 8, minMoves 13-14, cars 9-11, trucks 3, obstacles 3-4, targets 1, sideExits 0 |
| c4-6 | 33 | cols 6, rows 8, minMoves 14-15, cars 10-12, trucks 3-4, obstacles 3-4, targets 1, sideExits 0 |
| c4-7 | 34 | cols 6, rows 8, minMoves 15-16, cars 10-12, trucks 3-4, obstacles 3-5, targets 1, sideExits 0 |
| c4-8 | 35 | cols 6, rows 8, minMoves 16-17, cars 10-12, trucks 3-4, obstacles 4-5, targets 1, sideExits 0 |
| c4-9 | 36 | cols 6, rows 8, minMoves 17-19, cars 10-12, trucks 3-4, obstacles 4-5, targets 1, sideExits 0 |
| c5-1 | 37 | cols 6, rows 8, minMoves 10-11, cars 8-10, trucks 2-3, obstacles 0-1, targets 2, sideExits 0 |
| c5-2 | 38 | cols 6, rows 8, minMoves 11-12, cars 8-10, trucks 2-3, obstacles 0-1, targets 2, sideExits 0 |
| c5-3 | 39 | cols 6, rows 8, minMoves 12-13, cars 9-11, trucks 2-3, obstacles 0-2, targets 2, sideExits 0 |
| c5-4 | 40 | cols 6, rows 8, minMoves 13-14, cars 9-11, trucks 3, obstacles 0-2, targets 2, sideExits 0 |
| c5-5 | 41 | cols 6, rows 8, minMoves 14-15, cars 9-11, trucks 3, obstacles 1-2, targets 2, sideExits 0 |
| c5-6 | 42 | cols 6, rows 8, minMoves 15-16, cars 10-12, trucks 3-4, obstacles 1-2, targets 2, sideExits 0 |
| c5-7 | 43 | cols 6, rows 8, minMoves 16-17, cars 10-12, trucks 3-4, obstacles 1-3, targets 2, sideExits 0 |
| c5-8 | 44 | cols 6, rows 8, minMoves 17-18, cars 10-12, trucks 3-4, obstacles 1-3, targets 2, sideExits 0 |
| c5-9 | 45 | cols 6, rows 8, minMoves 18-20, cars 10-12, trucks 3-4, obstacles 2-3, targets 2, sideExits 0 |
| c6-1 | 46 | cols 6, rows 8, minMoves 11-12, cars 8-10, trucks 2-3, obstacles 0-1, targets 1, sideExits 1 |
| c6-2 | 47 | cols 6, rows 8, minMoves 12-13, cars 8-10, trucks 2-3, obstacles 0-1, targets 1, sideExits 1 |
| c6-3 | 48 | cols 6, rows 8, minMoves 13-14, cars 9-11, trucks 2-3, obstacles 0-2, targets 2, sideExits 1 |
| c6-4 | 49 | cols 6, rows 8, minMoves 14-15, cars 9-11, trucks 3, obstacles 0-2, targets 2, sideExits 1-2 |
| c6-5 | 50 | cols 6, rows 8, minMoves 15-16, cars 10-12, trucks 3-4, obstacles 0-2, targets 2, sideExits 1-2 |
| c6-6 | 51 | cols 7, rows 9, minMoves 15-16, cars 12-13, trucks 4-5, obstacles 4-6, targets 1, sideExits 1 |
| c6-7 | 52 | cols 7, rows 9, minMoves 16-17, cars 10-11, trucks 5-6, obstacles 5-7, targets 2, sideExits 1-2 |
| c6-8 | 53 | cols 7, rows 9, minMoves 17-18, cars 10-11, trucks 5-6, obstacles 5-7, targets 2, sideExits 1-2 |
| c6-9 | 54 | cols 7, rows 9, minMoves 18-20, cars 10-11, trucks 5-6, obstacles 6-8, targets 2, sideExits 2 |
| c7-1 | 55 | cols 7, rows 9, minMoves 13-14, cars 12-13, trucks 4-5, obstacles 5-7, targets 1, sideExits 0-1 |
| c7-2 | 56 | cols 7, rows 9, minMoves 14-15, cars 10-11, trucks 5-6, obstacles 5-7, targets 2, sideExits 0-1 |
| c7-3 | 57 | cols 7, rows 9, minMoves 15-16, cars 12-13, trucks 4-5, obstacles 5-7, targets 1, sideExits 1 |
| c7-4 | 58 | cols 7, rows 9, minMoves 16-17, cars 10-11, trucks 5-6, obstacles 6-8, targets 2, sideExits 1-2 |
| c7-5 | 59 | cols 7, rows 9, minMoves 17-18, cars 10-11, trucks 5-6, obstacles 6-8, targets 2, sideExits 0-2 |
| c7-6 | 60 | cols 7, rows 9, minMoves 18-19, cars 12-13, trucks 5, obstacles 6-8, targets 1, sideExits 1 |
| c7-7 | 61 | cols 7, rows 9, minMoves 19-20, cars 10-11, trucks 5-6, obstacles 6-8, targets 2, sideExits 1-2 |
| c7-8 | 62 | cols 7, rows 9, minMoves 20-21, cars 10-11, trucks 5-6, obstacles 6-8, targets 2, sideExits 1-2 |
| c7-9 | 63 | cols 7, rows 9, minMoves 21-23, cars 10-11, trucks 5-6, obstacles 6-8, targets 2, sideExits 2 |

## Posizioni iniziali

Formato di `TrafficLayout`: `LxA`, poi `R`/`B` auto obiettivo rossa/blu, `C` auto, `T` camion + `x.y` (casella in alto a sinistra) + `h`/`v` (orizzontale/verticale) + lato dell'uscita `U`/`R`/`D`/`L` per le auto obiettivo; `#x.y` barriera.

- 1: `4x6 R1.4vU C0.0h C0.1v`
- 2: `4x6 R3.3vU C0.4h C2.2h C2.0v`
- 3: `4x6 R0.4vU C0.1h C0.2h C2.5h C0.0h`
- 4: `4x6 R3.3vU C2.1h C1.4h C2.0h C1.0v`
- 5: `4x6 R2.4vU C2.0h C0.0v C1.3h C1.0v C0.5h C3.3v`
- 6: `5x7 R2.5vU C1.2v C3.5h C2.3h C3.4h C1.4h C0.3v C4.2v`
- 7: `5x7 R0.4vU C4.2v C4.0v C2.1v C2.0h C2.3v C0.3h C0.0h C3.5h`
- 8: `5x7 R2.4vU C0.4h C3.0h C0.2v C0.0v C2.3h C3.1v C1.0h C4.3v`
- 9: `5x7 R2.3vU C3.3h C3.5h C1.5h C4.0v C0.1h C2.2h C3.6h C1.0h C2.1h`
- 10: `5x7 R3.4vU T1.6h C1.1v C0.4v C3.1h C2.1v`
- 11: `5x7 R2.4vU T1.3v C2.3h C2.0h C0.6h C1.1h C2.2h C0.2h`
- 12: `5x7 R1.3vU T3.0v C2.3v C3.4h C3.6h C1.1h C4.2v C0.1v C1.6h`
- 13: `5x7 R2.5vU T1.4v T0.1v C2.2h C0.4v C3.6h C2.4h C2.1h C4.1v`
- 14: `5x7 R2.5vU T4.3v T0.1v C1.1h C1.4h C2.0h C1.3h C0.4v C3.1v`
- 15: `5x7 R3.1vU T0.2h T1.0h C3.4h C1.1h C2.3v C0.3h C4.5v C1.5h C4.0v C0.0v`
- 16: `5x7 R4.5vU T3.3v T1.2v T2.3v C3.1h C2.1v C0.2v C3.2h C2.6h C0.6h C1.0h C0.1h`
- 17: `5x7 R0.4vU T0.0h T2.1v C4.5v C1.5h C1.6h C3.1h C1.1v C0.3h C3.5v C4.3v`
- 18: `5x7 R4.4vU T2.0h T2.2v C0.0v C1.5h C0.3h C3.1h C1.6h C1.1h C3.4v C3.6h C0.5v`
- 19: `6x8 R4.3vU T1.2v T0.5h C0.7h C3.6v C2.3h C4.2h C4.1h C2.1h C0.2v C3.5h C0.0v C1.6h`
- 20: `6x8 R3.2vU T2.2v T2.5v T4.3v C3.0h C0.2h C5.0v C0.0v C5.4v C1.0h C0.6h C3.1h C0.3v C4.2h`
- 21: `6x8 R3.6vU T4.5v T0.0h T0.6h C3.0h C5.0v C0.1h C3.1h C2.5h C4.4h C2.3v C2.1v C0.2v C5.2v C1.4v`
- 22: `6x8 R0.6vU T1.0v T3.1h T0.3h C5.2v C4.3v C5.4v C3.5h C3.0h C2.7h C0.5h C3.2v C5.6v C2.4v C1.6v`
- 23: `6x8 R0.5vU T0.0h T3.4h T3.1v C2.1v C0.4h C4.5h C2.5h C1.5v C3.6h C1.7h C0.1h C1.2v C2.3v C3.0h`
- 24: `6x8 R5.2vU T0.4h T1.1h T3.5v C2.2h C0.6h C1.2v C0.0v C4.1h C4.7h C1.0h C0.2v C3.3v C4.6h C3.0h`
- 25: `6x8 R4.5vU T0.2v T2.5v T5.5v C3.4h C0.0v C3.0v C2.1v C5.3v C1.6v C1.0h C0.5h C0.6v C2.3v C3.2v`
- 26: `6x8 R2.4vU T0.7h T1.6h T3.0v T5.0v C1.4v C4.0v C1.2h C0.1v C5.4v C3.4v C0.0h C5.6v C0.5v C0.3v C3.7h C1.1h`
- 27: `6x8 R1.5vU T2.1v T5.1v T4.4v T3.4v C4.0v C0.2v C0.1h C1.4h C2.0h C0.0h C4.7h C3.1v C4.2v C2.7h C0.6v C2.5v C5.5v`
- 28: `6x8 R0.5vU T0.3h T1.5h T2.1h C3.3v C0.2h C0.4h C2.2h C4.5h C2.6v C1.0v C3.7h #3.0 #5.3`
- 29: `6x8 R4.5vU T3.5v T2.4v T2.1v C3.4h C3.3h C0.0v C1.0h C5.6v C5.3v C0.2h C0.3h C1.7h C1.5v #5.2 #0.6`
- 30: `6x8 R3.6vU T0.3v T0.7h T5.1v C1.4v C2.5h C1.0v C4.1v C0.0v C5.4v C1.2v C4.6h C2.2h #4.0 #0.6 #1.6`
- 31: `6x8 R3.2vU T1.3v T2.3v T3.1h C0.2h C4.2v C2.7h C1.0v C3.4h C0.0v C4.0h C2.1v C0.3v C5.6v C3.5h #5.5 #0.6 #4.6`
- 32: `6x8 R2.5vU T1.2v T3.3v T4.4v C2.2h C5.1v C0.6h C2.0h C5.4v C0.5h C4.2v C1.0v C2.1h C2.7h #5.0 #4.1 #0.2`
- 33: `6x8 R5.6vU T3.4h T1.7h T1.2v C1.1h C2.6h C3.2h C0.6v C4.0h C4.1h C4.5v C2.3v C0.5h C3.0v #0.2 #4.3 #2.5`
- 34: `6x8 R5.4vU T3.2v T2.1h T0.1v T0.4h C1.0v C2.7h C2.0h C4.0h C4.2h C4.3h C1.5h C0.6v C3.5h C4.7h #0.0 #2.2 #0.5 #2.6 #4.6`
- 35: `6x8 R1.5vU T3.0h T1.2h T0.1v C5.3v C3.3v C4.5h C0.4h C2.6h C2.5h C4.2v C4.6v C0.7h C2.7h #2.0 #4.1 #5.2 #2.3`
- 36: `6x8 R5.6vU T0.2v T3.1v T2.4h C4.0h C2.0h C2.7h C3.5v C1.3h C1.6v C1.4v C0.5v C1.0v C4.5h #0.0 #4.2 #4.3 #2.6 #0.7`
- 37: `6x8 R0.4vU B4.4vU T0.3h T5.1v T3.2v C3.6v C1.1h C1.6h C1.4v C2.4v C5.4v C1.7h C3.1h #3.5`
- 38: `6x8 R3.6vU B4.1vU T0.4h T2.1v T2.5v C0.3h C2.0h C5.6v C5.1v C0.7h C0.5h C3.4h C4.0h C5.3v`
- 39: `6x8 R0.4vU B2.6vU T3.7h T4.4v T0.0h C3.0v C1.5h C0.6h C2.4h C3.2v C1.1v C5.4v C1.3v C4.1h #4.3 #3.5`
- 40: `6x8 R0.0vU B2.6vU T2.5h T3.2h T5.3v C4.1h C0.6h C1.4v C2.4h C1.0v C2.0h C5.6v C3.6h C1.2h C2.3h #3.1`
- 41: `6x8 R1.5vU B4.2vU T2.3v T5.4v T2.7h C1.0h C1.2h C0.6v C3.4h C0.0v C5.2v C3.6h C0.4h C0.2v C3.1v #5.0 #2.6`
- 42: `6x8 R0.6vU B3.4vU T4.3v T2.3v T1.6h C1.0v C5.2v C2.1v C3.2h C2.0h C5.4v C0.5h C4.6v C4.0v C2.7h #5.1 #1.3`
- 43: `6x8 R0.6vU B2.4vU T1.0v T2.1h T3.3v C2.6h C4.5h C0.4h C5.2v C5.0v C2.7h C2.2h C1.6v C4.6v C1.3h C0.5h #4.3 #4.4`
- 44: `6x8 R0.6vU B2.4vU T5.0v T1.7h T3.4h T0.0h C1.1h C0.2h C2.2h C3.0v C0.5h C1.6h C4.5h C4.6h C4.3h C2.3h #3.6 #4.7`
- 45: `6x8 R4.3vU B1.1vU T5.2v T0.6h T2.0h C0.0v C2.1v C4.5h C0.2v C0.7h C5.6v C4.1h C1.3h C2.5h C3.2h C0.4v C3.6h #3.3 #2.4 #3.7`
- 46: `6x8 R3.1hL T4.2v T2.1v T5.1v C2.5v C2.0h C0.3h C3.5h C4.0h C5.6v C2.4h C1.0v #1.5`
- 47: `6x8 R0.5hR T1.0v T3.4h C5.2v C5.6v C1.3v C2.7h C3.5v C4.1h C4.6v C0.7h #3.2`
- 48: `6x8 R0.1vU B0.0hR T2.2h T3.4h T0.5h C2.3v C1.1v C4.5v C3.1h C5.0v C1.6v C5.6v C3.7h C1.3v C2.0v C5.2v #4.3 #3.6`
- 49: `6x8 R3.6hL B3.3vD T1.2v T0.1v T2.5v C4.0v C4.2h C3.5h C5.5v C0.5h C4.4h C2.2h C1.1h C3.7h #4.3 #1.7`
- 50: `6x8 R1.5hL B3.4vU T0.3h T1.0h T5.1v T0.4v C3.1h C5.4v C4.7h C2.2h C4.0h C3.3h C2.7h C2.6h C4.5v C0.7h`
- 51: `7x9 R1.1hL T1.6h T0.5h T4.2v T6.0v C4.7v C2.7v C0.4h C3.5h C4.6h C5.0v C3.1v C6.4v C2.2v C0.3h C4.0v C0.1v C5.4v #0.0 #5.2 #6.3 #0.6 #1.7`
- 52: `7x9 R6.3vD B1.2vU T3.0v T4.3v T3.3v T2.4v T2.1v C5.6v C1.0h C0.5h C4.2h C4.0v C0.6v C0.0v C0.2v C3.6h C5.5h C3.8h #5.0 #3.7 #4.7 #2.8 #5.8`
- 53: `7x9 R5.2hL B2.8hL T1.5h T4.3h T3.2v T0.1v T0.0h T0.6v C1.1h C6.7v C1.7v C3.0v C2.2v C6.4v C4.4v C4.7v C1.4h C2.7h C1.6h #5.1 #6.1 #1.3 #4.6 #5.7`
- 54: `7x9 R0.5hR B4.4hL T4.6h T2.2h T2.4v T3.3h T0.2v T6.3v C2.8h C6.7v C3.0v C3.4v C1.7h C5.0h C1.3h C0.6h C0.1h C6.1v #0.0 #2.0 #1.2 #5.2 #3.7 #0.8 #5.8`
- 55: `7x9 R3.2hL T0.7h T0.1v T6.2v T0.0h C4.3v C2.5v C0.6h C3.0v C6.5v C0.8h C4.8h C6.0v C3.7h C1.1h C3.4v C2.2v C4.0v #3.3 #5.3 #2.4 #5.7 #2.8 #3.8`
- 56: `7x9 R5.7vU B3.5vU T4.1v T0.4v T3.4h T1.5v T6.5v C0.0v C0.7v C3.0h C2.7h C4.5v C0.2v C2.3h C5.3h C1.8h C1.3v #2.0 #2.1 #6.1 #1.2 #2.2 #4.7`
- 57: `7x9 R2.7hR T5.0v T2.6h T2.0h T0.5v T3.2v C3.1h C5.4v C1.6v C3.5h C6.2v C5.8h C0.0h C2.8h C0.4h C4.7v C0.8h C1.5h #0.1 #6.1 #1.2 #4.2 #0.3 #6.5 #5.6`
- 58: `7x9 R0.0hR B0.4vU T6.3v T4.0v T4.7h T0.1h T3.4h T0.7h C3.8h C6.0v C2.4v C0.8h C3.6v C2.3h C0.3h C5.5v C4.3h C0.2h C5.2h #5.1 #3.2 #3.5 #1.6 #4.6 #5.8 #6.8`
- 59: `7x9 R6.4vD B3.1vD T4.1h T5.4v T2.5v T1.5v T3.7h T0.0h C3.8h C3.4h C0.7v C0.1h C3.5h C3.0h C5.3h C1.8h C0.2v C1.3v #5.0 #2.1 #1.2 #5.2 #4.3 #0.5 #0.6`
- 60: `7x9 R2.6hR T1.0h T0.3h T0.4h T4.0h T3.8h C0.5v C6.1v C1.5h C0.0v C4.2h C5.5h C4.1h C3.5h C4.3v C5.6v C6.6v C1.6v C4.6v #1.1 #2.2 #6.3 #0.7 #1.8 #6.8`
- 61: `7x9 R6.3vU B5.1hR T0.6h T4.2h T4.4v T5.3v T1.7h T2.8h C3.1v C2.4h C4.0h C5.8h C0.1v C3.3h C1.4v C2.0v C5.7h C2.2v #3.0 #0.3 #1.3 #3.5 #0.7 #4.7 #1.8`
- 62: `7x9 R5.2vU B3.3hR T0.6h T2.2h T4.8h T0.1v T3.5v C1.7v C5.0h C4.0v C6.2v C1.5h C0.7v C5.1h C4.4h C4.7h C1.2v C0.0h #2.1 #0.4 #1.4 #3.4 #0.5 #4.5 #2.7 #3.8`
- 63: `7x9 R0.3hR B5.7hL T4.3v T1.2h T4.2h T0.0h T3.5v C0.6v C1.4v C1.6v C0.1v C5.3v C3.3v C0.4v C2.1h C5.0v C3.0h C2.8h #2.4 #2.5 #6.6 #1.8 #4.8 #5.8 #6.8`

## Tempi

```
Traffic jam timing (Editor, Mono; exact minimum, no estimate)
  solver, 13 Story levels 7x9 (3414-19280 positions): average 12,4 ms, max 22,5 ms
  light bulb, first hint of a 7x9 level (table + move): average 12,8 ms, max 24,6 ms
  Challenge generator ch-13 (7x9, 1 target car(s)), 20 seeds: average 525 ms, max 2432 ms, minimum moves 10-13
  Challenge generator ch-14 (7x9, 2 target car(s)), 20 seeds: average 757 ms, max 2174 ms, minimum moves 9-12
  Challenge generator ch-15 (7x9, 2 target car(s)), 20 seeds: average 1172 ms, max 3834 ms, minimum moves 9-12
  Challenge generator ch-16 (7x9, 2 target car(s)), 20 seeds: average 1224 ms, max 4288 ms, minimum moves 10-13
```
