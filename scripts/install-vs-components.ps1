$ErrorActionPreference = 'Stop'

$setup = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\setup.exe"
$installPath = 'C:\Program Files\Microsoft Visual Studio\18\Insiders'
$config = Join-Path $PSScriptRoot 'vs-insiders-install.vsconfig'

Write-Host 'Installing MAUI + C++ on Visual Studio Insiders...'
Write-Host 'UAC prompt may appear - please accept.'
Write-Host 'Installation takes 15-30 minutes.'

$args = @(
    'modify',
    '--installPath', $installPath,
    '--config', $config,
    '--includeRecommended',
    '--passive',
    '--norestart'
)

$proc = Start-Process -FilePath $setup -ArgumentList $args -Verb RunAs -PassThru
Write-Host "Installer started (PID $($proc.Id)). Waiting..."

while (-not $proc.HasExited) {
    Start-Sleep -Seconds 20
    $msvc = Join-Path $installPath 'VC\Tools\MSVC'
    if (Test-Path $msvc) {
        Write-Host "MSVC found at $msvc"
    }
}

Write-Host "Exit code: $($proc.ExitCode)"
if ($proc.ExitCode -ne 0) {
    throw "Installation failed with exit code $($proc.ExitCode)."
}

Write-Host 'Installation completed.'
