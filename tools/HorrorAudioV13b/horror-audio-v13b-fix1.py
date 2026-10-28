#!/usr/bin/env python3
"""Horror Audio v13b fix1: reutiliza cache y prepara WAV antes de aplicar."""
from __future__ import annotations
import argparse
import hashlib
import io
import os
from pathlib import Path
import shutil
import subprocess
import sys
import urllib.request
import zipfile
import time
import json

HOME = Path.home()
SUBJECT = HOME / 'Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak'
PUBLIC = SUBJECT / '1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity'
PROJECT = PUBLIC / 'Laberintoa3D_Final'
PRIVATE = SUBJECT / 'Baliabideak/Backups/Laberintoa_Horror/Repositorio_Privado/Laberintoa3D_Horror_Backup_Privado'
SNAPS = SUBJECT / 'Baliabideak/Backups/Laberintoa_Horror/Snapshots'
UNITY = HOME / 'Unity/Hub/Editor/6000.6.0f1/Editor/Unity'
PKG = Path(__file__).resolve().parent
ASSETS = PROJECT / 'Assets/Laberintoa/Horror/AudioV13'
SCRIPT = PROJECT / 'Assets/Laberintoa/Scripts/HorrorAudioV13.cs'
ORIG_TOOLS = PUBLIC / 'tools/HorrorBuilderV13'
NEW_TOOLS = PUBLIC / 'tools/HorrorAudioV13b'
RES_DIR = PROJECT / 'Assets/Resources/HorrorAudioV13b'
EDITOR_DIR = PROJECT / 'Assets/Editor/HorrorAudioV13b'
OLD_EDITOR = PROJECT / 'Assets/Editor/HorrorBuilderV13'
WAV_NAMES = ['menu_ambience','bunker_ambience','forest_ambience','footstep','shotgun','reload','monster','damage','ending']
BASE = 'https://opengameart.org/sites/default/files/'
SOURCES = {
  'steps01': ('01-footstep_0.ogg', 500),
  'steps02': ('02-footstep.ogg', 500),
  'steps03': ('03-footstep.ogg', 500),
  'steps04': ('04-footstep.ogg', 500),
  'steps05': ('05-footstep.ogg', 500),
  'steps06': ('06-footstep.ogg', 500),
  'step_types': ('%5Bkdd%5DDifferentSteps_0.zip', 800),
  'shotgun': ('sounds.zip', 18000),
  'reload': ('shotgunsounds.zip', 2000),
  'monster': ('monster-sounds-volume-2.zip', 6000),
  'damage': ('5Hit_Sounds.zip', 1200),
  'menu': ('ambient_horror_0.ogg', 8000),
  'bunker': ('dungeon_ambient_1_0.ogg', 8000),
  'forest': ('Forest_Ambience.mp3', 8000),
}
SOURCES_META = {
  'steps': 'https://opengameart.org/content/footsteps-0',
  'step_types': 'https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud',
  'shotgun': 'https://opengameart.org/content/gunshot-sounds',
  'reload': 'https://opengameart.org/content/shotgun-reload-sound-effects',
  'monster': 'https://opengameart.org/content/monster-sound-pack-volume-1',
  'damage': 'https://opengameart.org/content/5-hit-sounds-dying',
  'menu': 'https://opengameart.org/content/ambient-horror',
  'bunker': 'https://opengameart.org/content/loopable-dungeon-ambience',
  'forest': 'https://opengameart.org/content/forest-ambience',
}

def die(msg):
    raise RuntimeError(msg)

def git(repo, *args):
    return subprocess.check_output(['git', '-C', str(repo), *args], text=True).rstrip('\n')

