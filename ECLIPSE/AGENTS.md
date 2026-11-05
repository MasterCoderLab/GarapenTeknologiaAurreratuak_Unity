# ECLIPSE — Reglas para agentes (Claude Code y Codex)

Este archivo lo leen **todos** los agentes que trabajan en `ECLIPSE/`. Claude Code lo carga desde `CLAUDE.md`; Codex lo lee directamente. Si una instrucción de una conversación contradice este archivo, gana este archivo, salvo que Ion diga lo contrario explícitamente.

## 1. Proyecto

- **ECLIPSE:** shooter táctico en tercera persona, sci-fi, 3 sectores (Laborategia, Energia, Erreaktorea), pistola EMP, inventario ligero, robots. Proyecto final de *Garapen teknologia aurreratuak* (DAM, Tolosaldea LHII).
- **Fuente de verdad:** `docs/Eclipse_Estrategia_Claude_v2.1_final.md`. Antes de decidir algo de diseño o arquitectura, consúltala.
- **Estado del trabajo:** `docs/estado.md`. Léelo al empezar cada sesión.
- **Entregas:** escenario el **6/11/2026 a las 14:15**; juego el **20/11/2026 a las 14:15**.
- **Roles:** el agente implementa; GPT revisa; **Ion decide, prueba en Unity y hace los commits**.

## 2. Entorno

- Fedora 44, GNOME. Unity **6000.6.0f1**, URP. API de Unity 6 (`linearVelocity`, `FindFirstObjectByType`); C# 9.
- Editor: `~/Unity/Hub/Editor/6000.6.0f1/Editor/Unity`.
- Portátil con GPU híbrida: Unity se lanza **siempre** en la NVIDIA con
  `__NV_PRIME_RENDER_OFFLOAD=1 __GLX_VENDOR_LIBRARY_NAME=nvidia __VK_LAYER_NV_optimus=NVIDIA_only` y `-force-vulkan`.
- Repo: `GarapenTeknologiaAurreratuak_Unity` (público), rama **`feature/eclipse`**. ECLIPSE es una carpeta más, junto a los juegos anteriores.

## 3. Límites (no negociables)

| Zona | Permiso |
| --- | --- |
| `Assets/_Eclipse/`, `docs/`, `LICENSES/` | Escritura normal |
| `Packages/`, `ProjectSettings/` | Solo con autorización explícita de Ion para ese cambio, diff mostrado antes y commit separado |
| `Assets/ThirdParty/` (kit, controlador) | **Solo lectura** siempre. Está en `.gitignore`: los assets de pago no se publican |
| Otras carpetas del repo (`Laberintoa3D_Final/`, `Plataformak2D*/`, `SpaceInvaders2D*/`, `tools/`) | **Intocables.** Solo lectura, como referencia |
| `.unity`, `.prefab`, `.meta`, `.asset`, `.mat`, `.controller`, `.anim`, `.lighting` | **Nunca** editar como texto el YAML. Los cambios en escenas y assets se hacen con herramientas de Editor (sección 5) o los hace Ion en Unity |

- **Git:** nunca `push`, `reset --hard`, `clean`, `rebase` ni borrar ramas. **Los commits los hace Ion** después de probar en Play.
- **Batchmode:** solo con Unity cerrado para este proyecto. Comprobar antes que no existe `Temp/UnityLockfile`.
- **Terceros:** no descargar, instalar ni importar nada sin que Ion lo apruebe. Revisar en solo lectura antes de abrir en Unity: un paquete con scripts de `Editor/` ejecuta código al importarlo.
- **No inventar:** distinguir siempre **comprobado**, **hipótesis** y **pendiente**. Si no se ha probado en Unity, no está hecho.

## 4. Código

- Carpetas (estrategia, sección 9.1): `Assets/_Eclipse/{Eszenak,Scriptak,Datuak,Prefabak,Editor,Tests}`.
- Ensamblados: `Eclipse.Runtime`, `Eclipse.Editor`, `Eclipse.Tests.EditMode`, `Eclipse.Tests.PlayMode`. La lógica pura no depende de `MonoBehaviour` y tiene tests EditMode.
- Nombres de clases según los contratos de la estrategia (sección 9.2): `GameManager`, `GameSession`, `Inbentarioa`, `EMPLogika`…
- **Comentarios, documentación XML y textos de interfaz en euskera.** Explicaciones a Ion, en español.
- KISS y DRY; validar antes de modificar; resultados explícitos; nunca un `catch` vacío.
- Suscripciones a `GameEvents` en `OnEnable` y bajas en `OnDisable`; ningún estado de escena en campos `static`.
- **Sectores autónomos:** cada sector vive en su raíz (`Sektorea1`, `Sektorea2`, `Sektorea3`); ningún objeto apunta a otro sector; nada persistente dentro de un sector.

## 5. Herramientas de Editor (estrategia, sección 12.1)

Lo repetitivo o masivo dentro de Unity se hace con una herramienta de Editor en C#, menú `ECLIPSE/…`, que sigue **siempre** este patrón:

1. **Vista previa** por defecto: un informe sin modificar nada.
2. **Aplicar** en un solo grupo de Undo (`Undo.SetCurrentGroupName` … `Undo.CollapseUndoOperations`).
3. **Idempotente:** la segunda ejecución informa "0 cambios".
4. **Alcance acotado:** solo el sector seleccionado o la carpeta indicada.
5. **Informe** en `docs/qa/tresnak/<herramienta>_<fecha>.md`.
6. En el log, una marca que se pueda buscar: `ECL_<HERRAMIENTA>_OK` o `ECL_<HERRAMIENTA>_ERROR`.

Instancias con `PrefabUtility.InstantiatePrefab`. En modo interactivo, la herramienta no guarda la escena: guarda Ion.

## 6. Forma de trabajar

1. Leer `docs/estado.md` y la parte de la estrategia que toque.
2. Proponer el cambio en pocas líneas: qué archivos, por qué y cómo se comprueba.
3. Implementar en pasos pequeños. Mostrar el diff.
4. Comprobar: compila, pasan los tests y Ion lo valida en Play.
5. Actualizar `docs/estado.md` (hecho, pendiente, decisiones) y proponer el mensaje de commit. **El commit lo hace Ion.**
