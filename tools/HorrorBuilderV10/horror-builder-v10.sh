#!/usr/bin/env bash
set -euo pipefail
export GIT_PAGER=cat PAGER=cat

# V10: bunker iluna + HUD berria + tiroen arabera etsaien osasuna.
# V7/V8/V9 lokala mantendu. SampleScene/NavMesh eta GitHub ez ukitu.
ASIG="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak"
PUBLICO="$ASIG/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUBLICO/Laberintoa3D_Final"
PRIVADO="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado"
SNAPSHOTS="$ASIG/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
ESCENA="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
SCRIPT_DIR="$PROY/Assets/Laberintoa/Scripts"
GAME="$SCRIPT_DIR/HorrorGameManager.cs"
SHOTGUN="$SCRIPT_DIR/HorrorShotgun.cs"
HEALTH="$SCRIPT_DIR/HorrorEnemyHealth.cs"
ATMOS="$SCRIPT_DIR/HorrorAtmosphereV9.cs"
HUD="$SCRIPT_DIR/HorrorHUDV10.cs"
DARK="$SCRIPT_DIR/HorrorBunkerDarknessV10.cs"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV10"
RESOURCES="$PROY/Assets/Resources"
ICONOS="$RESOURCES/HorrorHUDV10"
TOOLS="$PUBLICO/tools/HorrorBuilderV10"
AQUI="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODO="${1:---audit}"
RESPALDO=""
TERMINADO=0
CREATED_RESOURCES=0

fallar() { echo "HBB10_ERROR: $*" >&2; exit 1; }
comando() { command -v "$1" >/dev/null 2>&1 || fallar "Falta herramienta: $1"; }

