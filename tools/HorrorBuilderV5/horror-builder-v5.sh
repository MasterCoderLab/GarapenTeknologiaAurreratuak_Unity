#!/usr/bin/env bash
set -euo pipefail

# Horror Builder v5 — texturas PBR CC0 de Poly Haven. SIN git add/commit/push.
BASE="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$BASE/Laberintoa3D_Final"
PRIV="$HOME/Projects/Laberintoa3D_Horror_Backup_Privado"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
SCENE="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
SAMPLE="$PROY/Assets/Laberintoa/Scenes/SampleScene.unity"
NAV="$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV5"
ART="$PROY/Assets/Laberintoa/Horror/BuilderV5"
TOOLS="$BASE/tools/HorrorBuilderV5"
HERE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CACHE="$HERE/descargas"
MODE="${1:---audit}"
DL_BASE="https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k"
BACKUP=""
SUCCESS=0

fail(){ echo "ERROR: $*" >&2; exit 1; }
need(){ command -v "$1" >/dev/null || fail "Falta: $1"; }

status_check(){
 python3 - "$BASE" "$1" <<'PY'
import subprocess,sys
repo,mode=sys.argv[1:]
raw=subprocess.check_output(['git','-C',repo,'status','--porcelain=v1','-z','--untracked-files=all'])
rows=[item for item in raw.split(b'\0') if item]
tracked={
 'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorGameManager.cs',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorGameManager.cs.meta',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorShotgun.cs',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorShotgun.cs.meta',
}
allowed=(
 'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV2',
 'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV3',
 'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV4',
 'Laberintoa3D_Final/Assets/Editor/HorrorNavMeshStartFix',
 'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV2',
 'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV4',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerHealth.cs',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs',
 'tools/HorrorBuilderV2/', 'tools/HorrorBuilderV3/', 'tools/HorrorBuilderV4/',
)
if mode=='after': allowed+=(
 'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV5',
 'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV5',
 'tools/HorrorBuilderV5/',
)
found={}
for r in rows:
    flag=r[:2].decode('ascii')
    name=r[3:].decode('utf-8')
    if flag not in (' M','??') or (name not in tracked and not name.startswith(allowed)):
        raise SystemExit('ERROR: cambio Git no autorizado: '+repr((flag,name)))
    found[name]=flag
if mode=='before':
    if len(rows)!=84: raise SystemExit('ERROR: v4 esperaba 84 archivos pendientes; ahora hay '+str(len(rows))+'. Auditar antes.')
    required={
        'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
        'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs',
        'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs',
        'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV4/HorrorBuilderV4.cs',
    }
    missing=required-found.keys()
    if missing: raise SystemExit('ERROR: faltan archivos de v4: '+repr(sorted(missing)))
else:
    if len(rows)<=84 or not any('/BuilderV5/' in x for x in found):
        raise SystemExit('ERROR: v5 no ha producido cambios identificables')
print(f'HBB5_STATUS_OK: {len(rows)} archivos locales autorizados; {mode}')
PY
}

