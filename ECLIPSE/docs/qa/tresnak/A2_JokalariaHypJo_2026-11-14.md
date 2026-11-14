# A2 — HypJo v2 como protagonista de ECLIPSE: informe de validación (14/11/2026)

**Estado:**
- **Comprobado en batch:** todo lo que figura en las tablas de este informe.
- **Pendiente:** tu validación jugando, con teclado y ratón reales. Hasta entonces, A2 no está cerrado y `docs/estado.md` no se ha tocado.

**Rama y Git:** `feature/eclipse-hud`, `HEAD 80d18f06`. Sin commits ni operaciones de Git.
**Copia previa y logs:** `~/ECLIPSE_babeskopia/a2_integrazioa_20261114_030650/` (copia de los `.cs` modificados, hashes de antes y después y logs de cada batch).

## 1. Qué se ha hecho

**Un solo jugador.** HypJo es la **capa visual** del jugador que ya existía (`EsploratzaileKontrola`), y `LevelManager` lo crea igual que antes. Siguen igual:
- `CharacterController`, Input System, movimiento, agacharse y salto;
- colisiones, mapa, inventario, HUD, terminales y la tecla V.

**Cómo entra en las escenas.** No se ha modificado ninguna `.unity` de las misiones. El modelo se carga por código desde `Assets/Resources/Jokalaria/PF_Jokalaria_HypJo.prefab`.

**Si falta el prefab,** el juego vuelve a la cápsula de antes. Está probado moviendo el prefab fuera de `Resources` y devolviéndolo después (mismo GUID).

**Animación** (`AC_Jokalaria`, solo capa visual, sin root motion):
- **De pie:** blend tree por velocidad: Idle 0 → Walk 0,975 → Jog 5,357 → Sprint 8,25 m/s.
  - Los umbrales son la velocidad natural de cada clip, medida en Blender. Así los pies no patinan a 3 m/s (andar) ni a 6 m/s (correr).
- **Agachado:** Crouch_Idle → Crouch_Fwd. El clip se reproduce a ×2 para casar con 1,5 m/s.
- **En el aire:** Jump_Loop.
- **Foot IK** en los estados que tocan el suelo.

**Sin clips laterales** (decisión aprobada):
- Solo el modelo gira hacia donde se mueve; la rotación del controlador no cambia.
- Para A3 ya están preparados `OrientazioModua.Kamera` y los parámetros `AbiaduraX` y `AbiaduraZ`, que servirán para apuntar, desplazarse de lado e ir marcha atrás sin rehacer nada.

**Cámara al hombro** (0,45 m a la derecha y 2,2 m atrás):
- Un único SphereCast desde la cabeza.
- Si la cámara queda a menos de 0,8 m, el modelo se oculta para no ver el interior del casco.
- En primera persona, el modelo solo proyecta sombra.
- La cámara anterior, centrada a 3 m, queda como comentario en el código y en la copia previa.

**Punto de anclaje para el arma:** `Arma_Eskuina`, en la mano derecha, preparado para A3.

## 2. Archivos

| Tipo | Ruta |
|---|---|
| Originales, **nuevos**, fuera de Git | `Assets/ThirdParty/HypJo_Cyborg/` (FBX Mixamo, 3 texturas 2K, licencia y procedencia), `Assets/ThirdParty/Quaternius_UAL/` (`UAL1_Standard.fbx`, `License.txt`, `README.txt`). SHA-256 verificados |
| Runtime, nuevos | `Assets/_Eclipse/Scriptak/Jokalaria/JokalariAnimLogika.cs` (lógica pura, `IJokalariEgoera`), `JokalariBisuala.cs` (adaptador), `Assets/_Eclipse/Scriptak/Proba/JokalariProbaAbiarazlea.cs` (solo para la escena de pruebas) |
| Runtime, **modificado** | `Assets/_Eclipse/Scriptak/Proba/EsploratzaileProba.cs`: +72/−19. Interfaz de solo lectura, la cápsula pasa a `KapsulaSortu()` sin cambios, carga del visual y cámara al hombro. **Movimiento, entrada, colisiones, agacharse y salto: sin cambios** |
| Editor, nuevos | `Assets/_Eclipse/Editor/Tresnak/JokalariBisualTresna.cs` (menú `ECLIPSE/Jokalaria/…`; vista previa, aplicar e idempotente), `EszenaEraikitzailea.JokalariProba.cs` (`BatchJokalariProba`, `BatchA2ReleaseProba`) |
| Editor, **modificado** | `EszenaEraikitzailea.Egiaztapena.cs`: +6/−4. La prueba de interacción comprobaba la cápsula `Gorputza`; ahora comprueba la visibilidad del cuerpo (la cápsula o HypJo). **Mismos criterios** |
| Tests, nuevo | `Assets/_Eclipse/Tests/EditMode/JokalariAnimLogikaTests.cs` (10 tests) |
| Generados por herramienta | `Assets/_Eclipse/Datuak/Jokalaria/` (`M_HypJo_Gorputza.mat`, `M_HypJo_Lepozapia.mat`, `AC_Jokalaria.controller`, `Proba/` con materiales y textura de cuadros), `Assets/Resources/Jokalaria/PF_Jokalaria_HypJo.prefab`, `Assets/_Eclipse/Eszenak/Proba/03_JokalariProba.unity` (escena nueva, no está en la build) |
| Documentación | `LICENSES/ThirdParty.md` (atribuciones comprobadas y cómo reconstruir desde un clon limpio), este informe, `docs/qa/kapturak/A2_Jokalaria/` |

