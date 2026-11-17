# ECLIPSE — Estado del trabajo

> Lo actualiza el agente al cerrar cada bloque de trabajo y lo revisa Ion. Lo más reciente, arriba.

## Ahora

- **Fase:** A2 "Protagonista HypJo" **aprobada por Ion** y publicada (`09d2c73a`). En curso: A3, primer bloque (núcleo de combate en escena aislada). Entrega 25/11.
- **Rama:** `feature/eclipse-hud`; `HEAD = origin = 09d2c73a` (A2), sobre `80d18f06` (estrategia v3.2), `716365f9` (A0) y `7992940e` (A1)
- **Kits:** `Sci Fi Modular Pack` (MagixBox) y `Sci-Fi Old rusty props PBR` (KMProd), en `Assets/ThirdParty/`, convertidos a URP. Protagonista: `Assets/ThirdParty/{HypJo_Cyborg,Quaternius_UAL}` (fuera de Git; respaldo privado `~/ECLIPSE_babeskopia/A2_ThirdParty_09d2c73a/`)
- **Visión fijada por Ion (14/11):** survival horror sci-fi inspirado en la atmósfera de Dead Space 3, sin copiar sus recursos.
  - **M1:** científicos y soldados; atmósfera y presentación.
  - **M2:** soldados, colapso y primer monstruo, con el primer combate real.
  - **M3:** invasión de monstruos, variantes, gore y huida.
  - Los NPC no son hostiles por defecto. Los encuentros se deciden por misión.
- **Siguiente paso:** A3, primer bloque:
  - lógica pura y tests;
  - pistola EMP, apuntado y enemigo provisional;
  - `04_BorrokaProba`.
  Revisión de Ion **antes** de tocar las misiones o introducir personajes definitivos.
- **Estrategia vigente:** `docs/estrategia/ECLIPSE_Estrategia_Definitiva_v3.2_2026-11-07.md`. Sustituye a v3.1 y a los prompts temporales de A1; `docs/Eclipse_Estrategia_Claude_v2.1_final.md` es solo histórica.

## Hecho

- 14/11 — **A3, primer bloque (sin commit, pendiente de la revisión de Ion jugando).** Informe: `docs/qa/tresnak/A3_Borroka_LehenBlokea_2026-11-14.md`.
  - **Qué hay:**
    - Lógica pura con 19 tests: `ArmaLogika`, `BizitzaLogika`, `GorputzKalteLogika` (zonas y partes separables) y `EtsaiEgoeraMakina`.
    - Pistola EMP: apuntado con la capa "Arma", disparo desde la retícula con comprobación pecho → boca → impacto, SFX de SoX.
    - Enemigo provisional (maniquí UAL CC0, 10 zonas) que aparece en runtime sobre `MailaMarka`, con NavMesh generado de los colliders.
    - Escena `Eszenak/Proba/04_BorrokaProba.unity`.
  - **Misiones:** sin cambios. El combate solo existe en la escena de pruebas.
  - **Comprobado (batch):**
    - EditMode 74/74; `BatchBorrokaProba` 42/42.
    - HypJo 54/21/21/21; HUD 66/89/69; interacción 25×3; robot 211/395/311 m.
    - Release y QA sin Development, target restaurado.
    - 0 cambios en escenas, lightmaps, `Assets/Settings`, `ProjectSettings` y `Packages`.
  - **Pendiente de Ion:**
    - Probar `04_BorrokaProba` con ratón, R y V.
    - Decidir qué hacer con las capturas que reescriben las regresiones (respaldadas en `~/ECLIPSE_babeskopia/a3_lehen_blokea_20261114_183802/regresio_kapturak/`).