# V7-V9 fitxategiak baimendu; V10ko fitxategi berriak ondoren bakarrik.
check_status() {
  python3 - "$PUBLICO" "$1" <<'PY_STATUS'
import subprocess, sys
repo, phase = sys.argv[1:]
base='Laberintoa3D_Final/'
scene=base+'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity'
move=base+'Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs'
originals={base+'Assets/Laberintoa/Scripts/'+f for f in (
    'HorrorGameManager.cs','HorrorShotgun.cs','HorrorEnemyHealth.cs')}
v7_exact={scene,base+'Assets/Editor/HorrorBuilderV7.meta',
    base+'Assets/Laberintoa/Horror/BuilderV7.meta',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs.meta'}
v7_prefix=[base+'Assets/Editor/HorrorBuilderV7/',base+'Assets/Laberintoa/Horror/BuilderV7/','tools/HorrorBuilderV7/']
v8_exact={base+'Assets/Editor/HorrorBuilderV8.meta'}
v8_prefix=[base+'Assets/Editor/HorrorBuilderV8/','tools/HorrorBuilderV8/']
v9_exact={move,base+'Assets/Editor/HorrorBuilderV9.meta',
    base+'Assets/Laberintoa/Scripts/HorrorAtmosphereV9.cs',
    base+'Assets/Laberintoa/Scripts/HorrorAtmosphereV9.cs.meta'}
v9_prefix=[base+'Assets/Editor/HorrorBuilderV9/','tools/HorrorBuilderV9/']
v10_exact=originals|{
    base+'Assets/Editor/HorrorBuilderV10.meta',
    base+'Assets/Resources.meta',base+'Assets/Resources/HorrorHUDV10.meta',
    base+'Assets/Laberintoa/Scripts/HorrorHUDV10.cs',
    base+'Assets/Laberintoa/Scripts/HorrorHUDV10.cs.meta',
    base+'Assets/Laberintoa/Scripts/HorrorBunkerDarknessV10.cs',
    base+'Assets/Laberintoa/Scripts/HorrorBunkerDarknessV10.cs.meta'}
v10_prefix=[base+'Assets/Editor/HorrorBuilderV10/',
    base+'Assets/Resources/HorrorHUDV10/', 'tools/HorrorBuilderV10/']
raw=subprocess.check_output(['git','-C',repo,'status','--porcelain=v1','-z','--untracked-files=all'])
rows=[r for r in raw.split(b'\0') if r]
seen=set()
for row in rows:
    status=row[:2].decode('ascii'); name=row[3:].decode('utf-8','surrogateescape')
    if status not in (' M','??'):
        sys.exit('HBB10_ERROR: Git egoera ustekabekoa '+repr((status,name)))
    tracked={scene,move}|(originals if phase=='after' else set())
    if name in tracked and status!=' M':
        sys.exit('HBB10_ERROR: tracked izan beharko luke: '+name)
    if name not in tracked and status!='??':
        sys.exit('HBB10_ERROR: tracked fitxategi ustekabekoa: '+name)
    allowed=(name in v7_exact|v8_exact|v9_exact or
        any(name.startswith(p) for p in v7_prefix+v8_prefix+v9_prefix) or
        (phase=='after' and (name in v10_exact or any(name.startswith(p) for p in v10_prefix))))
    if not allowed:sys.exit('HBB10_ERROR: baimenik gabeko fitxategia: '+name)
    seen.add(name)
required={scene,move,base+'Assets/Laberintoa/Scripts/HorrorAtmosphereV9.cs',
    base+'Assets/Editor/HorrorBuilderV7/HorrorBuilderV7.cs',
    base+'Assets/Editor/HorrorBuilderV8/HorrorBuilderV8.cs',
    base+'Assets/Editor/HorrorBuilderV9/HorrorBuilderV9.cs',
    base+'Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs',
    'tools/HorrorBuilderV9/horror-builder-v9.sh'}
if not required.issubset(seen):sys.exit('HBB10_ERROR: V7-V9 osatu gabe '+repr(sorted(required-seen)))
if phase=='before':
    if len(seen)!=36:sys.exit(f'HBB10_ERROR: V9k 36 aldaketa espero; {len(seen)} aurkitu')
    if seen & originals:sys.exit('HBB10_ERROR: tiro/manager kodea jada aldatuta')
else:
    required_v10=originals|{
        base+'Assets/Editor/HorrorBuilderV10/HorrorBuilderV10.cs',
        base+'Assets/Laberintoa/Scripts/HorrorHUDV10.cs',
        base+'Assets/Laberintoa/Scripts/HorrorBunkerDarknessV10.cs',
        base+'Assets/Resources/HorrorHUDV10/shell_ready.png',
        base+'Assets/Resources/HorrorHUDV10/shell_empty.png',
        base+'Assets/Resources/HorrorHUDV10/bunker_vignette.png',
        'tools/HorrorBuilderV10/horror-builder-v10.sh'}
    if not required_v10.issubset(seen):
        sys.exit('HBB10_ERROR: V10 osatu gabe '+repr(sorted(required_v10-seen)))
print(f'HBB10_STATUS_OK: {len(seen)} aldaketa baimendu; {phase}')
PY_STATUS
}

protected_hashes() {
  sha256sum \
    "$PROY/Assets/Laberintoa/Scenes/SampleScene.unity" \
    "$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorMouseLook.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorFlashlight.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs" \
    "$PROY/Assets/Laberintoa/Scripts/HorrorViewmodelSafety.cs" \
    "$PROY/Assets/Editor/HorrorBuilderV7/HorrorBuilderV7.cs" \
    "$PROY/Assets/Editor/HorrorBuilderV8/HorrorBuilderV8.cs" \
    "$PROY/Assets/Editor/HorrorBuilderV9/HorrorBuilderV9.cs" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLaburra.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaErtaina.mat" \
    "$PROY/Assets/Laberintoa/Horror/BuilderV7/Materials/HB7_HormaLuzea.mat" \
    "$PROY/ProjectSettings/TagManager.asset" \
    "$PROY/Packages/manifest.json"
}

# Egungo fitxategien zati zehatzak soilik aldatzen dira; patroiak oker badaude ezer ez.
patch_code() {
 python3 - "$GAME" "$SHOTGUN" "$HEALTH" "$ATMOS" "$AQUI" "${1:---check}" <<'PY_PATCH'
from pathlib import Path
import sys
GM, SG, HP, AT, ROOT = (Path(p) for p in sys.argv[1:6])
mode = sys.argv[6]
source={p:p.read_text() for p in (GM,SG,HP,AT)}
changes={}

def replace(path,old,new):
    text=changes.get(path,source[path])
    if text.count(old)!=1 or (new and new in text):
        raise SystemExit('HBB10_PATCH_ERROR: ez dator bat: '+path.name+' / '+old[:90])
    changes[path]=text.replace(old,new,1)

replace(GM,'    public int Lives => lives;',
    '    public int Lives => lives;\n    public int DefeatedCount => defeated;')
replace(GM,'''        GUI.Box(new Rect(14, 12, 560, 112), "");
        GUI.Label(new Rect(26, 22, 530, 26), "BIZITZAK  " + lives + "/3    ETSAIAK  " + defeated + "/3", smallStyle);
        GUI.Label(new Rect(26, 48, 530, 26), shotgun != null ? shotgun.StatusText : "Aurkitu eskopeta", smallStyle);
        GUI.Label(new Rect(26, 76, 530, 24), "WASD: mugitu  ESPAZIOA: jauzi  F: linterna  E: hartu", GUI.skin.label);
''','')
replace(SG,'    public string StatusText', '''    public bool Collected => collected;
    public bool IsReloading => reloading;
    public int Loaded => loaded;
    public int Reserve => reserve;

    public string StatusText''')
replace(SG,'if (enemy != null) enemy.TakeShot();',
    'if (enemy != null) enemy.TakeShot(cam.transform.position, cam.transform.forward, hit.point);')
replace(AT,'    private bool hasieratuta;',
    '    private bool hasieratuta;\n\n    public float BunkerBlend => bunkerNahasketa;')
origHP=source[HP]
required=['public sealed class HorrorEnemyHealth','private int hitPoints = 2;',
          'public void TakeShot()','manager.EnemyDefeated();']
for part in required:
    if origHP.count(part)!=1:
        raise SystemExit('HBB10_PATCH_ERROR: HPren bertsioa ezezaguna: '+part)
changes[HP]=(ROOT/'src/Scripts/HorrorEnemyHealth.cs').read_text()
if mode=='--apply':
    for file,value in changes.items():file.write_text(value)
    print('HBB10_PATCH_OK: manager HUD zaharra kendu; kartutxoak; 3 HP eta headshot; atmosfera lotura.')
else: print('HBB10_PATCH_AUDIT_OK: 4 script-en ereduak aurkituta; aldaketarik gabe.')
PY_PATCH
}

preauditar() {
  for prog in git python3 sha256sum cp grep pgrep mktemp; do comando "$prog"; done
  for file in "$UNITY" "$ESCENA" "$GAME" "$SHOTGUN" "$HEALTH" "$ATMOS"; do
    [[ -f "$file" ]] || fallar "Fitxategia falta da: $file"
  done
  for file in \
    "$AQUI/src/Editor/HorrorBuilderV10.cs" \
    "$AQUI/src/Scripts/HorrorEnemyHealth.cs" \
    "$AQUI/src/Scripts/HorrorHUDV10.cs" \
    "$AQUI/src/Scripts/HorrorBunkerDarknessV10.cs" \
    "$AQUI/src/Resources/HorrorHUDV10/shell_ready.png" \
    "$AQUI/src/Resources/HorrorHUDV10/shell_empty.png" \
    "$AQUI/src/Resources/HorrorHUDV10/bunker_vignette.png"; do
    [[ -f "$file" ]] || fallar "V10 iturburua falta da: $file"
  done
  for new in "$EDITOR" "$EDITOR.meta" "$TOOLS" "$ICONOS" "$ICONOS.meta" \
      "$HUD" "$HUD.meta" "$DARK" "$DARK.meta"; do
    [[ ! -e "$new" ]] || fallar "V10 dagoeneko existitzen da: $new"
  done
  [[ "$(git -C "$PUBLICO" branch --show-current)" == feature/laberintoa-horror ]] || fallar 'Rama publikoa okerra'
  [[ "$(git -C "$PRIVADO" branch --show-current)" == main ]] || fallar 'Rama pribatua okerra'
  [[ "$(git -C "$PUBLICO" rev-parse --short=8 HEAD)" == c5370157 ]] || fallar 'Commit publikoa ez da espero zena'
  [[ "$(git -C "$PRIVADO" rev-parse --short=7 HEAD)" == b2691f0 ]] || fallar 'Commit pribatua ez da espero zena'
  [[ "$(git -C "$PUBLICO" rev-parse HEAD)" == "$(git -C "$PUBLICO" ls-remote origin refs/heads/feature/laberintoa-horror | cut -f1)" ]] || fallar 'Publikoa ez da GitHubekin bat etortzen'
  [[ "$(git -C "$PRIVADO" rev-parse HEAD)" == "$(git -C "$PRIVADO" ls-remote origin refs/heads/main | cut -f1)" ]] || fallar 'Pribatua ez da GitHubekin bat etortzen'
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fallar 'Pribatua ez dago garbi'
  [[ -z "$(git -C "$PUBLICO" diff --cached --name-only)" ]] || fallar 'Publikoan staged fitxategiak daude'
  protected_hashes >/dev/null || fallar 'Babestutako fitxategiak falta dira'
  check_status before
  patch_code --check
  echo 'HBB10_PREAUDIT_OK: V9ren 36 aldaketa, HUD/tiro patroiak eta repo-ak egiaztatuta.'
}

rollback() {
  local code="$?"
  if [[ "$TERMINADO" == 0 && -n "$RESPALDO" ]]; then
    echo 'HBB10_ROLLBACK: V9ko 5 fitxategiak leheneratzen; V10ko berriak soilik kentzen.' >&2
    cp -a "$RESPALDO/Laberintoa_Horror.unity" "$ESCENA" || true
    for n in HorrorGameManager.cs HorrorShotgun.cs HorrorEnemyHealth.cs HorrorAtmosphereV9.cs; do
      cp -a "$RESPALDO/$n" "$SCRIPT_DIR/$n" || true
    done
    rm -rf -- "$EDITOR" "$TOOLS" "$ICONOS"
    rm -f -- "$EDITOR.meta" "$ICONOS.meta" "$HUD" "$HUD.meta" "$DARK" "$DARK.meta"
    if [[ "$CREATED_RESOURCES" == 1 ]]; then
      rmdir "$RESOURCES" 2>/dev/null || true
      rm -f -- "$RESOURCES.meta"
    fi
    sha256sum -c "$RESPALDO/protegidos.sha256" || true
    echo "HBB10_ERROR_LOG: $RESPALDO/unity.log" >&2
    echo 'HBB10_RESULTADO: ERROR — V9ra itzuli da; ez errepikatu auditatu gabe.' >&2
  fi
  return "$code"
}

aplicar() {
  preauditar
  if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
    pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
    [[ -e "$PROY/Library/UnityLockfile" ]]; then
    fallar 'Unity irekita dago: eszena gorde eta Unity itxi.'
  fi
  echo '=== 1. V9REN BABES-KOPIA ==='
  mkdir -p "$SNAPSHOTS"
  RESPALDO="$(mktemp -d "$SNAPSHOTS/HorrorBuilderV10_XXXXXXXX")"
  cp -a "$ESCENA" "$RESPALDO/Laberintoa_Horror.unity"
  for n in HorrorGameManager.cs HorrorShotgun.cs HorrorEnemyHealth.cs HorrorAtmosphereV9.cs; do
    cp -a "$SCRIPT_DIR/$n" "$RESPALDO/$n"
  done
  protected_hashes > "$RESPALDO/protegidos.sha256"
  git -C "$PUBLICO" status --short --branch > "$RESPALDO/git_antes.txt"
  [[ -d "$RESOURCES" ]] || CREATED_RESOURCES=1
  trap rollback EXIT
  trap 'exit 130' INT
  echo "Respaldo: $RESPALDO"

  echo '=== 2. V10 KODEA, KARTUTXOAK ETA HUD ==='
  mkdir -p "$EDITOR" "$ICONOS" "$TOOLS"
  cp "$AQUI/src/Editor/HorrorBuilderV10.cs" "$EDITOR/HorrorBuilderV10.cs"
  cp "$AQUI/src/Scripts/HorrorHUDV10.cs" "$HUD"
  cp "$AQUI/src/Scripts/HorrorBunkerDarknessV10.cs" "$DARK"
  cp "$AQUI/src/Resources/HorrorHUDV10/"*.png "$ICONOS/"
  cp -a "$AQUI/src" "$AQUI/docs" "$TOOLS/"
  cp "$AQUI/horror-builder-v10.sh" "$TOOLS/horror-builder-v10.sh"
  patch_code --apply

  echo '=== 3. UNITY BATCH — ILUNTASUNA, HUD ETA TIROAK ==='
  local code=0
  __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
    "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
    -executeMethod HorrorBuilderV10.Apply -logFile "$RESPALDO/unity.log" || code=$?
  grep -E 'HBB10_|error CS[0-9]+|Exception|Error:' "$RESPALDO/unity.log" | tail -n 50 || true
  [[ "$code" == 0 ]] || fallar "Unity devolvio $code; revisar $RESPALDO/unity.log"
  grep -q 'HBB10_PRECHECK_OK:' "$RESPALDO/unity.log" || fallar 'Falta HBB10_PRECHECK_OK'
  grep -q 'HBB10_OK:' "$RESPALDO/unity.log" || fallar 'Falta HBB10_OK'

  echo '=== 4. BABESTUTAKO FITXATEGIAK ETA GIT ==='
  sha256sum -c "$RESPALDO/protegidos.sha256" || fallar 'Babestutako fitxategia aldatu da'
  for file in "$HUD.meta" "$DARK.meta" "$EDITOR/HorrorBuilderV10.cs.meta" \
    "$ICONOS/shell_ready.png.meta" "$ICONOS/shell_empty.png.meta" "$ICONOS/bunker_vignette.png.meta"; do
      [[ -f "$file" ]] || fallar "Unity .meta falta da: $file"
  done
  if [[ "$(sha256sum "$ESCENA" | cut -d' ' -f1)" == "$(sha256sum "$RESPALDO/Laberintoa_Horror.unity" | cut -d' ' -f1)" ]]; then
    fallar 'Eszena ez da aldatu'
  fi
  git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --check
  check_status after
  [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || fallar 'Pribatua aldatu da'
  TERMINADO=1
  echo 'HBB10_LOCAL_OK: bunker ilunagoa, kartutxoen HUD, bizitza gorria, etsai 3HP/headshot; V9 kontrolak, NavMesh eta PBR ez dira aldatu.'
  echo "Respaldo y log: $RESPALDO"
  echo 'HBB10_RESULTADO: OK — Unity Play probatu; GitHub ez da aldatu.'
}

case "$MODO" in
  --audit|--dry-run) echo '=== HORROR BUILDER V10 — AUDITORIA SOLO LECTURA ==='; preauditar; echo 'HBB10_AUDIT_OK';;
  --apply) aplicar;;
  --help|-h) echo 'Uso: bash horror-builder-v10.sh --audit | --apply';;
  *) fallar "Opcion desconocida: $MODO";;
esac
