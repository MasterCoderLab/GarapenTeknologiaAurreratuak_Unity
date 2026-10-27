#!/usr/bin/env bash
set -euo pipefail
export GIT_PAGER=cat PAGER=cat

# Horror Builder v8: V6 hormak + bunker ilunagoa + kanpo argitsuagoa.
# V7 ez da ezabatzen; GitHub ez da aldatzen.
ASIG="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUBLICO="$ASIG/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUBLICO/Laberintoa3D_Final"
PRIVADO="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado"
SNAPSHOTS="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
ESCENA="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
EDITOR_V8="$PROY/Assets/Editor/HorrorBuilderV8"
TOOLS_V8="$PUBLICO/tools/HorrorBuilderV8"
AQUI="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODO="${1:---audit}"
RESPALDO=""
TERMINADO=0

fallar() { echo "HBB8_ERROR: $*" >&2; exit 1; }
comando() { command -v "$1" >/dev/null 2>&1 || fallar "Falta herramienta: $1"; }

# Egoera lokala: V7k utzitako aldaketak bakarrik, V8 aurretik eta ondoren.
check_status() {
  python3 - "$PUBLICO" "$1" <<'PY_STATUS'
import subprocess, sys
repo, phase = sys.argv[1:]
raw = subprocess.check_output(['git','-C',repo,'status','--porcelain=v1','-z','--untracked-files=all'])
rows = [x for x in raw.split(b'\0') if x]
base = 'Laberintoa3D_Final/'
scene = base + 'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity'
v7_exact = {
    scene,
    base+'Assets/Editor/HorrorBuilderV7.meta',
    base+'Assets/Laberintoa/Horror/BuilderV7.meta',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs.meta'
}
v7_prefixes = [base+'Assets/Editor/HorrorBuilderV7/',
               base+'Assets/Laberintoa/Horror/BuilderV7/',
               'tools/HorrorBuilderV7/']
v8_exact = {base+'Assets/Editor/HorrorBuilderV8.meta'}
v8_prefixes = [base+'Assets/Editor/HorrorBuilderV8/', 'tools/HorrorBuilderV8/']
if phase not in ('before', 'after'):
    raise SystemExit('HBB8_ERROR: fase Git ezezaguna')
seen=set()
for row in rows:
    if len(row)<4: raise SystemExit('HBB8_ERROR: Git sarrera okerra')
    flag=row[:2].decode('ascii')
    name=row[3:].decode('utf-8','surrogateescape')
    if flag not in (' M','??'):
        raise SystemExit('HBB8_ERROR: Git egoera ustekabekoa '+repr((flag,name)))
    if name == scene:
        if flag != ' M': raise SystemExit('HBB8_ERROR: eszena ez da lehendik zegoena')
    elif flag != '??':
        raise SystemExit('HBB8_ERROR: jatorrizko fitxategi bat aldatu da: '+name)
    if not (name in v7_exact or any(name.startswith(p) for p in v7_prefixes)
            or (phase == 'after' and (name in v8_exact or any(name.startswith(p) for p in v8_prefixes)))):
        raise SystemExit('HBB8_ERROR: Git fitxategi ez-baimendua: '+name)
    seen.add(name)
required_v7={scene,
  base+'Assets/Editor/HorrorBuilderV7/HorrorBuilderV7.cs',
  base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
  base+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLaburra.mat',
  base+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaErtaina.mat',
  base+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLuzea.mat',
  'tools/HorrorBuilderV7/horror-builder-v7.sh'}
if not required_v7.issubset(seen):
    raise SystemExit('HBB8_ERROR: V7 ez dago osorik: '+repr(sorted(required_v7-seen)))
if phase=='before':
    if len(seen)!=20:
        raise SystemExit(f'HBB8_ERROR: V7k 20 aldaketa zituen, orain {len(seen)}. Ikuskatu.')
else:
    required_v8={base+'Assets/Editor/HorrorBuilderV8/HorrorBuilderV8.cs',
                 'tools/HorrorBuilderV8/horror-builder-v8.sh',
                 'tools/HorrorBuilderV8/src/Editor/HorrorBuilderV8.cs',
                 'tools/HorrorBuilderV8/docs/LEEME.md'}
    if not required_v8.issubset(seen):
        raise SystemExit('HBB8_ERROR: V8 instalazio osatugabea '+repr(sorted(required_v8-seen)))
print(f'HBB8_STATUS_OK: {len(seen)} aldaketa baimendu; {phase}')
PY_STATUS
}

