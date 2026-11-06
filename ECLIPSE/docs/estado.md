# ECLIPSE — Estado del trabajo

> Lo actualiza el agente al cerrar cada bloque de trabajo y lo revisa Ion. Lo más reciente, arriba.

## Ahora

- **Fase:** 3 — Construcción de los tres niveles (`Mision01_Laborategia`, `Mision02_Energia`, `Mision03_Erreaktorea`)
- **Rama:** `feature/eclipse`
- **Kits:** `Sci Fi Modular Pack` (MagixBox) y `Sci-Fi Old rusty props PBR` (KMProd), en `Assets/ThirdParty/`, convertidos a URP
- **Siguiente paso:** Ion recorre las tres misiones en Play (explorador + E en terminales) y valida proporciones, ambiente y FPS. Pendiente de decisión: añadir las escenas a Build Profiles (`ProjectSettings`, requiere autorización).

## Hecho

- 06/11 — **Comprobaciones pendientes de la fase 3 (batch; informes en `docs/qa/tresnak/`, herramienta `EszenaEraikitzailea.Egiaztapena.cs`):**
  - **Occlusion culling:** prueba controlada, misma vista con y sin oclusión, comparada píxel a píxel, solo desde puntos transitables y con cámara en 1.ª y 3.ª persona. Antes: 624 de 1184 vistas en M3 perdían geometría (hasta 8746 px). Causa: piezas finas marcadas como oclusores. Corregido (solo paredes y cascos ocluyen; suelo, pared y techo de sala se dibujan siempre) y recalculado. Ahora: 6 de 4112 vistas, 112 px como máximo. Desde dentro de un sólido sigue ocultando; el jugador no puede estar ahí.
  - **Tiempos:** los 17 ms de `Bobinak` y `AzkenBorroka` no se reproducen: medianas 4,7 y 5,2 ms en 25 pasadas. Hay picos de 40–80 ms repartidos al azar por cualquier vista, no ligados a la geometría; causa sin determinar (la prueba reserva memoria en cada pasada). Peor mediana con oclusión: 9,0 ms. Es tiempo en batch a 1280×720 con lectura de píxeles: **no equivale a FPS**. La oclusión ayuda en vistas cargadas (M1 `Esnatzea` 10,5→6,8; M3 `Laberintoa` 8,2→4,6) y apenas en otras.
  - **Interacción (Play en batch, teclado virtual de Input System):** tecla E real: no actúa lejos ni en terminal de otra fase; sí en el activo, y cambia el indicador. Trigger de terminales y datu-nukleoak correctos. Con `Mugitu`, `Begiratu` y `KameraModuaEzarri` invocados directamente: W 3,00 m/s, Shift+W 6,00, S/A/D correctos, pitch ±85°, no atraviesa paredes. Cámara en 3.ª persona: 13.056 vistas, 0 atraviesan pared.
  - **Herramientas:** 13/13 en las tres misiones: Aurrebista no ensucia la escena ni crea objetos y detecta cambios sin corregirlos; Aplikatu corrige y respeta objetos ajenos; 0 cambios en la segunda ejecución; un Undo revierte y Redo restaura (`Undo.PerformUndo` por código).
  - **Corregido en herramientas:** `LevelManager.Konfiguratu` (null frente a cadena vacía daba un cambio fantasma en M3); `ArgiztapenPreseta` ya no devuelve pasillos y vidrios al lightmap; Aurrebista cuenta también las luces.
  - **Corregido en escenas:** 13 pantallas estaban dentro de las paredes (M2 9, M3 4), y las pantallas salían quemadas. Se movieron solo esas piezas con Aplikatu y se bajó la emisión (el bake no se tocó: las pantallas no contribuyeron al bake actual como se ven ahora). Hashes de `01` y `02` intactos.
