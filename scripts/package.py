"""Package only distributable files; never collect local config, music, or cache."""
from pathlib import Path
import argparse
import hashlib
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    'ChatGPTGraceLauncher.exe', 'ChatGPTGraceLauncher.exe.config',
    'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll',
    'startup.html', 'preview.html', 'Setup.cmd', 'Setup.ps1', 'README.md', 'LICENSE',
    'THIRD_PARTY_NOTICES.md', 'html2canvas-LICENSE.txt', 'WebView2-LICENSE.txt',
    'docs/COMPATIBILITY.md', 'docs/MUSIC.md', 'docs/preview.png', 'docs/transition.png',
]

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--version', default='1.0.0')
    args = parser.parse_args()
    if not re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', args.version):
        parser.error('Use a numeric major.minor.patch version.')
    folder = ROOT / 'dist' / 'AtGrace'
    output = ROOT / 'artifacts'
    output.mkdir(exist_ok=True)
    archive = output / f'Codex-At-Grace-v{args.version}-Windows-x64.zip'
    for name in FILES:
        if not (folder / name).is_file():
            raise FileNotFoundError(folder / name)
    with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as package:
        for name in FILES:
            package.write(folder / name, 'AtGrace/' + name)
        package.writestr('AtGrace/media/README.txt',
            'Optional music: add your own legitimate MP3 as menu-music.mp3 here.\n'
            'Suggested track: Elden Ring - Tsukasa Saitoh, Elden Ring (Original Soundtrack).\n'
            'Every pass starts at 00:15. Music fades out and stops before handoff.\n'
            'No recording is included. See ../docs/MUSIC.md.\n')
    checksum = hashlib.sha256(archive.read_bytes()).hexdigest()
    (output / 'SHA256SUMS.txt').write_text(checksum + '  ' + archive.name + '\n', encoding='ascii')
    print(archive)
    print('SHA256:', checksum)
