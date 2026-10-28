# Horror Audio v13b Fix 1 — sin descargar dos veces

Corrige nombres de ficheros dentro de ZIP (`sounds/shotty.wav`, `4 Shell Reload.mp3`, pasos hojas, Monster 1-3, hit/die1). Reutiliza **las 14 descargas** del intento fallido dentro de `Snapshots/HorrorAudioV13b_xms2zinc/raw`. No usa Internet para preparar ni aplicar. No borra backups ni Builder v13 antiguo. Mismas escenas/scripts base; no hace commits.

**Fase 1 — Solo lectura**

```bash
ASIG="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
SNAP="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
RAW="$SNAP/HorrorAudioV13b_xms2zinc/raw"
SCRIPT="$HOME/Downloads/HorrorAudio_v13b_fix1/horror-audio-v13b-fix1.py"
python3 "$SCRIPT" --audit --cache "$RAW"
```

Esperado: `HBB13BF_CACHE_OK`.

**Fase 2 — Preparar audios para escucharlos, sin tocar Unity**

```bash
python3 "$SCRIPT" --prepare --cache "$RAW"
```

Esperado: `HBB13BF_PREPARACION_OK` y la ruta `HBB13BF_STAGE: .../HorrorAudioV13bFix_PREPARADO_...` (se muestra dos veces). Escucha los WAV de las carpetas `output/` y `resources/` desde el explorador. Si los sonidos no gustan, no aplicar.

**Fase 3 — Aplicar tras aprobación con Unity cerrado**

```bash
STAGE="<ruta completa mostrada por HBB13BF_STAGE>"
python3 "$SCRIPT" --apply --stage "$STAGE"
```

Crea copia de WAV/script/herramientas previas en el mismo snapshot preparado, controla hashes, ejecuta Unity Batch, verifica `SampleScene`, NavMesh, escenas, BuildSettings, crea recursos en `Assets/Resources/HorrorAudioV13b`, y copia herramientas a `tools/HorrorAudioV13b`. Si falla durante cambios intenta rollback.

La copia ya realizada `HorrorAudio_Preparacion_ueFaClou` se mantiene intacta. No hacer commit hasta aprobar en Play.

**Nota de calidad:** Los WAV son grabaciones de terceros, pero no garantizan calidad cinematográfica ni loops perfectos. Probarlos escuchando. Manifest conserva URL y SHA-256 de los originales CC0 según OpenGameArt.
