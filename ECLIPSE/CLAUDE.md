@AGENTS.md

# Específico de Claude Code

- **Al empezar la sesión:** lee `docs/estado.md` y di en tres líneas dónde estamos y qué toca.
- **Al cerrar un bloque de trabajo:** actualiza `docs/estado.md` con lo hecho, lo pendiente y las decisiones, y propón el mensaje de commit. Ion hace el commit.
- **Cambios grandes** (más de 3 archivos o algo que toque escenas, prefabs o ajustes): primero un plan y espera a que Ion lo apruebe.
- Para buscar en el proyecto, no entres en `Library/`, `Temp/`, `Logs/` ni `Assets/ThirdParty/` salvo que haga falta para la tarea.

## Compact Instructions

Al compactar, conserva siempre: la tarea en curso y su criterio de "hecho", los archivos tocados en esta sesión, las decisiones que Ion ha tomado y los errores pendientes de resolver. Las reglas de `AGENTS.md` no hace falta resumirlas: se vuelven a leer del disco.
