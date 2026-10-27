#!/usr/bin/env bash
set -euo pipefail

# Horror Builder v2 — solo mejora visual, sin Git commit ni push.
# Requiere partida v1 ya validada y versionada en público y privado.
BASE="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUBLICO="$BASE/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PRIVADO="$HOME/Projects/Laberintoa3D_Horror_Backup_Privado"
PROY="$PUBLICO/Laberintoa3D_Final"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
SCENE="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
SAMPLE="$PROY/Assets/Laberintoa/Scenes/SampleScene.unity"
NAV="$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset"
AI="$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs"
ANIM="$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs"
B1="$PROY/Assets/Editor/HorrorBuilderV1/HorrorBuilderV1.cs"
EDITOR2="$PROY/Assets/Editor/HorrorBuilderV2"
ASSETS2="$PROY/Assets/Laberintoa/Horror/BuilderV2"
TOOLS2="$PUBLICO/tools/HorrorBuilderV2"
DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODE="${1:---audit}"
BACKUP=""
SUCCESS=0

usage(){ cat <<'HELP'
Horror Builder v2 — Mejora visual sin alterar la navegación.
  bash horror-builder-v2.sh --audit   # Solo lectura; Unity puede estar abierto.
  bash horror-builder-v2.sh --apply   # Unity cerrado; crea backup + mejoras visuales.
  bash horror-builder-v2.sh --help
NUNCA hace git add, commit, push ni elimina assets previos.
HELP
}
fail(){ printf 'ERROR: %s\n' "$*" >&2; exit 1; }
need(){ command -v "$1" >/dev/null || fail "Falta herramienta: $1"; }

check_state(){
  need git; need sha256sum; need python3
  for f in "$SCENE" "$SAMPLE" "$NAV" "$AI" "$ANIM" "$B1" \
       "$PROY/Assets/Laberintoa/Horror/Animations/HorrorCreepAI.controller" \
       "$PROY/Assets/Creep Horror Creature/Prefabs/Creep1.prefab" \
       "$PROY/Assets/Creep Horror Creature/Prefabs/Creep2.prefab" \
       "$PROY/Assets/Creep Horror Creature/Prefabs/Creep3.prefab" \
       "$UNITY" "$DIR/src/Editor/HorrorBuilderV2.cs"; do
    [[ -f "$f" ]] || fail "No existe: $f"
  done
  [[ "$(git -C "$PUBLICO" rev-parse --show-toplevel)" = "$PUBLICO" ]] || fail 'Ruta pública inválida'
  [[ "$(git -C "$PRIVADO" rev-parse --show-toplevel)" = "$PRIVADO" ]] || fail 'Ruta privada inválida'
  [[ "$(git -C "$PUBLICO" branch --show-current)" = 'feature/laberintoa-horror' ]] || fail 'Rama pública inesperada'
  [[ "$(git -C "$PRIVADO" branch --show-current)" = 'main' ]] || fail 'Rama privada inesperada'
  [[ "$(git -C "$PUBLICO" rev-parse --short=8 HEAD)" = 'fd55fd79' ]] || fail 'Commit público inesperado. Auditar antes de modificar.'
  [[ "$(git -C "$PRIVADO" rev-parse --short=7 HEAD)" = 'f7a47d7' ]] || fail 'Commit privado inesperado. Auditar antes de modificar.'
  [[ "$(git -C "$PUBLICO" remote get-url origin)" = 'https://github.com/MasterCoderLab/GarapenTeknologiaAurreratuak_Unity.git' ]] || fail 'Remoto público inesperado'
  [[ "$(git -C "$PRIVADO" remote get-url origin)" = 'https://github.com/MasterCoderLab/Laberintoa3D_Horror_Backup_Privado.git' ]] || fail 'Remoto privado inesperado'
  [[ -z "$(git -C "$PUBLICO" status --porcelain=v1 --untracked-files=all)" ]] || fail 'El repo público tiene cambios locales. No se toca.'
  [[ -z "$(git -C "$PRIVADO" status --porcelain=v1 --untracked-files=all)" ]] || fail 'El privado tiene cambios locales. No se toca.'
  for path in "$EDITOR2" "$EDITOR2.meta" "$ASSETS2" "$ASSETS2.meta" "$TOOLS2"; do
    [[ ! -e "$path" ]] || fail "Builder v2 ya existe (no se duplica): $path"
  done
  # El proyecto local debe coincidir con la versión pública recién validada.
  [[ "$(git -C "$PUBLICO" rev-parse HEAD)" = "$(git -C "$PUBLICO" rev-parse origin/feature/laberintoa-horror)" ]] || fail 'Publico no sincronizado con origin'
  [[ "$(git -C "$PRIVADO" rev-parse HEAD)" = "$(git -C "$PRIVADO" rev-parse origin/main)" ]] || fail 'Privado no sincronizado con origin'
  sha256sum "$SCENE" "$SAMPLE" "$NAV" >/dev/null
}

