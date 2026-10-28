#!/usr/bin/env bash
set -euo pipefail
export GIT_PAGER=cat PAGER=cat GIT_TERMINAL_PROMPT=0

# Horror Builder V12 (paquete corregido) — Hasiera/menu nagusia eta etsaien barrako testu txikiagoa.
# V11 jokagarritasuna ez aldatu, SampleScene eta Horror eszena ez ukitu.
ASIG="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUB="$ASIG/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUB/Laberintoa3D_Final"
PRIV="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado"
SNAP="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
AQUI="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
HEALTH="$PROY/Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs"
GAME="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
MENU="$PROY/Assets/Laberintoa/Scenes/Hasiera.unity"
BUILD="$PROY/ProjectSettings/EditorBuildSettings.asset"
SCRIPTS="$PROY/Assets/Laberintoa/Scripts"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV12"
MENU_CODE="$SCRIPTS/HorrorMainMenuV12.cs"
RESOURCES="$PROY/Assets/Laberintoa/Horror/BuilderV12"
TOOLS="$PUB/tools/HorrorBuilderV12"
MODE="${1:---audit}"
BACKUP=""
DONE=0

fail() { echo "HBB12_ERROR: $*" >&2; exit 1; }
sha_protegidos() {
  sha256sum \
    "$GAME" \
    "$PROY/Assets/Laberintoa/Scenes/SampleScene.unity" \
    "$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset" \
    "$SCRIPTS/HorrorPlayerMovement.cs" \
    "$SCRIPTS/HorrorPlayerJump.cs" \
    "$SCRIPTS/HorrorMouseLook.cs" \
    "$SCRIPTS/HorrorGameManager.cs" \
    "$SCRIPTS/HorrorShotgun.cs" \
    "$SCRIPTS/HorrorHUDV10.cs" \
    "$SCRIPTS/HorrorAtmosphereV9.cs" \
    "$SCRIPTS/HorrorBunkerDarknessV10.cs" \
    "$SCRIPTS/HorrorFlashlight.cs" \
    "$SCRIPTS/HorrorViewmodelSafety.cs" \
    "$SCRIPTS/HorrorEnemyAI.cs" \
    "$SCRIPTS/HorrorNavMeshRuntime.cs" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat" \
    "$PROY/ProjectSettings/TagManager.asset" \
    "$PROY/Packages/manifest.json"
}
preaudit() {
  for cmd in git gh python3 sha256sum cmp grep pgrep mktemp cp; do
    command -v "$cmd" >/dev/null || fail "Falta herramienta: $cmd"
  done
  [[ -f "$UNITY" && -f "$BUILD" && -f "$HEALTH" && -f "$GAME" ]] || fail 'Unity o proyecto incompleto'
  [[ -d "$PUB/.git" && -d "$PRIV/.git" ]] || fail 'Repositorio Git inexistente'
  [[ "$(git -C "$PUB" branch --show-current)" == feature/laberintoa-horror ]] || fail 'Rama pública incorrecta'
  [[ "$(git -C "$PRIV" branch --show-current)" == main ]] || fail 'Rama privada incorrecta'
  [[ "$(git -C "$PUB" rev-parse --short=8 HEAD)" == df68bc64 ]] || fail 'Commit público diferente de df68bc64'
  [[ -z "$(git -C "$PUB" status --porcelain --untracked-files=all)" ]] || fail 'Repo público no está limpio'
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'Repo privado no está limpio'
  [[ "$(gh repo view MasterCoderLab/GarapenTeknologiaAurreratuak_Unity --json isPrivate --jq '.isPrivate')" == false ]] || fail 'Público no verificado'
  [[ "$(gh repo view MasterCoderLab/Laberintoa3D_Horror_Backup_Privado --json isPrivate --jq '.isPrivate')" == true ]] || fail 'Privado no verificado'
  [[ "$(git -C "$PUB" remote get-url origin)" == 'https://github.com/MasterCoderLab/GarapenTeknologiaAurreratuak_Unity.git' ]] || fail 'Remote público inesperado'
  [[ "$(git -C "$PRIV" remote get-url origin)" == 'https://github.com/MasterCoderLab/Laberintoa3D_Horror_Backup_Privado.git' ]] || fail 'Remote privado inesperado'
  [[ "$(git -C "$PUB" rev-parse HEAD)" == "$(git -C "$PUB" ls-remote origin refs/heads/feature/laberintoa-horror | cut -f1)" ]] || fail 'GitHub público no sincronizado'
  [[ "$(git -C "$PRIV" rev-parse HEAD)" == "$(git -C "$PRIV" ls-remote origin refs/heads/main | cut -f1)" ]] || fail 'GitHub privado no sincronizado'
  [[ ! -e "$MENU" && ! -e "$MENU.meta" && ! -e "$MENU_CODE" && ! -e "$MENU_CODE.meta" && ! -e "$EDITOR" && ! -e "$EDITOR.meta" && ! -e "$RESOURCES" && ! -e "$RESOURCES.meta" && ! -e "$TOOLS" ]] || fail 'Ya hay archivos V12; no repetir'
  [[ -f "$AQUI/src/Editor/HorrorBuilderV12.cs" && -f "$AQUI/src/Scripts/HorrorMainMenuV12.cs" ]] || fail 'Builder descargado incompleto'
  python3 - "$HEALTH" "$BUILD" <<'PY'
from pathlib import Path
import sys
health = Path(sys.argv[1]).read_text()
build = Path(sys.argv[2]).read_text()
if health.count('"ETSAIA  " + hitPoints + "/3"') != 1:
    sys.exit('HBB12_ERROR: la etiqueta de salud ha cambiado; no modificar')
if 'Assets/Laberintoa/Scenes/Hasiera.unity' in build:
    sys.exit('HBB12_ERROR: Hasiera ya está en el Build Settings')
if build.count('path: Assets/Laberintoa/Scenes/Laberintoa_Horror.unity') != 1:
    sys.exit('HBB12_ERROR: build settings de escena de juego inesperada')
print('HBB12_LABEL_AUDIT_OK: Etsaia 3/3 identificado')
PY
  # Egiaztatu ZIP honen iturburuko bi C# fitxategiak osorik daudela.
  (cd "$AQUI" && sha256sum --check SHA256SUMS) || fail 'Iturburuak ez datoz bat ZIP zuzenduan'
  python3 - "$AQUI/src/Editor/HorrorBuilderV12.cs" <<'PYV12'
from pathlib import Path
import sys
s=Path(sys.argv[1]).read_text()
expected=[
    'foreach (GameObject enemyRoot in game.GetRootGameObjects())',
    'enemies += enemyRoot.GetComponentsInChildren<HorrorEnemyHealth>(true).Length;',
    'GameObject root = new GameObject("HB12_Gaueko_Basoa");',
]
if any(s.count(item)!=1 for item in expected) or 'foreach (GameObject root in game.GetRootGameObjects())' in s:
    sys.exit('HBB12_ERROR: CS0136 zuzenketa ez dago egiaztatuta')
print('HBB12_SOURCE_FIX_OK: GameObject enemyRoot + menu root bereizita')
PYV12
  sha_protegidos >/dev/null || fail 'Archivo protegido inexistente'
  echo 'HBB12_STATUS_OK: ambos GitHub limpios; v11 guardado; v12 ausente.'
  echo 'HBB12_PREAUDIT_OK: nueva Hasiera + opciones/volumen + texto Etsaia, sin tocar gameplay.'
}
rollback() {
  local rc=$?
  trap - EXIT
  if [[ "$DONE" == 0 && -n "$BACKUP" ]]; then
    echo 'HBB12_ROLLBACK: restaurar EnemyHealth y Build Settings; retirar SOLO archivos nuevos V12.' >&2
    cp -a "$BACKUP/HorrorEnemyHealth.cs" "$HEALTH" || true
    cp -a "$BACKUP/EditorBuildSettings.asset" "$BUILD" || true
    rm -f -- "$MENU" "$MENU.meta" "$MENU_CODE" "$MENU_CODE.meta"
    rm -rf -- "$EDITOR" "$RESOURCES" "$TOOLS"
    rm -f -- "$EDITOR.meta" "$RESOURCES.meta"
    sha256sum -c "$BACKUP/protegidos.sha256" || true
    echo "HBB12_ERROR_LOG: $BACKUP/unity.log" >&2
    echo 'HBB12_RESULTADO: ERROR — rollback de v12 aplicado.' >&2
  fi
  return "$rc"
}
check_changes() {
  python3 - "$PUB" <<'PY'
import pathlib, subprocess, sys
repo=pathlib.Path(sys.argv[1])
raw=subprocess.check_output(['git','-C',str(repo),'status','--porcelain=v1','-z','--untracked-files=all'])
rows=[x for x in raw.split(b'\0') if x]
allow_files={
    'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs',
    'Laberintoa3D_Final/ProjectSettings/EditorBuildSettings.asset',
    'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Hasiera.unity',
    'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Hasiera.unity.meta',
    'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorMainMenuV12.cs',
    'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorMainMenuV12.cs.meta',
    'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV12.meta',
    'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV12.meta',
}
allow_prefixes=[
    'Laberintoa3D_Final/Assets/Editor/HorrorBuilderV12/',
    'Laberintoa3D_Final/Assets/Laberintoa/Horror/BuilderV12/',
    'tools/HorrorBuilderV12/',
]
paths=[]
for r in rows:
    code=r[:2].decode()
    path=r[3:].decode('utf-8','surrogateescape')
    if code not in (' M','??') or not(path in allow_files or any(path.startswith(x) for x in allow_prefixes)):
        sys.exit('HBB12_ERROR: cambio inesperado: '+code+' '+path)
    paths.append(path)
for required in ['Laberintoa3D_Final/ProjectSettings/EditorBuildSettings.asset', 'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs','Laberintoa3D_Final/Assets/Laberintoa/Scenes/Hasiera.unity']:
    if required not in paths:sys.exit('HBB12_ERROR: cambio esencial no encontrado: '+required)
print('HBB12_STATUS_OK: '+str(len(paths))+' archivos V12 permitidos')
PY
}
apply() {
  preaudit
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || [[ -e "$PROY/Library/UnityLockfile" ]]; then
    fail 'Unity sigue abierto; guarda y cierra antes de aplicar'
  fi
  echo '=== 1. RESPALDO LOCAL V11 ==='
  mkdir -p "$SNAP"
  BACKUP="$(mktemp -d "$SNAP/HorrorBuilderV12_XXXXXXXX")"
  cp -a "$HEALTH" "$BACKUP/HorrorEnemyHealth.cs"
  cp -a "$BUILD" "$BACKUP/EditorBuildSettings.asset"
  sha_protegidos > "$BACKUP/protegidos.sha256"
  git -C "$PUB" log -1 --oneline > "$BACKUP/HEAD_antes.txt"
  trap rollback EXIT
  trap 'exit 130' INT
  echo "Respaldo: $BACKUP"
  echo '=== 2. INSTALAR V12: MENU Y TEXTO ETSAIA ==='
  python3 - "$HEALTH" <<'PY'
from pathlib import Path
import sys
p=Path(sys.argv[1]);s=p.read_text()
a='"ETSAIA  " + hitPoints + "/3"'
if s.count(a)!=1:sys.exit('HBB12_ERROR: etiqueta no identificada')
p.write_text(s.replace(a,'"ETSAIA"',1))
print('HBB12_LABEL_PATCH_OK: ETSAIA sin 3/3')
PY
  mkdir -p "$EDITOR" "$TOOLS"
  cp "$AQUI/src/Editor/HorrorBuilderV12.cs" "$EDITOR/HorrorBuilderV12.cs"
  cp "$AQUI/src/Scripts/HorrorMainMenuV12.cs" "$MENU_CODE"
  cp -a "$AQUI/src" "$AQUI/docs" "$TOOLS/"
  cp "$AQUI/horror-builder-v12.sh" "$TOOLS/horror-builder-v12.sh"
  echo '=== 3. UNITY BATCH — HASIERA / BUILD SETTINGS ==='
  local rc=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV12.Apply -logFile "$BACKUP/unity.log" || rc=$?
  grep -E 'HBB12_|error CS[0-9]+|Exception|Error:' "$BACKUP/unity.log" | tail -n 40 || true
  [[ "$rc" == 0 ]] || fail "Unity devolvio $rc; revisar $BACKUP/unity.log"
  grep -q 'HBB12_PRECHECK_OK:' "$BACKUP/unity.log" || fail 'Falta HBB12_PRECHECK_OK'
  grep -q 'HBB12_OK:' "$BACKUP/unity.log" || fail 'Falta HBB12_OK'
  echo '=== 4. COMPROBAR PROTEGIDOS Y GIT ==='
  sha256sum -c "$BACKUP/protegidos.sha256" || fail 'Unity ha cambiado un archivo protegido'
  [[ -f "$MENU" && -f "$MENU.meta" && -f "$EDITOR/HorrorBuilderV12.cs.meta" && -f "$MENU_CODE.meta" ]] || fail 'Falta escena/script o .meta'
  [[ -f "$RESOURCES/Meshes/HB12_PinuKonoa.asset" ]] || fail 'Falta malla forestal'
  git -C "$PUB" -c core.whitespace=-blank-at-eol diff --check || fail 'Whitespace en el proyecto'
  check_changes
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fail 'Repositorio privado modificado inesperadamente'
  [[ "$(git -C "$PUB" rev-parse --short=8 HEAD)" == df68bc64 ]] || fail 'Commit público cambiado'
  DONE=1
  echo "Respaldo y log: $BACKUP"
  echo 'HBB12_LOCAL_OK: Hasiera, opciones volumen y ETSAIA sin cifras; Laberintoa_Horror intacto; sin Git.'
  echo 'HBB12_RESULTADO: OK — abrir la escena Hasiera y probar JOLASTU, AUKERAK e IRTEN.'
}
case "$MODE" in
  --audit|--dry-run) echo '=== HORROR BUILDER V12 — SOLO LECTURA ==='; preaudit; echo 'HBB12_AUDIT_OK';;
  --apply) apply;;
  --help|-h) echo 'Uso: bash horror-builder-v12.sh --audit | --apply';;
  *) fail "Opción desconocida: $MODE";;
esac
