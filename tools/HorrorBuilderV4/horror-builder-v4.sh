#!/usr/bin/env bash
set -euo pipefail

# Horror Builder v4: auditoria, respaldo y cambio LOCAL. Nunca hace git add/commit/push.
BASE="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$BASE/Laberintoa3D_Final"
PRIV="$HOME/Projects/Laberintoa3D_Horror_Backup_Privado"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
SCENE="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
SAMPLE="$PROY/Assets/Laberintoa/Scenes/SampleScene.unity"
NAV="$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset"
SCRIPTS="$PROY/Assets/Laberintoa/Scripts"
GAME="$SCRIPTS/HorrorGameManager.cs"
GUN="$SCRIPTS/HorrorShotgun.cs"
JUMP="$SCRIPTS/HorrorPlayerJump.cs"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV4"
MATERIALS="$PROY/Assets/Laberintoa/Horror/BuilderV4"
TOOLS="$BASE/tools/HorrorBuilderV4"
HERE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODE="${1:---audit}"
BACKUP=""
SUCCESS=0

fail(){ echo "ERROR: $*" >&2; exit 1; }
need(){ command -v "$1" >/dev/null || fail "Falta el comando: $1"; }

check_status(){
  python3 - "$BASE" "$1" <<'PY'
import subprocess,sys
repo,phase=sys.argv[1:]
raw=subprocess.check_output(['git','-C',repo,'status','--porcelain=v1','-z','--untracked-files=all'])
items=[x for x in raw.split(b'\0') if x]
modified={
'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs',
}
prefixes=(
'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV2',
'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV3',
'Laberintoa3D_Final/Assets/Editor/HorrorNavMeshStartFix',
'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV2',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorGameManager.cs',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorShotgun.cs',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerHealth.cs',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs',
'tools/HorrorBuilderV2/',
'tools/HorrorBuilderV3/',
)
if phase=='after':
    prefixes+= (
      'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV4',
      'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV4',
      'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs',
      'tools/HorrorBuilderV4/',
    )
seen=set()
for item in items:
    if len(item)<4: raise SystemExit('ERROR: Git status corrupto')
    flag=item[:2].decode(); name=item[3:].decode('utf-8')
    if flag not in (' M','??') or (name not in modified and not name.startswith(prefixes)):
        raise SystemExit('ERROR: ruta/estado no autorizado: '+repr((flag,name)))
    seen.add(name)
required={
'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorGameManager.cs',
'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorShotgun.cs',
}
if not required <= seen: raise SystemExit('ERROR: falta el estado local original de v3: '+repr(sorted(required-seen)))
if phase=='after' and not any('/HorrorBuilderV4/HorrorBuilderV4.cs' in n for n in seen):
    raise SystemExit('ERROR: no se encuentra el BuilderV4 nuevo')
print(f'HBB4_STATUS_OK: {len(items)} ficheros locales autorizados; {phase}')
PY
}