report(){
  echo '=== HORROR BUILDER V2 · PREAUDITORÍA SOLO LECTURA ==='
  check_state
  echo 'OK: repositorios limpios, ramas y commits correctos.'
  echo 'OK: escena Horror v1, NavMesh, Creep1/2/3, IA y Animator presentes.'
  echo 'OK: no hay una instalación anterior del Builder v2.'
  echo '=== OPERACIONES PLANEADAS (SIN CAMBIOS DE FÍSICA) ==='
  echo '1. Texturas originales PROCEDURALES: hormigón, madera, tierra, corteza, hojas y óxido.'
  echo '2. Sustituir copas-esfera de los árboles por mallas de pinos; conservar colisionadores de troncos.'
  echo '3. Detalles industriales, tablones de chabola, hierbas, luz y niebla nocturna.'
  echo '4. Verificar integridad con Unity y comparar hashes de SampleScene, NavMesh e IA.'
  echo '5. SOLO cambios locales. Ningún git add/commit/push.'
  echo 'HBB2_AUDIT_LOCAL_OK'
}

rollback(){
  local code="$1"
  if [[ "$SUCCESS" == 1 || -z "$BACKUP" ]]; then return "$code"; fi
  echo >&2
  echo 'HBB2_ROLLBACK: fallo detectado; restaurando escena previa y eliminando SOLO archivos de v2.' >&2
  cp -a "$BACKUP/Laberintoa_Horror.unity" "$SCENE"
  # Las carpetas y los .meta eran inexistentes al entrar en --apply.
  rm -rf -- "$EDITOR2" "$EDITOR2.meta" "$ASSETS2" "$ASSETS2.meta" "$TOOLS2"
  sha256sum -c "$BACKUP/protegidos.sha256" || echo 'ATENCION: diferencias en fichero protegido; no reintentar.' >&2
  printf 'Respaldo y log: %s\n' "$BACKUP" >&2
  return "$code"
}

