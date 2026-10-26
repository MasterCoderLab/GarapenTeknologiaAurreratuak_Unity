#!/usr/bin/env bash
set -euo pipefail

# Horror Builder v1 — Fedora + Unity 6000.6.0f1.
# --dry-run: solo consulta. --apply: checkpoint animaciones + construcción (sin push final).

BASE="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUBLICO="$BASE/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUBLICO/Laberintoa3D_Final"
PRIVADO="$HOME/Projects/Laberintoa3D_Horror_Backup_Privado"
SCENE="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
TEACHER="$PROY/Assets/Laberintoa/Scenes/SampleScene.unity"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
EDITOR_TARGET="$PROY/Assets/Editor/HorrorBuilderV1"
TOOL_REPO="$PUBLICO/tools/HorrorBuilderV1"
MAT_TARGET="$PROY/Assets/Laberintoa/Horror/BuilderV1"
NAV_TARGET="$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset"
CMD="${1:---dry-run}"
SUCCESS=0
BACKUP=""

helpmsg() {
  cat <<'TXT'
Horror Builder v1 (Fedora/Unity)
  bash horror-builder.sh --dry-run    # auditoría local sin cambios ni Unity
  bash horror-builder.sh --apply      # checkpoint y construcción en Unity
  bash horror-builder.sh --help
Para --apply: guardar escena y cerrar Unity.
TXT
}

need(){ command -v "$1" >/dev/null || { echo "ERROR: falta $1"; exit 1; }; }
check_base(){
  for f in "$SCENE" "$TEACHER" "$UNITY" \
      "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs" \
      "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs" \
      "$PROY/Assets/Laberintoa/Horror/Animations/HorrorCreepAI.controller" \
      "$PROY/Assets/Creep Horror Creature/Prefabs/Creep1.prefab"; do
    test -f "$f" || { echo "ERROR: falta $f"; exit 1; }
  done
  test -d "$PRIVADO/.git" || { echo "ERROR: repo privado no existe: $PRIVADO"; exit 1; }
  test "$(git -C "$PUBLICO" branch --show-current)" = "feature/laberintoa-horror" || {
    echo 'ERROR: rama pública inesperada'; exit 1;
  }
  test "$(git -C "$PRIVADO" branch --show-current)" = "main" || {
    echo 'ERROR: rama privada inesperada'; exit 1;
  }
  test ! -e "$NAV_TARGET" || { echo 'ERROR: Builder v1 ya aplicado o intentado. Auditar antes de repetir.'; exit 1; }
  test ! -e "$EDITOR_TARGET" || { echo 'ERROR: existe carpeta Editor HorrorBuilderV1.'; exit 1; }
  test ! -e "$TOOL_REPO" || { echo 'ERROR: existe tools/HorrorBuilderV1 en repo; audita antes.'; exit 1; }
  test ! -e "$MAT_TARGET" || { echo 'ERROR: existe carpeta materiales BuilderV1.'; exit 1; }
  test ! -e "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs" || {
    echo 'ERROR: existe HorrorPlayerMovement.cs.'; exit 1;
  }
}

status_report(){
  echo; echo '=== PROYECTO Y GIT (SOLO CONSULTA) ==='
  printf 'Proyecto: %s\n' "$PROY"
  git -C "$PUBLICO" status --short --branch
  echo '--- Repositorio privado ---'
  git -C "$PRIVADO" status --short --branch
  echo '--- Ficheros de Builder disponibles ---'
  find "$SCRIPT_DIR/src" -maxdepth 3 -name '*.cs' -printf '  %P\n' | sort
  echo; echo 'La versión v1 construye entorno PROVISIONAL (sin escopeta funcional ni UI de victoria).'
}