- 06/11 — **Checkpoint D: iluminación.** Salas horneadas (`UnityComputeGPU`, 16 texel/m, `ECLIPSE_Mailak.lighting`): M1 3 lightmaps/574 s, M2 3/676 s, M3 2/502 s. Pasillos fuera del lightmap: Light Probes y luces Realtime sin sombras (M1 26, M2 28, M3 33 en total, con pulsos y terminales). Reflection Probes por sala y pasillo. Occlusion culling calculado (726–870 KB). Datos: 94–115 MB por misión.
- 06/11 — **Pruebas sobre el estado final (batch, no validación de Ion):** inundación 0 fallos en las tres; robot en Play: M1 211 m/2 terminales, carga M2; M2 394 m/4, carga M3; M3 311 m/4, fin de misión. Hesias abiertos solo al completar la fase. Los 9 tests EditMode pasaron antes del checkpoint D y `MisioLogika` no ha cambiado desde entonces. Hashes de `01_Instalazioa` y `02_PrototipoDiseinua` intactos.
- 06/11 — **Checkpoints A, B y C: tres escenas de misión** con `EszenaEraikitzailea.Mailak.cs` + `.MailaDatuak.cs` + `.MailaProba.cs` (menú `ECLIPSE/Mailak/…`; batch `BatchMailaEraiki`, `BatchMailaProbatu`, `BatchMailaRobota` con `-eclipseMaila 1|2|3`). Laborategia: 17 salas, 23 pasillos, 4,38 M tri. Energia: 17 salas, 23 pasillos, pasarela a 4,6 m, 4,70 M tri. Erreaktorea: 14 salas, 17 pasillos, reactor central y plataforma a 3 m, 3,71 M tri. El prototipo de 11 salas costaba 8,45 M: cada `Profile1` usa dos `Floor1` en vez de sus cuatro baldosas de 88.000 triángulos, salas grandes con suelo propio y sin `Box3`.
- 06/11 — **Sistema de misión mínimo** en `Scriptak/Misioak/` (`MisioLogika` pura con 9 tests EditMode, `LevelManager`, `MisioTerminala`, `BlokeoAtea`, `MisioIrteera`, `MisioBilduma`, `MailaMarka`) y `GameEvents`. El `LevelManager` crea el explorador en `Spawn_Jokalaria`. El HUD es temporal (OnGUI).
- 06/11 — **Comprobado automáticamente (no es validación de Ion):** inundación con cápsula (todo lo pisable desde el spawn: 0 huecos, 0 fugas, todas las salas, terminales y marcas alcanzables, salida inalcanzable hasta completar fases); robot en **Play real** con el `CharacterController` del explorador: M1 211 m/2 terminales, M2 394 m/4 terminales (escalera y pasarela), M3 311 m/4 terminales; hesia abierto e irteera; M1 carga M2 y M2 carga M3. `01_Instalazioa` y `02_PrototipoDiseinua` con hash intacto. Copias en `~/ECLIPSE_babeskopia/fase3_*`.
- 06/11 — **Prototipo `02_PrototipoDiseinua` construido** con `EszenaEraikitzailea.Prototipoa.cs` (menú `ECLIPSE/Prototipoa/`, entradas `BatchPrototipoa` y `BatchPrototipoProba`): rejilla de 3,75 m, 11 salas (cruces 1×1 en L, T y +), 14 pasillos, 3 bucles, callejón con puerta y caja, señales que indican el destino, color por zona y tramos oscuros. **Comprobado en batch:** cápsula de 1,80 m en todos los pasillos (6561 pasos, 0 choques, 0 sin suelo), 0 fugas de rayos, 18 props con colisión, reaplicar solo cambia lo modificado, hash de `01_Instalazioa` intacto (`8d9019c398fc0c7a`). Capturas `docs/qa/kapturak/P_*.png`. **Pendiente:** Ion en Play (explorador real, FPS, proporciones); Ctrl+Z no probado en el Editor.
- 06/11 — **Rendimiento del prototipo:** 8,45 M de triángulos y 1878 renderers en total, 36 luces sin sombras; 4–13 ms por vista a 1280×720 (batch, con lectura de píxeles; no equivale a FPS en Play). `Profile1` ≈108 000 triángulos por sección y `Floor1` ≈10 000 por lauza suman el 93 %. Hasta 5,8 M en el campo de visión: no hay occlusion culling. Hay que resolverlo antes de ampliar.
- 06/11 — Explorador temporal `Assets/_Eclipse/Scriptak/Proba/EsploratzaileProba.cs` (primer código de `Eclipse.Runtime`): se crea solo en Play en `01_Instalazioa`, fuera de builds finales. **Comprobado:** compila con Roslyn contra Unity e Input System. **Pendiente:** compilar en el Editor y probarlo en Play.
- 06/11 — **Bake real en la escena:** `UnityComputeGPU`, 32 texel/m, 4 lightmaps, 9 cubemaps, 8 Reflection Probes, 841 Light Probes; UV2 en 25 modelos con autorización de Ion. Piezas propias (núcleo, señales, tileable, trim sheet), luces por sector y skybox. **Comprobado:** cápsula 0 choques, salas cerradas, idempotencia de `EszenaPropioa`, `ArgiztapenPreseta` y `EszenaEraikitzailea`. **Pendiente:** validación visual y en Play.
- 06/11 — Un batch quedó colgado en `Camera.Render()` antes de hornear (con `-gfx-ring-buffer-size 1024` y 79 mapas de sombra). Finalizado con SIGKILL; datos intactos frente a la copia. Corregido: sin esa opción, capturas sin sombras de las Point del kit y límite de tiempo por proceso.
- 06/11 — Materiales del profesor: **no disponibles en este equipo** (`docs/temario_matrizea.md`, sección 1).
- 05/11 — **`01_Instalazioa` construida con los prefabs reales en batchmode** (S1 con techo y props; S2 y S3 en blockout). **Comprobado en Unity:** 53 prefabs medidos; segunda ejecución con 0 cambios; cápsula de 1,8 m de extremo a extremo de cada sector sin talkas (606, 507 y 383 pasos), suelo en todo el recorrido, 0 fugas en 32 direcciones por sala; 674 prefabs conectados; 0 materiales erróneos. **Pendiente:** validación de Ion en Play.
- 05/11 — Rediseño de `EszenaEraikitzailea` con las medidas reales: pasillos con `Profile1`/`ProfileDoor`/`ProfileEnd`, uniones con `WallIntersection`, salas con `Wall12` a dos alturas (5 m), techo con `TopWall4`, colliders generados en `Kolisioak/`. Nueva `EbidentziaKapturak` (solo lectura: capturas y verificación física).
- 05/11 — `EszenaEraikitzailea`: marca `ECL_ESZENAERAIKITZAILEA_HASI` al arrancar cada orden, menú único `Dena eraiki`, entrada `BatchDenaEraiki`; `DoorFrame` con forma inesperada ya no bloquea. `Informea`: los informes del mismo segundo ya no se sobrescriben. **Comprobado:** compila con el Roslyn de Unity y pasa una simulación del código real contra un doble de la API de Unity (7 kits sintéticos, 2509 comprobaciones, 0 fallos; el doble está en el scratchpad, no en el repo). **Pendiente:** ejecutar en Unity; la simulación no da medidas reales del kit.
- 05/11 — Diagnóstico de Neurketa: Unity no estaba colgado y la DLL de las 21:01 contiene el menú, pero no hay rastro de ejecución en `Logs/Editor.log` ni informe. La causa de que la orden no llegara sigue sin conocerse; ahora la marca `_HASI` lo distingue.
- 05/11 — Escrita `EszenaEraikitzailea` (menú `ECLIPSE/Eszena/`): Neurketa, Egitura, Sektorea1 eraiki y Blockout S2-S3. **Comprobado:** compila con el Roslyn de Unity fuera del editor. **Hipótesis:** el papel de cada prefab y qué cara de las paredes mira hacia dentro (solo se han usado nombres de archivo). **Pendiente:** ejecutarla en Unity; hasta entonces no está hecha.
- 05/11 — Escena `01_Instalazioa` creada por Ion (Main Camera + Directional Light). `ArgiztapenPreseta` y `ProbeJartzailea` compilan en Unity (0 `error CS` en `Logs/Editor.log`, commit `5cc9fc6e`), aún sin probar.
- 05/11 — Escritas `ArgiztapenPreseta` y `ProbeJartzailea` con el ensamblado `Eclipse.Editor` (`Assets/_Eclipse/Editor/`). **Comprobado:** compilan con el Roslyn de Unity fuera del editor. **Pendiente:** compilar dentro de Unity y probarlas; hasta entonces no están hechas.
- 05/11 — Copia de los 37 `.meta` de modelos del kit en `~/ECLIPSE_babeskopia/ThirdParty_meta_2026-11-05.tgz`, antes de tocar UV2.
- 05/11 — `AGENTS.md`: el lockfile que hay que comprobar es `Temp/UnityLockfile`.
- 05/11 — Proyecto Unity 6000.6.0f1 URP creado en `ECLIPSE/`; reglas (`AGENTS.md`, `CLAUDE.md`, `.claude/settings.json`, `.gitignore`, `.gitattributes`) instaladas.