def audit():
    print('=== HORROR AUDIO V13B - AUDITORIA SOLO LECTURA ===', flush=True)
    for bin in ('git', 'ffmpeg', 'ffprobe', 'unzip', 'gh'):
        if shutil.which(bin) is None: die('Falta herramienta: '+bin+' (no se ha modificado nada)')
    for p in (UNITY, ASSETS, SCRIPT, ORIG_TOOLS, OLD_EDITOR, PROJECT/'Assets/Laberintoa/Scenes/Hasiera.unity', PROJECT/'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity'):
        if not p.exists(): die('Falta ruta base: '+str(p))
    if RES_DIR.exists() or NEW_TOOLS.exists() or EDITOR_DIR.exists(): die('V13b ya existe o hay restos; no repetir apply')
    if git(PUBLIC, 'branch', '--show-current') != 'feature/laberintoa-horror': die('Rama publica inesperada')
    if git(PUBLIC, 'rev-parse', '--short=8', 'HEAD') != 'df68bc64': die('Checkpoint publico inesperado')
    if git(PRIVATE, 'branch', '--show-current') != 'main': die('Rama privada inesperada')
    if git(PRIVATE, 'status', '--porcelain', '--untracked-files=all'): die('Privado no limpio')
    if git(PUBLIC,'rev-parse','HEAD') != git(PUBLIC,'ls-remote','origin','refs/heads/feature/laberintoa-horror').split()[0]: die('Publico no sincronizado')
    if git(PRIVATE,'rev-parse','HEAD') != git(PRIVATE,'ls-remote','origin','refs/heads/main').split()[0]: die('Privado no sincronizado')
    # La version actual solo puede contener los cambios locales de v12 y v13.
    lines = git(PUBLIC, 'status', '--porcelain=v1', '-z', '--untracked-files=all')
    # -z llega como cadena que conserva NUL: no se autorizan renombres/ borrados previos.
    entries = [v for v in lines.split('\0') if v]
    if len(entries) < 30 or len(entries) > 95: die('Numero inesperado de cambios locales: '+str(len(entries)))
    paths = []
    prefix = 'Laberintoa3D_Final/'
    allowed = (prefix+'Assets/Editor/HorrorBuilderV12', prefix+'Assets/Laberintoa/Horror/BuilderV12',
         prefix+'Assets/Laberintoa/Scenes/Hasiera.unity', prefix+'Assets/Laberintoa/Scripts/HorrorMainMenuV12',
         prefix+'Assets/Laberintoa/Scripts/HorrorEnemyHealth.cs', prefix+'ProjectSettings/EditorBuildSettings.asset',
         'tools/HorrorBuilderV12/', prefix+'Assets/Editor/HorrorBuilderV13',
         prefix+'Assets/Laberintoa/Horror/AudioV13', prefix+'Assets/Laberintoa/Scripts/HorrorAudioV13.cs',
         prefix+'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity', 'tools/HorrorBuilderV13/')
    for entry in entries:
        if len(entry)<4 or entry[:2] not in (' M','??') or not entry[3:].startswith(allowed):
            die('Cambio no autorizado: '+repr(entry))
        paths.append(entry[3:])
    for expected in ('tools/HorrorBuilderV13/',prefix+'Assets/Laberintoa/Horror/AudioV13', prefix+'Assets/Laberintoa/Scenes/Hasiera.unity'):
        if not any(p.startswith(expected) for p in paths): die('Falta estado v12/v13: '+expected)
    for name in WAV_NAMES:
        p = ASSETS / (name+'.wav')
        if not p.is_file() or not (ASSETS/(name+'.wav.meta')).is_file(): die('Audio/meta v13 ausente: '+name)
    code = SCRIPT.read_text()
    for v in ['public sealed class HorrorAudioV13', 'public AudioClip menuGiroa', 'private void Update()']:
        if v not in code: die('Controlador v13 no esperado: '+v)
    if 'HorrorAudioV13b' in code: die('Ya hay cambios v13b en el script')
    if not (PKG/'src/Scripts/HorrorAudioV13.cs').is_file(): die('Falta codigo v13b')
    print('HBB13B_STATUS_OK:',len(entries),'cambios v12+v13 permitidos')
    print('HBB13B_AUDIT_OK: ninguna modificacion; fuentes CC0 identificadas; descargas pendientes')


