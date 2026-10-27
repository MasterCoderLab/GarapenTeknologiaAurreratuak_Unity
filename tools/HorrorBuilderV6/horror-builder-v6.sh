#!/usr/bin/env bash
set -euo pipefail

# Horror Builder v6 - ikuspegia eta hormen akabera. GitHub ez da aldatzen.
PUBLICO="$HOME/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity"
PROY="$PUBLICO/Laberintoa3D_Final"
PRIVADO="/home/ionberistain/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado"
RESPALDOS="/home/ionberistain/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/Baliabideak/Backups/Laberintoa_Horror/Snapshots"
UNITY="$HOME/Unity/Hub/Editor/6000.6.0f1/Editor/Unity"
ESCENA="$PROY/Assets/Laberintoa/Scenes/Laberintoa_Horror.unity"
ORIGINAL="$PROY/Assets/Laberintoa/Scenes/SampleScene.unity"
NAV="$PROY/Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset"
MOVIMIENTO="$PROY/Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs"
MOUSE="$PROY/Assets/Laberintoa/Scripts/HorrorMouseLook.cs"
EDITOR="$PROY/Assets/Editor/HorrorBuilderV6"
ARTE="$PROY/Assets/Laberintoa/Horror/BuilderV6"
HERRAMIENTAS="$PUBLICO/tools/HorrorBuilderV6"
AQUI="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MODO="${1:---audit}"
RESPALDO=""
TERMINADO=0

error(){ echo "HBB6_ERROR: $*" >&2; exit 1; }
need(){ command -v "$1" >/dev/null || error "Falta herramienta: $1"; }

# Rutas permitidas del historial v2-v5. Ningun otro fichero puede cambiar.
auditar_git(){
 python3 - "$PUBLICO" "$1" <<'PY'
from pathlib import Path
import subprocess,sys
repo,mode=sys.argv[1:]
raw=subprocess.check_output(['git','-C',repo,'status','--porcelain=v1','-z','--untracked-files=all'])
rows=[x for x in raw.split(b'\0') if x]
start='Laberintoa3D_Final/'
exact={start+'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
       start+'Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs',
       start+'Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs',
       start+'Assets/Laberintoa/Scripts/HorrorGameManager.cs',
       start+'Assets/Laberintoa/Scripts/HorrorGameManager.cs.meta',
       start+'Assets/Laberintoa/Scripts/HorrorShotgun.cs',
       start+'Assets/Laberintoa/Scripts/HorrorShotgun.cs.meta'}
allowed_files=[
 'Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs',
 'Assets/Laberintoa/Scripts/HorrorPlayerHealth.cs',
 'Assets/Laberintoa/Scripts/HorrorPlayerJump.cs',
]
for f in allowed_files: exact.update({start+f,start+f+'.meta'})
prefixes=[]
for name in ('HorrorBuilderV2','HorrorBuilderV3','HorrorBuilderV4','HorrorBuilderV5','HorrorNavMeshStartFix'):
 path=start+'Assets/Editor/'+name
 exact.add(path+'.meta');prefixes.append(path+'/')
for name in ('BuilderV2','BuilderV4','BuilderV5'):
 path=start+'Assets/Laberintoa/Horror/'+name
 exact.add(path+'.meta');prefixes.append(path+'/')
for name in ('HorrorBuilderV2','HorrorBuilderV3','HorrorBuilderV4','HorrorBuilderV5'):
 prefixes.append('tools/'+name+'/')
if mode=='after':
 for path in (start+'Assets/Editor/HorrorBuilderV6',start+'Assets/Laberintoa/Horror/BuilderV6'):
  exact.add(path+'.meta'); prefixes.append(path+'/')
 exact.add(start+'Assets/Laberintoa/Scripts/HorrorMouseLook.cs')
 exact.add(start+'Assets/Laberintoa/Scripts/HorrorMouseLook.cs.meta')
 prefixes.append('tools/HorrorBuilderV6/')
found=set()
for r in rows:
 if len(r)<4: raise SystemExit('HBB6_ERROR: entrada de Git incorrecta')
 flag=r[:2].decode('ascii');name=r[3:].decode('utf-8')
 if flag not in (' M','??') or not (name in exact or any(name.startswith(pre) for pre in prefixes)):
  raise SystemExit('HBB6_ERROR: cambio inesperado en Git: '+repr((flag,name)))
 found.add(name)
required={start+'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
          start+'Assets/Laberintoa/Scripts/HorrorPlayerMovement.cs',
          start+'Assets/Editor/HorrorBuilderV5/HorrorBuilderV5.cs',
          start+'Assets/Laberintoa/Horror/BuilderV5/Materials/HB5_HormaLuzea.mat'}
if not required.issubset(found):
 raise SystemExit('HBB6_ERROR: v5 no esta completo o faltan cambios: '+str(sorted(required-found)))
if mode=='before' and len(found)!=135:
 raise SystemExit('HBB6_ERROR: v5 tenia 135 cambios, ahora hay '+str(len(found))+'. Revisar antes de aplicar.')
if mode=='after' and (len(found)<=135 or start+'Assets/Laberintoa/Scripts/HorrorMouseLook.cs' not in found):
 raise SystemExit('HBB6_ERROR: no se detecta la instalacion completa de v6')
print(f'HBB6_STATUS_OK: {len(found)} rutas autorizadas; {mode}')
PY
}

