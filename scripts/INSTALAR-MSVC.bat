@echo off
setlocal EnableExtensions

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Pedindo permissoes de Administrador...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs -Wait"
    exit /b %errorLevel%
)

echo ============================================
echo  Instalar MSVC (C++) para MAUI Windows
echo ============================================
echo.

set "SETUP=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\setup.exe"
set "VS_INSIDERS=C:\Program Files\Microsoft Visual Studio\18\Insiders"
set "CONFIG=%~dp0vs-insiders-install.vsconfig"

echo [1/2] Visual Studio Build Tools 2022 + C++...
winget install --id Microsoft.VisualStudio.2022.BuildTools --accept-package-agreements --accept-source-agreements --disable-interactivity --override "--quiet --wait --norestart --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"
if errorlevel 1 (
    echo winget falhou; a tentar setup.exe directamente...
    "%SETUP%" install --installPath "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools" --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended --quiet --norestart
)

echo.
echo [2/2] Visual Studio Insiders + C++ (opcional)...
if exist "%SETUP%" (
    "%SETUP%" modify --installPath "%VS_INSIDERS%" --config "%CONFIG%" --includeRecommended --quiet --norestart
)

echo.
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\VC\Tools\MSVC" (
    echo MSVC instalado em Build Tools 2022.
) else if exist "%VS_INSIDERS%\VC\Tools\MSVC" (
    echo MSVC instalado em VS Insiders.
) else (
    echo AVISO: MSVC ainda nao encontrado. Reinicie o PC e tente de novo.
)

echo Feche e reabra o Visual Studio.
pause
