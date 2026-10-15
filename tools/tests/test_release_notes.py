import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('release_notes', Path(__file__).resolve().parents[1] / 'release_notes.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ReleaseNotesTests(unittest.TestCase):
    def test_extracts_only_matching_release(self):
        text = '# FluffySim 2.0.0.0 — prepared\n\nOffline simulation.\n\n# FluffySim 1.2.0.0\nOld release.\n'
        self.assertEqual(module.extract(text, '2.0.0.0'), 'Offline simulation.\n')

    def test_missing_duplicate_and_empty_entries_rejected(self):
        for text in ('# FluffySim 1.2.0.0\nOld', '# FluffySim 2.0.0.0\n', '# FluffySim 2.0.0.0\nOne\n# FluffySim 2.0.0.0\nTwo'):
            with self.assertRaises(ValueError):
                module.extract(text, '2.0.0.0')

    def test_invalid_version_rejected(self):
        with self.assertRaises(ValueError):
            module.extract('', '2.0.0')
