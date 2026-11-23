"""Packaging regression: add-ons must resolve to one FemC implementation."""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from install_dev import canonical_app, install, read_json, write_json


class DevInstallTests(unittest.TestCase):
    def test_deduplicates_ids_without_reordering_other_mods(self):
        config = {'EnabledMods': ['first', 'p3rpc.femc.dev', 'addon', 'p3rpc.femc'],
                  'SortedMods': ['first', 'p3rpc.femc', 'p3rpc.femc.dev', 'addon']}
        result = canonical_app(config)
        self.assertEqual(result['EnabledMods'], ['first', 'p3rpc.femc', 'addon'])
        self.assertEqual(result['SortedMods'], ['first', 'p3rpc.femc', 'addon'])
        self.assertIn('p3rpc.femc.dev', config['EnabledMods'])

    def test_install_preserves_settings_and_excludes_old_packages(self):
        with tempfile.TemporaryDirectory() as temp:
            root, repo, build = [Path(temp) / name for name in ('loader', 'repo', 'build')]
            build.mkdir()
            (build / 'p3rpc.femc.dll').write_bytes(b'new assembly')
            (build / 'p3rpc.femc.deps.json').write_text('{}')
            (repo / 'p3rpc.femc/Redirector').mkdir(parents=True)
            (repo / 'p3rpc.femc/Redirector/model').write_bytes(b'Kotone')
            write_json(repo / 'code/p3rpc.femc/p3rpc.femc/ModConfig.json', {
                'ModId': 'p3rpc.femc', 'PluginData': {'GitHubRelease': {'update': True}}})
            for folder, mod_id in [('upstream', 'p3rpc.femc'), ('dev', 'p3rpc.femc.dev')]:
                write_json(root / 'Mods' / folder / 'ModConfig.json', {'ModId': mod_id})
            write_json(root / 'Mods/addon/ModConfig.json', {'ModId': 'addon', 'ModDependencies': ['p3rpc.femc']})
            write_json(root / 'Apps/p3r.exe/AppConfig.json', {'EnabledMods': ['p3rpc.femc.dev', 'addon']})
            settings = root / 'User/Mods/p3rpc.femc/Config.json'
            write_json(settings, {'color': 'upstream'})
            write_json(root / 'User/Mods/p3rpc.femc.dev/Config.json', {'color': 'dev'})
            with patch('install_dev.subprocess.check_output', return_value='test-revision'):
                backup = install(root, build, repo)
                install(root, build, repo)  # Reinstall preserves the active settings.
            mods = [read_json(p) for p in (root / 'Mods').glob('*/ModConfig.json')]
            self.assertEqual(sorted(m['ModId'] for m in mods), ['addon', 'p3rpc.femc'])
            self.assertEqual(read_json(settings), {'color': 'dev'})
            self.assertEqual(read_json(backup / 'Config.json'), {'color': 'upstream'})
            self.assertTrue((backup / 'upstream/ModConfig.json').exists())
            self.assertTrue((backup / 'dev/ModConfig.json').exists())
            config = read_json(root / 'Mods/p3rpc.femc/ModConfig.json')
            self.assertNotIn('GitHubRelease', config['PluginData'])
            self.assertEqual(config['ModName'], 'FemC-Dev')
            enabled = read_json(root / 'Apps/p3r.exe/AppConfig.json')['EnabledMods']
            self.assertIn('p3rpc.femc', enabled)  # Costume Framework's exact-ID test.
            self.assertNotIn('p3rpc.femc.dev', enabled)
            self.assertEqual((root / 'Mods/p3rpc.femc/p3rpc.femc.dll').read_bytes(), b'new assembly')


if __name__ == '__main__':
    unittest.main()
