"""在游戏副本和专用测试存档路径运行探针，不触碰正式存档。"""
from pathlib import Path
import shutil, subprocess, json, sys
root=Path(__file__).resolve().parents[2]
work=Path(__file__).resolve().parent
lang=sys.argv[1] if len(sys.argv)>1 else 'zhs'
settings=root/'.work/probe-run/userdata/default/1/settings.save'
data=json.loads(settings.read_text());data['language']='zhs' if lang=='scene' else lang;data['mod_settings']['mods_enabled']=True
settings.write_text(json.dumps(data))
app=work/'SlayTheSpire2.app/Contents/MacOS'
for name in ['OTTO.dll','OTTO.json','OTTO.pck']:
    shutil.copy2(root/'OTTO/dist-beta/OTTO'/name,app/'mods/OTTO'/name)
shutil.copy2(work/'bin/Debug/net9.0/OTTOConfirmedProbe.dll',app/'mods/OTTOConfirmedProbe/OTTOConfirmedProbe.dll')
log=work/f'engine-{lang}.stdout.log'
with log.open('w') as output:
    result=subprocess.run([str(app/'Slay the Spire 2'),'--headless','--path',str(root/'.work/probe-run'),'--force-steam=off','--log-file',str(work/f'engine-{lang}.log')],stdout=output,stderr=subprocess.STDOUT,timeout=120)
print('engine exit',result.returncode)
lines=log.read_text(errors='replace').splitlines()
for i,line in enumerate(lines):
    if 'OTTO_' in line and ('_OK' in line or '_FAILED' in line):
        print('\n'.join(lines[i:i+5]) if '_FAILED' in line else line)

assert result.returncode == 0
assert any(("OTTO_SCENE_OK" if lang=="scene" else "OTTO_RULES_UPDATE_OK:") in line for line in lines), "Beta verification failed; see engine log"