- 14/11 — **A2 "Protagonista HypJo" cerrada (commit `09d2c73a`, validada por Ion jugando).** Informe: `docs/qa/tresnak/A2_JokalariaHypJo_2026-11-14.md`.
  - **Qué es:** HypJo v2 es la **capa visual** del `EsploratzaileKontrola` de siempre. La carga `Resources/Jokalaria/PF_Jokalaria_HypJo.prefab`, y si no se puede, se usa la cápsula.
  - **Qué no cambia:** ninguna `.unity` de las misiones; mapa, inventario, HUD, V y controles siguen igual.
  - **Cámara al hombro:** (0,45; 0; −2,2) m. El modelo se orienta hacia el movimiento.
  - **Animator:** blend tree con la velocidad natural de los clips UAL y Foot IK. El modelo se baja lo que mide el `skinWidth`.
  - **Comprobado (batch):**
    - EditMode 55/55; escena neutra `03_JokalariProba` 54/54; M1, M2 y M3 21/21 cada una.
    - HUD 66/89/69; cámara 13 056 vistas sin atravesar paredes; robot 211/395/311 m.
    - Vuelta a la cápsula 9/9.
    - Release y QA sin Development, con el target devuelto a Windows.
    - 0 cambios en escenas, lightmaps, `Assets/Settings`, `ProjectSettings` y `Packages`.
  - **Validación de Ion:** probado jugando y aceptado.
  - **Limpieza del 14/11 (respaldo en `~/ECLIPSE_babeskopia/a3_garbiketa_20261114_182054/`):**
    - Restauradas desde Git las 20 capturas `HUD_*` y `M*_uholdea` que habían sobrescrito las regresiones.
    - Retiradas las 18 capturas extra y 12 informes intermedios.
    - Se versionan los 19 informes finales de A2.

- 07/11 — **Estrategia v3.2 incorporada:** copia en `docs/estrategia/`; `AGENTS.md` la declara única vigente, con entrega 25/11 y rama de trabajo actualizadas (se retiran las citas a secciones de v2.1 que ya no existen, y `EMPLogika`); `/Build_Windows/` añadido a `ECLIPSE/.gitignore`. Comprobado: `HEAD = origin = 716365f9`; `7992940e` contiene A1 y `716365f9` solo `EditorBuildSettings` (M1, M2, M3).
- 07/11 — **A1 "Jokalaria Release" (informe: `docs/qa/tresnak/A1_JokalariaRelease_2026-11-07.md`).** El explorador y el HUD ya no dependen de `UNITY_EDITOR || DEVELOPMENT_BUILD`. `AutoProbaQA.cs` solo se compila con `ECLIPSE_AUTOPROBA`, símbolo que únicamente da la build QA de `BatchReleaseProba`: la Release no lo contiene.
  - **Comprobado (batch, no validación de Ion):** EditMode 45/45; robot 211/395/311 m; HUD 66/89/69 sin fallos; reflexión con control negativo. Build QA `[Simulado]`: M1 → M3 completo, `cc=True`, `debug=False`, código 0; control negativo con error y código 1. Build Release `[Sin interacción]`: arranca M1 con jugador, `debug=False`, sin autoprobador; 60 tipos, solo difiere `AutoProbaQA`; player `nondevelopment`.
  - **Validación manual de Ion (07/11):** ejecutó la Release Linux y completó las tres misiones; todo correcto. La prueba en Windows nativo sigue siendo de A7.
  - **Cierre técnico:** `activeBuildTarget` devuelto a `StandaloneWindows64` y comprobado por la API; capturas de `docs/qa/kapturak` restauradas al respaldo (18 restauradas, 18 eliminadas).
  - **Escenas, lightmaps, `Assets/Settings`, `ProjectSettings` y `Packages`:** 0 archivos cambiados frente a `~/ECLIPSE_babeskopia/a1_antes_033845`.
