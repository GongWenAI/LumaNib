from pathlib import Path
import os
import hashlib
import json
import struct
import shutil
import zipfile

root = Path(__file__).resolve().parent
folder = root / 'dist/LumaNib-1.59-win-x64'
exe = folder / 'LumaNib.exe'
data = exe.read_bytes()
assert data[:2] == b'MZ'
pe = struct.unpack_from('<I', data, 0x3c)[0]
assert data[pe:pe+4] == b'PE\0\0'
assert struct.unpack_from('<H', data, pe+4)[0] == 0x8664
runtime = json.loads((folder / 'LumaNib.runtimeconfig.json').read_text())
assert 'includedFrameworks' in runtime['runtimeOptions']
for name in ['LumaNib.dll', 'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll', 'PresentationFramework.dll', 'wpfgfx_cor3.dll']:
    assert (folder / name).is_file(), name
cache=os.environ.get('NUGET_PACKAGES')
if cache:
    nuget=Path(cache)
elif (root/'work/nuget-packages').is_dir():
    nuget=root/'work/nuget-packages'
else:
    nuget=Path.home()/'.nuget/packages'
licenses=folder / 'Runtime-Licenses'
licenses.mkdir(exist_ok=True)
for package, names in [('microsoft.netcore.app.runtime.win-x64', ['LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT']), ('microsoft.windowsdesktop.app.runtime.win-x64', ['LICENSE'])]:
    version=runtime['runtimeOptions']['includedFrameworks'][0]['version']
    for name in names:
        shutil.copyfile(nuget / package / version / name, licenses / (package+'-'+name+'.txt'))
(folder / '使用说明.txt').write_text((root / 'USER_GUIDE.md').read_text(), encoding='utf-8-sig')
(folder / 'README-English.txt').write_text((root / 'USER_GUIDE_EN.md').read_text(), encoding='utf-8-sig')
(folder / 'Run-SelfTest.cmd').write_bytes(b'@echo off\r\ncd /d "%~dp0"\r\nstart "" /wait "%~dp0LumaNib.exe" --self-test\r\n')
for name in ['LICENSE','COPYRIGHT.txt']:
    shutil.copy2(root.parent/name,folder/name)
archive = root.parent / 'dist/LumaNib-1.59-Windows-x64.zip'
archive.parent.mkdir(exist_ok=True)
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for path in sorted(folder.rglob('*')):
        if path.is_file():
            assert not path.name.startswith('._')
            z.write(path, folder.name + '/' + path.relative_to(folder).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert z.read(folder.name + '/LumaNib.exe') == data
sha = hashlib.sha256(archive.read_bytes()).hexdigest()
(root / 'work').mkdir(exist_ok=True)
(root / 'work/package-check.json').write_text(json.dumps({
    'archive': str(archive), 'bytes': archive.stat().st_size, 'sha256': sha,
    'architecture': 'PE32+ x86-64', 'version': '1.59',
    'self_contained': True, 'windows_runtime_tested': False
}, indent=2))
print(archive)
print(f'{archive.stat().st_size / 1024 / 1024:.1f} MiB, SHA256 {sha}')
