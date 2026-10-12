import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile
import xml.etree.ElementTree as ET

spec = importlib.util.spec_from_file_location('package_release', Path(__file__).resolve().parents[1] / 'package_release.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ReleasePackageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'Debug'
        self.source.mkdir()
        (self.source / 'FluffySim.json').write_text(json.dumps(dict(Name='FluffySim', InternalName='FluffySim', AssemblyVersion=module.version() + '.0')))
        (self.source / 'FluffySim.deps.json').write_text(json.dumps(dict(runtimeTarget=dict(name='target'), targets={'target': {'FluffySim/' + module.version(): {'runtime': {'FluffySim.dll': {}, 'Dependency.dll': {}}}}})))
        for name in ('FluffySim.dll', 'Dependency.dll', 'AnoMech.dll', 'Dalamud.dll', 'private.log'):
            (self.source / name).write_bytes(b'test')

    def test_runtime_dependencies_included_stale_plugins_and_logs_excluded(self):
        output = module.package(self.source, self.root / 'archives')
        with zipfile.ZipFile(output) as archive:
            names = archive.namelist()
        self.assertIn('FluffySim.dll', names)
        self.assertIn('Dependency.dll', names)
        self.assertIn('LICENSE.md', names)
        self.assertNotIn('AnoMech.dll', names)
        self.assertNotIn('Dalamud.dll', names)
        self.assertNotIn('private.log', names)
        self.assertEqual(output.name, 'FluffySim-' + module.version() + '.0.zip')

    def test_missing_dependency_rejected(self):
        (self.source / 'Dependency.dll').unlink()
        with self.assertRaisesRegex(ValueError, 'Missing runtime'):
            module.package(self.source, self.root / 'archives')

    def test_mismatched_version_rejected(self):
        (self.source / 'FluffySim.json').write_text('{}')
        with self.assertRaisesRegex(ValueError, 'Manifest'):
            module.package(self.source, self.root / 'archives')

    def test_release_or_input_output_rejected(self):
        for output in (self.source, self.source / 'archives', self.root / 'Release'):
            with self.assertRaises(ValueError):
                module.package(self.source, output)

    def test_existing_archive_not_overwritten(self):
        module.package(self.source, self.root / 'archives')
        with self.assertRaises(FileExistsError):
            module.package(self.source, self.root / 'archives')


class BrandingMetadataTests(unittest.TestCase):
    def test_display_name_and_project_identity(self):
        manifest = json.loads((module.ROOT / 'AnoMech/FluffySim.json').read_text())
        project = ET.parse(module.ROOT / 'AnoMech/AnoMech.csproj')
        self.assertEqual(manifest['Name'], 'FluffySim')
        self.assertIn('AnoMech', manifest['Description'])
        self.assertEqual(project.findtext('.//AssemblyName'), 'FluffySim')
        self.assertEqual(project.findtext('.//RootNamespace'), 'AnoMech')
        self.assertIsNone(project.find('.//Version'))
        self.assertIn('../FluffySim.Version.props', [x.attrib.get('Project') for x in project.findall('Import')])

    def test_no_unfiltered_release_zip_or_automatic_publish(self):
        target = ET.parse(module.ROOT / 'AnoMech/DalamudPackager.targets')
        self.assertEqual(target.find('.//DalamudPackager').attrib['MakeZip'], 'false')
        workflow = (module.ROOT / '.github/workflows/release-plugin.yml').read_text()
        self.assertIn('dotnet build -c Debug', workflow)
        self.assertNotIn('version_bump', workflow)
        self.assertNotIn('contents: write', workflow)
