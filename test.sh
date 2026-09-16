#!/bin/zsh
set -euo pipefail

project_dir="${0:A:h}"
app_dir="$project_dir/dist/Codex代理启动器.app"
executable="$app_dir/Contents/MacOS/CodexProxyLauncher"
temporary_dir="$(mktemp -d)"
trap 'rm -rf "$temporary_dir"' EXIT

xcrun swiftc -O "$project_dir/Sources/ProxyConfiguration.swift" \
  "$project_dir/Tests/ProxyConfigurationTests.swift" \
  -o "$temporary_dir/proxy-tests"
"$temporary_dir/proxy-tests"

test -x "$executable"
plutil -lint "$app_dir/Contents/Info.plist"
codesign --verify --strict "$app_dir"
xcrun lipo -archs "$executable" | rg -q 'arm64'
xcrun lipo -archs "$executable" | rg -q 'x86_64'
test -s "$app_dir/Contents/Resources/CodexProxy.icns"
"$executable" --self-test

echo "bundle checks: OK"
