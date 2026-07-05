@echo off
setlocal EnableExtensions

:: Re-lancar como Administrador se necessario
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo A pedir permissoes de Administrador...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs -Wait"
    exit /b %errorLevel%
)

echo ============================================
echo  OkutangaPDF - Instalar dependencias MAUI
echo ============================================
echo.

set "ROOT=%~dp0.."
set "SETUP=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\setup.exe"
set "VS_PATH=C:\Program Files\Microsoft Visual Studio\18\Insiders"
set "CONFIG=%ROOT%\scripts\vs-insiders-install.vsconfig"

echo [1/5] Workloads .NET MAUI (dotnet)...
"%ProgramFiles%\dotnet\dotnet.exe" workload restore "%ROOT%\OkutangaPDF.csproj"
if errorlevel 1 goto :fail

echo.
echo [2/5] OpenJDK 17 (Android SDK)...
where winget >nul 2>&1 && (
    winget install --id Microsoft.OpenJDK.17 --accept-package-agreements --accept-source-agreements --disable-interactivity
)

echo.
echo [3/5] Visual Studio Insiders: MAUI + C++ Desktop...
if not exist "%SETUP%" (
    echo ERRO: Visual Studio Installer nao encontrado.
    goto :fail
)
"%SETUP%" modify --installPath "%VS_PATH%" --config "%CONFIG%" --includeRecommended --passive --norestart
if errorlevel 1 (
    echo AVISO: modify devolveu codigo %errorlevel%. Verifique o Visual Studio Installer.
)

echo.
echo [4/5] Android SDK...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\scripts\install-android-sdk.ps1"
if errorlevel 1 (
    echo AVISO: Android SDK pode estar incompleto. Verifique JAVA_HOME.
)

echo.
echo [5/5] Restore e build Windows...
"%ProgramFiles%\dotnet\dotnet.exe" restore "%ROOT%\OkutangaPDF.csproj"
"%ProgramFiles%\dotnet\dotnet.exe" build "%ROOT%\OkutangaPDF.csproj" -c Debug -f net10.0-windows10.0.19041.0
if errorlevel 1 goto :fail

echo.
echo ============================================
echo  Concluido. Reabra o Visual Studio.
echo  Alvo: Windows Machine
echo ============================================
pause
exit /b 0

:fail
echo.
echo Instalacao falhou. Veja mensagens acima.
pause
exit /b 1