check(){
  need git; need python3; need sha256sum; need cmp
  for f in "$SCENE" "$SAMPLE" "$NAV" "$GAME" "$GUN" "$UNITY" \
           "$SCRIPTS/HorrorPlayerMovement.cs" "$SCRIPTS/HorrorNavMeshRuntime.cs" \
           "$SCRIPTS/HorrorEnemyAI.cs" "$SCRIPTS/HorrorEnemyAnimation.cs" \
           "$SCRIPTS/HorrorEnemyHealth.cs" "$SCRIPTS/HorrorPlayerHealth.cs" \
           "$PROY/Assets/Laberintoa/Horror/BuilderV2/HB2_Concrete.mat" \
           "$PROY/Assets/Laberintoa/Horror/BuilderV2/HB2_ForestFloor.mat" \
           "$HERE/src/Editor/HorrorBuilderV4.cs" "$HERE/src/Scripts/HorrorPlayerJump.cs" \
           "$HERE/src/Scripts/HorrorGameManager.cs" "$HERE/src/Scripts/HorrorShotgun.cs" \
           "$HERE/reference/HorrorGameManager.cs" "$HERE/reference/HorrorShotgun.cs"; do
    [[ -f "$f" ]] || fail "No existe: $f"
  done
  for f in "$EDITOR" "$EDITOR.meta" "$MATERIALS" "$MATERIALS.meta" \
           "$TOOLS" "$JUMP" "$JUMP.meta"; do
    [[ ! -e "$f" ]] || fail "Builder v4 ya instalado o con restos: $f"
  done
  [[ "$(git -C "$BASE" rev-parse --show-toplevel)" = "$BASE" ]] || fail 'Repo publico inesperado'
  [[ "$(git -C "$PRIV" rev-parse --show-toplevel)" = "$PRIV" ]] || fail 'Repo privado inesperado'
  [[ "$(git -C "$BASE" branch --show-current)" = 'feature/laberintoa-horror' ]] || fail 'Rama publica incorrecta'
  [[ "$(git -C "$PRIV" branch --show-current)" = 'main' ]] || fail 'Rama privada incorrecta'
  [[ "$(git -C "$BASE" rev-parse --short=8 HEAD)" = fd55fd79 ]] || fail 'Commit publico cambiado; auditar antes'
  [[ "$(git -C "$PRIV" rev-parse --short=7 HEAD)" = f7a47d7 ]] || fail 'Commit privado cambiado; auditar antes'
  [[ "$(git -C "$BASE" rev-parse HEAD)" = "$(git -C "$BASE" rev-parse origin/feature/laberintoa-horror)" ]] || fail 'Publico sin sincronizar'
  [[ "$(git -C "$PRIV" rev-parse HEAD)" = "$(git -C "$PRIV" rev-parse origin/main)" ]] || fail 'Privado sin sincronizar'
  [[ -z "$(git -C "$BASE" diff --cached --name-only)" ]] || fail 'Publico con staging inesperado'
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'Privado con cambios locales'
  check_status before
  cmp -s "$GAME" "$HERE/reference/HorrorGameManager.cs" || fail 'HorrorGameManager distinto del v3 original. No se sobrescribe.'
  cmp -s "$GUN" "$HERE/reference/HorrorShotgun.cs" || fail 'HorrorShotgun distinto del v3 original. No se sobrescribe.'
  [[ ! -f "$PROY/Assets/Editor/HorrorBuilderV4/HorrorBuilderV4.cs" ]] || fail 'BuilderV4 preexistente'
  echo 'HBB4_PREAUDIT_OK: UI v3 original verificada; ninguna modificacion ajena ni stage'
}

rollback(){
  local code="$?"
  [[ "$SUCCESS" = 0 && -n "$BACKUP" ]] || return "$code"
  echo 'HBB4_ROLLBACK: restableciendo SOLO escena y scripts de v3; retirando nuevos recursos v4.' >&2
  cp -a "$BACKUP/Laberintoa_Horror.unity" "$SCENE"
  cp -a "$BACKUP/HorrorGameManager.cs" "$GAME"
  cp -a "$BACKUP/HorrorShotgun.cs" "$GUN"
  rm -rf -- "$EDITOR" "$MATERIALS" "$TOOLS"
  rm -f -- "$EDITOR.meta" "$MATERIALS.meta" "$JUMP" "$JUMP.meta"
  sha256sum -c "$BACKUP/protegidos.sha256" || echo 'AVISO: hash protegido distinto. Revisar antes de repetir.' >&2
  echo "Respaldo y log: $BACKUP" >&2
  return "$code"
}