def get_file(name, target):
    rel, max_kb = SOURCES[name]
    headers = {'User-Agent':'Mozilla/5.0 (X11; Linux x86_64) HorrorUnityDAM/1.0',
               'Referer':'https://opengameart.org/'}
    urls = [BASE+rel, 'https://lpc.opengameart.org/sites/default/files/'+rel]
    errors=[]
    for url in urls:
        try:
            req = urllib.request.Request(url, headers=headers)
            with urllib.request.urlopen(req, timeout=35) as response:
                data = response.read(max_kb*1024+1)
            if len(data)<600 or len(data)>max_kb*1024: die('descarga con tamano invalido')
            if rel.endswith('.zip') and not zipfile.is_zipfile(io.BytesIO(data)): die('No es ZIP valido')
            if rel.endswith('.ogg') and not data.startswith(b'OggS'): die('No es un OGG valido')
            if rel.endswith('.mp3') and not (data.startswith(b'ID3') or data[0]==0xff): die('No es MP3 valido')
            target.write_bytes(data)
            print('HBB13B_DOWNLOAD_OK:',name,len(data),'bytes',flush=True)
            return {'url':url,'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data)}
        except Exception as e:
            errors.append(str(e)[:180])
    die('Descarga '+name+' fallida: '+' | '.join(errors))


def audio_members(blob):
    with zipfile.ZipFile(io.BytesIO(blob)) as z:
        files=[]
        for item in z.infolist():
            if item.is_dir() or item.file_size < 300 or item.file_size > 20_000_000 or '__MACOSX/' in item.filename or '/._' in item.filename: continue
            if Path(item.filename).suffix.lower() not in ('.ogg','.wav','.mp3','.flac'): continue
            files.append((item.filename, z.read(item)))
        if not files: die('ZIP sin muestras de audio')
        return files

def choose(members, preferred, exclude=()):
    def key(item):
        name = item[0].lower()
        return (sum(15 for x in preferred if x in name) - sum(20 for x in exclude if x in name),
                min(len(item[1]),1_000_000))
    ordered = sorted(members, key=key, reverse=True)
    if preferred and not any(x in ordered[0][0].lower() for x in preferred):
        die('Archivo buscado no identificado en ZIP; archivos: '+', '.join(n for n,_ in members[:25]))
    return ordered


def convert(blob, ext, dest, seconds, gain=1.0):
    dest.parent.mkdir(parents=True, exist_ok=True)
    ext = ext.lower().replace('.','')
    p = subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-nostdin','-y',
       '-f', 'ogg' if ext=='ogg' else ('wav' if ext=='wav' else ('mp3' if ext=='mp3' else 'flac')),
       '-i','pipe:0','-ac','1','-ar','44100','-t',str(seconds),'-af',f'volume={gain}',
       '-c:a','pcm_s16le',str(dest)],input=blob,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if p.returncode or not dest.exists() or dest.stat().st_size<5000:
        die('ffmpeg no pudo convertir '+dest.name+': '+p.stderr.decode(errors='replace')[-500:])
    probe=subprocess.run(['ffprobe','-v','error','-show_entries','format=duration',
       '-of','default=noprint_wrappers=1:nokey=1',str(dest)],capture_output=True,text=True)
    if probe.returncode or float(probe.stdout.strip() or '0') < .07: die('WAV corrupto: '+dest.name)


