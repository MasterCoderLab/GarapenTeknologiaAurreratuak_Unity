# Horror Builder v2 — Mejora visual del escenario (DAM 2AG3 · 2026/27)

## Estado de partida verificado

- Público `MasterCoderLab/GarapenTeknologiaAurreratuak_Unity`, rama `feature/laberintoa-horror`, commit `fd55fd79`.
- Privado `MasterCoderLab/Laberintoa3D_Horror_Backup_Privado`, rama `main`, commit `f7a47d7`.
- Proyecto: `Laberintoa3D_Final`, Unity **6000.6.0f1**, URP 17.6.0, Fedora, NVIDIA/Vulkan.
- Horror Builder v1 probado y versionado: 4 techos, 45 paredes originales + nueva salida, 4 suelos, IA/animaciones de 3 enemigos, linterna F, NavMesh, bosque y chabola.

## Cambios de esta fase

1. **Texturas procedurales originales** de 256×256 px: hormigón deteriorado, tierra, tablones, corteza, metal oxidado, hojas. Son **generadas por código**; no se incorporan assets de terceros.
2. **Bosque**: sustituye las dos copas-esfera visuales de cada árbol por tres capas cónicas de malla propia; mantiene los troncos y sus colliders originales. También cambia su corteza, materiales del suelo y camino, añade hierbas y luz lunar tenue.
3. **Búnker**: mejora la textura de los cuatro techos, añade tuberías, raíles, cables y materiales de cajas. Todos los objetos decorativos creados en v2 carecen de Collider; no bloquean la ruta.
4. **Chabola**: envejece la madera, cambia tejado y añade tablones irregulares, marco, pequeño porche y mayor silueta de escopeta (todavía es decorativa, sin disparo).
5. **Atmósfera**: densidad de niebla algo mayor, luces de emergencia y punto cálido tenue fuera de la chabola.

## Lo que NO cambia

- No modifica el código de IA, animaciones, jugador ni linterna.
- No modifica el NavMesh v1 ni sus referencias. No añade obstáculos físicos al escenario.
- No modifica la escena `SampleScene` original ni modelos Creep.
- **No hace git add, git commit ni git push**. Los commits público/privado se preparan por separado tras validación visual.
- NO se considera acabado artísticamente: son texturas generadas por código y mejoras de geometría. En el futuro se podrán sustituir por modelos de naturaleza y materiales PBR de alta calidad con licencias apropiadas.

## Ejecución

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v2.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v2/horror-builder-v2.sh" --audit
```

Resultado esperado: `HBB2_AUDIT_LOCAL_OK`. El `--audit` solo lee, y puede ejecutarse con Unity abierto.

Guarda y cierra Unity totalmente. Después:

```bash
bash "$HOME/Downloads/HorrorBuilder_v2/horror-builder-v2.sh" --apply
```

El `--apply` crea un respaldo independiente `~/LaberintoaHorror_BuilderV2_XXXXXXXX`, copia una herramienta Editor C# a `Assets/Editor/HorrorBuilderV2`, crea los assets en `Assets/Laberintoa/Horror/BuilderV2`, y llama a Unity batch. Verifica el fichero `SampleScene`, el `NavMesh`, el script de IA, las animaciones y el Editor v1 mediante SHA-256. Si Unity falla o la validación no pasa, el Bash restaura la **escena** y elimina **únicamente** carpetas y `.meta` creados por v2; no deshace commits existentes.

Si ves cualquier error, **no vuelvas a ejecutar `--apply`** antes de revisar el log. Si hay cambios manuales posteriores, tampoco se debe aplicar este script: está diseñado específicamente para los commits citados.

## Verificación visual en Unity

1. Abre `Assets/Laberintoa/Scenes/Laberintoa_Horror.unity`.
2. En `Scene`, comprueba pinos, hierba, texturas del suelo, chabola y techo industrial.
3. En `Game`, verifica linterna F, recorrido al bosque, FPS y colores de los materiales (ninguno magenta).
4. Acércate a Creep1/Creep2/Creep3 y confirma que aparecen con IA/animaciones; las pruebas exhaustivas de combate/colisiones quedan para la fase posterior.
5. Envía captura exterior/interior y salida `HBB2_OK`. Solo después prepara checkpoints privados y públicos **separados**.

## Limitación de verificación

La sintaxis de Bash, integridad ZIP y revisión estática del C# se han comprobado. No es posible compilar ni ejecutar Unity en el contenedor de creación. El primer test real de compilación/importación se ejecuta en tu Fedora con `--apply`, no está garantizado hasta verlo en el log.