- 06/11 — **Agacharse, salto y colisiones de tuberías en M2** (decisión de Ion: mantener `PF_Hodia_Grisa__005` donde está y pasar agachado, en vez de subirla).
  - **Movimiento (común a las tres misiones, `EsploratzaileProba.cs`):** Ctrl izquierdo mientras se mantiene: altura 1,10 m, 1,5 m/s, sin correr ni saltar; no se levanta si hay algo encima. Espacio: salto de 1,00 m, solo desde el suelo; el impulso se corta si choca la cabeza.
  - **Colliders añadidos en `Mision02_Energia` (`KolisioAzkenak` en `MailaDatuak`, para que sobrevivan a una regeneración):**
    - `Geometria/Sorgailuak/Kolisioak/Kolisioa__130`: `BoxCollider` 0,5 × 1,4 × 0,5 m en (52,875; 5,1; 46,875), sobre `PF_Hodia_Grisa__002` (vertical, atraviesa la pasarela norte; quedan 1,6 m libres al lado).
    - `Geometria/Sorgailuak/Kolisioak/Kolisioa__131`: `BoxCollider` 0,35 × 0,35 × 18,75 m en (58,125; 6,2; 39,375), sobre `PF_Hodia_Grisa__005` (pasarelas sur y norte; 1,425 m libres debajo).
    - Sin collider, a propósito: `__004` (2,13 m sobre la pasarela, se pasa de pie) y `PF_Hodia_Laranja__000` (1,75 m: roza la cabeza 5 cm; con collider obligaría a agacharse por 5 cm).
  - **Escena:** se aplicó con `BatchM2HodiKolisioak`, que solo guarda si la vista previa da exactamente 2 cambios. Diff de `Mision02_Energia.unity`: 108 líneas añadidas, 0 eliminadas. Segunda vista previa: 0 cambios. Copia previa en `~/ECLIPSE_babeskopia/m2_kolisioak_*`. Lightmaps, LightingData, oclusión, Reflection Probes, materiales y prefabs: hash idéntico. M1 y M3: hash idéntico.
  - **Grafo y mapa:** la inundación usa la altura agachado y marca los nodos donde no se cabe de pie (M2 79, M1 18, M3 2), con coste doble para que solo se elijan si hacen falta. Se ven en ámbar en el mapa. El HUD avisa `[Ctrl] Makurtu azpitik pasatzeko` a menos de 3 m y añade `[Ctrl] Makurtu` y `[Zuriunea] Jauzi` a los controles.
  - **Comprobado (batch, no validación de Ion):** EditMode 45/45. HUD y colisiones en Play: M1 66/66, M2 89/89, M3 69/69. Con colliders reales en M2: de pie choca con `__005` en ambas pasarelas (se detiene en x 57,60 y 58,65), agachado pasa; bajo la tubería, soltar Ctrl no levanta ni permite saltar; `__002` detiene también saltando y se rodea de pie; `__004` y la naranja se pasan de pie. Robot por las líneas del mapa en M2: 393 m, 4 terminales, salida y carga de M3; 58 tramos, 0 cruzan pared. Robot original: M1 211 m, M2 395 m, M3 311 m. Interacción: M2 y M3 correctas; M1 solo falla en las 3 vistas conocidas de `PF_DatuNukleoa`. Informe final de M2: `docs/qa/tresnak/EszenaEraikitzailea_2026-11-06_130302.md`.
