from pathlib import Path
import subprocess, zipfile, plistlib, hashlib, json, shutil
root=Path(__file__).resolve().parent
app=root/'LumaNib.app'
version=plistlib.loads((app/'Contents/Info.plist').read_bytes())['CFBundleShortVersionString']
stage=root/'work/releases'/('LumaNib-'+version+'-macOS-arm64')
stage.mkdir(parents=True,exist_ok=True)
subprocess.run(['codesign','--verify','--strict',str(app)],check=True)
subprocess.run(['ditto','--norsrc','--noextattr',str(app),str(stage/'LumaNib.app')],check=True)
for name in ['LICENSE','COPYRIGHT.txt']:
    shutil.copy2(root/name,stage/name)
(stage/'README.md').write_text('''# LumaNib for macOS

Extract the full package and open LumaNib.app. Requires Apple Silicon and macOS 14+.
完整解压后打开 LumaNib.app。需要 Apple Silicon 和 macOS 14 及以上。

The app stays in the menu bar after closing settings. Enable General → Launch at
login if desired; it is off by default. 关闭设置后仍在菜单栏运行；需要自启时，在
“通用 → 登录后自动启动”中开启，默认关闭。

[Full guide / 完整说明](https://github.com/GongWenAI/LumaNib/blob/main/docs/GETTING_STARTED.md)

Copyright © 2026 宫文. See LICENSE and COPYRIGHT.txt. Free use, modification and
free sharing are allowed; sale requires written permission. 允许免费使用、修改
和免费分享，未经作者书面许可禁止销售，完整条款见 LICENSE。
''')
(root/'dist').mkdir(exist_ok=True)
archive=root/'dist'/(stage.name+'.zip')
subprocess.run(['ditto','-c','-k','--norsrc','--noextattr','--keepParent',str(stage),str(archive)],check=True)
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert z.read(stage.name+'/LumaNib.app/Contents/MacOS/LumaNib')==(app/'Contents/MacOS/LumaNib').read_bytes()
print(json.dumps({'archive':str(archive),'sha256':hashlib.sha256(archive.read_bytes()).hexdigest()}))
