# ECLIPSE — Estrategia definitiva v3.2 (documento maestro consolidado)

**Fecha de revisión:** 07/11/2026 · **Entrega:** 25/11/2026 · **Presupuesto para assets:** 0 €  
**Origen:** v3.1 de Claude, Plan A1 revisión 2 ejecutado y auditado, validación manual M1–M3 e historial Git confirmado.  
**Estado:** A1 cerrada y publicada; A0 lista de escenas publicada, ajustes URP/perfil Windows pendientes; A2 todavía no implementada.  
**Autoridad:** este documento sustituye v3.1 como hoja de ruta. `AGENTS.md` debe apuntar a esta versión. Los informes de QA son evidencia, no reemplazan la estrategia.  
**Git:** siempre lo gestiona Ion. Ningún agente hace `git add`, `commit`, `push`, `reset`, `checkout` ni limpieza destructiva sin encargo explícito.

---

## 1. Situación real al cerrar A1

| Área | Estado verificado o pendiente | Próxima acción |
|---|---|---|
| Rama Git | `feature/eclipse-hud`, `HEAD = origin = 716365f9` tras los commits del 07/11 | Preservar avances; solo Ion publica |
| **A1 — jugador Release** | ✅ Commit `7992940e`; `EsploratzaileKontrola` existe en build sin Development | No reabrir ni renombrar controlador sin una fase nueva |
| **A0 — escenas del build** | ✅ Commit `716365f9`; `EditorBuildSettings` contiene M1, M2 y M3 (ya no `SampleScene`) | Conservar el orden M1 → M2 → M3 |
| Release Linux | ✅ Ion ha completado manualmente las 3 misiones con teclado real; jugador presente; `debug=False` | Guardar como evidencia A1; no extrapolarlo a Windows nativo |
| Pruebas A1 | ✅ 45/45 EditMode; robots M1/M2/M3 = 211/395/311 m; HUD = 66/89/69 comprobaciones; QA M1→M3; control negativo sin jugador | Regresión en fases posteriores |
| Separación QA/Release | ✅ `AutoProbaQA.cs` bajo `#if ECLIPSE_AUTOPROBA`; compilación QA con `BuildPlayerOptions.extraScriptingDefines`; **ausente de Release** | Verificar ausencia en A7 |
| Unity target | ✅ `activeBuildTarget` vuelto a `StandaloneWindows64` | No cambiar arbitrariamente |
| Escenas, iluminación y capturas | ✅ hashes de las 5 escenas sin diferencias en A1, 18 capturas restauradas y 18 nuevas retiradas | No tocar los lightmaps ni rebakear |
| Render URP | ⚠️ Cuatro archivos siguen modificados en Git local; diferencias conocidas incluyen nuevos campos y valores de prefiltrado en `PC_RPAsset` | Auditar sin restablecer ni versionar por inercia; los valores de prefiltrado pueden alterar variantes/shaders |
| Perfil Windows | ⚠️ `Assets/Settings/Build Profiles/Windows.asset`: `m_Development: 1`, `m_OverrideGlobalSceneList: 0`; perfil local sin seguimiento | Definir y validar Release sin Development antes de A7, sin romper el perfil de trabajo |
| `Build_Windows/` | ⚠️ Carpeta local sin seguimiento de ~3,9 GB | Añadir exclusión específica a `.gitignore`; no publicar binarios grandes |
| ThirdParty | Packs existentes locales; no garantizados en un clon limpio | No modificar; registrar dependencias, licencias y reproducibilidad |
| `Creep Horror Creature` | En caché; FBX/LOD, tres pieles y clips detectados; **licencia todavía sin validar** | Gate de assets previo a su importación |
| Blender | No instalado en la última auditoría | Solo ruta alternativa, si MVP falla |
| `AGENTS.md` | Menciona estrategia v2.1 y plazo antiguo 20/11 | Actualizarlo de forma mínima a v3.2 y 25/11 |
| Audio y armas | Assets todavía por escoger y aprobar | Gate de A2/A3 |
| Windows nativo + SFX | Aún no validado para la nueva Release | A7, antes del 25/11 |

**No deducir una ausencia de cambios de `git diff --stat` cuando `git status` los enumera.** Reconciliar esa discrepancia antes de decidir sobre `UniversalRenderPipelineGlobalSettings.asset` y `GraphicsSettings.asset`. No interpretar variantes URP como cambios meramente cosméticos sin comprobarlas.

---

## 2. Arquitectura definitiva (decisiones únicas)