def cache_audit(cache):
    if not cache.is_dir(): die('No existe cache raw: '+str(cache))
    result={}
    for name,(rel,max_kb) in SOURCES.items():
        source=cache/(name+Path(rel).suffix)
        if not source.is_file(): die('Falta descarga: '+str(source))
        data=source.read_bytes()
        if len(data)<600 or len(data)>max_kb*1024: die('Tamano invalido: '+name)
        ext=Path(rel).suffix.lower()
        if ext=='.zip':
            if not zipfile.is_zipfile(io.BytesIO(data)): die('ZIP invalido: '+name)
            with zipfile.ZipFile(io.BytesIO(data)) as z:
                if z.testzip(): die('ZIP con CRC invalido: '+name)
        if ext=='.ogg' and not data.startswith(b'OggS'): die('OGG invalido: '+name)
        if ext=='.mp3' and not (data.startswith(b'ID3') or data[0]==255): die('MP3 invalido: '+name)
        result[name]={'url':'https://opengameart.org/sites/default/files/'+rel,
                      'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data)}
    def exists(key, filenames):
        for filename in filenames:
            if filename not in [n for n,_ in audio_members((cache/(key+Path(SOURCES[key][0]).suffix)).read_bytes())]:
                die('Falta en '+key+': '+filename)
    exists('shotgun',['sounds/shotty.wav'])
    exists('reload',['ShotgunSounds/4 Shell Reload.mp3'])
    exists('step_types',['leaves01.ogg','leaves02.ogg','mud02.ogg','gravel.ogg'])
    exists('monster',[f'Monster-Sounds-Volume-2/Monster-{i}.wav' for i in range(1,4)])
    exists('damage',['ogg/hit1.ogg','ogg/die1.ogg'])
    return result


def prepare(stage, cache):
    raw=stage/'raw';raw.mkdir()
    hashes=cache_audit(cache)
    for name,(rel,max_kb) in SOURCES.items():
        shutil.copy2(cache/(name+Path(rel).suffix),raw/(name+Path(rel).suffix))

    (stage/'FUENTES_SHA256.json').write_text(json.dumps({'credits':SOURCES_META,'files':hashes},indent=2)+'\n')
    produced=stage/'output'; produced.mkdir()
    resources=stage/'resources'; resources.mkdir()
    def b(k): return (raw/(k+Path(SOURCES[k][0]).suffix)).read_bytes()
    convert(b('menu'),'ogg',produced/'menu_ambience.wav',38,.85)
    convert(b('bunker'),'ogg',produced/'bunker_ambience.wav',38,.86)
    convert(b('forest'),'mp3',produced/'forest_ambience.wav',38,.70)
    for i in range(1,7):
        key=f'steps{i:02d}'
        convert(b(key),'ogg',resources/f'concrete_{i:02d}.wav',1.1,.66)
    shutil.copy2(resources/'concrete_01.wav',produced/'footstep.wav')
    step_members=audio_members(b('step_types'))
    step_names=['leaves01.ogg','leaves02.ogg','mud02.ogg','gravel.ogg']
    different=[next((item for item in step_members if item[0]==name),None) for name in step_names]
    if any(x is None for x in different): die('Faltan pasos de bosque')
    for i,(fname,data) in enumerate(different[:4],1):
        convert(data,Path(fname).suffix,resources/f'forest_{i:02d}.wav',1.4,.78)
    # Priorizar un disparo de escopeta real, no pistola ni rifle.
    shotgun=next((item for item in audio_members(b('shotgun')) if item[0]=='sounds/shotty.wav'), None)
    if shotgun is None: die('Falta sounds/shotty.wav')
    convert(shotgun[1],Path(shotgun[0]).suffix,produced/'shotgun.wav',2.1,.80)
    reload=next((item for item in audio_members(b('reload')) if item[0]=='ShotgunSounds/4 Shell Reload.mp3'), None)
    if reload is None: die('Falta 4 Shell Reload.mp3')
    convert(reload[1],Path(reload[0]).suffix,produced/'reload.wav',1.9,.80)
    monster_members=audio_members(b('monster'))
    monster_names=[f'Monster-Sounds-Volume-2/Monster-{i}.wav' for i in range(1,4)]
    monsters=[next((item for item in monster_members if item[0]==name),None) for name in monster_names]
    if any(x is None for x in monsters): die('Faltan Monster-1..3.wav')
    for i,(fname,data) in enumerate(monsters[:3],1):
        convert(data,Path(fname).suffix,resources/f'monster_{i:02d}.wav',2.9,.72)
    shutil.copy2(resources/'monster_01.wav',produced/'monster.wav')
    hitpack=audio_members(b('damage'))
    hits=[next((item for item in hitpack if item[0]=='ogg/hit1.ogg'),None)]
    deaths=[next((item for item in hitpack if item[0]=='ogg/die1.ogg'),None)]
    if hits[0] is None or deaths[0] is None: die('Faltan hit1/die1.ogg')
    convert(hits[0][1],Path(hits[0][0]).suffix,produced/'damage.wav',2.0,.85)
    convert(deaths[0][1],Path(deaths[0][0]).suffix,produced/'ending.wav',2.4,.7)
    manifest={'selected':{'shotgun':shotgun[0], 'reload':reload[0], 'monster':[x[0] for x in monsters[:3]],
                 'forest_steps':[x[0] for x in different[:4]], 'damage':hits[0][0],'ending':deaths[0][0]},
              'sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [*produced.glob('*.wav'),*resources.glob('*.wav')]}}
    (stage/'SELECCION_Y_HASHES.json').write_text(json.dumps(manifest,indent=2)+'\n')
    if len(list(produced.glob('*.wav'))) != 9 or len(list(resources.glob('*.wav'))) < 10:
        die('No se han producido los audio esperados')
    (stage/'PREPARADO_OK.txt').write_text('HBB13BF_PREPARACION_OK\n')
    print('HBB13BF_PREPARACION_OK: 9 sonidos reales + variaciones disponibles para escucha',flush=True)


def unity_is_open():
    if (PROJECT/'Library/UnityLockfile').exists(): return True
    try:
        p=subprocess.run(['pgrep','-af','/Editor/Unity'],capture_output=True,text=True)
        return p.returncode==0 and any('Laberintoa3D_Final' in line and '-projectPath' in line for line in p.stdout.splitlines())
    except FileNotFoundError: return False


def apply(snap):
    audit()
    if unity_is_open(): die('Unity esta abierto; guarda y cierra el Editor antes de v13b')
    if not snap.is_dir() or not snap.name.startswith('HorrorAudioV13bFix_PREPARADO_'):
        die('Directorio PREPARADO incorrecto')
    if snap.resolve().parent != SNAPS.resolve(): die('Directorio PREPARADO fuera de Snapshots')
    if not (snap/'PREPARADO_OK.txt').is_file(): die('Falta PREPARADO_OK; no aplicar')
    raw=snap/'raw'
    original=cache_audit(raw)
    source_manifest=json.loads((snap/'FUENTES_SHA256.json').read_text())['files']
    if any(original[k]['sha256']!=source_manifest[k]['sha256'] for k in SOURCES):
        die('Cache del preparado modificada')
    out_manifest=json.loads((snap/'SELECCION_Y_HASHES.json').read_text())['sha256']
    expected_files=[*(snap/'output').glob('*.wav'),*(snap/'resources').glob('*.wav')]
    if len(list((snap/'output').glob('*.wav')))!=9 or len(list((snap/'resources').glob('*.wav')))!=13:
        die('WAV preparados incompletos')
    for f in expected_files:
        if hashlib.sha256(f.read_bytes()).hexdigest()!=out_manifest.get(f.name):
            die('WAV preparado fue modificado: '+str(f))
    print('HBB13BF_WAV_HASH_OK: preparados intactos',flush=True)
    print('RESPALDO:',snap,flush=True)
    backup=snap/'original_v13';backup.mkdir()
    # Tomar copia de seguridad incluso de las metadatas y herramientas previas.
    shutil.copytree(ASSETS,backup/'AudioV13')
    shutil.copy2(SCRIPT,backup/'HorrorAudioV13.cs')
    shutil.copytree(ORIG_TOOLS,backup/'HorrorBuilderV13')
    shutil.copytree(OLD_EDITOR,backup/'Editor_HorrorBuilderV13')
    hashes_before={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in
        [PROJECT/'Assets/Laberintoa/Scenes/Hasiera.unity',PROJECT/'Assets/Laberintoa/Scenes/Laberintoa_Horror.unity',
         PROJECT/'Assets/Laberintoa/Scenes/SampleScene.unity',PROJECT/'Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset',
         PROJECT/'ProjectSettings/EditorBuildSettings.asset',PROJECT/'ProjectSettings/TagManager.asset']}
    (snap/'escenas_protegidas.json').write_text(json.dumps(hashes_before,indent=2)+'\n')
    touched=False
    try:
        touched=True
        for name in WAV_NAMES:
            shutil.copy2(snap/'output'/f'{name}.wav',ASSETS/f'{name}.wav')
        shutil.copy2(PKG/'src/Scripts/HorrorAudioV13.cs',SCRIPT)
        RES_DIR.mkdir(parents=True)
        for file in (snap/'resources').glob('*.wav'):
            shutil.copy2(file,RES_DIR/file.name)
        EDITOR_DIR.mkdir(parents=True)
        shutil.copy2(PKG/'src/Editor/HorrorAudioV13bVerify.cs',EDITOR_DIR/'HorrorAudioV13bVerify.cs')
        logfile=snap/'unity.log';termfile=snap/'terminal.log'
        print('=== UNITY BATCH - VERIFICACION V13B ===',flush=True)
        with termfile.open('w') as out:
            p=subprocess.run([str(UNITY),'-batchmode','-quit','-force-vulkan','-projectPath',str(PROJECT),
                '-executeMethod','HorrorAudioV13bVerify.Verify','-logFile',str(logfile)],
                stdout=out,stderr=subprocess.STDOUT,env={**os.environ,'__NV_PRIME_RENDER_OFFLOAD':'1','__VK_LAYER_NV_optimus':'NVIDIA_only'})
        log=logfile.read_text(errors='replace') if logfile.exists() else ''
        if p.returncode!=0 or 'HBB13B_OK:' not in log:
            print('=== ULTIMOS ERRORES UNITY ===',file=sys.stderr)
            print('\n'.join(line for line in log.splitlines() if any(x in line for x in ('error CS','Exception','HBB13B','Aborting')))[-3500:],file=sys.stderr)
            print(termfile.read_text(errors='replace')[-2200:],file=sys.stderr)
            die('Unity no confirmo HBB13B_OK: codigo de salida='+str(p.returncode))
        for filename,sha in hashes_before.items():
            if hashlib.sha256(Path(filename).read_bytes()).hexdigest()!=sha: die('Archivo protegido modificado: '+filename)
        # Conservamos el Builder anterior y los .meta: no mover herramientas historicas.
        NEW_TOOLS.mkdir(parents=True)
        for folder in ('src','docs'):
            shutil.copytree(PKG/folder,NEW_TOOLS/folder)
        shutil.copy2(PKG/'horror-audio-v13b-fix1.py',NEW_TOOLS/'horror-audio-v13b-fix1.py')
        shutil.copy2(snap/'FUENTES_SHA256.json',NEW_TOOLS/'FUENTES_SHA256.json')
        shutil.copy2(snap/'SELECCION_Y_HASHES.json',NEW_TOOLS/'SELECCION_Y_HASHES.json')
        print('HBB13B_LOCAL_OK: sonidos reales reemplazados, fuentes CC0 documentadas, originales salvados')
        print('HBB13BF_RESULTADO: OK - escuchar en Unity, sin commit ni push')
    except Exception:
        if touched:
            print('HBB13B_ROLLBACK: restaurando muestras originales, script y carpetas v13b',file=sys.stderr)
            shutil.copytree(backup/'AudioV13',ASSETS,dirs_exist_ok=True)
            shutil.copy2(backup/'HorrorAudioV13.cs',SCRIPT)
            if RES_DIR.exists(): shutil.rmtree(RES_DIR)
            if RES_DIR.with_suffix('.meta').exists(): RES_DIR.with_suffix('.meta').unlink()
            if EDITOR_DIR.exists(): shutil.rmtree(EDITOR_DIR)
            if EDITOR_DIR.with_suffix('.meta').exists(): EDITOR_DIR.with_suffix('.meta').unlink()
            if NEW_TOOLS.exists(): shutil.rmtree(NEW_TOOLS)
            if not ORIG_TOOLS.exists(): shutil.copytree(backup/'HorrorBuilderV13',ORIG_TOOLS)
            if not OLD_EDITOR.exists(): shutil.copytree(backup/'Editor_HorrorBuilderV13',OLD_EDITOR)
            if (snap/'Editor_HorrorBuilderV13.meta').exists() and not OLD_EDITOR.with_suffix('.meta').exists():
                shutil.copy2(snap/'Editor_HorrorBuilderV13.meta',OLD_EDITOR.with_suffix('.meta'))
            print('HBB13B_ROLLBACK_OK: revisa logs en '+str(snap),file=sys.stderr)
        raise

if __name__=='__main__':
    try:
        parser=argparse.ArgumentParser()
        modes=parser.add_mutually_exclusive_group(required=True)
        modes.add_argument('--audit',action='store_true')
        modes.add_argument('--prepare',action='store_true')
        modes.add_argument('--apply',action='store_true')
        parser.add_argument('--cache',type=Path,help='Ruta raw del intento fallido')
        parser.add_argument('--stage',type=Path,help='Ruta PREPARADO creada por --prepare')
        opts=parser.parse_args()
        if opts.audit:
            if not opts.cache or opts.stage: parser.error('--audit requiere --cache')
            audit()
            cache_audit(opts.cache)
            print('HBB13BF_CACHE_OK: 14 fuentes y rutas internas verificadas; solo lectura')
        elif opts.prepare:
            if not opts.cache or opts.stage: parser.error('--prepare requiere --cache')
            audit()
            cache_audit(opts.cache)
            SNAPS.mkdir(parents=True,exist_ok=True)
            stage=Path(__import__('tempfile').mkdtemp(prefix='HorrorAudioV13bFix_PREPARADO_',dir=SNAPS))
            print('HBB13BF_STAGE:',stage,flush=True)
            prepare(stage,opts.cache)
            print('HBB13BF_STAGE:',stage,flush=True)
        else:
            if not opts.stage or opts.cache: parser.error('--apply requiere --stage')
            apply(opts.stage)
    except Exception as e:
        print('HBB13BF_RESULTADO: ERROR - '+str(e),file=sys.stderr)
        sys.exit(1)
