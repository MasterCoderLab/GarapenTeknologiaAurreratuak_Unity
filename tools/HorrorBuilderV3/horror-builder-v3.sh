#!/usr/bin/env bash
set -euo pipefail

# Horror Builder v3 — jugabilidad sin operaciones Git y sin cambiar NavMesh/IA.
BASE="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$BASE/Laberintoa3D_Final"
PRIV="$HOME/Projects/Laberintoa3D_Horror_Backup_Privado"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
SCENE="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
PLAYER="$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs"
SAMPLE="$PROY/Assets/Laberintoa/Scenes/SampleScene.unity"
NAV="$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset"
AI="$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs"
ANIM="$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs"
RUNTIME="$PROY/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV3"
TOOLS="$BASE/tools/HorrorBuilderV3"
DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODE="${1:---audit}"
BACKUP=""
SUCCESS=0

fail(){ echo "ERROR: $*" >&2; exit 1; }
need(){ command -v "$1" >/dev/null || fail "Falta $1"; }

# Examen exhaustivo del working tree; no hay stage en ninguno de los 2 repos.
check_status(){
  python3 - "$BASE" "$1" <<'PY'
import subprocess,sys
repo,phase=sys.argv[1:]
raw=subprocess.check_output(['git','-C',repo,'status','--porcelain=v1','-z','--untracked-files=all'])
items=[x for x in raw.split(b'\x00') if x]
mod={
 'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs',
}
if phase=='after':
    mod.add('Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs')
prefixes=(
 'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV2',
 'Laberintoa3D_Final/Assets/Editor/HorrorNavMeshStartFix',
 'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV2',
 'tools/HorrorBuilderV2/',
)
if phase=='after':
    prefixes=prefixes+(
      'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV3',
      'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorGameManager.cs',
      'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorShotgun.cs',
      'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerHealth.cs',
      'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs',
      'tools/HorrorBuilderV3/',
    )
seen=set()
for item in items:
    if len(item)<4: raise SystemExit('ERROR: estado Git no reconocido')
    flag=item[:2].decode(); name=item[3:].decode()
    if flag in (' M', '??') and (name in mod or name.startswith(prefixes)):
        seen.add(name)
    else: raise SystemExit('ERROR: ruta o estado inesperado en Git: '+repr((flag,name)))
if not {'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
        'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs'} <= seen:
    raise SystemExit('ERROR: faltan cambios previos de v2 y NavMesh Fix')
if phase=='after':
    mandatory={'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs',
               'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorShotgun.cs',
               'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorGameManager.cs'}
    if not mandatory <= seen: raise SystemExit('ERROR: faltan ficheros v3')
print('HBB3_STATUS_OK: '+str(len(items))+' cambios locales autorizados; fase '+phase)
PY
}

