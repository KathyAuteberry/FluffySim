"""Package an already built plugin. Never builds or writes into the input directory."""
import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]

DOCUMENTATION = (
    'AGENTS.md',
    'README.md',
    'docs/NEXT-UPSTREAM-AUDIT.md',
    'docs/README.md',
    'docs/development/AGENTS.md',
    'docs/development/HARD-DIFFICULTY-AUDIT.md',
    'docs/development/JOB-RECAST-AUDIT.md',
    'docs/development/NEXT-DEVELOPMENT-AUDIT.md',
    'docs/encounters/dsr-sanctity-meteors-events.json',
    'docs/encounters/dsr-sanctity-meteors-evidence.md',
    'docs/encounters/dsr-sanctity-tower-height.md',
    'docs/encounters/dsr/P3-PRESENTATION-AUDIT.md',
    'docs/encounters/dsr/P7-PORT.md',
    'docs/encounters/dsr/ROADMAP.md',
    'docs/encounters/fru/ROADMAP.md',
    'docs/features/spectator/COVERAGE.md',
    'docs/features/spectator/DEVELOPMENT-GARAGE.md',
    'docs/features/spectator/DEVELOPMENT-P3.md',
    'docs/features/spectator/MODE.md',
    'docs/features/spectator/PARTY-PROJECTION.md',
    'docs/integration/UPSTREAM-INTEGRATION.md',
    'docs/integration/UPSTREAM-QA-INTEGRATION-2026-10-21.md',
    'docs/legal/AI-DECLARATION.md',
    'docs/legal/LICENSE.md',
    'docs/legal/THIRD-PARTY-NOTICES.md',
    'docs/releases/CHANGELOG.md',
    'docs/releases/RELEASING.md',
    'docs/research/asumech-comparison.md',
    'docs/research/dsr-eyes.md',
    'docs/research/dsr-inventory.md',
    'docs/research/dsr-native-repairs.md',
    'docs/research/dsr-nidhogg.md',
    'docs/research/dsr-p1.md',
    'docs/research/dsr-p5.md',
    'docs/research/dsr-p6.md',
    'docs/research/dsr-practice-cooldowns.md',
    'docs/research/dsr-progress.md',
    'docs/research/fru-clear-audit-2026-10-31.md',
    'docs/research/fru-completionist.md',
    'docs/research/fru-native-capture-inventory.md',
    'docs/research/fru-native-playtest.md',
    'docs/research/fru-second-clear-audit-2026-11-01.md',
    'docs/safety/OFFLINE-DSR-REPAIR.md',
    'docs/safety/PACKET-FILTER-AUDIT.md',
    'docs/safety/PAUSE-INVESTIGATION.md',
    'docs/tea/EVIDENCE.md',
    'docs/tea/IMPLEMENTATION.md',
    'docs/tea/LIMIT-CUT.md',
    'docs/tea/P1.md',
    'docs/tea/P2.md',
    'docs/tea/P3.md',
    'docs/tea/P4.md',
    'docs/tea/STASIS.md',
    'docs/tea/evidence-summary.json',
    'docs/user-guide/INSTALLATION.md',
    'docs/user-guide/README.md',
    'docs/verification/COMMUNITY-QA-CT-WRATH-RETEST.md',
    'docs/verification/COMMUNITY-QA-NATIVE-RETEST.md',
    'docs/verification/COMMUNITY-QA-PARADISE-WROTH.md',
    'docs/verification/COMMUNITY-QA-ROUND-2.md',
    'images/icon.png',
    'tests/SafetyTests/README.md',
)


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
        for name in DOCUMENTATION:
            archive.write(ROOT / name, name)
    return output


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', required=True, type=Path, help='Already built directory containing FluffySim.dll')
    parser.add_argument('--output', required=True, type=Path, help='Separate ZIP destination; must not be installed plugin output')
    args = parser.parse_args()
    print(package(args.input, args.output))
