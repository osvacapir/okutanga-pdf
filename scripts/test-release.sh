#!/usr/bin/env bash
# Bateria de verificação Release — Windows + Android + testes unitários
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "=== okutangaPDF — bateria de testes Release ==="

echo ""
echo "[1/4] Testes unitários"
dotnet test "OkutangaPDF.Tests/OkutangaPDF.Tests.csproj" -c Release --verbosity minimal

echo ""
echo "[2/4] Build Windows Release"
dotnet build "okutangaPDF.csproj" -c Release -f net10.0-windows10.0.19041.0 -r win-x64 \
  -p:OkutangaPdfWindowsOnly=true

echo ""
echo "[3/4] Build Android Release (arm64)"
dotnet restore "okutangaPDF.csproj" \
  -p:OkutangaPdfWindowsOnly=false \
  -p:TargetFrameworks=net10.0-android \
  -p:RuntimeIdentifier=android-arm64
dotnet build "okutangaPDF.csproj" -c Release -f net10.0-android -r android-arm64 \
  -p:OkutangaPdfWindowsOnly=false --no-restore

echo ""
echo "[4/4] Verificação de artefactos store (dry-run publish)"
dotnet publish "okutangaPDF.csproj" -c Release -f net10.0-android -r android-arm64 \
  -p:OkutangaPdfWindowsOnly=false \
  -p:PublishForPlayStore=true \
  -p:AndroidKeyStore=false \
  --no-restore 2>&1 | tail -5

echo ""
echo "=== Concluído com sucesso ==="
