"""Build a self-contained scene using only Python's standard library."""
from pathlib import Path
import argparse
import base64
import json

ROOT = Path(__file__).resolve().parents[1]

def data(path, mime):
    return 'data:' + mime + ';base64,' + base64.b64encode(path.read_bytes()).decode('ascii')

def build(output):
    source = ROOT / 'src' / 'web'
    text = (source / 'preview.template.html').read_text(encoding='utf-8')
    patch = data(ROOT / 'assets' / 'grace-patch.png', 'image/png')
    replacements = {
        '__GRACE_LOCAL_SVG__': (source / 'grace-local.svg').read_text(encoding='utf-8').replace('__GRACE_PATCH__', patch),
        '__H2C_JS__': (ROOT / 'vendor' / 'html2canvas-1.4.1.min.js').read_text(encoding='utf-8'),
        '__VORTEX_JS__': (source / 'vortex.js').read_text(encoding='utf-8'),
        '__MUSIC_JS__': (source / 'music.js').read_text(encoding='utf-8'),
        '__PROGRESS_JS__': (source / 'progress.js').read_text(encoding='utf-8'),
        '__BACKGROUND__': data(ROOT / 'assets' / 'background.png', 'image/png'),
        '__MUSIC_CONFIG__': json.dumps({'source': 'media/menu-music.mp3', 'startSeconds': 15, 'volume': .22, 'fadeInMs': 1000, 'fadeOutMs': 1800}),
    }
    mist = '''<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="400"><defs><radialGradient id="fade" cx=".5" cy=".5" r=".5" gradientTransform="translate(.5 .5) scale(1.24 .96) translate(-.5 -.5)"><stop offset=".05" stop-color="white"/><stop offset=".8" stop-color="black"/></radialGradient><mask id="soft"><rect width="1200" height="400" fill="url(#fade)"/></mask></defs><filter id="mist"><feTurbulence type="fractalNoise" baseFrequency=".008 .024" numOctaves="4" seed="19"/><feColorMatrix type="matrix" values="0 0 0 0 .72 0 0 0 0 .75 0 0 0 0 .7 0 0 0 1 0"/><feComponentTransfer><feFuncA type="gamma" amplitude="1.3" exponent="2.2" offset="-.08"/></feComponentTransfer><feGaussianBlur stdDeviation="2"/></filter><g mask="url(#soft)"><rect width="100%" height="100%" filter="url(#mist)"/></g></svg>'''
    replacements['__MIST_TEXTURE__'] = 'data:image/svg+xml;base64,' + base64.b64encode(mist.encode()).decode()
    for key, value in replacements.items():
        text = text.replace(key, value)
    output.mkdir(parents=True, exist_ok=True)
    (output / 'media').mkdir(exist_ok=True)
    (output / 'startup.html').write_text(text.replace('<body>', '<body class="production">'), encoding='utf-8')
    (output / 'preview.html').write_text(text.replace('<body>', '<body class="production" data-demo="true">'), encoding='utf-8')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, default=ROOT / 'dist' / 'AtGrace')
    args = parser.parse_args()
    build(args.output.resolve())
    print('Built local scene:', args.output)
