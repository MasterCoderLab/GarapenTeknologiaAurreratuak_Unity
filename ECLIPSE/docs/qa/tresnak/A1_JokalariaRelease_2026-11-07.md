# A1 — Jokalaria Release: informe (07/11/2026)

Rama `feature/eclipse-hud`, sobre `671541e0`, sin commit. Plan aprobado: revisión 2. Todo lo automático es batch; **no sustituye a la validación de Ion**.

## Cambios

- `Scriptak/Proba/EsploratzaileProba.cs`: sin `#if`; comentario corregido.
- `Scriptak/Misioak/LevelManager.cs`: sin el `#if` de `JokalariaSortu()`; marca `ECL_JOKALARIA_SORTUTA <pos> cc=<bool> debug=<bool>`.
- `Scriptak/Hud/HudKudeatzailea.cs`: sin los 5 `#if`.
- **Nuevo** `Scriptak/Proba/AutoProbaQA.cs`: entero dentro de `#if ECLIPSE_AUTOPROBA`.
- `Editor/Tresnak/EszenaEraikitzailea.HudProba.cs`: `BatchJokalariKonpilazioa` con reflexión y control negativo.
- **Nuevo** `Editor/Tresnak/EszenaEraikitzailea.ReleaseProba.cs`: `BatchReleaseProba`, dos builds y su validación.

`ECLIPSE_AUTOPROBA` solo existe en `BuildPlayerOptions.extraScriptingDefines` de la build QA: no está en `ProjectSettings` ni en Build Profiles.

## Cierre (07/11)

- **Validación manual:** Ion ejecutó la Release Linux y completó las tres misiones.
- **`activeBuildTarget`:** devuelto a `StandaloneWindows64` con `-buildTarget Win64`. Comprobado por la API con `BatchReleaseProba -eclipseBuildGabe`, sin build (`EszenaEraikitzailea_2026-11-07_042026.md`).
- **Capturas:** las 18 modificadas se restauraron desde `~/ECLIPSE_babeskopia/a1_antes_033845/kapturak` y las 18 nuevas se eliminaron; la carpeta es idéntica al respaldo. Las listas de abajo documentan lo que hubo durante las pruebas.

## Resultados

| Prueba | Tipo | Resultado |
|---|---|---|
| Compilación en Unity | — | 0 `error CS` |
| EditMode | Lógica pura | 45/45 |
| `BatchJokalariKonpilazioa` | Sin interacción | `EsploratzaileKontrola` presente, `AutoProbaQA` ausente; control negativo (DLL antigua): tipo ausente |
| `BatchMailaRobota` M1/M2/M3 | **[Simulado]** `CharacterController.Move` directo y `Erabili()` | 211 / 395 / 311 m |
| `BatchHudProba` M1/M2/M3 | **[Teclado virtual]** M, Tab, Esc, E; reflexión sobre `Mugitu` | 66 / 89 / 69 comprobaciones, 0 fallos |
| Build QA, recorrido M1 → M3 | **[Simulado]** `HelburuaBete` e `IrteeraLortu`, sin caminar | Secuencia completa, código 0, `debug=False`; `cc=True kontrola=True` en las tres |
| Build QA, control negativo | **[Simulado]** jugador destruido | `cc=False`, `ECL_AUTOPROBA_ERROR`, código 1, misión sin terminar |
| Build Release, arranque | **[Sin interacción]** | M1: `cc=True`, `debug=False`, misión iniciada, 0 errores. `-eclipseAutoProba` no tiene efecto |
| DLL Release frente a QA | Sin interacción | 60 tipos en Release; solo difieren `AutoProbaQA` y su corrutina |
| Variante del player | Sin interacción | `UnityPlayer.so` idéntico a `linux64_player_nondevelopment_mono` |
| Grafo en player frente a Editor | Sin interacción | M1 9743 = 9743; M2 11013 = 11013; M3 9626 frente a 9627 (±1 %) |
| **Release Linux jugada, M1 → M3** | **[Teclado real: Ion]** | **Correcta: Ion completó las tres misiones (07/11)** |
| Windows nativo | — | No ejecutado: corresponde a A7 |

