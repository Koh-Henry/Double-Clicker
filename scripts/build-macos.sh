#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_root="$(cd "$script_dir/.." && pwd)"
requested_arch="${1:-$(uname -m)}"

case "$requested_arch" in
  arm64|aarch64)
    runtime_id="osx-arm64"
    ;;
  x64|x86_64)
    runtime_id="osx-x64"
    ;;
  *)
    echo "Unsupported macOS architecture: $requested_arch" >&2
    exit 1
    ;;
esac

artifact_root="$project_root/artifacts/$runtime_id"
app_bundle="$artifact_root/Minecraft Double Clicker.app"
macos_directory="$app_bundle/Contents/MacOS"

case "$app_bundle" in
  "$project_root"/artifacts/*) ;;
  *)
    echo "Refusing to replace an app bundle outside the project artifacts directory." >&2
    exit 1
    ;;
esac

rm -rf "$app_bundle"
mkdir -p "$macos_directory"

dotnet publish "$project_root/MinecraftDoubleClicker.csproj" \
  --configuration Release \
  --runtime "$runtime_id" \
  --self-contained true \
  --output "$macos_directory" \
  -p:PublishSingleFile=true \
  -p:UseAppHost=true \
  -p:DebugType=None

cp "$project_root/packaging/macos/Info.plist" "$app_bundle/Contents/Info.plist"
chmod +x "$macos_directory/MinecraftDoubleClicker"

if [[ -n "${SIGNING_IDENTITY:-}" ]]; then
  while IFS= read -r candidate; do
    if file "$candidate" | grep -q 'Mach-O'; then
      codesign --force --timestamp --options runtime \
        --entitlements "$project_root/packaging/macos/MinecraftDoubleClicker.entitlements" \
        --sign "$SIGNING_IDENTITY" "$candidate"
    fi
  done < <(find "$macos_directory" -type f)

  codesign --force --timestamp --options runtime \
    --entitlements "$project_root/packaging/macos/MinecraftDoubleClicker.entitlements" \
    --sign "$SIGNING_IDENTITY" "$app_bundle"
fi

echo "macOS app: $app_bundle"