1. **Jugador:** el controlador actual (`Eclipse.Proba.EsploratzaileKontrola`) **se mantiene**. A1 solo quita las dependencias de Editor/Dev. Renombrar o mover a `Jokalaria/` llega más tarde y por separado.
2. **Animación:** modelo Humanoid + Animator alimentado por el controlador existente. **Sin Cinemachine.** Animation Rigging solo si las manos fallan, y tras comprobar que existe versión para 6000.6.
3. **Cámara:** reutilizar la cámara ya validada, con desplazamiento al hombro; al disparar, alinear retícula/cámara y boca del arma sin permitir impactos a través de paredes cercanas. Mantener el cambio con `V`; forzar tercera persona al apuntar **solo si Ion lo aprueba tras probar el prototipo**.
4. **Combate:** lógica pura testeable (`ArmaLogika`, `BizitzaLogika`, `EtsaiEgoeraMakina`, `AtalLogika`) + adaptadores finos. Un arma propia (kitbash). El HUD observa los eventos.
5. **Enemigo:** 1 criatura (`Creep`, si pasa el gate) con NavMesh **construida desde colliders físicos en una escena de prueba primero**, excluyendo por capa los triggers (`Gela_*`, terminales, salida). FSM pequeña. Se usan los `Din_Marka_Etsaia_*` existentes.
6. **Desmembramiento, ruta preferente y condicionada a prototipo:** **2 `SkinnedMeshRenderer` en Unity que comparten el esqueleto**.
   - Probar primero la división en `Gorputza` y `Besoa` por pesos de huesos (umbral inicial ≥ 0,5). **Es una hipótesis técnica, no una solución garantizada**: evaluar triángulos cortados, pesos, costuras y deformaciones en una escena aislada. Conservar rig, Avatar y clips originales.
   - Al amputar:
     - `BakeMesh` captura la pose del instante para el trozo físico (con pool y limpieza);
     - se desactiva el renderer del brazo;
     - aparece el prop de muñón con sangre;
     - la IA pierde ese ataque.
   - **Plan B:** Blender con `bpy` automatizado y exportación verificada (Blender no está instalado); no asumir que `bisect_plane` resuelva por sí solo skinning, UV o muñones. **Plan C:** heridas y reacciones sin amputación.
   - **MVP = un brazo.** La pierna y la cabeza solo si sobra tiempo.
7. **Sangre:** primero partículas con pool. Decal Renderer Feature (cambio global, aditivo) solo en A4, aprobado y medido. El SSAO ya usa DepthNormals.
8. **Escenas:** todo lo nuevo entra **por datos y con el generador** (raíces `Din_*`, no estáticas, sin GI). **No se rehornea.** Nada a mano en las `.unity`.
9. **NPC:** escenas dirigidas sin IA de combate. **Tiroteo guionizado** en lugar de un aliado con IA.
10. **Assets 0 €:** candidatos Mixamo (términos propios de Adobe), Quaternius (verificar licencia específica del pack), Sonniss GDC, Freesound **solo con licencia CC0 verificada** y Kenney. Registrar origen, URL, licencia, atribución y usos permitidos de **cada** recurso en `LICENSES/ThirdParty.md`. La criatura `Creep` no se importa hasta confirmar su licencia. No asumir que todo recurso gratuito es CC0.
11. **Builds:** Windows x64 **Release** con Mono. Wine es una prueba auxiliar; Windows nativo es obligatorio antes del 24/11. Las builds van **fuera del repo**.
12. **Duración comprometida: 10–15 min.** 15–25 min es objetivo extra.


---

## 3. Calendario con puertas de aceptación

| Fecha | Fase | Cierre exigido |
|---|---|---|
| **07/11** | **A0 parcial + A1 completa ✅** | Release Linux jugada M1→M3; 45/45 tests; QA separado; commits `7992940e` y `716365f9` publicados |
| **08/11** | Preparación A2 y gate de assets | Confirmar licencia de `Creep`; seleccionar protagonista y animaciones; confirmar control de cámara y arma. Auditar URP/perfil sin comprometer escenas |
| **08–11/11** | A2 Protagonista Humanoid animado + cámara al hombro + arma visible, **en escena aislada** | Animaciones, pies, cámara, V y controles OK visualmente en Play y una build; no romper A1 |
| **12–15/11** | A3 Slice: arma, munición, recarga, salud, botiquín, 1 criatura NavMesh + FSM, impactos de sangre sin amputación | Encuentro jugable en M2 + Release de prueba; gate de combate |
| **16–18/11** | A4 Prototipo de amputación (un brazo), o fallback; decals solo con aprobación | Gate gore: sin artefactos ni pérdida de rendimiento; si falla, Blender/bpy o heridas |
| **19–21/11** | A5 Campaña: M1 presagio, cadáveres/registro y NPC si procede; M2 2–3 encuentros y tiroteo dirigido si viable; M3 3–4 + reactor + final | 10–15 min jugables bien dirigidos; M1→M3 sin bloqueos; narrativa y UI **en euskera** |
| **22–23/11** | A6 Audio, rendimiento, opciones, `productName`, revisión narrativa/UI y cierre de defectos | Feature freeze el 22/11; build Windows completa antes del pulido final |
| **24/11** | A7 Windows Release nativo + Wine secundario + SFX + documento de entrega | DLL sin `AutoProbaQA`, sin Development, hashes, licencias, ejecución Windows real |
| **25/11** | Entrega y margen | No abrir nuevas features |

