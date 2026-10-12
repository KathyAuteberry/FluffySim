"""Package an already built plugin. Never builds or writes into the input directory."""
import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def version():
    return ET.parse(ROOT / 'FluffySim.Version.props').findtext('.//FluffySimVersion')


def package(source, destination):
    source, destination = Path(source).resolve(), Path(destination).resolve()
    if 'Release' in destination.parts or destination == source or source in destination.parents:
        raise ValueError('ZIP output must be separate from build/plugin output and outside Release directories')
    release = version()
    manifest = json.loads((source / 'FluffySim.json').read_text())
    if (manifest.get('Name'), manifest.get('InternalName'), manifest.get('AssemblyVersion')) != ('FluffySim', 'FluffySim', release + '.0'):
        raise ValueError('Manifest identity/version does not match FluffySim.Version.props')
    deps = json.loads((source / 'FluffySim.deps.json').read_text())
    target = deps['targets'][deps['runtimeTarget']['name']]
    if 'FluffySim/' + release not in target:
        raise ValueError('Dependency metadata version does not match')
    files = {'FluffySim.json', 'FluffySim.deps.json'}
    for library in target.values():
        for kind in ('runtime', 'native', 'resources'):
            for asset in library.get(kind, {}):
                files.add(Path(asset).name)
    if 'AnoMech.dll' in files:
        raise ValueError('Old plugin DLL must not be packaged')
    missing = [name for name in sorted(files) if not (source / name).is_file()]
    if missing:
        raise ValueError('Missing runtime files: ' + ', '.join(missing))
    if any((source / name).is_symlink() for name in files):
        raise ValueError('Runtime files must not be symlinks')
    output = destination / ('FluffySim-' + release + '.0.zip')
    destination.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED) as archive:
        for name in sorted(files):
            archive.write(source / name, name)
        for name in ('LICENSE.md', 'README.md', 'INSTALLATION.md', 'RELEASING.md', 'CHANGELOG.md', 'THIRD-PARTY-NOTICES.md', 'HARD-DIFFICULTY-AUDIT.md', 'FRU-ROADMAP.md', 'DSR-ROADMAP.md', 'images/icon.png'):
            archive.write(ROOT / name, name)
    return output


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', required=True, type=Path, help='Already built directory containing FluffySim.dll')
    parser.add_argument('--output', required=True, type=Path, help='Separate ZIP destination; must not be installed plugin output')
    args = parser.parse_args()
    print(package(args.input, args.output))
