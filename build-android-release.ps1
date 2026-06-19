# Gera APK Release (multi-ABI) com appsettings.Production.json embebido.
# API Laravel em produção: https://api.vosikola.com/api/v1/ (ver appsettings.Production.json). sup.vosikola.com é frontend, não a API.
# Requisitos: JDK Android, Android SDK, workload maui-android instalado (`dotnet workload restore`).
param(
    [string] $OutputDirectory = "$(Join-Path $PSScriptRoot 'artifacts' 'android-release')"
)

$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "OlondongeApp.csproj"

Write-Host "Publicando para: $OutputDirectory" -ForegroundColor Cyan
dotnet publish $proj `
    -c Release `
    -f net10.0-android `
    -o $OutputDirectory `
    /p:AndroidPackageFormats=apk

$apk = Get-ChildItem -Path $OutputDirectory -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($apk) {
    Write-Host "APK: $($apk.FullName)" -ForegroundColor Green
} else {
    Write-Warning "Nenhum .apk encontrado em $OutputDirectory — verifique erros de build (ícone, SDK, assinatura)."
}