## 3. Pruebas

| Prueba | Resultado |
|---|---|
| Herramienta `JokalariBisuala` | Vista previa sin cambios en disco → Aplicar → **segunda vista previa: 0 cambios**. Avatar válido con 22 huesos (sin dedos); 43 clips UAL |
| EditMode | **55/55** (45 anteriores + 10 nuevos) |
| Escena neutra `03_JokalariProba`, Play con teclado virtual | **54/54.** De pie, andar (3,00 m/s), correr (6,00), agachado (1,50), marcha atrás y laterales con el modelo girado −180°, −90° y +90° y el controlador sin girar. Salto de 1,00 m con estado *Airean* y aterrizaje. Tubería de 1,425 m: de pie bloquea; agachado pasa, con la cabeza a 1,19 m. Escalón de 0,3 m sin falsos *Airean*. Pasillo de 2,4 m: la cámara nunca entra en la pared. 0 errores en el log |
| Pies y altura | Reposo −0,1 cm; agachado ±0,2 cm; andando, mínimo −0,3 cm. Altura de pie 1,76 m (cápsula 1,80); agachado 1,06 m |
| M1, M2 y M3 (`Mision01_Laborategia`, `Mision02_Energia`, `Mision03_Erreaktorea`) con el flujo de `LevelManager`, sin guardar las escenas | **21/21 en cada una:** un solo jugador con HypJo, HUD activo, spawn, cámara al hombro libre, reposo y andar a 3 m/s con los pies a ±0,3 cm, agacharse, cambio de primera a tercera persona, 0 errores |
| Regresión HUD, mapa e inventario | M1 **66/66**, M2 **89/89**, M3 **69/69** (como en A1) |
| Regresión de interacción y cámara en tercera persona | 25 por misión. **13 056 vistas: 0 atraviesan paredes, 0 dentro de sólidos** (igual que en A1) |
| Regresión del robot de misión | **211 / 395 / 311 m** (contrato de A1). Cada misión carga la siguiente con el jugador |
| Compilación del jugador sin Development | `EsploratzaileKontrola` incluido, `AutoProbaQA` no |
| Vuelta a la cápsula (prefab retirado) | **9/9.** Cápsula, movimiento, agacharse y cámaras; el prefab vuelve con el mismo GUID |
| Build QA y Release (Linux, sin Development, M1→M2→M3) | **57 líneas, 0 fallos.** HypJo se carga en los dos ejecutables (`ECL_JOKALARIA_BISUALA_OK`). `AutoProbaQA` solo está en QA. **Target devuelto a `StandaloneWindows64`** en un `finally`. Las builds de A1 se conservan en `~/ECLIPSE_babeskopia/proba_release_A1_20261107/` |
| Integridad | 0 cambios en las 5 escenas existentes y sus lightmaps, `Assets/Settings`, `ProjectSettings`, `Packages` y los 5 archivos gráficos sin commit. 0 archivos cambiados en el `ThirdParty` que ya existía. `EditorBuildSettings` sin tocar (M1, M2 y M3, en ese orden) |

## 4. Incidencias y decisiones técnicas