protected_hashes() {
  sha256sum \
    "$PROY/Assets/Laberintoa/Scenes/SampleScene.unity" \
    "$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs" \
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
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLuzea.mat" \
    "$PROY/ProjectSettings/TagManager.asset" \
    "$PROY/Packages/manifest.json"
}

preauditar() {
  comando git; comando python3; comando sha256sum; comando cp
  [[ -f "$UNITY" ]] || fallar "Unity ez da aurkitu: $UNITY"
  [[ -f "$ESCENA" ]] || fallar "Eszena ez da aurkitu: $ESCENA"
  [[ -f "$AQUI/src/Editor/HorrorBuilderV8.cs" ]] || fallar 'V8ko Editor script-a falta da'
  for file in \
    "$PROY/Assets/Laberintoa/Scenes/SampleScene.unity" \
    "$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorShotgun.cs" \
    "$PROY/Assets/Editor/HorrorBuilderV7/HorrorBuilderV7.cs" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLuzea.mat"; do
    [[ -f "$file" ]] || fallar "Falta: $file"
  done
  for path in "$EDITOR_V8" "$EDITOR_V8.meta" "$TOOLS_V8"; do
    [[ ! -e "$path" ]] || fallar "V8 aurretik existitzen da: $path"
  done
  [[ "$(git -C "$PUBLICO" branch --show-current)" == feature/laberintoa-horror ]] || fallar 'Rama publikoa okerra'
  [[ "$(git -C "$PRIVADO" branch --show-current)" == main ]] || fallar 'Rama pribatua okerra'
  [[ "$(git -C "$PUBLICO" rev-parse --short=8 HEAD)" == c5370157 ]] || fallar 'Commit publikoa aldatu da'
  [[ "$(git -C "$PRIVADO" rev-parse --short=7 HEAD)" == b2691f0 ]] || fallar 'Commit pribatua aldatu da'
  [[ "$(git -C "$PUBLICO" rev-parse HEAD)" == "$(git -C "$PUBLICO" ls-remote origin refs/heads/feature/laberintoa-horror | cut -f1)" ]] || fallar 'Publikoa urrunekoarekiko desberdina'
  [[ "$(git -C "$PRIVADO" rev-parse HEAD)" == "$(git -C "$PRIVADO" ls-remote origin refs/heads/main | cut -f1)" ]] || fallar 'Pribatua urrunekoarekiko desberdina'
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fallar 'Pribatua ez dago garbi'
  [[ -z "$(git -C "$PUBLICO" diff --cached --name-only)" ]] || fallar 'Publikoan staged aldaketak daude'
  check_status before
  echo 'HBB8_PREAUDIT_OK: V7 osorik; V6ko materialak eskuragarri; aldaketak eszenara mugatuak.'
}

rollback() {
  local exit_code="$?"
  if [[ "$TERMINADO" -eq 0 && -n "$RESPALDO" ]]; then
    echo 'HBB8_ROLLBACK: aurreko V7 eszena leheneratzen; V8ko fitxategiak soilik kentzen.' >&2
    cp -a "$RESPALDO/Laberintoa_Horror.unity" "$ESCENA" || echo 'HBB8_ALERTA: ezin izan da eszena leheneratu' >&2
    rm -rf -- "$EDITOR_V8" "$TOOLS_V8"
    rm -f -- "$EDITOR_V8.meta"
    sha256sum -c "$RESPALDO/protegidos.sha256" || echo 'HBB8_ALERTA: babestutako fitxategi bat aldatu da' >&2
    echo "HBB8_ERROR_LOG: $RESPALDO/unity.log" >&2
    echo 'HBB8_RESULTADO: ERROR — rollback V7ra; ez errepikatu egiaztatu gabe.' >&2
  fi
  return "$exit_code"
}

