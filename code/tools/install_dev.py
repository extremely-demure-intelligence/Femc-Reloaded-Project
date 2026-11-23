#!/usr/bin/env python3
"""Install FemC-Dev with the canonical ID required by costumes and add-ons.

Close the game and Reloaded II first. Backups live outside Mods so dependency
resolution cannot load the old build alongside the development build.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

MOD_ID = 'p3rpc.femc'
LEGACY_ID = 'p3rpc.femc.dev'
REPO = Path(__file__).resolve().parents[2]


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(mode='w', dir=path.parent, delete=False) as f:
        json.dump(data, f, indent=2)
        f.write('\n')
    Path(f.name).replace(path)


def canonical_app(config):
    config = dict(config)
    for key in ('EnabledMods', 'SortedMods'):
        config[key] = list(dict.fromkeys(
            MOD_ID if item == LEGACY_ID else item for item in config.get(key, [])))
    if MOD_ID not in config['EnabledMods']:
        config['EnabledMods'].append(MOD_ID)
    return config


def install(root, build, repo=REPO):
    for proc in Path('/proc').glob('[0-9]*/comm'):
        try:
            name = proc.read_text().strip().lower()
        except OSError:
            continue
        if name in ('p3r.exe', 'reloaded-ii.exe'):
            raise SystemExit('Close Persona 3 Reload and Reloaded II before installing.')
    app_path = root / 'Apps/p3r.exe/AppConfig.json'
    app = canonical_app(read_json(app_path))
    for name in ('p3rpc.femc.dll', 'p3rpc.femc.deps.json'):
        if not (build / name).is_file():
            raise ValueError(f'Missing build output: {build / name}')
    config = read_json(repo / 'code/p3rpc.femc/p3rpc.femc/ModConfig.json')
    config.update(ModId=MOD_ID, ModName='FemC-Dev',
                  ModDescription='Local development build. Uses the canonical FemC ID for add-on and costume compatibility.',
                  ModR2RManagedDll32='', ModR2RManagedDll64='', ReleaseMetadataFileName='')
    config.get('PluginData', {}).pop('GitHubRelease', None)
    # Include alternate directory names, not just the IDs used as folder names.
    previous = [p.parent for p in (root / 'Mods').glob('*/ModConfig.json')
                if read_json(p).get('ModId') in (MOD_ID, LEGACY_ID)]
    destination = root / 'Mods' / MOD_ID
    if destination.exists() and destination not in previous:
        raise ValueError(f'Unrecognized destination: {destination}')
    settings = root / 'User/Mods' / MOD_ID / 'Config.json'
    legacy_settings = root / 'User/Mods' / LEGACY_ID / 'Config.json'
    selected_settings = legacy_settings if LEGACY_ID in read_json(app_path).get('EnabledMods', []) and legacy_settings.exists() else settings
    settings_data = read_json(selected_settings) if selected_settings.exists() else None
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    backup = root / 'FemcDevBackups' / stamp
    with tempfile.TemporaryDirectory(prefix='.femc-stage-', dir=root) as temporary:
        stage = Path(temporary) / MOD_ID
        stage.mkdir()
        subprocess.run(['cp', '-a', '--reflink=auto', str(repo / 'p3rpc.femc') + '/.', str(stage)], check=True)
        for item in build.iterdir():
            if item.is_file() and (item.suffix in ('.dll', '.pdb') or item.name.endswith('.deps.json') or item.name == 'Preview.png'):
                shutil.copy2(item, stage / item.name)
        write_json(stage / 'ModConfig.json', config)
        revision = subprocess.check_output(['git', '-C', str(repo), 'rev-parse', 'HEAD'], text=True).strip()
        dirty = bool(subprocess.check_output(['git', '-C', str(repo), 'status', '--porcelain'], text=True))
        write_json(stage / 'FemcDevBuild.json', dict(revision=revision, dirty=dirty,
                   packaged_at=stamp, mod_id=MOD_ID,
                   dll_sha256=hashlib.sha256((stage / 'p3rpc.femc.dll').read_bytes()).hexdigest()))
        backup.mkdir(parents=True)
        shutil.copy2(app_path, backup / 'AppConfig.json')
        had_settings = settings.exists()
        if had_settings:
            shutil.copy2(settings, backup / 'Config.json')
        moved = []
        installed = False
        try:
            for old in previous:
                old.rename(backup / old.name)
                moved.append(old)
            stage.rename(destination)
            installed = True
            if settings_data is not None:
                write_json(settings, settings_data)
            write_json(app_path, app)
        except BaseException:
            if installed:
                destination.rename(Path(temporary) / 'failed-install')
            for old in reversed(moved):
                (backup / old.name).rename(old)
            shutil.copy2(backup / 'AppConfig.json', app_path)
            if had_settings:
                shutil.copy2(backup / 'Config.json', settings)
            elif settings.exists():
                settings.unlink()
            raise
    print(f'Installed FemC-Dev ({MOD_ID}): {destination}')
    print(f'Previous packages and configuration: {backup}')
    return backup


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reloaded-root', type=Path, required=True)
    parser.add_argument('--build-dir', type=Path, required=True)
    args = parser.parse_args()
    install(args.reloaded_root.resolve(), args.build_dir.resolve())