**Corregidas durante la integración:**
- **Emisión de P3 apagada:** con `EmissiveIsBlack`, URP desactiva `_EMISSION` al reimportar, así que las capturas de P3 se hicieron sin emisión. Ahora se usa `RealtimeEmissive`, como en `EszenaPropioa`: la emisión cian se ve y no cambia ningún bake.
- **Pies flotando 5 cm:** era el `skinWidth` del `CharacterController`. El modelo visual se baja esa misma cantidad.
- **Nombres de los clips UAL** (`Armature|Idle_Loop`): se renombran en la importación.
- **Un batch que abortó por un error de compilación dejó `Temp/UnityLockfile`:** se comprobó que no había ningún proceso vivo y se quitó.

**Pendiente de tu decisión (no se ha tocado):**
- **Capturas antiguas sobrescritas:** las pruebas de regresión de HUD y robot vuelven a escribir sus capturas en `docs/qa/kapturak/`. Hay **20 PNG versionados modificados** y **19 nuevos**; en las nuevas ya aparece HypJo. Puedes quedártelas o restaurar las anteriores con Git. No las he abierto porque incluyen el kit de la Asset Store.
- **Informes intermedios:** en `docs/qa/tresnak/` hay 11 `JokalariBisuala_*` y 7 `JokalariProba_*`. Los primeros recogen fallos que ya están corregidos. Los definitivos son los últimos de cada serie, además de este informe.

## 5. Capturas y vídeos (en Fedora, con cualquier visor o reproductor)

**Carpeta:** `docs/qa/kapturak/A2_Jokalaria/`.

**`neutro_*`** (escena neutra, revisadas):
- primera persona;
- tercera persona y vista lateral de: reposo, andar, correr, marcha atrás, lateral, agachado, agachado andando, salto y bajo la tubería;
- cápsula de reserva.

**Vídeos:** `neutro_bideoa_{ibili,korrika,makur_ibili,jauzia}.mp4`, en H.264.

**`ion_M1_*`, `ion_M2_*`, `ion_M3_*`** (misiones):
- `01_lehen_pertsona`, `3p_geldirik`, `3p_ibili`, `3p_makurtuta` y `1p`.
- **No las he abierto** porque incluyen el kit de la Asset Store. Revísalas tú.

## 6. Qué tienes que probar tú

1. Abre Unity en ECLIPSE.
2. **Escena neutra:** abre `Assets/_Eclipse/Eszenak/Proba/03_JokalariProba.unity` → Play.
3. **Juego real:** abre `Mision01_Laborategia.unity` → Play.
4. Comprueba:
   - **WASD:** andar, laterales y marcha atrás. El modelo gira hacia donde se mueve.
   - **Shift:** correr.
   - **Ctrl:** agacharse y andar agachado. Pasa bajo la tubería naranja de la escena neutra y bajo `__005` en M2.
   - **Espacio:** saltar.
   - **V:** cambiar de cámara. En primera persona solo se ve la sombra del modelo.
   - **Además:** M (mapa), Tab (inventario), E (terminales) y Esc con clic.
5. **En qué fijarse:** los pies (que no patinen ni floten), la cámara al hombro en los pasillos, la braga y el casco, y lo oscuro que se ve el metal en cada sala.
6. **Lo que el batch no puede comprobar:** la tecla V, el ratón y el bloqueo del cursor; solo se pueden probar con teclado y ratón reales. Se probó lo equivalente llamando al método.

## 7. Propuesta de commit (lo haces tú cuando lo valides)

**Excluir** `Assets/Settings/*`, `ProjectSettings/GraphicsSettings.asset`, `Build Profiles/`, `.gitignore` y las capturas `HUD_*` / `M*_uholdea` hasta que decidas qué hacer con ellas.

```
ECLIPSE A2: HypJo v2 protagonista (geruza bisuala, Animator, sorbaldako kamera)

- JokalariBisuala + JokalariAnimLogika (10 test): HypJo Resources-etik, kapsula ordezko gisa
- EsploratzaileKontrola: egoera irakurtzeko interfazea eta sorbaldako kamera (mugimendua aldatu gabe)
- JokalariBisualTresna: inportazioa, materialak, AC_Jokalaria, aurrefabrikatua eta 03_JokalariProba
- Probak: EditMode 55/55, JokalariProba 54 + 3x21, HUD/robota/interakzioa A1 bezala, Release
- LICENSES/ThirdParty.md: HypJo (CC BY 4.0), Mixamo, Quaternius UAL (CC0)
```
