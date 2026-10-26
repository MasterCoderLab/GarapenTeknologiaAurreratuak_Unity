# Horror Builder v1 — Garapen Teknologia Aurreratuak (2AG3, 2026/27)

## Qué hace

Entrega grande para `Laberintoa3D_Final` con **arquitectura auditable y reversible**:

1. **Checkpoint previo de las animaciones**: verifica rama `feature/laberintoa-horror`, Git LFS y visibilidad del repositorio privado; sincroniza `Assets`, `Packages` y `ProjectSettings` al repositorio privado completo y envía los scripts/escena/controlador propios al público. Este paso **sí hace commit y push**, antes de cualquier construcción.
2. **Auditoría nativa Unity**: 3 enemigos con prefabs/IA/animaciones reales, 46 paredes, 4 suelos y 15 waypoints. Evita los falsos errores P0 del lector YAML.
3. **Limpieza del tutorial** **solo en `Laberintoa_Horror`**: elimina 15 waypoints, 3 Sphere antiguas y 3 componentes `Enemigo` desactivados; conserva `Enemigo.cs` y `SampleScene` sin modificaciones. Conserva las IA, prefabs y animaciones.
4. **Jugador**: sustituye el antiguo script de movimiento por `HorrorPlayerMovement` (Rigidbody + CapsuleCollider), sin cambiar la escena del profesor. La vida sigue siendo provisional.
5. **Interior**: cierra cuatro sectores con techo de hormigón, añade cuatro luces débiles y unas cajas provisionales, y abre salida de unos 3 m en el muro exterior del sector superior derecho (aprox. x=25.5, z=22).
6. **Exterior**: suelo de bosque, más de 45 árboles procedurales, rocas, niebla y noche. Son **placeholders**, no modelos naturales finales.
7. **Cabaña**: chabola abierta con tejado, madera, iluminación y **escopeta visual provisional no funcional**. No se inventa mecánica de disparos/victoria.
8. **Linterna F**: `Spot Light` en la Main Camera.
9. **NavMesh**: genera `NavMesh_Horror_BuilderV1.asset` separado del antiguo y cambia la referencia de `Horror_NavMesh` para permitir pasar del laberinto al bosque y a la cabaña. Valida caminos de los 3 enemigos y del jugador antes de guardar.
10. **Revisión post-guardado**: reabre escena y comprueba componentes, techo, árboles, controlador de linterna y NavMesh. **No sube la construcción automáticamente**.

## Requisitos

- Fedora, Unity 6000.6.0f1 instalado en `~/Unity/Hub/Editor/...`, NVIDIA/Vulkan.
- Repositorio público `GarapenTeknologiaAurreratuak_Unity` rama `feature/laberintoa-horror`.
- Repositorio privado **ya creado**: `MasterCoderLab/Laberintoa3D_Horror_Backup_Privado` rama `main`, Git LFS operativo.
- Unity Asset Store Creep importado, tres animaciones IA locales instaladas.
- `gh`, `git-lfs`, `git`, `rsync`, `python3`.
- **Guardar escena y cerrar Unity antes de `--apply`**.

## Ejecución

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v1.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v1/horror-builder.sh" --dry-run
bash "$HOME/Downloads/HorrorBuilder_v1/horror-builder.sh" --apply
```

`--dry-run` **no modifica nada** y puede usarse con Unity abierto. `--apply` hace commits **del estado de animaciones previo**, crea una copia local de la escena antes de construir y ejecuta Unity en modo batch. **No hace commit/push del nuevo mundo**.

## Mensajes esperados

- `HBB1_PRECHECK` confirma la auditoría nativa de componentes/prefabs.
- `HBB1_NAV_OK` confirma las comprobaciones de rutas de navegación.
- `HBB1_OK` confirma que la escena construida se guardó y reabrió correctamente.

Si falla Unity, script restaura la escena local previa y retira **solo los archivos nuevos instalados por él**. Conserva el respaldo/log bajo `~/LaberintoaHorror_BuilderV1_XXXXXXXX/`; no deshace los commits previos de animaciones. **No vuelvas a ejecutarlo automáticamente si falla: envía el log.**

## Qué sigue sin estar terminado

- Animaciones y colisiones comprobadas **en Play** y comportamiento real de la IA en la puerta.
- Ambientación final realista: recursos de bosque, texturas PBR, puertas, interiores y la chabola definitiva.
- Escopeta usable, munición, daño al enemigo, sistema de vidas, victoria/derrota, audio y exportación.
- La nueva construcción necesita una **validación visual** y luego un nuevo checkpoint de ambos repositorios.

## Seguridad y licencias

No se añade ningún asset original de Unity Asset Store a Git público. Los modelos originales siguen en la copia privada. El script versionado `tools/HorrorBuilderV1/` será parte del **próximo commit** del proyecto después de verificar la escena.

Nota: las API de Unity se han revisado estáticamente, **pero no hemos ejecutado Unity en este contenedor**. El resultado en tu Fedora y la ejecución del juego son la verificación definitiva.
