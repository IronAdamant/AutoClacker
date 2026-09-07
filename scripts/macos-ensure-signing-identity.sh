#!/usr/bin/env bash
# Ensure a stable codesigning identity named "AutoClacker Signing".
# Ad-hoc (`codesign -s -`) gets a new CDHash every sign, so TCC Accessibility
# does not stick across copies. A self-signed identity with a fixed CN does.
set -euo pipefail

CN="AutoClacker Signing"
DIR="${HOME}/.autoclacker-codesign"
KEYCHAIN="${HOME}/Library/Keychains/login.keychain-db"
if [[ ! -f "$KEYCHAIN" ]]; then
  KEYCHAIN="${HOME}/Library/Keychains/login.keychain"
fi

valid_identity() {
  security find-identity -p codesigning -v 2>/dev/null | grep -F -q "$CN"
}

if valid_identity; then
  printf '%s\n' "$CN"
  exit 0
fi

mkdir -p "$DIR"
chmod 700 "$DIR"
umask 077

CFG="${DIR}/cert.cnf"
KEY="${DIR}/autoclacker.key"
CRT="${DIR}/autoclacker.crt"
P12="${DIR}/autoclacker.p12"
PASS="autoclacker-codesign"

if [[ ! -f "$CRT" || ! -f "$KEY" ]]; then
  cat > "$CFG" <<'EOF'
[ req ]
distinguished_name = dn
prompt = no
x509_extensions = codesign_ext

[ dn ]
CN = AutoClacker Signing
O = IronAdamant

[ codesign_ext ]
basicConstraints = CA:FALSE
keyUsage = digitalSignature
extendedKeyUsage = codeSigning
EOF

  openssl req -new -newkey rsa:2048 -x509 -days 3650 -nodes \
    -keyout "$KEY" -out "$CRT" -config "$CFG" >/dev/null 2>&1
fi

# macOS `security import` rejects OpenSSL 3's default PBES2 MAC.
openssl pkcs12 -export -legacy \
  -inkey "$KEY" -in "$CRT" -out "$P12" \
  -name "$CN" -passout "pass:${PASS}" >/dev/null 2>&1

if ! security find-certificate -c "$CN" >/dev/null 2>&1; then
  if ! security import "$P12" -k "$KEYCHAIN" -P "$PASS" \
    -T /usr/bin/codesign -T /usr/bin/security >/dev/null; then
    echo "error: keychain import of '${P12}' failed" >&2
    exit 1
  fi
fi

# Self-signed certs import as CSSMERR_TP_NOT_TRUSTED until marked trusted.
security add-trusted-cert -d -r trustRoot -k "$KEYCHAIN" "$CRT" >/dev/null 2>&1 || true
security set-key-partition-list -S apple-tool:,apple:,codesign: -s \
  -k "" "$KEYCHAIN" >/dev/null 2>&1 || true

if ! valid_identity; then
  echo "error: failed to install codesigning identity '${CN}'" >&2
  exit 1
fi

printf '%s\n' "$CN"