No se ejecutó `BatchInterakzioProba`, que no estaba en la aceptación.

**Sin demostrar automáticamente:** caminar, triggers, input y shaders en las builds (`-batchmode -nographics`: render nulo y sin teclado). La Release no recorre M2 y M3 sola porque no lleva autoprobador; esas misiones se ejecutaron en la build QA.

## Orden real del log (build QA)

`ECL_AUTOPROBA_HASI`, y por misión: `ECL_JOKALARIA_SORTUTA` → `ECL_NABIGAZIOA_OK` → `ECL_MISIOA_HASI` → `ECL_AUTOPROBA_ESZENA` → `ECL_AUTOPROBA_JOKALARIA` → `ECL_MISIOA_HELBURUA` (2/4/4) → `ECL_MISIOA_ATEA_IREKITA` → `ECL_MISIOA_OK`; al final, `ECL_AUTOPROBA_AMAITUTA`. Coincide con el orden previsto a partir de `LevelManager.Start`.

Logs: `~/ECLIPSE_babeskopia/proba_release/qa/player_qa.log`, `qa/player_qa_kontrola.log` y `release/player_release.log`. Informe de la herramienta: `EszenaEraikitzailea_2026-11-07_040618.md`.

## Diferencias detectadas frente a la copia previa (`~/ECLIPSE_babeskopia/a1_antes_033845`)

- `Assets/Settings`, `ProjectSettings`, `Packages` y `Assets/_Eclipse/Eszenak` (escenas, lightmaps): **0 archivos cambiados**.
- `activeBuildTarget`: pasó de `StandaloneWindows64` a `StandaloneLinux64` durante el build; **devuelto a Windows con autorización de Ion** (ver Cierre).
- `docs/qa/kapturak`: 18 modificadas y 18 nuevas, generadas por las pruebas; **restauradas y eliminadas con autorización de Ion** (ver Cierre).

### Capturas modificadas
- `HUD_M1_amaiera_1080.png`
- `HUD_M1_amaiera_720.png`
- `HUD_M1_mapa_720.png`
- `HUD_M1_mapa_amaiera_1080.png`
- `HUD_M2_amaiera_1080.png`
- `HUD_M2_inbentarioa_1080.png`
- `HUD_M2_makurtu_abisua_1080.png`
- `HUD_M2_makurtu_abisua_720.png`
- `HUD_M2_mapa_amaiera_1080.png`
- `HUD_M3_amaiera_1080.png`
- `HUD_M3_hasiera_1080.png`
- `HUD_M3_hasiera_720.png`
- `HUD_M3_inbentarioa_1080.png`
- `HUD_M3_mapa_1080.png`
- `HUD_M3_mapa_amaiera_1080.png`
- `HUD_M3_mapa_goia_1080.png`
- `M1_uholdea.png`
- `M3_uholdea.png`

### Capturas nuevas
- `HUD_M1_mapa_amaiera_720.png`
- `HUD_M1_terminala_1080.png`
- `HUD_M1_terminala_720.png`
- `HUD_M2_amaiera_720.png`
- `HUD_M2_hasiera_720.png`
- `HUD_M2_inbentarioa_720.png`
- `HUD_M2_mapa_720.png`
- `HUD_M2_mapa_amaiera_720.png`
- `HUD_M2_mapa_goia_720.png`
- `HUD_M2_terminala_1080.png`
- `HUD_M2_terminala_720.png`
- `HUD_M3_amaiera_720.png`
- `HUD_M3_inbentarioa_720.png`
- `HUD_M3_mapa_720.png`
- `HUD_M3_mapa_amaiera_720.png`
- `HUD_M3_mapa_goia_720.png`
- `HUD_M3_terminala_1080.png`
- `HUD_M3_terminala_720.png`