- 06/11 — **HUD, marcadores, mapa e inventario base (fases A–E), sin tocar escenas.** Todo se crea en Play desde `LevelManager.Start`; si el HUD falla, sigue el OnGUI anterior (esa vía de respaldo existe, pero no se ha probado).
  - **A, HUD:** `Scriptak/Hud/HudKudeatzailea.cs` y `HudFabrika.cs`, uGUI con `Text` y letra `LegacyRuntime`, porque TMP pide importar Essential Resources (mejora pendiente). Misión, objetivo, fase y barra, ruta, datu-nukleoak, salida, aviso de E, avisos y controles. Reutiliza `MisioLogika` y `LevelManager`; no duplica objetivos.
  - **B, marcadores:** `HudMarkatzaileak.cs` y `HelburuHautaketa.cs` (puros y con tests). Solo terminales de la fase actual, salida al terminar, 2 datu-nukleoak a menos de 25 m y barreras cerradas a menos de 15 m. Fuera de pantalla van al borde con flecha.
  - **C, mapa:** `MapaPanela.cs`, `Nabigazioa/NabigazioSarea.cs` (grafo dirigido y A*, puro) y `NabigazioEraikitzailea.cs`. **El grafo no se deduce de los nombres:** es la misma inundación con cápsula que validó el robot, en Play, y lo que no se alcanza no existe. Los nombres `Gela_C_Pasilloa_A_B` solo se contrastan: diagnóstico 0 avisos en las tres. Sin ruta, no se dibuja línea y se avisa. Se ven paredes internas y obstáculos; dos plantas en M2 y M3 (flechas para cambiar).
  - **D, inventario:** `Inbentarioa/{Inbentarioa,GameSession,GameManager}.cs` y `Hud/InbentarioPanela.cs`. Tab, 4 accesos rápidos (1–4), mochila de 12, datu-nukleoak en la misión y en total. Empieza vacío: hoy no existe ningún objeto recogible ni arma. `ElementuDatuak.EmpPistolaId` reserva el id sin entregarlo. `GameManager` (`DontDestroyOnLoad`) conserva la sesión entre misiones.
  - **Corregido:** `MisioTerminala.JokalariaBarruan` quedaba activo si el `CharacterController` se desactivaba dentro del trigger, y permitía completar un terminal lejano con E. Ahora comprueba también que las cajas se solapen.
  - **Herramienta nueva:** `EszenaEraikitzailea.HudProba.cs` (`BatchHudProba -eclipseMaila N`, `BatchJokalariKonpilazioa`).
  - **Comprobado (batch, no validación de Ion):**
    - EditMode 42/42 (9 anteriores + 33 nuevos).
    - HUD en Play con teclado virtual: M1 57/57, M2 65/65, M3 60/60. Incluye M/Tab/Esc, E bloqueada con el mapa abierto, aviso de E, datu-nukleo real, bloqueo de la salida y giro de 360° con 0 marcadores fuera de pantalla.
    - Robot que recorre **las líneas que dibuja el mapa** pulsando E: M1 207 m, M2 392 m, M3 305 m; 130 tramos, 0 cruzan una pared siguiendo el suelo; carga la misión siguiente, el HUD se crea de nuevo y los datu-nukleoak persisten.
    - El grafo tarda 390–560 ms al cargar (batch) y sus nodos coinciden con la inundación en M2 (10934; M3 9625 frente a 9624 de la prueba anterior).
    - Scripts de jugador compilados sin `DEVELOPMENT_BUILD` (StandaloneLinux64, sin build).
    - Regresión: robot original M1 211 m, M2 394 m, M3 311 m; interacción M2 y M3 correctas; M1 solo falla en las 3 vistas conocidas de `PF_DatuNukleoa`. Estas dos regresiones se ejecutaron antes de los últimos retoques, que solo tocaron la simplificación de rutas y textos del HUD.
    - Hashes de las 5 escenas intactos; 0 cambios en `ProjectSettings`, `Packages`, `ThirdParty`, `.unity` y `.lighting`.
    - Informes: `docs/qa/tresnak/EszenaEraikitzailea_2026-11-06_{114233,114411,114536}.md`. Capturas: `docs/qa/kapturak/HUD_*`.
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
| 14/11 | A2: HypJo v2 es la capa visual del controlador existente, cargada desde `Resources` (sin tocar escenas). Assets originales en `ThirdParty/` (no se publican). Cámara al hombro (0,45; 0; −2,2). Sin clips laterales: solo gira el modelo | Ion |
| 14/11 | Visión: progresión M1 (científicos y soldados, atmósfera) → M2 (soldados, primer monstruo, primer combate) → M3 (invasión, variantes, gore, huida). NPC no hostiles por defecto. Desmembramiento: primero la arquitectura de zonas; las piezas visuales, después del combate básico | Ion |
| 14/11 | A3: NPC y enemigos por aparición en runtime y herramientas de Editor, reutilizando los marcadores `MailaMarka`; las escenas definitivas no se reconstruyen | Ion |
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

- Build Profiles: lista de escenas publicada en `716365f9`. Queda el perfil Windows (ver "Antes de A3").

## Defectos conocidos y pendientes (sin bakes nuevos)

