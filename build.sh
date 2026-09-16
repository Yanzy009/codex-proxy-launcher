#!/bin/zsh
set -euo pipefail

project_dir="${0:A:h}"
app_dir="$project_dir/dist/Codex代理启动器.app"
contents_dir="$app_dir/Contents"
macos_dir="$contents_dir/MacOS"
resources_dir="$contents_dir/Resources"
temporary_dir="$(mktemp -d)"
trap 'rm -rf "$temporary_dir"' EXIT

rm -rf "$app_dir"
mkdir -p "$macos_dir" "$resources_dir"

for architecture in arm64 x86_64; do
  xcrun swiftc -O -target "$architecture-apple-macosx13.0" \
    -framework AppKit -framework CFNetwork -framework SystemConfiguration \
    "$project_dir/Sources/ProxyConfiguration.swift" \
    "$project_dir/Sources/main.swift" \
    -o "$temporary_dir/launcher-$architecture"
done
xcrun lipo -create "$temporary_dir/launcher-arm64" "$temporary_dir/launcher-x86_64" \
  -output "$macos_dir/CodexProxyLauncher"

xcrun swiftc -O -parse-as-library -framework AppKit "$project_dir/Sources/IconGenerator.swift" \
  -o "$temporary_dir/icon-generator"
"$temporary_dir/icon-generator" "$temporary_dir/CodexProxy.iconset"
xcrun iconutil -c icns "$temporary_dir/CodexProxy.iconset" \
  -o "$resources_dir/CodexProxy.icns"

cp "$project_dir/Resources/Info.plist" "$contents_dir/Info.plist"
codesign --force --sign - "$app_dir"

echo "$app_dir"
