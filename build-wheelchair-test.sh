#!/bin/bash
# 轮椅动画独立测试包：保留正式发行目录与默认构建，不发布 Workshop。
set -euo pipefail
otto_project_dir="$(cd "$(dirname "$0")" && pwd)"
cd "$otto_project_dir"
if [[ $# -gt 1 || ( $# -eq 1 && "$1" != "--install" ) ]]; then
    echo '用法：./build-wheelchair-test.sh [--install]' >&2; exit 2
fi
# 两套 API 都必须在开发机存在；这些游戏 DLL 仅供编译，不打入发行包。
otto_stable_data="${OTTO_STABLE_DATA_DIR:-$otto_project_dir/../.work/engine/SlayTheSpire2.app/Contents/Resources/data_sts2_macos_x86_64}"
otto_beta_data="${OTTO_BETA_DATA_DIR:-$(dotnet msbuild OTTO.csproj -getProperty:Sts2DataDir -nologo)}"
otto_steam_dir="$(dotnet msbuild OTTO.csproj -getProperty:SteamLibraryPath -nologo)"
otto_baselib="${OTTO_BETA_BASELIB_PATH:-$otto_steam_dir/workshop/content/2868840/3737335127/BaseLib/BaseLib.dll}"
for otto_ref in "$otto_stable_data/sts2.dll" "$otto_beta_data/sts2.dll" "$otto_baselib"; do
    if [[ ! -f "$otto_ref" ]]; then echo "缺少编译引用：$otto_ref" >&2; exit 1; fi
done
# Rebuild 防止两次构建共用中间目录时误用另一分支的缓存。
dotnet build OTTO.csproj -t:Rebuild -p:UniversalBuild=true -p:WheelchairTestBuild=true -p:Version=1.0.5-wheelchair-test.1 -p:BetaBuild=false \
    "-p:Sts2DataDir=$otto_stable_data" -p:ModStageDir=dist-test/OTTO/assemblies/stable \
    -p:UseSharedCompilation=false -p:NuGetAudit=false -nodeReuse:false
dotnet build OTTO.csproj -t:Rebuild -p:UniversalBuild=true -p:WheelchairTestBuild=true -p:Version=1.0.5-wheelchair-test.1 -p:BetaBuild=true \
    "-p:Sts2DataDir=$otto_beta_data" "-p:BetaBaseLibPath=$otto_baselib" \
    -p:ModStageDir=dist-test/OTTO/assemblies/beta \
    -p:UseSharedCompilation=false -p:NuGetAudit=false -nodeReuse:false
dotnet build Loader/OTTO.Loader.csproj -p:Version=1.0.5-wheelchair-test.1 "-p:Sts2DataDir=$otto_stable_data" \
    -p:UseSharedCompilation=false -p:NuGetAudit=false -nodeReuse:false
cp Loader/bin/Debug/net9.0/OTTO.dll dist-test/OTTO/OTTO.dll
python3 - <<'MANIFEST'
from pathlib import Path
import json
manifest=json.loads(Path('OTTO.universal.json').read_text())
manifest['name']='OTTO（轮椅动画测试版）'
manifest['version']='v1.0.5-wheelchair-test.1'
manifest['description']='轮椅动画测试：坐姿待机、轮椅下砸、手推轮圈防御、坐在轮椅上闭眼睡着。\n\n'+manifest['description']
Path('dist-test/OTTO/OTTO.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
MANIFEST
otto_godot_bin="$(dotnet msbuild OTTO.csproj -getProperty:GodotPath -nologo)"
"$otto_godot_bin" --headless --editor --path "$otto_project_dir" --import
"$otto_godot_bin" --headless --path "$otto_project_dir" --script tools/create_wheelchair_scene.gd
"$otto_godot_bin" --headless --editor --path "$otto_project_dir" --import
"$otto_godot_bin" --headless --path "$otto_project_dir" --export-pack BasicExport "$otto_project_dir/dist-test/OTTO/OTTO.pck"
# 白名单打包，避免把探针、游戏 DLL 或构建文件混入 Workshop。
python3 - <<'PY'
from pathlib import Path
import zipfile
root=Path('dist-test/OTTO')
names=['OTTO.dll','OTTO.json','OTTO.pck','assemblies/stable/OTTO.Content.dll','assemblies/beta/OTTO.Content.dll']
assert {str(p.relative_to(root)) for p in root.rglob('*') if p.is_file()}==set(names),'发行目录含非预期文件'
with zipfile.ZipFile('dist-test/OTTO-v1.0.5-wheelchair-test.1.zip','w',zipfile.ZIP_DEFLATED) as z:
    for name in names:
        assert (root/name).stat().st_size>0
        z.write(root/name,'OTTO/'+name)
PY
if [[ "${1:-}" == "--install" ]]; then
    otto_mods_dir="$(dotnet msbuild OTTO.csproj -getProperty:ModsPath -nologo)"
    test -n "$otto_mods_dir"
    mkdir -p "$otto_mods_dir/OTTO"
    cp -R dist-test/OTTO/. "$otto_mods_dir/OTTO/"
    echo "OTTO 通用版已安装：$otto_mods_dir/OTTO"
fi
echo "通用包：$otto_project_dir/dist-test/OTTO-v1.0.5-wheelchair-test.1.zip"
