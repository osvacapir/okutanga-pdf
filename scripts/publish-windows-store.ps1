# Publicar MSIX para Microsoft Store
# Requer certificado de assinatura Partner Center associado ao projeto

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

Write-Host "=== okutangaPDF — Microsoft Store MSIX ===" -ForegroundColor Cyan
Write-Host "Empresa: Zetabyte.tech · osvacapir@gmail.com"

dotnet publish okutangaPDF.csproj `
  -c Release `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:OkutangaPdfWindowsOnly=true `
  -p:PublishForStore=true `
  -o "artifacts/msix-store"

Write-Host ""
Write-Host "MSIX em: artifacts/msix-store" -ForegroundColor Green
Get-ChildItem -Path "artifacts/msix-store" -Filter "*.msix*" -Recurse -ErrorAction SilentlyContinue
