#!/bin/bash
# 一键编译和打包；传入 --install 时，同时安装到本机游戏的 mods 目录。
set -euo pipefail
otto_project_dir="$(cd "$(dirname "$0")" && pwd)"
cd "$otto_project_dir"

if [[ $# -gt 1 || ( $# -eq 1 && "$1" != "--install" ) ]]; then
    echo '用法：./build.sh [--install]' >&2
    exit 2
fi

# 从项目配置读取编辑器路径，避免脚本和 Directory.Build.props 各维护一份。
otto_godot_bin="$(dotnet msbuild OTTO.csproj -getProperty:GodotPath -nologo)"
if [[ ! -x "$otto_godot_bin" ]]; then
    echo '请在 Directory.Build.props 中设置 MegaDot 4.5.1 的可执行文件路径。' >&2
    exit 1
fi

# 编译 C#。csproj 中的 StageMod 会把 DLL 和清单复制到 dist/OTTO。
dotnet build OTTO.csproj -p:UseSharedCompilation=false -nodeReuse:false

# Godot 需要先把 PNG 导入为纹理资源，再将图片和本地化 JSON 打成 PCK。
# 导出预设已排除源代码、构建脚本和 dist，防止把旧分发包再装进新包。
"$otto_godot_bin" --headless --editor --path "$otto_project_dir" --import
"$otto_godot_bin" --headless --path "$otto_project_dir" \
    --export-pack BasicExport "$otto_project_dir/dist/OTTO/OTTO.pck"
test -s dist/OTTO/OTTO.dll
test -s dist/OTTO/OTTO.pck

if [[ "${1:-}" == "--install" ]]; then
    # 自动识别平台路径。macOS 的 mods 位于游戏 .app/Contents/MacOS 下。
    otto_mods_dir="$(dotnet msbuild OTTO.csproj -getProperty:ModsPath -nologo)"
    test -n "$otto_mods_dir"
    mkdir -p "$otto_mods_dir/OTTO"
    cp dist/OTTO/OTTO.dll dist/OTTO/OTTO.pck dist/OTTO/OTTO.json "$otto_mods_dir/OTTO/"
    echo "OTTO 已安装：$otto_mods_dir/OTTO"
else
    echo "OTTO 分发包：$otto_project_dir/dist/OTTO"
fi