**Si el 15/11 no funciona todavía un encuentro jugable**, recortar en este orden: NPC vivos → variantes del monstruo → tiroteo dirigido → amputación. **Nunca recortar:** arma funcional, criatura, final, build. El objetivo comprometido es **10–15 minutos de buena calidad**; 15–25 solo si realmente está terminado.

**Asignación de A5:** reservar tiempo explícito para textos/voz no obligatoria/registros, subtítulos en euskera, objetivos comprensibles, transiciones, final y prueba completa de la campaña; no dedicar los tres días solo a colocar enemigos.

---

## 4. A1 — historial y contrato de regresión (FASE CERRADA)

El plan de A1 de v3.1 se **sustituye** por lo realmente implementado, probado y publicado. **No volver a ejecutar el prompt A1 antiguo.**

- `Assets/_Eclipse/Scriptak/Proba/EsploratzaileProba.cs`: controlador disponible en Release, sin compilarlo solo para Editor/Development.
- `Assets/_Eclipse/Scriptak/Misioak/LevelManager.cs`: creación del jugador fuera de los guardas de Editor/Development; log de presencia de CharacterController y `debug=False`.
- `Assets/_Eclipse/Scriptak/Hud/HudKudeatzailea.cs`: HUD sin guardas que lo excluyan de Release.
- `Assets/_Eclipse/Scriptak/Proba/AutoProbaQA.cs`: **solo existe en binario de QA** gracias a `#if ECLIPSE_AUTOPROBA`. La opción `-eclipseAutoProba` no debe habilitar autoprobador en Release final.
- `Assets/_Eclipse/Editor/Tresnak/EszenaEraikitzailea.ReleaseProba.cs`: construye dos variantes **sin Development**, QA con símbolo exclusivo y Release sin él, comprueba tipos, marcas y errores. La QA **simula** completar terminales mediante llamadas directas; no prueba caminar ni pulsar E.
- La prueba negativa elimina/detecta el jugador y debe fallar con código 1.
- Pruebas ejecutadas: EditMode 45/45, robot M1/M2/M3 211/395/311 m; HUD 66/89/69; QA de 3 misiones; Release con jugador; Ion terminó M1→M3 manualmente en el ejecutable Linux.
- Verificación de hashes al cierre: 5 escenas, paquetes, ajustes protegidos y lightmaps sin cambios por A1. Target Windows recuperado; capturas de QA restauradas; no borrar respaldos sin autorización.
- **Pendiente fuera de A1:** Windows nativo de la entrega y revisión del perfil gráfico/Windows.

**Regla de QA:** etiquetar siempre resultados como *simulado*, *teclado virtual*, *sin interacción* o *teclado real*. No confundir la ejecución `-batchmode -nographics` con validar shaders, aspecto, puntería, recorrido o input físico. Registrar fallos y causas; nunca alterar el validador solo para que pase.

---

## 5. A2 — alcance concreto y aceptación

**Objetivo:** reemplazar visualmente la cápsula provisional por un personaje sci-fi animado, con cámara al hombro y arma visible, **sin reemplazar la lógica de control estable**.

### Gate inicial: selección y permisos

1. Ion aprueba el modelo Humanoid gratuito, origen, licencia, compatibilidad y apariencia. Mixamo o pack equivalente es un candidato, no una selección confirmada.
2. Registrar en `LICENSES/ThirdParty.md` los términos de uso; no importar `Creep` si no se ha validado su ficha/licencia, aunque todavía no se necesite en A2.
3. Seleccionar animaciones mínimas: idle, caminar, correr, agacharse y apuntar; salto o recarga solo si está integrado en la fase. Verificar root motion/Avatar y retargeting sobre el mismo modelo.
4. Ion decide el comportamiento de `V` (mantener libre primera/tercera o forzar tercera al apuntar). No cambiarlo unilateralmente.

### Implementación aislada

- Trabajar en una escena de prueba generada o entorno aislado, no sobre las tres `.unity` de misiones hasta aprobar el prototipo.
- Mantener `CharacterController`, sistema Input, colisiones, inventario, mapa, HUD, agacharse, salto y terminales existentes.
- Colocar el `Animator` como capa visual; no mover la raíz física por root motion sin justificación aprobada.
- Camera shoulder offset y retícula con comprobación de oclusión; arma prop visible y ligada al personaje. **A2 no obliga todavía a implementar daño, munición o IA** (A3).
- Sin Cinemachine de entrada; evitar nuevas dependencias salvo necesidad demostrada.
- Si falla el retargeting o aparecen deformaciones, detener la integración, documentar el caso y probar un segundo candidato; no sacrificar controles estables por una apariencia mejor.

