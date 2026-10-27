#!/usr/bin/env bash
set -euo pipefail
export GIT_PAGER=cat PAGER=cat

# Horror Builder V11: iluntasun fisikoa + etsai barraren bistaratzearen zuzenketa.
# Ez aldatu tiroa/headshot, HUD, objektuak, materialak, NavMesh edo GitHub.
ASIG="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUB="$ASIG/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUB/Laberintoa3D_Final"
PRIV="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado"
SNAPSHOTS="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
SCRIPTS="$PROY/Assets/Laberintoa/Scripts"
ESCENA="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV11"
TOOLS="$PUB/tools/HorrorBuilderV11"
AQUI="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RESPALDO=""
COMPLETADO=0
MODO="${1:---audit}"
ORIG=(HorrorAtmosphereV9.cs HorrorBunkerDarknessV10.cs HorrorEnemyHealth.cs)

fallar() { echo "HBB11_ERROR: $*" >&2; exit 1; }
protegidos() {
  sha256sum \
    "$ESCENA" \
    "$PROY/Assets/Laberintoa/Scenes/SampleScene.unity" \
    "$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset" \
    "$SCRIPTS/HorrorGameManager.cs" \
    "$SCRIPTS/HorrorShotgun.cs" \
    "$SCRIPTS/HorrorPlayerMovement.cs" \
    "$SCRIPTS/HorrorPlayerJump.cs" \
    "$SCRIPTS/HorrorMouseLook.cs" \
    "$SCRIPTS/HorrorHUDV10.cs" \
    "$SCRIPTS/HorrorFlashlight.cs" \
    "$SCRIPTS/HorrorEnemyAI.cs" \
    "$SCRIPTS/HorrorEnemyAnimation.cs" \
    "$SCRIPTS/HorrorNavMeshRuntime.cs" \
    "$SCRIPTS/HorrorViewmodelSafety.cs" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat" \
    "$PROY/ProjectSettings/TagManager.asset" \
    "$PROY/Packages/manifest.json"
}

# Unicamente consulta: Git, codigos exactos y estado del proyecto.
preauditar() {
  for cmd in git python3 sha256sum cp grep pgrep mktemp; do
    command -v "$cmd" >/dev/null || fallar "Falta herramienta: $cmd"
  done
  [[ -f "$UNITY" && -f "$ESCENA" ]] || fallar 'Unity edo eszena falta da'
  [[ -d "$PRIV/.git" && -d "$PUB/.git" ]] || fallar 'Git biltegi bat falta da'
  [[ "$(git -C "$PUB" branch --show-current)" == feature/laberintoa-horror ]] || fallar 'Rama publikoa okerra'
  [[ "$(git -C "$PRIV" branch --show-current)" == main ]] || fallar 'Rama pribatua okerra'
  [[ "$(git -C "$PUB" rev-parse --short=8 HEAD)" == c5370157 ]] || fallar 'Commit publikoa ez da c5370157'
  [[ "$(git -C "$PRIV" rev-parse --short=7 HEAD)" == b2691f0 ]] || fallar 'Commit pribatua ez da b2691f0'
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fallar 'Pribatua ez dago garbi'
  [[ -z "$(git -C "$PUB" diff --cached --name-only)" ]] || fallar 'Publikoan staged fitxategiak daude'
  [[ "$(git -C "$PUB" rev-parse HEAD)" == "$(git -C "$PUB" ls-remote origin refs/heads/feature/laberintoa-horror | cut -f1)" ]] || fallar 'GitHub publikoan aldeak daude'
  [[ "$(git -C "$PRIV" rev-parse HEAD)" == "$(git -C "$PRIV" ls-remote origin refs/heads/main | cut -f1)" ]] || fallar 'GitHub pribatuan aldeak daude'
  [[ ! -e "$EDITOR" && ! -e "$EDITOR.meta" && ! -e "$TOOLS" ]] || fallar 'V11 lehendik sortuta dago'
  python3 "$AQUI/validate_v10_state.py" "$PUB" after || fallar 'V10eko Git egoera ustekabekoa'
  python3 - "$PUB" <<'PY_COUNT'
import subprocess,sys
raw=subprocess.check_output(['git','-C',sys.argv[1], 'status','--porcelain=v1','-z','--untracked-files=all'])
count=len([r for r in raw.split(b'\0') if r])
if count != 63: sys.exit(f'HBB11_ERROR: 63 aldaketa espero; {count} aurkitu')
print('HBB11_STATUS_OK: 63 aldaketa baimendu; before')
PY_COUNT
  for i in "${ORIG[@]}"; do
    [[ -f "$SCRIPTS/$i" ]] || fallar "Falta script: $i"
    [[ -f "$AQUI/reference/$i" && -f "$AQUI/src/Scripts/$i" ]] || fallar "Falta V11 iturria: $i"
    cmp -s -- "$SCRIPTS/$i" "$AQUI/reference/$i" || fallar "Script aldatuta dago: $i (ez da ukitu)"
    cmp -s -- "$AQUI/src/Scripts/$i" "$AQUI/reference/$i" && fallar "V11 berriak ez du ezer aldatzen: $i"
  done
  [[ -f "$AQUI/src/Editor/HorrorBuilderV11.cs" ]] || fallar 'V11 Editor script falta da'
  protegidos >/dev/null || fallar 'Babestutako fitxategi bat falta da'
  echo 'HBB11_PREAUDIT_OK: 63 aldaketa lokal, V10eko 3 script zehatz eta bi GitHub-ak egiaztatuta.'
}

