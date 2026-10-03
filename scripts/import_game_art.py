import sys, re, json
from pathlib import Path

from PIL import Image
from deppth2.sggpio import PackageWithManifestReader
import argparse
parser=argparse.ArgumentParser(description="Import icons from your own installed Hades II. Game files are read-only.")
parser.add_argument('--game', required=True, type=Path, help='Hades II installation folder containing Content')
parser.add_argument('--app', required=True, type=Path, help='Extracted Game Coach folder containing GameCoach.exe')
args=parser.parse_args()
content=args.game.resolve()/'Content'
app=args.app.resolve()
if not (content/'Packages/1080p/GUI.pkg').is_file(): parser.error('GUI.pkg was not found under the selected game folder')
if not (app/'GameCoach.exe').is_file(): parser.error('Choose the extracted app folder containing GameCoach.exe')
if app == content or content in app.parents: parser.error('The output must be outside game Content')
def definitions(file):
    text=file.read_text(encoding='utf-8-sig')
    text=re.sub(r'--\[\[[\s\S]*?\]\](?:--)?|--[^\r\n]*','',text)
    result={}
    for start in re.finditer(r'(?m)^\t([A-Za-z0-9_]+)\s*=\s*\n\t\{',text):
        begin=start.end()-1; depth=0
        for token in re.finditer(r'"(?:\\.|[^"\\])*"|[{}]',text[begin:]):
            if token[0]=='{': depth+=1
            if token[0]=='}':
                depth-=1
                if depth==0: result[start[1]]=text[begin:begin+token.end()]; break
    return result
def field(body,name):
    m=re.search(r'(?m)^\t\t'+re.escape(name)+r'\s*=\s*"([^"\n]+)"',body)
    return m[1] if m else ''
defs={}
for file in (content/'Scripts').glob('TraitData*.lua'): defs.update(definitions(file))
icons={k:field(v,'Icon') for k,v in defs.items() if field(v,'Icon')}
for k,body in definitions(content/'Scripts/MetaUpgradeData.lua').items():
    icon,trait=field(body,'Image'),field(body,'TraitName')
    if icon and trait: icons[trait]=icon
animations={}
for file in (content/'Game/Animations').glob('GUI*.sjson'):
    for m in re.finditer(r'(?ms)^\t\{\s*Name\s*=\s*"([^"\n]+)"(.*?)^\t\}',file.read_text(encoding='utf-8-sig')):
        path=re.search(r'FilePath\s*=\s*"([^"\n]+)"',m[2])
        if path: animations[m[1]]=path[1]
paths={k:animations[v] for k,v in icons.items() if v in animations}
needed=set(paths.values()); found={}
out=app/'assets/boons'; out.mkdir(parents=True,exist_ok=True)
with PackageWithManifestReader(str(content/'Packages/1080p/GUI.pkg')) as package:
    for entry in package:
        atlas=entry.manifest_entry
        if not atlas or not hasattr(atlas,'subAtlases'): continue
        selected=[a for a in atlas.subAtlases if a['name'] in needed and not a.get('isMip',False)]
        if not selected: continue
        sheet=entry._get_image()
        for a in selected:
            r=a['rect']; crop=sheet.crop((r['x'],r['y'],r['x']+r['width'],r['y']+r['height']))
            art=entry._get_original_image(crop,a['originalSize'],a['topLeft'],a['scaleRatio'])
            art.thumbnail((192,192),Image.Resampling.LANCZOS)
            name=re.sub(r'[^a-zA-Z0-9_-]','_',a['name'])+'.png'
            art.save(out/name,optimize=True)
            found[a['name']]='boons/'+name
index={k:found[v] for k,v in paths.items() if v in found}
(out.parent/'boon-icons.json').write_text(json.dumps(index,indent=2),encoding='utf-8')
(out.parent/'ARTWORK.txt').write_text('Hades II boon, Arcana, weapon and keepsake art belongs to Supergiant Games.\nRead from this PC\'s installed game for contextual companion recaps.\nNo game packages are modified.\nPackage reader: deppth2 0.1.6.7 (https://github.com/SGG-Modding/deppth).\nApp brand and navigation glyphs are original vector artwork in AppGlyph.cs.\n',encoding='utf-8')
print('Mapped',len(index),'traits to',len(found),'local game icons')
