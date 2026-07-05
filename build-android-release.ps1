# Gera APK Release (multi-ABI) para sideload / testes.
# Requisitos: JDK 17+, Android SDK, workload maui-android (`dotnet workload restore`).
param(
    [string] $OutputDirectory = "$(Join-Path $PSScriptRoot 'artifacts' 'android-release')"
)

$ErrorActionPreference = "Stop"
$proj = Join-Path $PSScriptRoot "okutangaPDF.csproj"

Write-Host "Publicando okutangaPDF para: $OutputDirectory" -ForegroundColor Cyan
dotnet publish $proj `
    -c Release `
    -f net10.0-android `
    -o $OutputDirectory `
    /p:AndroidPackageFormats=apk

$apk = Get-ChildItem -Path $OutputDirectory -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($apk) {
    Write-Host "APK: $($apk.FullName)" -ForegroundColor Green
} else {
    Write-Warning "Nenhum .apk encontrado em $OutputDirectory — verifique erros de build (SDK, assinatura)."
}