## Pendiente (6/11)

- [x] Prueba del kit y elección
- [ ] Probar las herramientas en Unity:
  - `Logs/Editor.log` sin `error CS`; menú `ECLIPSE/` visible
  - Con `Sektorea1` y una `Gela_Proba`: Aurrebista (la escena no queda sucia) → Aplikatu → Ctrl+Z (deshace todo) → Aplikatu → Aplikatu otra vez ("0 aldaketa")
  - Cambiar el tamaño del BoxCollider y reejecutar `ProbeJartzailea`: se actualiza sin duplicar
  - UV2: Utzi (nada cambia), luego Aktibatu UV2
  - Bake pequeño: el probe refleja geometría y no hay artefactos de UV
- [ ] Probar `EszenaEraikitzailea`:
  - Neurketa: confirmar con el informe qué es cada pieza, y si tienen collider
  - Egitura → Sektorea1 eraiki → Blockout S2-S3, cada uno con Aurrebista y Aplikatu; Ctrl+Z deshace todo; la segunda ejecución da "0 aldaketa"
  - Paredes con la cara correcta hacia dentro; sin huecos; la cápsula pasa por las puertas
- [ ] Sector 1 con calidad de entrega; sectores 2 y 3 con distribución, coberturas y marcadores
- [x] Lightmap (Compute Light Baker en la NVIDIA), Reflection Probes con Box Projection, Light Probes (con defecto de manchas abierto)
- [x] Luces Baked, Mixed y Realtime; emisivos
- [ ] Post-procesado
- [x] Capturas de evidencia (con y sin bake, con y sin probe)
- [ ] Memoria
- [ ] Commit y etiqueta `entrega-1`