- **Parches rectangulares en lightmaps:** causa raíz sin identificar. A 16 texel/m, solo salas: limpio; con pasillos: parches. Descartado a 16 texel/m: número de luces, número de instancias y mallas compartidas sala/pasillo; UV2 dentro de rango. Las muestras (×16 directas, ×8 indirectas) solo se probaron a 4 texel/m, donde hay parches siempre, así que **no quedan descartadas**. Probablemente es el mismo defecto de `01_Instalazioa`. Solución aplicada: pasillos fuera del lightmap.
- **Lightmaps de las misiones:** auditoría limpia (0 piezas o luces sin hornear). Quedan, sin corregir porque exigen bake: manchas ovaladas pequeñas en algunas paredes (están en el lightmap; no vienen de los Reflection Probes) y M3 horneado con el suelo anterior, de rebote algo distinto.
- **Light Probes casi negras:** 69, casi todas en `Mantentzea` (M1 40, M2 4, M3 23). Son zonas oscuras a propósito; un robot futuro se vería muy oscuro ahí. Revisar al añadir enemigos.
- **Oclusión residual:** 6 vistas con 1–112 px de diferencia. En uso real no debería notarse; atención si aparece algo al girar en `Borroka` de M1.
- **Explorador temporal:** en M1, 3 de 4704 vistas en 3.ª persona dejan la cámara dentro de `PF_DatuNukleoa` (colisión de malla cóncava). En torno al 20 % de las vistas, la cámara se acerca a menos de 1 m de la cabeza por falta de sitio (mínimo 0,17 m). A tener en cuenta en el diseño de la cámara definitiva.
- **HUD, limitaciones:** maquetado y comprobado solo en 16:9 (1920×1080 y 1280×720). El inventario se maneja con teclado: no hay `EventSystem` ni clic. No pausa el juego (la estrategia lo pide; sin enemigos aún no afecta). Con varios objetivos juntos, los marcadores pueden solaparse: los lejanos solo muestran icono. Las rutas toman el camino más corto y pueden pasar cerca de las paredes, aunque sin tocarlas.
- **Visual:** las pantallas son rectángulos lisos sin contenido; M1 se ve muy contrastado y poco azul. Sin el explorador real no se han valorado en movimiento.
- **Herramientas, limitaciones:** Aurrebista no cuenta los objetos de misión (se sincronizan en Aplikatu). `BatchMailaPrestatu`, `BatchMailaBake` y `BatchOklusioDoitu` son solo batch, guardan la escena y no siguen el patrón Aurrebista/Aplikatu de `AGENTS.md`.

## Para Ion en Play (no se puede automatizar en batch)

- **Agacharse y saltar:** sensación de altura, velocidad y cámara al agacharse (1.ª y 3.ª persona) y con el ratón y las teclas reales. En batch se invoca `Mugitu` directamente y el robot fuerza el agachado.
- **Ctrl en el Editor:** comprobar si Ctrl+W, Ctrl+S, etc. activan atajos del Editor durante Play. En una build no debería afectar.
- **HUD:** con el mapa o el inventario abiertos, WASD y ratón no deben mover ni girar (en batch el cursor no se bloquea y no se puede probar). Al cerrar con Esc, el cursor debe volver a quedar capturado; en el Editor, Unity libera el cursor con Esc y puede hacer falta un clic.
- **HUD:** legibilidad real, marcadores al moverse, FPS con el HUD activo y si la pausa de ~0,5 s al cargar cada misión se nota.
- Bloqueo de cursor: **V, Esc y clic** dependen de él; y WASD/ratón en el flujo normal (lo automático invoca los métodos directamente).
- **FPS reales** (Game view → Stats) en M1 `Esnatzea`, M2 `Pasilloa` y M3 `Erreaktorea`.
- Sensación de la cámara en 3.ª persona, el ambiente y las proporciones.
- Ctrl+Z real y los menús `ECLIPSE/Mailak/…`.

## Pendiente para la versión ejecutable del profesor

- **Lista de escenas:** resuelta en `716365f9`.
- **Perfil Windows:** tiene `m_Development: 1`; la build entregable debe generarse sin Development.
- **Jugador:** resuelto en A1; el explorador va en Release. Falta la partida manual de Ion.
- **A7:** comprobar en la build final que `AutoProbaQA` no está en `Eclipse.Runtime.dll` (`grep -c -a AutoProbaQA` debe dar 0).
- **Final de M3:** solo muestra un aviso; no hay pantalla final ni forma de salir del juego (tampoco menú ni pausa).
- **Probar la build:** nada de esto se ha ejecutado fuera del Editor.

## Antes de A3 (decisiones de Ion, estrategia v3.2)

- **Resuelto en A2 (14/11):** protagonista HypJo v2 + rig Mixamo + clips UAL (CC0); licencias en `LICENSES/ThirdParty.md`.
- **Cuatro archivos gráficos** (`DefaultVolumeProfile`, `PC_RPAsset`, `UniversalRenderPipelineGlobalSettings`, `GraphicsSettings`): auditados el 14/11.
  - **Qué son:** campos de serialización de Unity 6000.6/URP 17.6 y el prefiltrado de variantes, que URP recalcula en cada build.
  - **Propuesta:** versionarlos tal cual, en un commit aparte. Son los valores con los que se han validado las Release del 07/11 y del 14/11.
