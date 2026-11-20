#!/usr/bin/env bash
# 把已构建的产物同步到工坊上传目录（workshop/content）。
# SteamCMD 打包的是 contentfolder，这里保持与仓库内 manifest / DLL / 本地化一致。
# 用法: bash workshop/package.sh   （deploy.sh 末尾会自动调用）
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(dirname "$HERE")"
DLL="$ROOT/build/MouseKeybinds.dll"

[ -f "$DLL" ] || { echo "缺少构建产物：$DLL（先运行 dotnet build 或 deploy.sh）" >&2; exit 1; }

mkdir -p "$HERE/content/Localization"
cp -f "$DLL" "$HERE/content/MouseKeybinds.dll"
cp -f "$ROOT/manifest.xml" "$HERE/content/manifest.xml"
cp -f "$ROOT/Localization/"*.csv "$HERE/content/Localization/"

echo "== 工坊上传目录已刷新 =="
find "$HERE/content" -type f | sort