check_animation_changes(){
  # Nunca incluir material Asset Store en público.
  python3 - "$PUBLICO" <<'PY'
import subprocess,sys
repo=sys.argv[1]
allowed={
  'Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
  'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs',
  'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs',
  'Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs.meta',
  'Laberintoa3D_Final/Assets/Editor/HorrorEnemyAnimationsBatch.cs',
  'Laberintoa3D_Final/Assets/Editor/HorrorEnemyAnimationsBatch.cs.meta',
  'Laberintoa3D_Final/Assets/Laberintoa/Horror/Animations.meta',
}
p=subprocess.run(['git','-C',repo,'status','--porcelain=v1','-uall'],capture_output=True,text=True,check=True)
items=[line[3:] for line in p.stdout.splitlines() if line]
extra=[p for p in items if p not in allowed and not p.startswith('Laberintoa3D_Final/Assets/Laberintoa/Horror/Animations/')]
if extra:
  print('ERROR: hay cambios ajenos a las animaciones. No se hará commit automático:',*extra,sep='\n  ')
  raise SystemExit(1)
print('OK: solo cambios esperados de animaciones:',len(items))
PY
  test -z "$(git -C "$PUBLICO" diff --cached --name-only)" || {
    echo 'ERROR: existen cambios ya staged en público.'; exit 1;
  }
  test -z "$(git -C "$PRIVADO" status --porcelain)" || {
    echo 'ERROR: backup privado no está limpio.'; exit 1;
  }
  test "$(gh repo view MasterCoderLab/Laberintoa3D_Horror_Backup_Privado --json isPrivate --jq .isPrivate)" = true || {
    echo 'ERROR: repo remoto privado no verificado.'; exit 1;
  }
  git -C "$PUBLICO" ls-files --error-unmatch .gitignore >/dev/null
}

checkpoint_animations(){
  echo; echo '=== CHECKPOINT: ANIMACIONES, COPIA COMPLETA PRIVADA ==='
  rsync -a --exclude='.git/' --exclude='Library/' --exclude='Temp/' \
    --exclude='Logs/' --exclude='obj/' --exclude='UserSettings/' \
    --exclude='Build/' --exclude='Builds/' --exclude='.vs/' \
    "$PROY/" "$PRIVADO/"
  git -C "$PRIVADO" add -A -- Assets Packages ProjectSettings
  git -C "$PRIVADO" -c core.whitespace=-blank-at-eol diff --cached --check
  if ! git -C "$PRIVADO" diff --cached --quiet; then
    git -C "$PRIVADO" commit -m 'Checkpoint: animaciones de tres enemigos conectadas a la IA'
    git -C "$PRIVADO" push origin main
  fi
  echo; echo '=== CHECKPOINT: SOLO CÓDIGO PROPIO EN GITHUB PUBLICO ==='
  git -C "$PUBLICO" add -A -- \
    Laberintoa3D_Final/Assets/Editor/HorrorEnemyAnimationsBatch.cs \
    Laberintoa3D_Final/Assets/Editor/HorrorEnemyAnimationsBatch.cs.meta \
    Laberintoa3D_Final/Assets/Laberintoa/Horror/Animations.meta \
    Laberintoa3D_Final/Assets/Laberintoa/Horror/Animations/ \
    Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs \
    Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs.meta \
    Laberintoa3D_Final/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs \
    Laberintoa3D_Final/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity
  git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --cached --check
  if ! git -C "$PUBLICO" diff --cached --quiet; then
    git -C "$PUBLICO" commit -m 'Laberintoa3D: hiru etsaien animazioak eta IA sinkronizatu'
    git -C "$PUBLICO" push origin feature/laberintoa-horror
  fi
  test -z "$(git -C "$PUBLICO" status --porcelain)" || {
    echo 'ERROR: tras checkpoint el público no está limpio.'; exit 1;
  }
  echo 'OK: animaciones respaldadas en los dos repositorios.'
}

rollback(){
  local code="$?"
  if [ "$SUCCESS" = 1 ] || [ -z "$BACKUP" ]; then return "$code"; fi
  echo; echo 'ERROR: construcción no validada. Restaurando escena y eliminando SOLO archivos que creó este Builder.'
  cp -a "$BACKUP/Laberintoa_Horror.unity" "$SCENE"
  rm -rf -- "$EDITOR_TARGET" "$EDITOR_TARGET.meta" "$MAT_TARGET" "$MAT_TARGET.meta" "$TOOL_REPO"
  rm -f -- "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs.meta" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorFlashlight.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorFlashlight.cs.meta" \
    "$NAV_TARGET" "$NAV_TARGET.meta"
  echo "Respaldo anterior y log: $BACKUP"
  echo 'Los checkpoints de animaciones previos NO se revierten: ya están seguros en GitHub.'
  return "$code"
}

