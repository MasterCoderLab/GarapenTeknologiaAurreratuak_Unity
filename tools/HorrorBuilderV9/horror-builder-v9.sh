#!/usr/bin/env bash
set -euo pipefail
export GIT_PAGER=cat PAGER=cat

# V9: A/D albo-mugimendua + bunkerra ilunago + kanpoko linterna biziago.
# V7/V8 lokalak mantendu, SampleScene/NavMesh eta GitHub EZ ukitu.
ASIG="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUBLICO="$ASIG/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUBLICO/Laberintoa3D_Final"
PRIVADO="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado"
SNAPSHOTS="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
ESCENA="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
MOVIMIENTO="$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs"
REL_MOVIMIENTO="Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs"
ATMOSFERA="$PROY/Assets/Laberintoa/Scripts/HorrorAtmosphereV9.cs"
EDITOR_V9="$PROY/Assets/Editor/HorrorBuilderV9"
TOOLS_V9="$PUBLICO/tools/HorrorBuilderV9"
AQUI="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODO="${1:---audit}"
RESPALDO=""
TERMINADO=0

fallar() { echo "HBB9_ERROR: $*" >&2; exit 1; }
comando() { command -v "$1" >/dev/null 2>&1 || fallar "Falta herramienta: $1"; }

# Git egoeraren zerrenda zuri zorrotza. 26 aldaketa aurretik V7/V8tik datoz.
check_status() {
  python3 - "$PUBLICO" "$1" <<'PY_STATUS'
import subprocess, sys
repo, phase = sys.argv[1:]
raw = subprocess.check_output(['git', '-C', repo, 'status', '--porcelain=v1', '-z', '--untracked-files=all'])
rows = [r for r in raw.split(b'\0') if r]
base = 'Laberintoa3D_Final/'
scene = base + 'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity'
move = base + 'Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs'
v7_exact = {scene, base+'Assets/Editor/HorrorBuilderV7.meta',
    base+'Assets/Laberintoa/Horror/BuilderV7.meta',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs.meta'}
v7_prefix = [base+'Assets/Editor/HorrorBuilderV7/',
    base+'Assets/Laberintoa/Horror/BuilderV7/', 'tools/HorrorBuilderV7/']
v8_exact = {base+'Assets/Editor/HorrorBuilderV8.meta'}
v8_prefix = [base+'Assets/Editor/HorrorBuilderV8/', 'tools/HorrorBuilderV8/']
v9_exact = {move, base+'Assets/Editor/HorrorBuilderV9.meta',
    base+'Assets/Laberintoa/Scripts/HorrorAtmosphereV9.cs',
    base+'Assets/Laberintoa/Scripts/HorrorAtmosphereV9.cs.meta'}
v9_prefix = [base+'Assets/Editor/HorrorBuilderV9/', 'tools/HorrorBuilderV9/']
if phase not in ('before','after'):
    sys.exit('HBB9_ERROR: fase ezezaguna')
seen=set()
for row in rows:
    flag=row[:2].decode('ascii')
    name=row[3:].decode('utf-8','surrogateescape')
    if flag not in (' M','??'):
        sys.exit('HBB9_ERROR: Git egoera ustekabekoa '+repr((flag,name)))
    if name in (scene,move):
        if flag!=' M': sys.exit('HBB9_ERROR: erregistroa ez da tracked '+name)
    elif flag!='??':
        sys.exit('HBB9_ERROR: aldatutako jatorrizko fitxategi ezezaguna '+name)
    allowed = (name in v7_exact or name in v8_exact or
        any(name.startswith(p) for p in v7_prefix+v8_prefix) or
        (phase=='after' and (name in v9_exact or any(name.startswith(p) for p in v9_prefix))))
    if not allowed: sys.exit('HBB9_ERROR: fitxategi ez-baimendua '+name)
    seen.add(name)
required = {scene,
    base+'Assets/Editor/HorrorBuilderV7/HorrorBuilderV7.cs',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
    base+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLaburra.mat',
    base+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaErtaina.mat',
    base+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLuzea.mat',
    base+'Assets/Editor/HorrorBuilderV8/HorrorBuilderV8.cs',
    'tools/HorrorBuilderV7/horror-builder-v7.sh',
    'tools/HorrorBuilderV8/horror-builder-v8.sh'}
if not required.issubset(seen):
    sys.exit('HBB9_ERROR: V7/V8 osatu gabe: '+repr(sorted(required-seen)))
if phase=='before':
    if len(seen)!=26: sys.exit(f'HBB9_ERROR: V8k 26 aldaketa zituen, {len(seen)} aurkitu dira')
    if move in seen: sys.exit('HBB9_ERROR: mugimendua aurretik aldatuta')
else:
    required_v9={move, base+'Assets/Editor/HorrorBuilderV9/HorrorBuilderV9.cs',
        base+'Assets/Laberintoa/Scripts/HorrorAtmosphereV9.cs',
        'tools/HorrorBuilderV9/horror-builder-v9.sh',
        'tools/HorrorBuilderV9/src/Editor/HorrorBuilderV9.cs',
        'tools/HorrorBuilderV9/src/Scripts/HorrorAtmosphereV9.cs',
        'tools/HorrorBuilderV9/docs/LEEME.md'}
    if not required_v9.issubset(seen):
        sys.exit('HBB9_ERROR: V9 ez da osorik: '+repr(sorted(required_v9-seen)))
print(f'HBB9_STATUS_OK: {len(seen)} aldaketa baimendu; {phase}')
PY_STATUS
}

protected_hashes() {
  sha256sum \
    "$PROY/Assets/Laberintoa/Scenes/SampleScene.unity" \
    "$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorMouseLook.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorShotgun.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorFlashlight.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorGameManager.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs" \
    "$PROY/Assets/Editor/HorrorBuilderV7/HorrorBuilderV7.cs" \
    "$PROY/Assets/Editor/HorrorBuilderV8/HorrorBuilderV8.cs" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLuzea.mat" \
    "$PROY/ProjectSettings/TagManager.asset" \
    "$PROY/Packages/manifest.json"
}

# Lerroz lerroko patch zehatza; arraroa bada ez du inolako aldaketarik egingo.
patch_movement() {
  python3 - "$MOVIMIENTO" "${1:---check}" <<'PY_PATCH'
from pathlib import Path
import sys
path, mode = Path(sys.argv[1]), sys.argv[2]
content = path.read_text()
replacements = [
    ('private float turnInput;', 'private float lateralInput;'),
    ('turnInput = Input.GetAxisRaw("Horizontal");',
     'lateralInput = Input.GetAxisRaw("Horizontal");'),
    ('float biraketa = turnInput * turnSpeed * Time.fixedDeltaTime + saguBiraketaZain;',
     'float biraketa = saguBiraketaZain;'),
    ('Vector3 desired = transform.forward * (forwardInput * speed);',
     'Vector3 desired = Vector3.ClampMagnitude(\n            transform.forward * forwardInput + transform.right * lateralInput, 1f) * speed;'),
]
for old, new in replacements:
    if content.count(old) != 1 or new in content:
        sys.exit('HBB9_MOVEMENT_ERROR: iturburu-eredua ez dator bat: '+old)
if mode == '--apply':
    result = content
    for old, new in replacements:
        result = result.replace(old,new,1)
    path.write_text(result)
    print('HBB9_MOVEMENT_PATCH_OK: WASD strafe, diagonala mugatuta, sagua bakarrik biratzeko')
else:
    print('HBB9_MOVEMENT_AUDIT_OK: 4 lerro aurkitu dira, aldaketarik gabe')
PY_PATCH
}

preauditar() {
  for program in git python3 sha256sum cp grep pgrep mktemp; do comando "$program"; done
  [[ -f "$UNITY" && -f "$ESCENA" && -f "$MOVIMIENTO" ]] || fallar 'Unity, eszena edo mugimendu-script-a falta dira.'
  [[ -f "$AQUI/src/Editor/HorrorBuilderV9.cs" && -f "$AQUI/src/Scripts/HorrorAtmosphereV9.cs" ]] || fallar 'V9 iturburuak falta dira.'
  for path in "$EDITOR_V9" "$EDITOR_V9.meta" "$TOOLS_V9" "$ATMOSFERA" "$ATMOSFERA.meta"; do
    [[ ! -e "$path" ]] || fallar "V9 lehendik dago: $path"
  done
  [[ "$(git -C "$PUBLICO" branch --show-current)" == feature/laberintoa-horror ]] || fallar 'Rama publikoa aldatu da'
  [[ "$(git -C "$PRIVADO" branch --show-current)" == main ]] || fallar 'Rama pribatua aldatu da'
  [[ "$(git -C "$PUBLICO" rev-parse --short=8 HEAD)" == c5370157 ]] || fallar 'Commit publikoa ez da espero zena'
  [[ "$(git -C "$PRIVADO" rev-parse --short=7 HEAD)" == b2691f0 ]] || fallar 'Commit pribatua ez da espero zena'
  [[ "$(git -C "$PUBLICO" rev-parse HEAD)" == "$(git -C "$PUBLICO" ls-remote origin refs/heads/feature/laberintoa-horror | cut -f1)" ]] || fallar 'Publikoa ez da GitHubekin bat etortzen'
  [[ "$(git -C "$PRIVADO" rev-parse HEAD)" == "$(git -C "$PRIVADO" ls-remote origin refs/heads/main | cut -f1)" ]] || fallar 'Pribatua ez da GitHubekin bat etortzen'
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fallar 'Pribatua ez dago garbi'
  [[ -z "$(git -C "$PUBLICO" diff --cached --name-only)" ]] || fallar 'Publikoan staged fitxategiak daude'
  git -C "$PUBLICO" diff --quiet -- "$REL_MOVIMIENTO" || fallar 'Mugimendua dagoeneko aldatuta dago'
  git -C "$PUBLICO" ls-files --error-unmatch "$REL_MOVIMIENTO" >/dev/null || fallar 'Mugimendua ez dago Git-en'
  protected_hashes >/dev/null || fallar 'Babestutako fitxategi batzuk falta dira'
  check_status before
  patch_movement --check
  echo 'HBB9_PREAUDIT_OK: V8 osoa; 26 aldaketa; mugimendu segurua; V9 absent.'
}

rollback() {
  local code="$?"
  if [[ "$TERMINADO" == 0 && -n "$RESPALDO" ]]; then
    echo 'HBB9_ROLLBACK: V8ko eszena eta mugimendua leheneratzen; V9ko fitxategiak soilik kentzen.' >&2
    cp -a "$RESPALDO/Laberintoa_Horror.unity" "$ESCENA" || true
    cp -a "$RESPALDO/HorrorPlayerMovement.cs" "$MOVIMIENTO" || true
    rm -rf -- "$EDITOR_V9" "$TOOLS_V9"
    rm -f -- "$EDITOR_V9.meta" "$ATMOSFERA" "$ATMOSFERA.meta"
    sha256sum -c "$RESPALDO/protegidos.sha256" || true
    echo "HBB9_ERROR_LOG: $RESPALDO/unity.log" >&2
    echo 'HBB9_RESULTADO: ERROR — rollback V8ra; ez errepikatu auditatu gabe.' >&2
  fi
  return "$code"
}

aplicar() {
  preauditar
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     [[ -e "$PROY/Library/UnityLockfile" ]]; then
    fallar 'Unity irekita dago; eszena gorde eta Unity itxi.'
  fi
  echo '=== 1. V8KO EGOERAREN BABES-KOPIA ==='
  mkdir -p "$SNAPSHOTS"
  RESPALDO="$(mktemp -d "$SNAPSHOTS/HorrorBuilderV9_XXXXXXXX")"
  cp -a "$ESCENA" "$RESPALDO/Laberintoa_Horror.unity"
  cp -a "$MOVIMIENTO" "$RESPALDO/HorrorPlayerMovement.cs"
  protected_hashes > "$RESPALDO/protegidos.sha256"
  git -C "$PUBLICO" status --short --branch > "$RESPALDO/git_antes.txt"
  trap rollback EXIT
  trap 'exit 130' INT
  echo "Respaldo: $RESPALDO"

  echo '=== 2. V9 SCRIPT-A ETA FPS MUGIMENDUA ==='
  mkdir -p "$EDITOR_V9" "$TOOLS_V9"
  cp "$AQUI/src/Editor/HorrorBuilderV9.cs" "$EDITOR_V9/HorrorBuilderV9.cs"
  cp "$AQUI/src/Scripts/HorrorAtmosphereV9.cs" "$ATMOSFERA"
  cp -a "$AQUI/src" "$AQUI/docs" "$TOOLS_V9/"
  cp "$AQUI/horror-builder-v9.sh" "$TOOLS_V9/horror-builder-v9.sh"
  patch_movement --apply

  echo '=== 3. UNITY BATCH — BUNKER, BASOA ETA KONTROLAK ==='
  local code=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV9.Apply -logFile "$RESPALDO/unity.log" || code=$?
  grep -E 'HBB9_|error CS[0-9]+|Exception|Error:' "$RESPALDO/unity.log" | tail -n 50 || true
  [[ "$code" == 0 ]] || fallar "Unity devolvio $code; revisar $RESPALDO/unity.log"
  grep -q 'HBB9_PRECHECK_OK:' "$RESPALDO/unity.log" || fallar 'Falta HBB9_PRECHECK_OK'
  grep -q 'HBB9_OK:' "$RESPALDO/unity.log" || fallar 'Falta HBB9_OK'

  echo '=== 4. FITXATEGI BABESTUAK + GIT ==='
  sha256sum -c "$RESPALDO/protegidos.sha256" || fallar 'Babestutako fitxategiren bat aldatu da'
  [[ -f "$EDITOR_V9/HorrorBuilderV9.cs.meta" && -f "$ATMOSFERA.meta" ]] || fallar 'V9ko .meta fitxategiak falta dira'
  [[ "$(sha256sum "$ESCENA" | cut -d' ' -f1)" != "$(sha256sum "$RESPALDO/Laberintoa_Horror.unity" | cut -d' ' -f1)" ]] || fallar 'Eszena ez da aldatu'
  [[ "$(sha256sum "$MOVIMIENTO" | cut -d' ' -f1)" != "$(sha256sum "$RESPALDO/HorrorPlayerMovement.cs" | cut -d' ' -f1)" ]] || fallar 'Mugimendua ez da aldatu'
  git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --check
  check_status after
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fallar 'Pribatua aldatu da'
  TERMINADO=1
  echo 'HBB9_LOCAL_OK: strafe A/D, bunker ilunagoa, basoko linterna indartsuagoa; NavMesh, materialak eta tiroa ukitu gabe.'
  echo "Respaldo y log: $RESPALDO"
  echo 'HBB9_RESULTADO: OK — Unity Play probatu; GitHub ez da aldatu.'
}

case "$MODO" in
  --audit|--dry-run) echo '=== HORROR BUILDER V9 — AUDITORIA SOLO LECTURA ==='; preauditar; echo 'HBB9_AUDIT_OK';;
  --apply) aplicar;;
  --help|-h) echo 'Uso: bash horror-builder-v9.sh --audit | --apply';;
  *) fallar "Opcion desconocida: $MODO";;
esac