# Solo comprueba la estructura; NO modifica nada.
preauditar(){
 need git;need python3;need sha256sum;need cmp
 for f in "$UNITY" "$ESCENA" "$ORIGINAL" "$NAV" "$MOVIMIENTO" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorShotgun.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerHealth.cs" \
   "$PROY/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs" \
   "$PROY/Assets/Laberintoa/Horror/BuilderV5/Materials/HB5_HormaLaburra.mat" \
   "$PROY/Assets/Laberintoa/Horror/BuilderV5/Materials/HB5_HormaErtaina.mat" \
   "$PROY/Assets/Laberintoa/Horror/BuilderV5/Materials/HB5_HormaLuzea.mat" \
   "$AQUI/src/Editor/HorrorBuilderV6.cs" "$AQUI/src/Scripts/HorrorMouseLook.cs"; do
   [[ -f "$f" ]] || error "Falta $f"
 done
 for path in "$MOUSE" "$MOUSE.meta" "$EDITOR" "$EDITOR.meta" "$ARTE" "$ARTE.meta" "$HERRAMIENTAS"; do
  [[ ! -e "$path" ]] || error "V6 ya existe o hay restos: $path"
 done
 [[ "$(git -C "$PUBLICO" branch --show-current)" == feature/laberintoa-horror ]] || error 'Rama publica incorrecta'
 [[ "$(git -C "$PRIVADO" branch --show-current)" == main ]] || error 'Rama privada incorrecta'
 [[ "$(git -C "$PUBLICO" rev-parse --short=8 HEAD)" == fd55fd79 ]] || error 'Commit publico nuevo: auditar antes'
 [[ "$(git -C "$PRIVADO" rev-parse --short=7 HEAD)" == f7a47d7 ]] || error 'Commit privado nuevo: auditar antes'
 [[ "$(git -C "$PUBLICO" rev-parse HEAD)" == "$(git -C "$PUBLICO" rev-parse origin/feature/laberintoa-horror)" ]] || error 'Publico sin sincronizar'
 [[ "$(git -C "$PRIVADO" rev-parse HEAD)" == "$(git -C "$PRIVADO" rev-parse origin/main)" ]] || error 'Privado sin sincronizar'
 [[ -z "$(git -C "$PUBLICO" diff --cached --name-only)" ]] || error 'Hay cambios staged publicos'
 [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || error 'Privado no esta limpio'
 auditar_git before
 python3 - "$MOVIMIENTO" <<'PY'
from pathlib import Path
import sys
s=Path(sys.argv[1]).read_text()
patterns=('    private float turnInput;',
 '        body.MoveRotation(body.rotation * Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f));')
for p in patterns:
 if s.count(p)!=1: raise SystemExit('HBB6_ERROR: estructura del script de movimiento no coincide. No modificar.')
if 'GehituSaguBiraketa' in s: raise SystemExit('HBB6_ERROR: movimiento de raton ya aplicado')
print('HBB6_MOVEMENT_AUDIT_OK: parche de dos puntos identificado sin ambiguedad')
PY
 echo 'HBB6_PREAUDIT_OK: v5 preservado; solo camara, movimiento y material de pared.'
}

rollback(){
 local codigo="$?"
 if [[ "$TERMINADO" == 0 && -n "$RESPALDO" ]]; then
  echo 'HBB6_ROLLBACK: restaurando escena y movimiento; retirando solo recursos V6.' >&2
  cp -a "$RESPALDO/Laberintoa_Horror.unity" "$ESCENA"
  cp -a "$RESPALDO/HorrorPlayerMovement.cs" "$MOVIMIENTO"
  rm -rf -- "$EDITOR" "$ARTE" "$HERRAMIENTAS"
  rm -f -- "$EDITOR.meta" "$ARTE.meta" "$MOUSE" "$MOUSE.meta"
  sha256sum -c "$RESPALDO/protegidos.sha256" || echo 'HBB6_ALERTA: revisar hash protegido distinto' >&2
  echo "Respaldo y log: $RESPALDO" >&2
  echo 'HBB6_RESULTADO: ERROR — rollback aplicado; NO volver a ejecutar sin revisar.' >&2
 fi
 return "$codigo"
}

parchear_movimiento(){
 python3 - "$MOVIMIENTO" <<'PY'
from pathlib import Path
import sys
f=Path(sys.argv[1]);s=f.read_text()
p1='    private float turnInput;'
r1='''    private float turnInput;
    private float saguBiraketaZain;

    /// <summary>
    /// Saguaren biraketa hurrengo fisika-urratsean aplikatu, Rigidbody errespetatuz.
    /// </summary>
    public void GehituSaguBiraketa(float graduak)
    {
        saguBiraketaZain += graduak;
    }'''
p2='        body.MoveRotation(body.rotation * Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f));'
r2='''        float biraketa = turnInput * turnSpeed * Time.fixedDeltaTime + saguBiraketaZain;
        saguBiraketaZain = 0f;
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, biraketa, 0f));'''
for a,b in ((p1,r1),(p2,r2)):
 if s.count(a)!=1: raise SystemExit('HBB6_ERROR: no se reconoce script, no modificar')
 s=s.replace(a,b,1)
f.write_text(s)
print('HBB6_MOVEMENT_PATCH_OK: Rigidbody + mouse yaw, sin cambiar velocidad ni salto')
PY
}

apply(){
 preauditar
 if pgrep -f '[E]ditor/Unity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
    pgrep -f '[U]nity.*-projectPath.*Laberintoa3D_Final' >/dev/null || \
    [[ -e "$PROY/Library/UnityLockfile" ]]; then
   error 'Unity abierto: guardar y File > Exit antes de aplicar'
 fi
 echo '=== 1. RESPALDO LOCAL V5 ==='
 mkdir -p "$RESPALDOS"
  RESPALDO="$(mktemp -d "$RESPALDOS/HorrorBuilderV6_XXXXXXXX")"
 cp -a "$ESCENA" "$RESPALDO/Laberintoa_Horror.unity"
 cp -a "$MOVIMIENTO" "$RESPALDO/HorrorPlayerMovement.cs"
 sha256sum "$ORIGINAL" "$NAV" \
  "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAI.cs" \
  "$PROY/Assets/Laberintoa/Scripts/HorrorEnemyAnimation.cs" \
  "$PROY/Assets/Laberintoa/Scripts/HorrorNavMeshRuntime.cs" \
  "$PROY/Assets/Laberintoa/Scripts/HorrorShotgun.cs" \
  "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerHealth.cs" \
  "$PROY/Assets/Laberintoa/Scripts/HorrorPlayerJump.cs" \
  "$PROY/Assets/Laberintoa/Scripts/HorrorGameManager.cs" \
  "$PROY/Assets/Laberintoa/Horror/BuilderV5/Materials/HB5_HormaLaburra.mat" \
  "$PROY/Assets/Laberintoa/Horror/BuilderV5/Materials/HB5_HormaErtaina.mat" \
  "$PROY/Assets/Laberintoa/Horror/BuilderV5/Materials/HB5_HormaLuzea.mat" \
  > "$RESPALDO/protegidos.sha256"
 git -C "$PUBLICO" status --short --branch > "$RESPALDO/git_antes.txt"
 trap rollback EXIT
 echo "Respaldo: $RESPALDO"
 echo '=== 2. INSTALAR SOLO V6 ==='
 mkdir -p "$EDITOR" "$ARTE/Materials" "$HERRAMIENTAS"
 printf '%s\n' 'Builder v6 - Horma PBR material leunduak, jatorrizko V5ak aldatu gabe.' > "$ARTE/Materials/LEEME_MATERIALAK.txt"
 cp "$AQUI/src/Editor/HorrorBuilderV6.cs" "$EDITOR/HorrorBuilderV6.cs"
 cp "$AQUI/src/Scripts/HorrorMouseLook.cs" "$MOUSE"
 cp -a "$AQUI/src" "$AQUI/docs" "$HERRAMIENTAS/"
 cp "$AQUI/horror-builder-v6.sh" "$HERRAMIENTAS/horror-builder-v6.sh"
 parchear_movimiento
 echo '=== 3. UNITY BATCH — CAMARA + PAREDES ==='
 local codigo=0
 __NV_PRIME_RENDER_OFFLOAD=1 __VK_LAYER_NV_optimus=NVIDIA_only \
  "$UNITY" -batchmode -quit -force-vulkan -projectPath "$PROY" \
  -executeMethod HorrorBuilderV6.Apply -logFile "$RESPALDO/unity.log" || codigo=$?
 grep -E 'HBB6_|error CS[0-9]+|Exception|Error:' "$RESPALDO/unity.log" | tail -n 45 || true
 [[ "$codigo" -eq 0 ]] || error "Unity ha fallado ($codigo): $RESPALDO/unity.log"
 grep -q 'HBB6_PRECHECK_OK:' "$RESPALDO/unity.log" || error 'Falta HBB6_PRECHECK_OK'
 grep -q 'HBB6_OK:' "$RESPALDO/unity.log" || error 'Falta HBB6_OK'
 echo '=== 4. ARCHIVOS PROTEGIDOS + GIT ==='
 sha256sum -c "$RESPALDO/protegidos.sha256" || error 'Un archivo protegido ha cambiado'
 for f in "$MOUSE.meta" "$EDITOR/HorrorBuilderV6.cs.meta" \
  "$ARTE/Materials/HB6_HormaLaburra.mat" \
  "$ARTE/Materials/HB6_HormaErtaina.mat" \
  "$ARTE/Materials/HB6_HormaLuzea.mat"; do
   [[ -f "$f" ]] || error "Falta recurso creado: $f"
 done
 git -C "$PUBLICO" -c core.whitespace=-blank-at-eol diff --check
 auditar_git after
 [[ -z "$(git -C "$PRIVADO" status --porcelain --untracked-files=all)" ]] || error 'Repo privado modificado'
 TERMINADO=1
 echo 'HBB6_LOCAL_OK: mejoras instaladas; sin commit ni push.'
 echo "Respaldo y log: $RESPALDO"
 echo 'HBB6_RESULTADO: OK — abrir Unity, probar mouse X/Y, colisiones y pared PBR.'
}

case "$MODO" in
 --audit|--dry-run) echo '=== HORROR BUILDER V6 - AUDITORIA SOLO LECTURA ===';preauditar;echo 'HBB6_AUDIT_OK: listo para aplicar con Unity cerrado';;
 --apply) apply;;
 --help|-h) echo 'Uso: bash horror-builder-v6.sh --audit | --apply';;
 *) error 'Opcion desconocida';;
esac