apply(){
  need git; need gh; need git-lfs; need rsync; need python3
  check_base
  echo '=== 1. SEGURIDAD Y AUTENTICACION ==='
  if pgrep -f "[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final" >/dev/null || \
     pgrep -f "[U]nity.*-projectPath.*Laberintoa3D_Final" >/dev/null; then
    echo 'ERROR: cierra Unity (File > Exit) antes de --apply.'; exit 1
  fi
  check_animation_changes
  test -f "$SCRIPT_DIR/src/Editor/HorrorBuilderV1.cs" || { echo 'ZIP incompleto'; exit 1; }
  echo; echo '=== 2. PROTEGER ANIMACIONES EN AMBOS GITHUB ==='
  checkpoint_animations
  echo; echo '=== 3. RESPALDO LOCAL ANTES DE CONSTRUIR ==='
  BACKUP="$(mktemp -d "$HOME/LaberintoaHorror_BuilderV1_XXXXXXXX")"
  cp -a "$SCENE" "$BACKUP/Laberintoa_Horror.unity"
  cp -a "$TEACHER" "$BACKUP/SampleScene.unity"
  sha256sum "$TEACHER" > "$BACKUP/SampleScene.sha256"
  echo "Respaldo: $BACKUP"
  trap rollback EXIT
  echo; echo '=== 4. INSTALAR FUENTES NUEVAS (SIN TOCAR LOS SCRIPTS ANTERIORES) ==='
  mkdir -p "$EDITOR_TARGET" "$MAT_TARGET" "$TOOL_REPO"
  cp -a "$SCRIPT_DIR/src" "$TOOL_REPO/src"
  cp -a "$SCRIPT_DIR/docs" "$TOOL_REPO/docs"
  cp "$SCRIPT_DIR/horror-builder.sh" "$TOOL_REPO/horror-builder.sh"
  cp "$SCRIPT_DIR/src/Editor/HorrorBuilderV1.cs" "$EDITOR_TARGET/HorrorBuilderV1.cs"
  cp "$SCRIPT_DIR/src/Scripts/HorrorPlayerMovement.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs"
  cp "$SCRIPT_DIR/src/Scripts/HorrorFlashlight.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorFlashlight.cs"
  echo; echo '=== 5. UNITY BATCH: AUDITAR + CONSTRUIR + VALIDAR ==='
  set +e
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV1.Apply -logFile "$BACKUP/unity.log"
  local code="$?"
  set -e
  grep -E 'HBB1_(PRECHECK|NAV_OK|OK)|Exception|error CS[0-9]+' "$BACKUP/unity.log" | tail -n 45 || true
  if [ "$code" -ne 0 ] || ! grep -q 'HBB1_OK:' "$BACKUP/unity.log"; then
    echo "ERROR: Unity devolvió $code o falta HBB1_OK. Ver: $BACKUP/unity.log"
    return 1
  fi
  echo; echo '=== 6. VALIDACION DE ARCHIVOS Y ESCENA ORIGINAL ==='
  sha256sum -c "$BACKUP/SampleScene.sha256"
  for f in "$NAV_TARGET" "$MAT_TARGET/M_HB_Hormigoia.mat" \
      "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs.meta" \
      "$PROY/Assets/Laberintoa/Scripts/HorrorFlashlight.cs.meta"; do
    test -f "$f" || { echo "ERROR: no se creó $f"; return 1; }
  done
  git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --check
  echo; echo '=== 7. RESULTADO ==='
  SUCCESS=1
  echo 'HBB1_OK: construcción guardada localmente. SIN COMMIT NI PUSH DEL MUNDO NUEVO.'
  echo 'Checkpoint de animaciones realizado en Github al inicio.'
  git -C "$PUBLICO" status --short
  echo "Respaldo y log Unity: $BACKUP"
  echo 'Abrir en Unity y validar antes de subir la construcción.'
}

case "$CMD" in
  --help|-h) helpmsg;;
  --dry-run) need git; check_base; status_report;;
  --apply) apply;;
  *) helpmsg; exit 2;;
esac