rollback() {
  local status=$?
  trap - EXIT
  if [[ "$COMPLETADO" == 0 && -n "$RESPALDO" ]]; then
    echo 'HBB11_ROLLBACK: hiru script zaharrak leheneratzen eta V11ko berriak kentzen.' >&2
    for i in "${ORIG[@]}"; do cp -a "$RESPALDO/$i" "$SCRIPTS/$i" || true; done
    # Bertsio honetan eszena ez da inoiz aldatu behar; hala ere, berreskuratu.
    cp -a "$RESPALDO/Laberintoa_Horror.unity" "$ESCENA" || true
    rm -rf -- "$EDITOR" "$TOOLS"
    rm -f -- "$EDITOR.meta"
    sha256sum -c "$RESPALDO/protegidos.sha256" || true
    echo "HBB11_ERROR_LOG: $RESPALDO/unity.log" >&2
    echo 'HBB11_RESULTADO: ERROR — V10era itzuli da; ez errepikatu.' >&2
  fi
  return "$status"
}

aplicar() {
  preauditar
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     [[ -e "$PROY/Library/UnityLockfile" ]]; then
    fallar 'Unity irekita dago; gorde eta itxi aurretik.'
  fi
  echo '=== 1. V10 EGOERAREN BABES-KOPIA ==='
  mkdir -p "$SNAPSHOTS"
  RESPALDO="$(mktemp -d "$SNAPSHOTS/HorrorBuilderV11_XXXXXXXX")"
  cp -a "$ESCENA" "$RESPALDO/Laberintoa_Horror.unity"
  for i in "${ORIG[@]}"; do cp -a "$SCRIPTS/$i" "$RESPALDO/$i"; done
  protegidos > "$RESPALDO/protegidos.sha256"
  python3 - "$PUB" "$RESPALDO/status_before.txt" <<'PY'
from pathlib import Path
import subprocess,sys
raw=subprocess.check_output(['git','-C',sys.argv[1],'status','--porcelain=v1','-z','--untracked-files=all'])
rows=[p for p in raw.split(b'\0') if p]
Path(sys.argv[2]).write_text(''.join(p[:2].decode()+' '+p[3:].decode('utf-8','surrogateescape')+'\n' for p in rows))
PY
  trap rollback EXIT
  trap 'exit 130' INT
  echo "Respaldo: $RESPALDO"
  echo '=== 2. V11 SCRIPT-AK INSTALATU ==='
  for i in "${ORIG[@]}"; do cp "$AQUI/src/Scripts/$i" "$SCRIPTS/$i"; done
  mkdir -p "$EDITOR" "$TOOLS"
  cp "$AQUI/src/Editor/HorrorBuilderV11.cs" "$EDITOR/HorrorBuilderV11.cs"
  cp -a "$AQUI/src" "$AQUI/reference" "$AQUI/docs" "$TOOLS/"
  cp "$AQUI/horror-builder-v11.sh" "$AQUI/validate_v10_state.py" "$AQUI/verify_changes.py" "$TOOLS/"

  echo '=== 3. UNITY BATCH — ARGIA ETA BARRAK ==='
  local code=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV11.Verify -logFile "$RESPALDO/unity.log" || code=$?
  grep -E 'HBB11_|error CS[0-9]+|Exception|Error:' "$RESPALDO/unity.log" | tail -n 50 || true
  [[ "$code" == 0 ]] || fallar "Unity devolvio $code; revisar $RESPALDO/unity.log"
  grep -q 'HBB11_PRECHECK_OK:' "$RESPALDO/unity.log" || fallar 'Falta HBB11_PRECHECK_OK'
  grep -q 'HBB11_OK:' "$RESPALDO/unity.log" || fallar 'Falta HBB11_OK'

  echo '=== 4. KODEA, BABESAK ETA GIT ==='
  sha256sum -c "$RESPALDO/protegidos.sha256" || fallar 'Babestutako fitxategi bat aldatu da'
  [[ -f "$EDITOR/HorrorBuilderV11.cs.meta" && -f "$EDITOR.meta" ]] || fallar 'V11eko Unity .meta fitxategiak falta dira'
  git -C "$PUB" -c core.whitespace=-blank-at-eol diff --check || fallar 'Whitespace error'
  python3 "$AQUI/verify_changes.py" "$PUB" "$RESPALDO/status_before.txt" "$TOOLS" || fallar 'Git fitxategiak aldatu dira'
  [[ -z "$(git -C "$PRIV" status --porcelain --untracked-files=all)" ]] || fallar 'Pribatua aldatu da'
  COMPLETADO=1
  echo 'HBB11_LOCAL_OK: pantaila-beltz uniforme kenduta; lainoa+ambient benetakoak; etsaien barra toraxetik kalkulatuta.'
  echo "Respaldo y log: $RESPALDO"
  echo 'HBB11_RESULTADO: OK — Play moduan argia eta 3 etsaien barrak egiaztatu. GitHub ez da aldatu.'
}

case "$MODO" in
  --audit|--dry-run) echo '=== HORROR BUILDER V11 — AUDITORIA SOLO LECTURA ==='; preauditar; echo 'HBB11_AUDIT_OK';;
  --apply) aplicar;;
  --help|-h) echo 'Uso: bash horror-builder-v11.sh --audit | --apply';;
  *) fallar "Opcion desconocida: $MODO";;
esac