check(){
  need git; need python3; need sha256sum; need cp; need cmp
  for file in "$SCENE" "$PLAYER" "$SAMPLE" "$NAV" "$AI" "$ANIM" "$RUNTIME" "$UNITY" \
     "$PROY/Assets/Editor/HorrorBuilderV2/HorrorBuilderV2.cs" \
     "$PROY/Assets/Editor/HorrorNavMeshStartFix/HorrorNavMeshStartFix.cs" \
     "$DIR/src/Editor/HorrorBuilderV3.cs"; do
    [[ -f "$file" ]] || fail "No existe: $file"
  done
  for code in HorrorGameManager HorrorShotgun HorrorPlayerHealth HorrorEnemyHealth; do
    [[ -f "$DIR/src/Scripts/$code.cs" ]] || fail "Falta fuente: $code"
    [[ ! -e "$PROY/Assets/Laberintoa/Scripts/$code.cs" ]] || fail "v3 ya instalado: $code"
    [[ ! -e "$PROY/Assets/Laberintoa/Scripts/$code.cs.meta" ]] || fail "Existe meta antigua: $code"
  done
  for dest in "$EDITOR" "$EDITOR.meta" "$TOOLS"; do
    [[ ! -e "$dest" ]] || fail "v3 parece ya instalado: $dest"
  done
  [[ "$(git -C "$BASE" rev-parse --show-toplevel)" = "$BASE" ]] || fail 'Directorio público incorrecto'
  [[ "$(git -C "$PRIV" rev-parse --show-toplevel)" = "$PRIV" ]] || fail 'Directorio privado incorrecto'
  [[ "$(git -C "$BASE" branch --show-current)" = 'feature/laberintoa-horror' ]] || fail 'Rama pública incorrecta'
  [[ "$(git -C "$PRIV" branch --show-current)" = 'main' ]] || fail 'Rama privada incorrecta'
  [[ "$(git -C "$BASE" rev-parse --short=8 HEAD)" = 'fd55fd79' ]] || fail 'Commit público cambiado; auditar manualmente'
  [[ "$(git -C "$PRIV" rev-parse --short=7 HEAD)" = 'f7a47d7' ]] || fail 'Commit privado cambiado; auditar manualmente'
  [[ "$(git -C "$BASE" rev-parse HEAD)" = "$(git -C "$BASE" rev-parse origin/feature/laberintoa-horror)" ]] || fail 'Público no sincronizado'
  [[ "$(git -C "$PRIV" rev-parse HEAD)" = "$(git -C "$PRIV" rev-parse origin/main)" ]] || fail 'Privado no sincronizado'
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'Privado contiene cambios'
  [[ -z "$(git -C "$BASE" diff --cached --name-only)" ]] || fail 'Público tiene archivos staged'
  check_status before
  python3 - "$PLAYER" <<'PY'
from pathlib import Path
import sys
s=Path(sys.argv[1]).read_text()
needle='Debug.Log("HORROR_PLAYER_HIT: bizitzen sistema oraindik egiteke");'
if s.count(needle)!=1: raise SystemExit('ERROR: movimiento jugador distinto al v1; no se parchea')
print('OK: PerderVida original identificado; solo se cambiará ese método')
PY
}

rollback(){
  local code="$?"
  [[ "$SUCCESS" == 0 && -n "$BACKUP" ]] || return "$code"
  echo 'HBB3_ROLLBACK: restaurando SOLO escena y movimiento previos; nuevo código v3 fuera.' >&2
  cp -a "$BACKUP/Laberintoa_Horror.unity" "$SCENE"
  cp -a "$BACKUP/HorrorPlayerMovement.cs" "$PLAYER"
  rm -rf -- "$EDITOR" "$EDITOR.meta" "$TOOLS"
  for code in HorrorGameManager HorrorShotgun HorrorPlayerHealth HorrorEnemyHealth; do
    rm -f -- "$PROY/Assets/Laberintoa/Scripts/$code.cs" \
              "$PROY/Assets/Laberintoa/Scripts/$code.cs.meta"
  done
  sha256sum -c "$BACKUP/protegidos.sha256" || echo 'AVISO: archivo protegido modificado; no reintentar.' >&2
  echo "Respaldo y Unity log: $BACKUP" >&2
  return "$code"
}