- **Perfil Windows** (`Build Profiles/Windows.asset`, sin seguimiento): mantiene `m_Development: 1`. Se corrige en A7, antes de la build entregable; hasta entonces no se versiona.
- **`V` al apuntar:** propuesta: no forzar nada. El clic derecho apunta en la cámara activa; en tercera persona, la cámara al hombro se acerca. Lo decide Ion tras probar A3.
- **Licencia de `Creep`:** sin validar.
  - **Además,** por la cláusula de IA del EULA, Claude no puede revisarlo visualmente.
  - **A3** usa un enemigo provisional CC0: el maniquí de Quaternius UAL.
  - **La criatura definitiva** se elige después.

## Decisiones para Ion

- **Tubería naranja `PF_Hodia_Laranja__000`:** sin collider; roza la cabeza 5 cm en la pasarela este. Con collider se pasaría agachado. Queda sin tocar salvo que Ion lo pida.
- **Salida sin indicador visual:** la sala `Irteera` no tiene nada que marque la salida (solo el trigger y el texto del HUD). ¿Puerta, señal o baliza? No se ha añadido por no ser un defecto, sino diseño.
- **Oclusión o no:** queda activa. Si en Play se ve algo raro, `BatchOklusioKendu` la retira sin tocar la iluminación.

## A3 saio luzea (14/11/2026) — egoera (Claude; Ion-en Play proba falta)

Txosten osoa: `docs/qa/tresnak/A3_SaioLuzea_Txostena_2026-11-14.md`. Commit-proposamena: `docs/qa/tresnak/A3_CommitProposamena_2026-11-14.md`.

**Eginda (batch-en probatuta; Unity-n Ion-ek jokatu gabe):**
- A: babeskopiak egiaztatuta, kaptura bertsionatuak leheneratuta, QA kapturak `Recordings/qa/<exekuzioa>/`-n (ez dute ezer gainidazten).
- B: borroka-prototipoa (`04_BorrokaProba`, 47/47) eta misio-demoa Editorean bakarrik (`ECLIPSE/Borroka/Demoa`, M1 0 / M2 2 / M3 6 etsai). EMP: v3.2 (munizioa/birkarga), ez da aldatu.
- C: sektore-atrezzoa runtime-n, Editorean aktibatuta (`ECLIPSE/Atrezzo/Demoa`); robotaren ibilbidea ez du oztopatzen (211/395/311 m atrezzoarekin).
- D: arma eta ontzi aktiboen ikerketa (`A3_D_ArmaEtaOntziAktiboak_2026-11-14.md`); ezer ez deskargatuta.
- E: NPC rolak eta desmenbratze modularra, `06_NpcProba` (36/36). Misioetan ez.
- F: amaierako zinematika `07_AmaieraZinematika` (Timeline 28 s, 18/18). Erreaktorea ez da ukitu.
- Erregresio osoa: 23 proba OK (Jokalaria 0–3, HUD 66/91/69, Interakzioa 25×3, Robota 211/395/311, Borroka 47, demo M1–M3, NPC, zinematika, konpilazioa, EditMode 89/89, Release + target leheneratuta). 373 fitxategi babestu hash berdinarekin.

**Erabakiak Ion-entzat:** amaierako testua («JARRAITUKO DU…» AGENTS-en arabera vs «CONTINUARÁ…»); NPC-en fuego amigo-a; aktiboen deskargak (D); M3 garaipena → 07 lotzea (EditorBuildSettings, baimena behar); tarteko txostenak babeskopiara eramatea.

**Pendiente:** Ion-en Play proba (04, 06, 07, demoak); borroka misioetan integratzea Ion-ek onartu ondoren; ontziaren eta armen eredu behin betikoak.

## A4 (15/11/2026) — contenido de misión en runtime y plan (Claude; sin commit)

Ion autorizó (14/11, noche) modificar moderadamente las misiones y pidió primero auditoría, lista de assets y plan: `docs/plan/A4_Auditoria_Assets_Plan_2026-11-15.md`. **Ninguna escena de misión se ha tocado.**

