#!/bin/zsh
set -euo pipefail

project_dir="${0:A:h}"
"$project_dir/build.sh"
"$project_dir/test.sh"

app_dir="$project_dir/dist/Codex代理启动器.app"
zip_path="$project_dir/dist/Codex-Proxy-Launcher-macOS-universal.zip"
checksum_path="$zip_path.sha256"

rm -f "$zip_path" "$checksum_path"
cd "$project_dir/dist"
ditto -c -k --keepParent 'Codex代理启动器.app' "$zip_path"
shasum -a 256 'Codex-Proxy-Launcher-macOS-universal.zip' > "$checksum_path"
shasum -a 256 -c 'Codex-Proxy-Launcher-macOS-universal.zip.sha256'

temporary_dir="$(mktemp -d)"
trap 'rm -rf "$temporary_dir"' EXIT
ditto -x -k 'Codex-Proxy-Launcher-macOS-universal.zip' "$temporary_dir"
codesign --verify --strict "$temporary_dir/Codex代理启动器.app"
plutil -lint "$temporary_dir/Codex代理启动器.app/Contents/Info.plist"
xcrun lipo -archs "$temporary_dir/Codex代理启动器.app/Contents/MacOS/CodexProxyLauncher" \
  | rg -q 'arm64'
xcrun lipo -archs "$temporary_dir/Codex代理启动器.app/Contents/MacOS/CodexProxyLauncher" \
  | rg -q 'x86_64'

echo "$zip_path"
echo "$checksum_path"
