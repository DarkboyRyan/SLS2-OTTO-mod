#!/bin/bash
# 编译当前已验证的 beta 分支；正式版仍使用 build.sh，两种发行包分开放置。
set -euo pipefail
otto_project_dir="$(cd "$(dirname "$0")" && pwd)"
cd "$otto_project_dir"
if [[ $# -gt 1 || ( $# -eq 1 && "$1" != "--install" ) ]]; then
    echo '用法：./build-beta.sh [--install]' >&2; exit 2
fi
# 可用环境变量指定不同 Steam 库的 BaseLib，默认读取本机订阅版本。
otto_steam_dir="$(dotnet msbuild OTTO.csproj -getProperty:SteamLibraryPath -nologo)"
otto_baselib="${OTTO_BETA_BASELIB_PATH:-$otto_steam_dir/workshop/content/2868840/3737335127/BaseLib/BaseLib.dll}"
if [[ ! -f "$otto_baselib" ]]; then
    echo '找不到 BaseLib 3.4.8：请安装创意工坊依赖，或设置 OTTO_BETA_BASELIB_PATH。' >&2; exit 1
fi
otto_godot_bin="$(dotnet msbuild OTTO.csproj -getProperty:GodotPath -nologo)"
test -x "$otto_godot_bin"
dotnet build OTTO.csproj -p:BetaBuild=true "-p:BetaBaseLibPath=$otto_baselib" \
    -p:UseSharedCompilation=false -nodeReuse:false
# 导入并导出共享美术资源；不覆盖 dist/OTTO 中的正式版。
"$otto_godot_bin" --headless --editor --path "$otto_project_dir" --import
"$otto_godot_bin" --headless --path "$otto_project_dir" \
    --export-pack BasicExport "$otto_project_dir/dist-beta/OTTO/OTTO.pck"
for otto_file in OTTO.dll OTTO.json OTTO.pck; do test -s "dist-beta/OTTO/$otto_file"; done
if [[ "${1:-}" == "--install" ]]; then
    otto_mods_dir="$(dotnet msbuild OTTO.csproj -getProperty:ModsPath -nologo)"
    test -n "$otto_mods_dir"
    mkdir -p "$otto_mods_dir/OTTO"
    cp dist-beta/OTTO/OTTO.{dll,json,pck} "$otto_mods_dir/OTTO/"
    echo "OTTO beta 已安装：$otto_mods_dir/OTTO"
else
    echo "OTTO beta 分发包：$otto_project_dir/dist-beta/OTTO"
fi
