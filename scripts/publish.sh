#!/usr/bin/env bash
# Self-contained single-file publish for AutoClacker.Desktop.
# Usage: scripts/publish.sh [RID]
# Default RID is the host runtime identifier from `dotnet --info`.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

usage() {
  cat <<'EOF'
Usage: scripts/publish.sh [RID]

Publishes a portable, self-contained, single-file AutoClacker binary.
No installer is produced — run the published executable directly.

  RID   Target runtime identifier. If omitted, uses the host RID
        reported by `dotnet --info`.

Examples:
  scripts/publish.sh              # this machine
  scripts/publish.sh win-x64
  scripts/publish.sh linux-x64
  scripts/publish.sh osx-arm64
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

detect_host_rid() {
  local rid=""
  rid="$(dotnet --info 2>/dev/null | awk -F': *' '/^[[:space:]]*RID:/{print $2; exit}')"
  rid="${rid//$'\r'/}"
  if [[ -n "$rid" ]]; then
    printf '%s\n' "$rid"
    return 0
  fi
  return 1
}

RID="${1:-}"
if [[ -z "$RID" ]]; then
  if ! RID="$(detect_host_rid)"; then
    echo "error: could not detect host RID from 'dotnet --info'." >&2
    echo "Pass a runtime identifier explicitly, e.g.:" >&2
    echo "  $0 osx-arm64" >&2
    exit 1
  fi
fi

OUT="publish/${RID}"
echo "Publishing AutoClacker (self-contained, single-file) RID=${RID} -> ${OUT}"

dotnet publish src/AutoClacker.Desktop \
  -c Release \
  -r "$RID" \
  --self-contained \
  -p:PublishSingleFile=true \
  -o "$OUT"

if [[ "$RID" == osx-* ]]; then
  APP="${OUT}/AutoClacker.app"
  echo "Wrapping macOS app bundle ${APP} (CFBundleName=AutoClacker)"
  rm -rf "$APP"
  mkdir -p "${APP}/Contents/MacOS" "${APP}/Contents/Resources"
  mv "${OUT}/AutoClacker" "${APP}/Contents/MacOS/AutoClacker"
  chmod +x "${APP}/Contents/MacOS/AutoClacker"
  cp src/AutoClacker.Desktop/Info.plist "${APP}/Contents/Info.plist"
  cp src/AutoClacker.Desktop/Assets/AppIcon.icns "${APP}/Contents/Resources/AppIcon.icns"
  if command -v codesign >/dev/null 2>&1; then
    IDENTITY="-"
    if IDENT_OUT="$("$ROOT/scripts/macos-ensure-signing-identity.sh")"; then
      IDENTITY="$IDENT_OUT"
    fi
    echo "Signing ${APP} with identity: ${IDENTITY}"
    codesign --force --sign "$IDENTITY" \
      --identifier com.ironadamant.autoclacker \
      "${APP}/Contents/MacOS/AutoClacker"
    codesign --force --sign "$IDENTITY" \
      --identifier com.ironadamant.autoclacker \
      "$APP"
  fi
  echo "Done. Open ${APP} (or run ${APP}/Contents/MacOS/AutoClacker)."
  echo "Put AutoClacker.app in /Applications and grant Accessibility once so macOS keeps the permission."
else
  echo "Done. Run the published binary from ${OUT} (AutoClacker.exe on Windows, ./AutoClacker on macOS/Linux)."
fi
echo "Output is gitignored (publish/)."