apply(){
  check_state
  [[ ! -e "$PROY/Library/UnityLockfile" ]] || fail 'UnityLockfile detectado: cierra Unity antes de aplicar.'
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null ||
     pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null; then
    fail 'Unity sigue abierto. File > Exit antes de --apply.'
  fi
  echo '=== 1. RESPALDO LOCAL ==='
  BACKUP="$(mktemp -d "$HOME/LaberintoaHorror_BuilderV2_XXXXXXXX")"
  cp -a "$SCENE" "$BACKUP/Laberintoa_Horror.unity"
  sha256sum "$SAMPLE" "$NAV" "$AI" "$ANIM" "$B1" > "$BACKUP/protegidos.sha256"
  git -C "$PUBLICO" status --short --branch > "$BACKUP/git_publico_antes.txt"
  git -C "$PRIVADO" status --short --branch > "$BACKUP/git_privado_antes.txt"
  echo "Respaldo: $BACKUP"
  trap 'rollback "$?"' EXIT

  echo '=== 2. INSTALAR SOLO HERRAMIENTA V2 ==='
  mkdir -p "$EDITOR2" "$ASSETS2" "$TOOLS2"
  cp -a "$DIR/src" "$DIR/docs" "$TOOLS2/"
  cp "$DIR/horror-builder-v2.sh" "$TOOLS2/horror-builder-v2.sh"
  cp "$DIR/src/Editor/HorrorBuilderV2.cs" "$EDITOR2/HorrorBuilderV2.cs"

  echo '=== 3. UNITY BATCH: MEJORA Y VALIDACIÓN ==='
  local result=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV2.Apply -logFile "$BACKUP/unity.log" || result=$?
  grep -E 'HBB2_|error CS[0-9]+|Exception|Error:' "$BACKUP/unity.log" | tail -n 70 || true
  [[ "$result" == 0 ]] || fail "Unity devolvió código $result. Consulta $BACKUP/unity.log"
  grep -q 'HBB2_PRECHECK_OK:' "$BACKUP/unity.log" || fail 'No se confirmó auditoría nativa'
  grep -q 'HBB2_ASSETS_OK:' "$BACKUP/unity.log" || fail 'No se confirmó generación de texturas'
  grep -q 'HBB2_OK:' "$BACKUP/unity.log" || fail 'No se confirmó el guardado de la escena'

  echo '=== 4. PROTECCIONES DE LA VERSIÓN V1 ==='
  sha256sum -c "$BACKUP/protegidos.sha256" || fail 'Se modificó un fichero protegido'
  [[ -f "$ASSETS2/HB2_Conifer.asset" ]] || fail 'No existe malla de pino'
  for name in Concrete Ground Bark Planks Rust Needles; do
    [[ -s "$ASSETS2/HB2_${name}.png" ]] || fail "Textura ausente: $name"
  done
  [[ -f "$EDITOR2/HorrorBuilderV2.cs.meta" ]] || fail 'Unity no generó meta del editor'
  [[ -f "$ASSETS2.meta" ]] || fail 'Unity no generó meta de Assets'
  # Evitar commits accidentales. Se permite SOLO el directorio v2 y la escena.
  python3 - "$PUBLICO" <<'PY'
import subprocess,sys
p=sys.argv[1]
raw=subprocess.check_output(['git','-C',p,'status','--porcelain=v1','-z','--untracked-files=all'])
entries=[x.decode('utf-8',errors='replace') for x in raw.split(b'\0') if x]
allowed='Laberintoa3D_Final/Assets/'
scene=allowed+'Laberintoa/Scenes/Laberintoa_Horror.unity'
paths=[]
for ent in entries:
  flag,name=ent[:2],ent[3:]
  if flag not in (' M','??') or not (name==scene or
   name.startswith(allowed+'Editor/HorrorBuilderV2') or
   name.startswith(allowed+'Laberintoa/Horror/BuilderV2') or
   name.startswith('tools/HorrorBuilderV2/')):
    raise SystemExit('ERROR: cambio ajeno a v2: '+repr(ent))
  paths.append(name)
print('HBB2_STATUS_OK: archivos locales solo de V2:',len(paths))
PY
  git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --check
  [[ -z "$(git -C "$PRIVADO" status --porcelain=v1 --untracked-files=all)" ]] || fail 'Se modificó el privado'
  echo '=== 5. RESULTADO ==='
  SUCCESS=1
  echo 'HBB2_OK: arte visual creado. SIN COMMIT / SIN PUSH en ninguno de los repositorios.'
  echo "Respaldo y log: $BACKUP"
  git -C "$PUBLICO" status --short
  echo 'Abrir Unity para inspeccionar aspecto y FPS antes del checkpoint privado/público.'
}

case "$MODE" in
  --audit|--dry-run) report;;
  --apply) apply;;
  --help|-h) usage;;
  *) usage; exit 2;;
esac
