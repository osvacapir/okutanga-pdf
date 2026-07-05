#!/usr/bin/env bash
# Publicar AAB para Google Play (requer keystore configurado)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

RID="${ANDROID_RID:-android-arm64}"
KEYSTORE="${ANDROID_SIGNING_KEYSTORE:-Keystore/crmtic-upload/crmtic-upload.keystore}"
STORE_PASS="${ANDROID_KEYSTORE_PASSWORD:?Defina ANDROID_KEYSTORE_PASSWORD}"
KEY_ALIAS="${ANDROID_KEY_ALIAS:?Defina ANDROID_KEY_ALIAS}"
KEY_PASS="${ANDROID_KEY_PASSWORD:?Defina ANDROID_KEY_PASSWORD}"

echo "=== okutangaPDF — Play Store AAB ==="
echo "Empresa: Zetabyte.tech · osvacapir@gmail.com"

dotnet restore okutangaPDF.csproj \
  -p:OkutangaPdfWindowsOnly=false \
  -p:TargetFrameworks=net10.0-android \
  -p:RuntimeIdentifier="$RID"

dotnet publish okutangaPDF.csproj \
  -c Release \
  -f net10.0-android \
  -r "$RID" \
  -p:OkutangaPdfWindowsOnly=false \
  -p:PublishForPlayStore=true \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$KEYSTORE" \
  -p:AndroidSigningStorePass="$STORE_PASS" \
  -p:AndroidSigningKeyAlias="$KEY_ALIAS" \
  -p:AndroidSigningKeyPass="$KEY_PASS"

echo ""
echo "AAB gerado em: bin/Release/net10.0-android/$RID/publish/"
find bin/Release/net10.0-android -name "*.aab" 2>/dev/null || true
