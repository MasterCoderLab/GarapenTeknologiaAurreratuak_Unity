# ECLIPSE — Estado del trabajo

> Lo actualiza el agente al cerrar cada bloque de trabajo y lo revisa Ion. Lo más reciente, arriba.

## Ahora

- **Fase:** 1 — Escenario (entrega 6/11/2026 a las 14:15)
- **Rama:** `feature/eclipse`
- **Kits:** `Sci Fi Modular Pack` (MagixBox) y `Sci-Fi Old rusty props PBR` (KMProd), en `Assets/ThirdParty/`, convertidos a URP
- **Siguiente paso:** Ion prueba `ArgiztapenPreseta` y `ProbeJartzailea` en Unity (lista abajo) y hace el commit; sigue montando `01_Instalazioa`

## Hecho

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
- [ ] Sector 1 con calidad de entrega; sectores 2 y 3 con distribución, coberturas y marcadores
- [ ] Lightmap (Compute Light Baker en la NVIDIA), Reflection Probes con Box Projection, Light Probes
- [ ] Luces Baked, Mixed y Realtime; emisivos; post-procesado
- [ ] Capturas de evidencia (con y sin bake, con y sin probe)
- [ ] Commit y etiqueta `entrega-1`

## Decisiones

| Fecha | Decisión | Quién |
| --- | --- | --- |
| 05/11 | ECLIPSE es una carpeta del repo de la asignatura, rama `feature/eclipse`; el kit va en `Assets/ThirdParty/` y no se publica | Ion |
| 05/11 | Hasta el 20/11 se trabaja con Claude Code CLI en `ECLIPSE/`; AI Assistant como cuaderno de decisiones | Ion |
| 05/11 | Kits: `Sci Fi Modular Pack` (MagixBox) y `Sci-Fi Old rusty props PBR` (KMProd) | Ion |
| 05/11 | Alcance de `ArgiztapenPreseta` para el 6/11: solo flags y UV2 (acción aparte con confirmación). Modos de luz, `ECLIPSE_Argiztapena` y perfiles quedan fuera, y UV2 no va en una `UV2Konfiguratzailea` separada | Ion |
| 05/11 | "Dinámico" = el objeto o un ancestro del sector tiene `Rigidbody`, `Animator` o `Animation`, o se llama `Din_*`, `Door` o `Doors` | Ion (plan) |
| 05/11 | Además de `ContributeGI` se marca `ReflectionProbeStatic`, para que los probes horneados capturen la geometría | Ion (plan) |
| 05/11 | `ThirdParty/` no está en Git, así que los `.meta` del kit no se revierten con Git (estrategia 12.1 y 17.13 no aplican): la protección es la copia en `~/ECLIPSE_babeskopia/`. Si se restaura el kit desde el paquete original se pierden los UV2 y hay que reaplicarlos y rehornear | Ion (plan) |

## Dudas abiertas

- ¿Qué pide exactamente el enunciado del 6/11 además de lo que está en la estrategia?
- ¿El profesor cuenta 3 sectores con transición como 3 niveles? (decisión de escenas, el 7/11)