## Decisiones

| Fecha | Decisión | Quién |
| --- | --- | --- |
| 05/11 | ECLIPSE es una carpeta del repo de la asignatura, rama `feature/eclipse`; el kit va en `Assets/ThirdParty/` y no se publica | Ion |
| 05/11 | Hasta el 20/11 se trabaja con Claude Code CLI en `ECLIPSE/`; AI Assistant como cuaderno de decisiones | Ion |
| 05/11 | Kits: `Sci Fi Modular Pack` (MagixBox) y `Sci-Fi Old rusty props PBR` (KMProd) | Ion |
| 05/11 | Planos: S1 Sarrera–Korridorea–Laborategia; S2 Fusibleak–Korridorea–Sorgailua; S3 Korridorea–Erreaktorea; en línea sobre X. Las salas de S2 y S3 van sin techo ni props | Ion |
| 05/11 | `ModuluEskala = 1,25` para los módulos de MagixBox: a 1:1 la puerta mide 1,85 m (cápsula 1,8 m) y el pasillo 2,56 m; a 1,25 la hoja mide 2,08 m y el pasillo 3,2 m | Claude, a confirmar por Ion |
| 06/11 | Explorador de pruebas sin guardarlo en la escena: se crea por código en Play y se retira con la carpeta `Scriptak/Proba/` cuando exista el jugador | Ion |
| 06/11 | Excepción autorizada: UV2 en los `.meta` de modelos del kit sin UV2. Copias en `~/ECLIPSE_babeskopia/` (`ThirdParty_meta_2026-11-05.tgz` antes, `ThirdParty_meta_UV2_2026-11-06.tgz` después). Git no los guarda | Ion |
| 05/11 | Hoja `Door` de `ProfileDoor` desactivada en cada instancia hasta que exista `PF_Atea`. Fuera por ahora: luces de sala y props de pared | Claude, a confirmar por Ion |
| 05/11 | Alcance de `ArgiztapenPreseta` para el 6/11: solo flags y UV2 (acción aparte con confirmación). Modos de luz, `ECLIPSE_Argiztapena` y perfiles quedan fuera, y UV2 no va en una `UV2Konfiguratzailea` separada | Ion |
| 05/11 | "Dinámico" = el objeto o un ancestro del sector tiene `Rigidbody`, `Animator` o `Animation`, o se llama `Din_*`, `Door` o `Doors` | Ion (plan) |
| 05/11 | Además de `ContributeGI` se marca `ReflectionProbeStatic`, para que los probes horneados capturen la geometría | Ion (plan) |
| 05/11 | `ThirdParty/` no está en Git, así que los `.meta` del kit no se revierten con Git (estrategia 12.1 y 17.13 no aplican): la protección es la copia en `~/ECLIPSE_babeskopia/`. Si se restaura el kit desde el paquete original se pierden los UV2 y hay que reaplicarlos y rehornear | Ion (plan) |

## Dudas abiertas

- ¿Qué pide exactamente el enunciado del 6/11 además de lo que está en la estrategia?
- ¿El profesor cuenta 3 sectores con transición como 3 niveles? (decisión de escenas, el 7/11)

## Decisiones del prototipo (06/11)

- Los pasillos se ajustan con secciones enteras de `Profile1`; solo se admite ±10 % en longitud (máximo actual +8,7 %), y la herramienta da error por encima. Con la rejilla, un pasillo sin puertas o con dos necesita ≥3 celdas; con una puerta, ≥6.
- Las señales indican adónde lleva la puerta, no la sala actual.
- Luces Realtime sin sombras y sin bake hasta validar geometría; por eso los props aún parecen flotar un poco.

