#!/usr/bin/env bash
# 编译并部署 Mod 到游戏 Mods 目录。
# 用法: ./deploy.sh        (默认游戏路径)
#       GAME_DIR="D:/xxx" ./deploy.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GAME="${GAME_DIR:-D:/steam/steamapps/common/Probably Stolen}"
MOD_ID="mouse_keybinds"
DEST="$GAME/Mods/$MOD_ID"

echo "== 编译 =="
dotnet build "$ROOT/src/MouseKeybinds.csproj" -c Release -p:GameDir="$GAME" --nologo -v:minimal

echo "== 部署到 $DEST =="
mkdir -p "$DEST"
cp -f "$ROOT/build/MouseKeybinds.dll" "$DEST/"
cp -f "$ROOT/manifest.xml" "$DEST/"
# 本地化资源（Localization/<locale>.csv）
if [ -d "$ROOT/Localization" ]; then
  mkdir -p "$DEST/Localization"
  cp -f "$ROOT/Localization/"*.csv "$DEST/Localization/"
fi
ls -la "$DEST"
echo "完成。启动游戏后检查 Player.log 里的 [ModLoader] 输出。"
