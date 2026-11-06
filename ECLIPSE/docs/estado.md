# ECLIPSE — Estado del trabajo

> Lo actualiza el agente al cerrar cada bloque de trabajo y lo revisa Ion. Lo más reciente, arriba.

## Ahora

- **Fase:** 1 — Escenario (entrega 6/11/2026 a las 14:15)
- **Rama:** `feature/eclipse`
- **Kits:** `Sci Fi Modular Pack` (MagixBox) y `Sci-Fi Old rusty props PBR` (KMProd), en `Assets/ThirdParty/`, convertidos a URP
- **Siguiente paso:** Ion valida en Unity y en Play (`docs/temario_matrizea.md`, sección 3), decide `ModuluEskala` y las manchas de lightmap, redacta la memoria y hace el commit

## Hecho

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
| 06/11 | Excepción autorizada: UV2 en los `.meta` de modelos del kit sin UV2. Copias en `~/ECLIPSE_babeskopia/` (`ThirdParty_meta_2026-11-05.tgz` antes, `ThirdParty_meta_UV2_2026-11-06.tgz` después). Git no los guarda | Ion |
| 05/11 | Hoja `Door` de `ProfileDoor` desactivada en cada instancia hasta que exista `PF_Atea`. Fuera por ahora: luces de sala y props de pared | Claude, a confirmar por Ion |
| 05/11 | Alcance de `ArgiztapenPreseta` para el 6/11: solo flags y UV2 (acción aparte con confirmación). Modos de luz, `ECLIPSE_Argiztapena` y perfiles quedan fuera, y UV2 no va en una `UV2Konfiguratzailea` separada | Ion |
| 05/11 | "Dinámico" = el objeto o un ancestro del sector tiene `Rigidbody`, `Animator` o `Animation`, o se llama `Din_*`, `Door` o `Doors` | Ion (plan) |
| 05/11 | Además de `ContributeGI` se marca `ReflectionProbeStatic`, para que los probes horneados capturen la geometría | Ion (plan) |
| 05/11 | `ThirdParty/` no está en Git, así que los `.meta` del kit no se revierten con Git (estrategia 12.1 y 17.13 no aplican): la protección es la copia en `~/ECLIPSE_babeskopia/`. Si se restaura el kit desde el paquete original se pierden los UV2 y hay que reaplicarlos y rehornear | Ion (plan) |

## Dudas abiertas

- ¿Qué pide exactamente el enunciado del 6/11 además de lo que está en la estrategia?
- ¿El profesor cuenta 3 sectores con transición como 3 niveles? (decisión de escenas, el 7/11)