## Decisiones de la fase 3 (06/11)

- Ion: tres escenas independientes con misión propia; esta decisión sustituye a la escena única de la estrategia (10.4). Identidad visual de los sectores según el encargo de Ion (Laborategia frío, Energia ámbar, Erreaktorea rojo y cian).
- Suelo: `Floor1` solo en cruces y salas de inicio; el resto, lauza propia de 2 triángulos. Rutas y coberturas validadas por inundación y robot.
- Solo se usan props con UV2 (`ModuluKostua`), para poder hornear sin tocar `ThirdParty`.
- Escenas nuevas: se restauran con herramientas de Editor; hay una regla que protege `.unity` de escrituras por shell.
- Ajuste de iluminación propio (`ECLIPSE_Mailak.lighting`, 16 texel/m): el de `01` no se toca.

## Pendiente que requiere a Ion

- Build Profiles: añadir las tres escenas (`ProjectSettings`). En el Editor la transición funciona sin ello; en una build no.

## Defectos conocidos y pendientes (sin bakes nuevos)

- **Parches rectangulares en lightmaps:** causa raíz sin identificar. A 16 texel/m, solo salas: limpio; con pasillos: parches. Descartado a 16 texel/m: número de luces, número de instancias y mallas compartidas sala/pasillo; UV2 dentro de rango. Las muestras (×16 directas, ×8 indirectas) solo se probaron a 4 texel/m, donde hay parches siempre, así que **no quedan descartadas**. Probablemente es el mismo defecto de `01_Instalazioa`. Solución aplicada: pasillos fuera del lightmap.
- **Lightmaps de las misiones:** auditoría limpia (0 piezas o luces sin hornear). Quedan, sin corregir porque exigen bake: manchas ovaladas pequeñas en algunas paredes (están en el lightmap; no vienen de los Reflection Probes) y M3 horneado con el suelo anterior, de rebote algo distinto.
- **Light Probes casi negras:** 69, casi todas en `Mantentzea` (M1 40, M2 4, M3 23). Son zonas oscuras a propósito; un robot futuro se vería muy oscuro ahí. Revisar al añadir enemigos.
- **Oclusión residual:** 6 vistas con 1–112 px de diferencia. En uso real no debería notarse; atención si aparece algo al girar en `Borroka` de M1.
- **Explorador temporal:** en M1, 3 de 4704 vistas en 3.ª persona dejan la cámara dentro de `PF_DatuNukleoa` (colisión de malla cóncava). En torno al 20 % de las vistas, la cámara se acerca a menos de 1 m de la cabeza por falta de sitio (mínimo 0,17 m). A tener en cuenta en el diseño de la cámara definitiva.
- **`JokalariaBarruan`** queda activo si se desactiva el `CharacterController` dentro de un trigger. Hoy solo ocurriría al recolocar tras una caída. Tener en cuenta con el jugador definitivo.
- **Visual:** las pantallas son rectángulos lisos sin contenido; M1 se ve muy contrastado y poco azul. Sin el explorador real no se han valorado en movimiento.
- **Herramientas, limitaciones:** Aurrebista no cuenta los objetos de misión (se sincronizan en Aplikatu). `BatchMailaPrestatu`, `BatchMailaBake` y `BatchOklusioDoitu` son solo batch, guardan la escena y no siguen el patrón Aurrebista/Aplikatu de `AGENTS.md`.

## Para Ion en Play (no se puede automatizar en batch)

- Bloqueo de cursor: **V, Esc y clic** dependen de él; y WASD/ratón en el flujo normal (lo automático invoca los métodos directamente).
- **FPS reales** (Game view → Stats) en M1 `Esnatzea`, M2 `Pasilloa` y M3 `Erreaktorea`.
- Sensación de la cámara en 3.ª persona, el ambiente y las proporciones.
- Ctrl+Z real y los menús `ECLIPSE/Mailak/…`.

## Decisiones para Ion

- **Salida sin indicador visual:** la sala `Irteera` no tiene nada que marque la salida (solo el trigger y el texto del HUD). ¿Puerta, señal o baliza? No se ha añadido por no ser un defecto, sino diseño.
- **Build Profiles:** añadir las tres escenas (`ProjectSettings`). En el Editor la transición funciona sin ello; en una build, no.
- **Oclusión o no:** queda activa. Si en Play se ve algo raro, `BatchOklusioKendu` la retira sin tocar la iluminación.