aplicar() {
  preauditar
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     [[ -e "$PROY/Library/UnityLockfile" ]]; then
    fallar 'Unity irekita dago; eszena gorde eta Unity itxi.'
  fi
  echo '=== 1. V7 EGOERAREN BABES-KOPIA ==='
  mkdir -p "$SNAPSHOTS"
  RESPALDO="$(mktemp -d "$SNAPSHOTS/HorrorBuilderV8_XXXXXXXX")"
  cp -a "$ESCENA" "$RESPALDO/Laberintoa_Horror.unity"
  protected_hashes > "$RESPALDO/protegidos.sha256"
  git -C "$PUBLICO" status --short --branch > "$RESPALDO/git_antes.txt"
  trap rollback EXIT
  trap 'exit 130' INT
  echo "Respaldo: $RESPALDO"

  echo '=== 2. V8 EDITOR SCRIPT ETA TRESNAK INSTALATU ==='
  mkdir -p "$EDITOR_V8" "$TOOLS_V8"
  cp "$AQUI/src/Editor/HorrorBuilderV8.cs" "$EDITOR_V8/HorrorBuilderV8.cs"
  cp -a "$AQUI/src" "$AQUI/docs" "$TOOLS_V8/"
  cp "$AQUI/horror-builder-v8.sh" "$TOOLS_V8/horror-builder-v8.sh"

  echo '=== 3. UNITY BATCH — V6 HORMAK + GAUEKO ARGIA ==='
  local code=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV8.Apply -logFile "$RESPALDO/unity.log" || code=$?
  grep -E 'HBB8_|error CS[0-9]+|Exception|Error:' "$RESPALDO/unity.log" | tail -n 45 || true
  [[ "$code" -eq 0 ]] || fallar "Unity devolvio $code; revisar $RESPALDO/unity.log"
  grep -q 'HBB8_PRECHECK_OK:' "$RESPALDO/unity.log" || fallar 'No se encuentra HBB8_PRECHECK_OK'
  grep -q 'HBB8_OK:' "$RESPALDO/unity.log" || fallar 'No se encuentra HBB8_OK'

  echo '=== 4. EGIAZTAPENAK ETA GIT ==='
  sha256sum -c "$RESPALDO/protegidos.sha256" || fallar 'Babestutako fitxategiren bat aldatu da'
  [[ -f "$EDITOR_V8/HorrorBuilderV8.cs.meta" ]] || fallar 'V8 .meta falta da'
  [[ "$(sha256sum "$ESCENA" | cut -d' ' -f1)" != "$(sha256sum "$RESPALDO/Laberintoa_Horror.unity" | cut -d' ' -f1)" ]] || fallar 'Eszena ez da aldatu'
  git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --check
  check_status after
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fallar 'Repo pribatua aldatu da'
  TERMINADO=1
  echo 'HBB8_LOCAL_OK: V6ko hormak + bunker ilunagoa + baso argiagoa; V7ko kamera eta jokoaren mekanikak ukitu gabe.'
  echo "Respaldo y log: $RESPALDO"
  echo 'HBB8_RESULTADO: OK — Unity Play probak egin; GitHub ez da aldatu.'
}

case "$MODO" in
  --audit|--dry-run) echo '=== HORROR BUILDER V8 — AUDITORIA SOLO LECTURA ==='; preauditar; echo 'HBB8_AUDIT_OK';;
  --apply) aplicar;;
  --help|-h) echo 'Uso: bash horror-builder-v8.sh --audit | --apply';;
  *) fallar "Opcion desconocida: $MODO";;
esac