### Gate de cierre

- 0 errores CS; tests 45/45 + robot/HUD en lo que aplique; revisar logs por fallos reales.
- En Play y build: WASD/Shift/Ctrl/Espacio/E/M/Tab/V, cámara y colisiones no han empeorado. Animación convincente, cámara sin clipping grave, arma colocada, materiales sin magenta.
- Ion valida **jugando**, mirando especialmente pies, cámara, animación y arma. No declarar A2 cerrada hasta esa aprobación.
- Los cambios de escenas, lighting, lightmaps, packages, ProjectSettings y Git necesitan autorización separada. No rebakear.

---

## 6. Método de trabajo Claude Code + Git + entregables

1. **Documento fuente de verdad:** esta v3.2 en `ECLIPSE/docs/estrategia/ECLIPSE_Estrategia_Definitiva_v3.2_2026-11-07.md` (ruta propuesta relativa a la raíz del repositorio). Ajustar `AGENTS.md` existente para enlazar esa ruta y fecha del 25/11, preservando el resto de instrucciones útiles.
2. **Prompt corto por fase:** indicar que lea `AGENTS.md`, este documento, el código real y `docs/estado.md`, luego planifique la fase y ejecute después de aprobar la planificación. No mantener una lista de correcciones separadas que haya que pegar en cada sesión.
3. **Autonomía:** permitir implementaciones normales en Auto Mode **dentro del alcance autorizado**; no detenerse por cada edición inocua. Auto Mode no equivale a autorización para acciones destructivas ni acceso ilimitado.
4. **Protecciones permanentes:** no modificar archivos `.unity`, `.lighting`, lightmaps horneados, assets terceros ni `ProjectSettings` sin aprobación concreta. Copia + hashes antes de procesos batch sensibles. **Jamás restaurar, resetear o borrar automáticamente archivos originales.** Presentar diffs y pedir decisión.
5. **Git:** solo Ion prepara commits y hace push. Evitar `git add -A`; añadir una entrada específica para `ECLIPSE/Build_Windows/` en el `.gitignore` correspondiente sin excluir recursos de Unity necesarios para clonación. No hacer staging de builds, archivos importados con licencia dudosa o caches.
6. **Repetibilidad:** 0 €; comentarios/UI del juego en euskera; archivos nuevos `.meta` de Unity; logs QA con pruebas ejecutadas vs pendientes; registrar cambios exactos, tests y cualquier alteración local.
7. **No inventar resultados:** una build QA automatizada sin input no sustituye jugar la Release real. Windows nativo sigue pendiente hasta A7.
8. **Limpieza de A0:** los cuatro assets gráficos modificados y el perfil Windows siguen siendo decisiones pendientes. No mezclarlos con el commit de documentación/A2 ni descartarlos por estética. Verificar y acordar qué versionar.

---

## 7. Cómo usar esta estrategia (único prompt de arranque sugerido para A2)

> Lee primero `AGENTS.md`, `docs/estrategia/ECLIPSE_Estrategia_Definitiva_v3.2_2026-11-07.md` y `docs/estado.md` dentro del proyecto ECLIPSE. Esta v3.2 reemplaza por completo a la v3.1 y a los prompts temporales de A1; respeta el código real y los commits `7992940e` y `716365f9`. Propón en **Plan Mode** exclusivamente A2 y el gate de assets, sin implementar aún y sin tocar Git, escenas ni iluminación. Después de mi aprobación, ejecuta A2 con autonomía para cambios ordinarios y termina con pruebas, diferencias e informe. Si hay discrepancias entre esta estrategia y el código, informa antes de actuar. No reabras A1.

**No hacer lo siguiente:** pegar el prompt histórico de A1, decirle al agente que copie v3.1 como fuente maestra, ni iniciar A3 sin cerrar visualmente A2.

---

## 8. Decisiones aún pendientes del usuario

- Licencia de `Creep Horror Creature` en My Assets (editor, licencia y uso), si se va a importar en A3.
- Protagonista Humanoid, clips gratuitos compatibles y licencias revisadas antes de A2.
- Comportamiento exacto de `V` al apuntar; conservar un modo cámara accesible y usable.
- Confirmar cambios opcionales en URP/Decal Renderer Feature antes de A4; intensidad de gore adecuada a evaluación académica.
- Gestión del perfil de Windows para que la **build entregable** se genere sin `Development Build`, y validación en Windows nativo en A7.
- Audio: selección y calidad de disparos, monstruo y ambiente; no reutilizar audio deficiente por inercia.
- Comprobación del contenido del SFX anterior es solo informativa; no bloquea A2.