**Hecho y probado en batch:**
- `MisioEdukia` (`Resources/Misioak/ME_Mision0X.asset`) + `MisioEdukiKargatzailea` (runtime, Editor y build) + `MisioEdukiTresna` (`ECLIPSE/Misioak/Edukia`, idempotente, posiciones de NPC calculadas desde la escena y validadas con NavMesh).
- Narrativa activa sin menús: M1 atrezzo + 3 científicos + 2 soldados (sin arma); M2 atrezzo + EMP + 2 monstruos + 2 soldados; M3 atrezzo + EMP + 6 monstruos y, al salir, `07_AmaieraZinematika` (solo en Editor hasta añadirla a Build Settings: permiso pendiente).
- Personajes y enemigos siguen siendo **provisionales** (maniquíes); el sistema cambia de prefab por nombre.
- Pruebas: `BatchMisioEdukiProba` M1 14/14, M2 13/13, M3 13/13. Regresión completa con el contenido activo: Jokalaria (sin contenido, prueba del modelo) 54/21/21/21, HUD 66/91/69, Interakzio 25×3, Robota 211/395/311 (con `-eclipseEtsairikGabe`), Borroka 47, NPC 36, Zinematika 18, EditMode 89/89, Release OK, 373 protegidos sin cambios. `BorrokaDemoProba` adaptado: en M2 los soldados pueden abatir al primer monstruo antes de que llegue al jugador (narrativa).
- Hallazgo: las misiones se generan desde `EszenaEraikitzailea.MailaDatuak.cs`; el rediseño arquitectónico es editar datos + reconstruir + rebake (~10 min GPU por misión), no mover objetos a mano.

**Decisiones de Ion (ver plan, sección 7):** descargas (nada descargado), piloto arquitectónico en M1, científicos (Mixamo + bata propia o Synty), mantener maniquíes mientras tanto, `07` en Build Settings, texto final («JARRAITUKO DU…»/«CONTINUARÁ…»).

**Pendiente/observado:** ~~`Biologia` (M1) no es alcanzable por el NavMesh~~ **corregido el 17/11: sí es alcanzable** (ver A5); el científico no se colocó allí por las distancias a puntos de interés. Soldados de M2 matan al monstruo en ~4 s (12 de daño × 2): ajustar daño o vida para que el jugador llegue a combatir.

## A5 (17/11/2026) — auditoría real y plan maestro (Claude; sin commit; **misiones sin tocar**)

Documento: `docs/plan/A5_Auditoria_PlanMaestro_2026-11-17.md`. Espera la aprobación de Ion.

**Comprobado hoy:**
- Git: 16 modificados y 634 sin seguimiento; copia verificable en `~/ECLIPSE_babeskopia/a4_auditoria_20261117/` (tar de 653 archivos + parche + SHA-256). 373 protegidos sin cambios.
- Entorno (`comprobar_entorno.sh --unirig`): 0 herramientas ausentes; UniRig con CUDA y checkpoints.
- Assets nuevos no Asset Store, auditados y renderizados en `a2_blender` (`assets_candidatos/a4_candidatos/`, `renders/a4/`, `scripts/a4_candidato.py`): 3 armas CC BY 4.0 (pistola aceptada como EMP; dos fusiles a escala ×4, adaptables), Blood Decals de Weasel Whistle (aceptados), MegaKit y Essentials de Quaternius CC0 (aceptados con adaptación), Modular Sci Fi Guns (descartado).
- `BatchNavMeshAuditoria` (nueva, solo lectura): `Biologia` accesible; **`Irteera` (M1–M3) y `J6` (M1–M2) inalcanzables para agentes** (NavMesh horneado con la puerta de bloqueo cerrada).
- Medido en los mapas: 28–32 % de la superficie transitable es pasillo; 23/23/17 pasillos de ~16–19 m; en M1 la sala mayor es el laberinto (288 m²) y `LabNagusia` solo 111 m².
- Build: solo M1–M3 en la lista; Release validada en Linux64, **Windows nunca generada**; `PC_Renderer` sin Decal Renderer Feature.

**No hecho por regla vigente:** paquetes de la Asset Store (Creep, zombis, PolygonR, Blood VFX) sin abrir; Ion debe elegir A/B/C (plan, sección 0).

**Propuesta:** salas emblemáticas con salas adosadas (nueva conexión `Irekidura` en el generador), menos pasillos y cruces, laberinto solo en M3; piloto en M1.
