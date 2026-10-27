#!/usr/bin/env bash
set -euo pipefail

# Horror Builder v7: bi zuzenketa bakarrik, GitHub ukitu gabe.
ASIG="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUBLICO="$ASIG/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUBLICO/Laberintoa3D_Final"
PRIVADO="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado"
SNAPSHOTS="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
SCENE="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV7"
MATERIALS="$PROY/Assets/Laberintoa/Horror/BuilderV7"
SAFETY="$PROY/Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs"
TOOLS_DIR="$PUBLICO/tools/HorrorBuilderV7"
HERE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODE="${1:---audit}"
BACKUP=""
SUCCESS=0

fail() { echo "HBB7_ERROR: $*" >&2; exit 1; }
need() { command -v "$1" >/dev/null 2>&1 || fail "Herramienta no instalada: $1"; }

# La version v6 YA esta confirmada en ambos repositorios.
# Antes de v7 exigimos ambos limpios; despues, UNICAMENTE ficheros propios v7.
check_status() {
  python3 - "$PUBLICO" "$1" <<'PY_STATUS'
import subprocess, sys
repo, phase = sys.argv[1:]
raw = subprocess.check_output(['git','-C',repo,'status','--porcelain=v1','-z','--untracked-files=all'])
items = [part for part in raw.split(b'\0') if part]
root = 'Laberintoa3D_Final/'
scene = root + 'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity'
exact = {scene,
 root+'Assets/Editor/HorrorBuilderV7.meta',
 root+'Assets/Laberintoa/Horror/BuilderV7.meta',
 root+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
 root+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs.meta'}
prefixes = [root+'Assets/Editor/HorrorBuilderV7/',
            root+'Assets/Laberintoa/Horror/BuilderV7/',
            'tools/HorrorBuilderV7/']
if phase == 'before':
    if items:
        raise SystemExit('HBB7_ERROR: v6 no esta limpio: '+str(len(items))+' cambios')
    print('HBB7_STATUS_OK: 0 cambios; repositorios preparados')
    raise SystemExit(0)
if phase != 'after':
    raise SystemExit('HBB7_ERROR: fase desconocida')
seen = set()
for row in items:
    if len(row) < 4: raise SystemExit('HBB7_ERROR: estado Git invalido')
    flag = row[:2].decode('ascii')
    name = row[3:].decode('utf-8', 'surrogateescape')
    if flag not in (' M','??'):
        raise SystemExit('HBB7_ERROR: operacion Git inesperada: '+repr((flag,name)))
    if (name == scene and flag != ' M') or (name != scene and flag != '??'):
        raise SystemExit('HBB7_ERROR: tipo de cambio inesperado: '+repr((flag,name)))
    if not (name in exact or any(name.startswith(p) for p in prefixes)):
        raise SystemExit('HBB7_ERROR: ruta no autorizada: '+repr(name))
    seen.add(name)
required={scene,
 root+'Assets/Editor/HorrorBuilderV7/HorrorBuilderV7.cs',
 root+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
 root+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLuzea.mat',
 root+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaErtaina.mat',
 root+'Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLaburra.mat',
 'tools/HorrorBuilderV7/horror-builder-v7.sh'}
if not required.issubset(seen):
    raise SystemExit('HBB7_ERROR: instalacion incompleta: '+repr(sorted(required-seen)))
print('HBB7_STATUS_OK: '+str(len(seen))+' rutas nuevas v7 autorizadas; after')
PY_STATUS
}
precheck() {
  need git; need python3; need sha256sum
  for f in "$UNITY" "$SCENE" "$HERE/src/Editor/HorrorBuilderV7.cs" \
      "$HERE/src/Scripts/HorrorViewmodelSafety.cs" \
      "$PROY/Assets/Laberintoa/Scenes/SampleScene.unity" \
      "$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset" \
      "$PROY/Assets/Laberintoa/Scripts/HorrorMouseLook.cs" \
      "$PROY/Assets/Laberintoa/Scripts/HorrorShotgun.cs" \
      "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLaburra.mat" \
      "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaErtaina.mat" \
      "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat"; do
    [[ -f "$f" ]] || fail "Falta el archivo: $f"
  done
  for path in "$EDITOR" "$EDITOR.meta" "$MATERIALS" "$MATERIALS.meta" \
      "$SAFETY" "$SAFETY.meta" "$TOOLS_DIR"; do
    [[ ! -e "$path" ]] || fail "Ya existe un elemento V7: $path"
  done
  [[ "$(git -C "$PUBLICO" branch --show-current)" == feature/laberintoa-horror ]] || fail 'Rama publica inesperada'
  [[ "$(git -C "$PRIVADO" branch --show-current)" == main ]] || fail 'Rama privada inesperada'
  [[ "$(git -C "$PUBLICO" rev-parse --short=8 HEAD)" == c5370157 ]] || fail 'Nuevo commit público, auditar'
  [[ "$(git -C "$PRIVADO" rev-parse --short=7 HEAD)" == b2691f0 ]] || fail 'Nuevo commit privado, auditar'
  [[ "$(git -C "$PUBLICO" rev-parse HEAD)" == "$(git -C "$PUBLICO" rev-parse origin/feature/laberintoa-horror)" ]] || fail 'Repositorio publico no sincronizado'
  [[ "$(git -C "$PRIVADO" rev-parse HEAD)" == "$(git -C "$PRIVADO" rev-parse origin/main)" ]] || fail 'Repositorio privado no sincronizado'
  [[ "$(git -C "$PUBLICO" rev-parse HEAD)" == "$(git -C "$PUBLICO" ls-remote origin refs/heads/feature/laberintoa-horror | cut -f1)" ]] || fail 'La rama publica remota ha cambiado'
  [[ "$(git -C "$PRIVADO" rev-parse HEAD)" == "$(git -C "$PRIVADO" ls-remote origin refs/heads/main | cut -f1)" ]] || fail 'La rama privada remota ha cambiado'
  [[ -z "$(git -C "$PUBLICO" diff --cached --name-only)" ]] || fail 'Hay archivos staged en publico' 
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fail 'Privado no esta limpio'
  check_status before
  echo 'HBB7_PREAUDIT_OK: solo renderizado de arma/linterna y nuevos materiales PBR de pared.'
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
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat" \
    "$PROY/ProjectSettings/TagManager.asset" \
    "$PROY/Packages/manifest.json"
}

rollback() {
  local exit_code="$?"
  if [[ "$SUCCESS" -eq 0 && -n "$BACKUP" ]]; then
    echo 'HBB7_ROLLBACK: restaurando escena y retirando SOLO elementos creados por V7.' >&2
    cp -a "$BACKUP/Laberintoa_Horror.unity" "$SCENE" || true
    rm -rf -- "$EDITOR" "$MATERIALS" "$TOOLS_DIR"
    rm -f -- "$EDITOR.meta" "$MATERIALS.meta" "$SAFETY" "$SAFETY.meta"
    # Unity puede haber creado la carpeta meta del subdirectorio Materials.
    [[ -d "$PROY/Assets/Laberintoa/Horror/BuilderV7" ]] && \
      rm -rf -- "$PROY/Assets/Laberintoa/Horror/BuilderV7"
    [[ -f "$PROY/Assets/Laberintoa/Horror/BuilderV7.meta" ]] && \
      rm -f -- "$PROY/Assets/Laberintoa/Horror/BuilderV7.meta"
    sha256sum -c "$BACKUP/protegidos.sha256" || echo 'HBB7_ALERTA: hash protegido distinto' >&2
    echo "HBB7_ERROR_LOG: $BACKUP/unity.log" >&2
    echo 'HBB7_RESULTADO: ERROR — rollback realizado; no repetir sin revisar.' >&2
  fi
  return "$exit_code"
}

apply() {
  precheck
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
     [[ -e "$PROY/Library/UnityLockfile" ]]; then
    fail 'Unity sigue abierto o hay un UnityLockfile; guardar escena y salir antes de aplicar.'
  fi
  echo '=== 1. RESPALDO DENTRO DE LA ASIGNATURA ==='
  mkdir -p "$SNAPSHOTS"
  BACKUP="$(mktemp -d "$SNAPSHOTS/HorrorBuilderV7_XXXXXXXX")"
  cp -a "$SCENE" "$BACKUP/Laberintoa_Horror.unity"
  protected_hashes > "$BACKUP/protegidos.sha256"
  git -C "$PUBLICO" status --short --branch > "$BACKUP/git_antes.txt"
  trap rollback EXIT
  echo "Respaldo: $BACKUP"
  echo '=== 2. INSTALAR SOLO V7 ==='
  mkdir -p "$EDITOR" "$MATERIALS/Materials" "$TOOLS_DIR"
  printf '%s\n' 'Builder v7: hormigoi-material berriak. V6ko materialak bere horretan.' > "$MATERIALS/Materials/LEEME_MATERIALAK.txt"
  cp "$HERE/src/Editor/HorrorBuilderV7.cs" "$EDITOR/HorrorBuilderV7.cs"
  cp "$HERE/src/Scripts/HorrorViewmodelSafety.cs" "$SAFETY"
  cp -a "$HERE/src" "$HERE/docs" "$TOOLS_DIR/"
  cp "$HERE/horror-builder-v7.sh" "$TOOLS_DIR/horror-builder-v7.sh"
  echo '=== 3. UNITY BATCH — PARETAK + IKUSGAI-MODELOAK ==='
  local unity_code=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
      "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
      -executeMethod HorrorBuilderV7.Apply -logFile "$BACKUP/unity.log" || unity_code=$?
  grep -E 'HBB7_|error CS[0-9]+|Exception|Error:' "$BACKUP/unity.log" | tail -n 45 || true
  [[ "$unity_code" -eq 0 ]] || fail "Unity devolvio $unity_code; revisar $BACKUP/unity.log"
  grep -q 'HBB7_PRECHECK_OK:' "$BACKUP/unity.log" || fail 'Falta HBB7_PRECHECK_OK'
  grep -q 'HBB7_OK:' "$BACKUP/unity.log" || fail 'Falta HBB7_OK'
  echo '=== 4. VERIFICACIONES Y ESTADO GIT ==='
  sha256sum -c "$BACKUP/protegidos.sha256" || fail 'Archivo protegido modificado'
  for file in "$EDITOR/HorrorBuilderV7.cs.meta" "$SAFETY.meta" \
     "$MATERIALS/Materials/HB7_HormaLaburra.mat" \
     "$MATERIALS/Materials/HB7_HormaErtaina.mat" \
     "$MATERIALS/Materials/HB7_HormaLuzea.mat"; do
    [[ -f "$file" ]] || fail "No se creo: $file"
  done
  git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --check
  check_status after
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fail 'Privado modificado'
  SUCCESS=1
  echo 'HBB7_LOCAL_OK: material de pared suavizado y vista de arma/linterna aislada; no Git.'
  echo "Respaldo y log: $BACKUP"
  echo 'HBB7_RESULTADO: OK — abrir Unity y comprobar cámara, escopeta y esquinas.'
}

case "$MODE" in
 --audit|--dry-run) echo '=== HORROR BUILDER V7 — AUDITORIA SOLO LECTURA ===';precheck;echo 'HBB7_AUDIT_OK';;
 --apply) apply;;
 --help|-h) echo 'Uso: bash horror-builder-v7.sh --audit | --apply';;
 *) fail "Opcion desconocida: $MODE";;
esac