apply(){
  check
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null; then
    fail 'Unity abierto: guardar escena y File > Exit antes de aplicar'
  fi
  [[ ! -e "$PROY/Library/UnityLockfile" ]] || fail 'UnityLockfile: cierra Unity correctamente'

  echo '=== 1. RESPALDO LOCAL DE V3 ==='
  BACKUP="$(mktemp -d "$HOME/HorrorBuilderV4_XXXXXXXX")"
  cp -a "$SCENE" "$BACKUP/Laberintoa_Horror.unity"
  cp -a "$GAME" "$BACKUP/HorrorGameManager.cs"
  cp -a "$GUN" "$BACKUP/HorrorShotgun.cs"
  sha256sum "$SAMPLE" "$NAV" \
    "$SCRIPTS/HorrorPlayerMovement.cs" "$SCRIPTS/HorrorNavMeshRuntime.cs" \
    "$SCRIPTS/HorrorEnemyAI.cs" "$SCRIPTS/HorrorEnemyAnimation.cs" \
    "$SCRIPTS/HorrorEnemyHealth.cs" "$SCRIPTS/HorrorPlayerHealth.cs" \
    "$PROY/Assets/Editor/HorrorBuilderV3/HorrorBuilderV3.cs" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV2/HB2_Concrete.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV2/HB2_ForestFloor.mat" \
    > "$BACKUP/protegidos.sha256"
  git -C "$BASE" status --short --branch > "$BACKUP/estado_previo.txt"
  echo "Respaldo: $BACKUP"
  trap rollback EXIT

  echo '=== 2. INSTALAR SOLO SCRIPTS Y MATERIAL BASE V4 ==='
  mkdir -p "$EDITOR" "$MATERIALS" "$TOOLS"
  cp "$HERE/src/Editor/HorrorBuilderV4.cs" "$EDITOR/HorrorBuilderV4.cs"
  cp "$HERE/src/Scripts/HorrorPlayerJump.cs" "$JUMP"
  cp "$HERE/src/Scripts/HorrorGameManager.cs" "$GAME"
  cp "$HERE/src/Scripts/HorrorShotgun.cs" "$GUN"
  cp -a "$HERE/src" "$HERE/docs" "$HERE/reference" "$TOOLS/"
  cp "$HERE/horror-builder-v4.sh" "$TOOLS/horror-builder-v4.sh"

  echo '=== 3. UNITY BATCH: CONSTRUCCION Y VALIDACION ==='
  local code=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV4.Apply -logFile "$BACKUP/unity.log" || code=$?
  grep -E 'HBB4_|error CS[0-9]+|Exception|Error:' "$BACKUP/unity.log" | tail -n 45 || true
  [[ "$code" -eq 0 ]] || fail "Unity devolvio codigo $code, ver $BACKUP/unity.log"
  grep -q 'HBB4_PRECHECK_OK:' "$BACKUP/unity.log" || fail 'Falta HBB4_PRECHECK_OK'
  grep -q 'HBB4_OK:' "$BACKUP/unity.log" || fail 'Falta HBB4_OK'

  echo '=== 4. VALIDAR PROTECCIONES Y ARCHIVOS ==='
  sha256sum -c "$BACKUP/protegidos.sha256" || fail 'Un archivo protegido ha cambiado'
  for file in "$EDITOR/HorrorBuilderV4.cs.meta" "$JUMP.meta" \
     "$MATERIALS/HB4_HormigoiZaharra.mat" "$MATERIALS/HB4_BasoLurZabaldua.mat" \
     "$MATERIALS/HB4_LinternaKristala.mat"; do
    [[ -f "$file" ]] || fail "No generado: $file"
  done
  git -C "$BASE" -c core.whitespace=-blank-at-eol diff --check
  check_status after
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'El privado dejo de estar limpio'
  SUCCESS=1
  echo 'HBB4_LOCAL_OK: salto, linterna, paredes, dos salidas traseras cerradas, suelo y HUD euskaraz.'
  echo 'SIN COMMIT NI PUSH. Probar en Unity antes de guardar los repositorios.'
  echo "Respaldo/log: $BACKUP"
  git -C "$BASE" status --short --branch | tail -n 26
}

case "$MODE" in
  --audit|--dry-run)
    echo '=== HORROR BUILDER V4 · AUDITORIA SOLO LECTURA ==='
    check
    echo 'Objetivos: ESPACIO salto, linterna visible, paredes, 2 aberturas traseras, suelo, textos euskaraz.'
    echo 'No modifica NavMesh, SampleScene, IA ni GitHub.'
    echo 'HBB4_AUDIT_OK';;
  --apply) apply;;
  --help|-h) echo 'Uso: bash horror-builder-v4.sh --audit | --apply';;
  *) fail 'Opcion invalida';;
esac