apply(){
  check
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null; then
    fail 'Unity abierto: File > Save y File > Exit primero'
  fi
  [[ ! -e "$PROY/Library/UnityLockfile" ]] || fail 'UnityLockfile presente; cierra Unity'
  echo '=== 1. RESPALDO LOCAL DEL ESTADO V2 + FIX ==='
  BACKUP="$(mktemp -d "$HOME/HorrorBuilderV3_XXXXXXXX")"
  cp -a "$SCENE" "$BACKUP/Laberintoa_Horror.unity"
  cp -a "$PLAYER" "$BACKUP/HorrorPlayerMovement.cs"
  sha256sum "$SAMPLE" "$NAV" "$AI" "$ANIM" "$RUNTIME" \
    "$PROY/Assets/Editor/HorrorBuilderV2/HorrorBuilderV2.cs" > "$BACKUP/protegidos.sha256"
  git -C "$BASE" status --short --branch > "$BACKUP/estado_antes.txt"
  echo "Respaldo: $BACKUP"
  trap rollback EXIT

  echo '=== 2. INSTALAR LOS CINCO SCRIPTS V3 ==='
  mkdir -p "$EDITOR" "$TOOLS"
  cp -a "$DIR/src" "$DIR/docs" "$TOOLS/"
  cp "$DIR/horror-builder-v3.sh" "$TOOLS/horror-builder-v3.sh"
  cp "$DIR/src/Editor/HorrorBuilderV3.cs" "$EDITOR/HorrorBuilderV3.cs"
  for code in HorrorGameManager HorrorShotgun HorrorPlayerHealth HorrorEnemyHealth; do
    cp "$DIR/src/Scripts/$code.cs" "$PROY/Assets/Laberintoa/Scripts/$code.cs"
  done
  python3 - "$PLAYER" <<'PY'
from pathlib import Path
import sys
p=Path(sys.argv[1]);s=p.read_text()
a='Debug.Log("HORROR_PLAYER_HIT: bizitzen sistema oraindik egiteke");'
b='HorrorPlayerHealth hp = GetComponent<HorrorPlayerHealth>();\n        if (hp != null) hp.TakeHit();'
if s.count(a)!=1: raise SystemExit('ERROR: PerderVida no se puede adaptar con seguridad')
p.write_text(s.replace(a,b))
print('HBB3_LIVES_PATCH_OK: PerderVida redirige al nuevo contador de vidas')
PY

  echo '=== 3. UNITY BATCH: CONSTRUIR Y VALIDAR ==='
  local code=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV3.Apply -logFile "$BACKUP/unity.log" || code=$?
  grep -E 'HBB3_|error CS[0-9]+|Exception|Error:' "$BACKUP/unity.log" | tail -n 55 || true
  [[ "$code" -eq 0 ]] || fail "Unity terminó con $code; revisar $BACKUP/unity.log"
  grep -q 'HBB3_PRECHECK_OK:' "$BACKUP/unity.log" || fail 'Falta HBB3_PRECHECK_OK'
  grep -q 'HBB3_OK:' "$BACKUP/unity.log" || fail 'Falta HBB3_OK'

  echo '=== 4. VERIFICAR PROTECCIONES ==='
  sha256sum -c "$BACKUP/protegidos.sha256" || fail 'Se modificó algún asset o script protegido'
  for code in HorrorGameManager HorrorShotgun HorrorPlayerHealth HorrorEnemyHealth; do
    [[ -f "$PROY/Assets/Laberintoa/Scripts/$code.cs.meta" ]] || fail "Unity no importó $code"
  done
  [[ -f "$EDITOR/HorrorBuilderV3.cs.meta" ]] || fail 'Editor V3 no importado'
  git -C "$BASE" -c core.whitespace=-blank-at-eol diff --check
  check_status after
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'El repo privado dejó de estar limpio'

  SUCCESS=1
  echo 'HBB3_LOCAL_OK: jugabilidad instalada. SIN COMMIT NI PUSH.'
  echo "Respaldo/log: $BACKUP"
  echo 'Abrir Unity. Play. Comprobar HORROR_NAV_READY y probar E, clic, R, vidas y victoria.'
}

case "$MODE" in
  --audit|--dry-run)
    echo '=== HORROR BUILDER V3 · AUDITORÍA SIN MODIFICACIONES ==='
    check
    echo 'Preparamos escopeta, munición, salud, HUD y fin del juego. No se toca NavMesh ni gráficos.'
    echo 'HBB3_AUDIT_OK';;
  --apply) apply;;
  --help|-h) echo 'Uso: horror-builder-v3.sh --audit | --apply';;
  *) fail 'Opción inválida';;
esac
