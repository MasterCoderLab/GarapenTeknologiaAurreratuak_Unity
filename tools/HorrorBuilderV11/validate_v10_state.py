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