check(){
 need git; need python3; need sha256sum; need cmp
 for f in "$UNITY" "$SCENE" "$SAMPLE" "$NAV" \
     "$PROY/Assets/Editor/HorrorBuilderV4/HorrorBuilderV4.cs" \
     "$PROY/Assets/Laberintoa/Horror/BuilderV4/HB4_HormigoiZaharra.mat" \
     "$PROY/Assets/Laberintoa/Horror/BuilderV4/HB4_BasoLurZabaldua.mat" \
     "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs" \
     "$PROY/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs" \
     "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs" \
     "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs" \
     "$PROY/Assets/Laberintoa/Scripts/HorrorGameManager.cs" \
     "$PROY/Assets/Laberintoa/Scripts/HorrorShotgun.cs" \
     "$HERE/src/Editor/HorrorBuilderV5.cs"; do
   [[ -f "$f" ]] || fail "Falta: $f"
 done
 for f in "$EDITOR" "$EDITOR.meta" "$ART" "$ART.meta" "$TOOLS"; do
   [[ ! -e "$f" ]] || fail "v5 ya existe o tiene restos: $f"
 done
 [[ "$(git -C "$BASE" rev-parse --show-toplevel)" = "$BASE" ]] || fail 'Ruta repo publico inesperada'
 [[ "$(git -C "$PRIV" rev-parse --show-toplevel)" = "$PRIV" ]] || fail 'Ruta repo privado inesperada'
 [[ "$(git -C "$BASE" branch --show-current)" = feature/laberintoa-horror ]] || fail 'Rama publica incorrecta'
 [[ "$(git -C "$PRIV" branch --show-current)" = main ]] || fail 'Rama privada incorrecta'
 [[ "$(git -C "$BASE" rev-parse --short=8 HEAD)" = fd55fd79 ]] || fail 'Commit publico distinto: auditar'
 [[ "$(git -C "$PRIV" rev-parse --short=7 HEAD)" = f7a47d7 ]] || fail 'Commit privado distinto: auditar'
 [[ "$(git -C "$BASE" rev-parse HEAD)" = "$(git -C "$BASE" rev-parse origin/feature/laberintoa-horror)" ]] || fail 'Publico sin sincronizar'
 [[ "$(git -C "$PRIV" rev-parse HEAD)" = "$(git -C "$PRIV" rev-parse origin/main)" ]] || fail 'Privado sin sincronizar'
 [[ -z "$(git -C "$BASE" diff --cached --name-only)" ]] || fail 'Cambios staged en publico'
 [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'Privado con cambios sin guardar'
 status_check before
 echo 'HBB5_PREAUDIT_OK: v4 intacto; IA, HUD y NavMesh no se modificaran.'
}

verify_jpegs(){
 python3 - "$CACHE" <<'PY'
from pathlib import Path
import sys
cache=Path(sys.argv[1]); problems=[]
for item in ('concrete','concrete_floor_worn_02','mud_forest'):
 for kind in ('diff','nor_gl','rough'):
  f=cache/f'{item}_{kind}_2k.jpg'
  if not f.is_file() or f.stat().st_size<20000:
   problems.append(str(f)+' (ausente o demasiado pequeño)'); continue
  with f.open('rb') as source:
   if source.read(2)!=b'\xff\xd8': problems.append(str(f)+' (no es JPEG)'); continue
   source.seek(-2,2)
   if source.read(2)!=b'\xff\xd9': problems.append(str(f)+' (JPEG incompleto)')
if problems: raise SystemExit('ERROR: descargas incompletas:\n  '+'\n  '.join(problems))
print('HBB5_TEXTURES_OK: 9 JPG de 2K comprobados (cabecera/fin/tamaño).')
PY
}

fetch(){
 need curl; need python3
 mkdir -p "$CACHE"
 echo '=== DESCARGA CC0 · POLY HAVEN (solo 9 JPG 2K) ==='
 for id in concrete concrete_floor_worn_02 mud_forest; do
   for kind in diff nor_gl rough; do
     file="${id}_${kind}_2k.jpg"
     if [[ -s "$CACHE/$file" ]]; then
       echo "Ya presente: $file"
       continue
     fi
     url="$DL_BASE/$id/$file"
     echo "Descargando $file"
     curl --fail --location --retry 3 --connect-timeout 20 --max-time 180 \
       --silent --show-error --output "$CACHE/$file.part" "$url" || fail "No se pudo descargar $file; cache conservada, proyecto intacto"
     mv -- "$CACHE/$file.part" "$CACHE/$file"
   done
 done
 verify_jpegs
 (cd "$CACHE" && sha256sum ./*.jpg > SHA256SUMS.txt)
 echo 'HBB5_FETCH_OK: texturas CC0 descargadas fuera del proyecto Unity.'
}

rollback(){
 local exitcode="$?"
 if [[ "$SUCCESS" = 0 && -n "$BACKUP" ]]; then
   echo 'HBB5_ROLLBACK: restaurando escena y retirando SOLAMENTE nuevos recursos v5.' >&2
   cp -a "$BACKUP/Laberintoa_Horror.unity" "$SCENE"
   rm -rf -- "$EDITOR" "$ART" "$TOOLS"
   rm -f -- "$EDITOR.meta" "$ART.meta"
   sha256sum -c "$BACKUP/protegidos.sha256" || echo 'AVISO: se detecta archivo protegido distinto' >&2
   echo "Log y copia: $BACKUP" >&2
 fi
 return "$exitcode"
}

apply(){
 check
 verify_jpegs
 (cd "$CACHE" && sha256sum -c SHA256SUMS.txt) || fail 'Se han alterado descargas desde --fetch'
 if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
    pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
    [[ -e "$PROY/Library/UnityLockfile" ]]; then
   fail 'Unity abierto: salir de Play, guardar escena y File > Exit'
 fi
 echo '=== 1. RESPALDO LOCAL ==='
 BACKUP="$(mktemp -d "$HOME/HorrorBuilderV5_XXXXXXXX")"
 cp -a "$SCENE" "$BACKUP/Laberintoa_Horror.unity"
 sha256sum "$SAMPLE" "$NAV" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerHealth.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorGameManager.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorShotgun.cs" \
   "$PROY/Assets/Editor/HorrorBuilderV4/HorrorBuilderV4.cs" \
   "$PROY/Assets/Laberintoa/Horror/BuilderV4/HB4_HormigoiZaharra.mat" \
   "$PROY/Assets/Laberintoa/Horror/BuilderV4/HB4_BasoLurZabaldua.mat" \
   > "$BACKUP/protegidos.sha256"
 git -C "$BASE" status --short --branch > "$BACKUP/git_before.txt"
 echo "Respaldo: $BACKUP"
 trap rollback EXIT
 echo '=== 2. INSTALAR SOLO MATERIAL PASS V5 ==='
 mkdir -p "$EDITOR" "$ART/Textures" "$ART/Materials" "$TOOLS"
 # Evitar que Unity batchmode ignore el directorio Materials al estar vacío.
 printf '%s\n' 'Horror Builder v5 - PBR materialak. Testurak: Poly Haven (CC0).' \
   > "$ART/Materials/LEEME_MATERIALAK.txt"
 cp "$HERE/src/Editor/HorrorBuilderV5.cs" "$EDITOR/HorrorBuilderV5.cs"
 cp -a "$HERE/src" "$HERE/docs" "$TOOLS/"
 cp "$HERE/horror-builder-v5.sh" "$TOOLS/horror-builder-v5.sh"
 cp -- "$CACHE"/*.jpg "$ART/Textures/"
 echo '=== 3. UNITY BATCH: IMPORTAR TEXTURAS, CREAR PBR Y REASIGNAR ==='
 local code=0
 __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
   "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV5.Apply -logFile "$BACKUP/unity.log" || code=$?
 grep -E 'HBB5_|error CS[0-9]+|Exception|Error:' "$BACKUP/unity.log" | tail -n 55 || true
 [[ "$code" -eq 0 ]] || fail "Unity devolvio $code; ver $BACKUP/unity.log"
 grep -q 'HBB5_PRECHECK_OK:' "$BACKUP/unity.log" || fail 'Falta HBB5_PRECHECK_OK'
 grep -q 'HBB5_OK:' "$BACKUP/unity.log" || fail 'Falta HBB5_OK'
 echo '=== 4. COMPROBAR PROTECCIONES ==='
 sha256sum -c "$BACKUP/protegidos.sha256" || fail 'Un fichero de jugabilidad/escena original/NavMesh ha cambiado'
 for f in "$ART/Materials/HB5_BunkerZorua.mat" \
          "$ART/Materials/HB5_BasoLurZabala.mat" \
          "$ART/Materials/HB5_HormaLuzea.mat" \
          "$ART/Materials/HB5_Mask_concrete.png" \
          "$ART/Textures/mud_forest_diff_2k.jpg" \
          "$EDITOR/HorrorBuilderV5.cs.meta"; do
   [[ -f "$f" ]] || fail "No generado: $f"
 done
 git -C "$BASE" -c core.whitespace=-blank-at-eol diff --check
 status_check after
 [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'Privado modificado'
 SUCCESS=1
 echo 'HBB5_LOCAL_OK: materiales PBR reales instalados. No hay commit ni push.'
 echo "Respaldo y log: $BACKUP"
 git -C "$BASE" status --short --branch | tail -n 24
}

case "$MODE" in
 --audit|--dry-run)
   echo '=== HORROR BUILDER V5 · AUDITORIA (SIN CAMBIOS) ==='
   check
   echo 'Materiales CC0: Poly Haven concrete / concrete_floor_worn_02 / mud_forest.'
   echo 'No se altera la geometria, las colisiones, el HUD, los scripts ni GitHub.'
   echo 'HBB5_AUDIT_OK';;
 --fetch) fetch;;
 --apply) apply;;
 --help|-h) echo 'Uso: bash horror-builder-v5.sh --audit | --fetch | --apply';;
 *) fail 'Opcion desconocida';;
esac
