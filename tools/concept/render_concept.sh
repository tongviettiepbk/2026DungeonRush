#!/bin/bash
# Chụp file concept HTML ra PNG bằng Chrome headless, tỉ lệ ×2 (1 px CSS = 2 đơn vị canvas Unity 1080×2160).
# Dùng: tools/concept/render_concept.sh <concept.html> <out.png> [rộng=1220] [cao=1160]
set -e
HTML="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
OUT="$(cd "$(dirname "$2")" && pwd)/$(basename "$2")"
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --disable-gpu \
  --allow-file-access-from-files --hide-scrollbars --virtual-time-budget=5000 --force-device-scale-factor=2 \
  --window-size="${3:-1220},${4:-1160}" --screenshot="$OUT" "file://$HTML" >/dev/null 2>&1
echo "saved $OUT"
