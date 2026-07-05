$ErrorActionPreference = 'Stop'

$sdkRoot = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
New-Item -ItemType Directory -Force -Path $sdkRoot | Out-Null

$zip = Join-Path $env:TEMP 'cmdline-tools.zip'
$uri = 'https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip'

Write-Host "Downloading Android command-line tools..."
$maxRetries = 5
for ($i = 1; $i -le $maxRetries; $i++) {
    try {
        curl.exe -L --retry 5 --retry-delay 3 -o $zip $uri
        if ((Get-Item $zip).Length -gt 1MB) { break }
        throw "Download incompleto"
    } catch {
        Write-Host "Tentativa $i falhou: $_"
        if ($i -eq $maxRetries) { throw }
        Start-Sleep -Seconds 5
    }
}

$extract = Join-Path $env:TEMP 'android-cmdline-tools'
if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
Expand-Archive -Path $zip -DestinationPath $extract -Force

$dest = Join-Path $sdkRoot 'cmdline-tools\latest'
New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
Move-Item (Join-Path $extract 'cmdline-tools') $dest

$sdkmanager = Join-Path $dest 'bin\sdkmanager.bat'
Write-Host "Installing Android SDK packages..."
$yes = "y`n" * 30
$yes | & $sdkmanager --sdk_root=$sdkRoot 'platform-tools' 'platforms;android-36' 'build-tools;36.0.0' 'cmdline-tools;latest'

[Environment]::SetEnvironmentVariable('ANDROID_HOME', $sdkRoot, 'User')
[Environment]::SetEnvironmentVariable('ANDROID_SDK_ROOT', $sdkRoot, 'User')

Write-Host "Android SDK instalado em: $sdkRoot"
