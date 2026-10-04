"""Allowlisted release packager: never zip a live application folder wholesale."""
import argparse
import hashlib
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

REPO = Path(__file__).resolve().parent.parent
VERSION = '0.1.1'
PACKAGE = f'hades2-game-coach-v{VERSION}-windows'
DOCS = ['INSTALL.md', 'USAGE.md', 'PRIVACY.md', 'DEVELOPMENT.md', 'VALIDATION.md']
PUBLIC_REPO = 'https://github.com/caganmiller/hades2-game-coach'

def release_bytes(path):
    data = path.read_bytes()
    if path.suffix.lower() == '.md':
        # Keep gallery references usable without bundling player screenshots.
        content = data.decode('utf-8')
        for target in ('docs/SCREENSHOTS.md', 'SCREENSHOTS.md'):
            content = content.replace('(' + target, '(' + PUBLIC_REPO + '/blob/main/docs/SCREENSHOTS.md')
        content = content.replace('(docs/images/', '(https://raw.githubusercontent.com/caganmiller/hades2-game-coach/main/docs/images/')
        data = content.encode('utf-8')
    return data

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--app', type=Path, default=REPO/'build/app')
    parser.add_argument('--out', type=Path, default=REPO/'dist')
    args = parser.parse_args()
    files = {'GameCoach.exe': args.app/'GameCoach.exe',
             'export_recap_pdf.py': REPO/'scripts/export_recap_pdf.py',
             'README.md': REPO/'README.md',
             'THIRD_PARTY_NOTICES.md': REPO/'THIRD_PARTY_NOTICES.md'}
    files.update({f'assets/{name}': REPO/'assets'/name for name in ['game-coach.ico', 'game-coach.png']})
    files.update({f'docs/{name}': REPO/'docs'/name for name in DOCS})
    files.update({f'scripts/{name}': REPO/'scripts'/name for name in ['import_game_art.py', 'requirements-art.txt', 'requirements-pdf.txt']})
    for name, path in files.items():
        if not path.is_file():
            raise SystemExit(f'Missing required release file: {name}')
    args.out.mkdir(parents=True, exist_ok=True)
    destination = args.out/(PACKAGE+'.zip')
    # Only these exact paths can enter the archive, even if build/app contains private data.
    manifest = []
    with ZipFile(destination, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
        for name, path in sorted(files.items()):
            data = release_bytes(path)
            archive.writestr(f'{PACKAGE}/{name}', data)
            manifest.append(f'{hashlib.sha256(data).hexdigest()}  {name}')
        archive.writestr(f'{PACKAGE}/MANIFEST.sha256', '\n'.join(manifest)+'\n')
    checksum = hashlib.sha256(destination.read_bytes()).hexdigest()
    (args.out/'SHA256SUMS.txt').write_text(f'{checksum}  {destination.name}\n', encoding='ascii')
    print(f'Packaged {len(files)} allowlisted files plus manifest: {destination}')

if __name__ == '__main__':
    main()
