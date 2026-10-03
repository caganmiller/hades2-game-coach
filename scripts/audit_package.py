"""Check distributable contents without printing any matched secret value."""
import argparse
import hashlib
import re
import subprocess
from pathlib import Path
from zipfile import ZipFile

REPO = Path(__file__).resolve().parent.parent
FORBIDDEN_NAMES = {'settings.json', 'local-model.json', 'run-memory.json', 'run-moments.json', 'coach-journal.json', 'usage-history.json', 'pdf-python.txt', 'boon-icons.json', 'artwork.txt'}
FORBIDDEN_DIRS = {'run-history', 'run-archive', 'run-snapshots', 'voice-cache', 'models', 'runtime', 'boons', 'test-output', '__pycache__'}
RULES = {
    'OpenAI-like credential': re.compile(r'\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{20,}'),
    'GitHub-like credential': re.compile(r'\b(?:gh[pousr]_[A-Za-z0-9]{25,}|github_pat_[A-Za-z0-9_]{30,})'),
    'private key material': re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
    'literal user profile path': re.compile(r'[A-Za-z]:[\\/]+Users[\\/]+[^\s<>"\r\n]+'),
}

def inspect(name, data):
    path = Path(name)
    problems = []
    if path.name.lower() in FORBIDDEN_NAMES or any(p.lower() in FORBIDDEN_DIRS for p in path.parts) or path.suffix.lower() in {'.dpapi', '.sav', '.gguf', '.wav', '.pcm', '.log', '.lnk'}:
        problems.append('private/generated file is not allowed')
    # Scan both regular source and .NET UTF-16 string data. Do not echo matches.
    for encoding in ('utf-8', 'utf-16-le'):
        text = data.decode(encoding, errors='ignore')
        for label, pattern in RULES.items():
            if pattern.search(text):
                problems.append(label)
    return [name + ': ' + label for label in sorted(set(problems))]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--zip', type=Path)
    parser.add_argument('--staged', action='store_true')
    args = parser.parse_args()
    failures, count = [], 0
    if args.staged:
        names = subprocess.check_output(['git', 'diff', '--cached', '--name-only', '-z', '--diff-filter=ACMR'], cwd=REPO).decode().split('\0')
        for name in filter(None, names):
            data = subprocess.check_output(['git', 'show', ':'+name], cwd=REPO)
            failures.extend(inspect(name, data)); count += 1
    if args.zip:
        with ZipFile(args.zip) as archive:
            names = archive.namelist()
            if len(names) != len(set(names)):
                failures.append('ZIP has duplicate entries')
            manifest_name = next((n for n in names if n.endswith('/MANIFEST.sha256')), None)
            if not manifest_name:
                failures.append('ZIP has no manifest')
            else:
                prefix = manifest_name.rsplit('/', 1)[0] + '/'
                listed = set()
                for line in archive.read(manifest_name).decode().splitlines():
                    expected, name = line.split('  ', 1)
                    full = prefix + name
                    listed.add(full)
                    if full not in names or hashlib.sha256(archive.read(full)).hexdigest() != expected:
                        failures.append('ZIP manifest mismatch: ' + name)
                if set(names) != listed | {manifest_name}:
                    failures.append('ZIP contains unmanifested files')
            for name in names:
                if name.startswith('/') or '..' in Path(name).parts:
                    failures.append('Unsafe ZIP path: ' + name)
                failures.extend(inspect(name, archive.read(name))); count += 1
    if not args.staged and not args.zip:
        parser.error('Select --staged and/or --zip')
    if failures:
        print('\n'.join(failures))
        raise SystemExit(1)
    print(f'PASS: {count} files; no prohibited files, credential patterns, or literal user-profile paths found. ZIP manifest verified when supplied.')

if __name__ == '__main__':
    main()
