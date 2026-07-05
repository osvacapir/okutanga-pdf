# Okutanga PDF – Documentação de escopo

## O que é

- App **.NET MAUI + Blazor** multiplataforma para **ler documentos PDF**.
- Projeto: `okutangaPDF.sln` + `okutangaPDF.csproj`.

## Stack

- .NET MAUI 10, Blazor WebView, pdf.js (embutido), SQLite (`Microsoft.Data.Sqlite`).

## Plataformas

- Android, iOS, Mac Catalyst, Windows (WinUI).

## Publicação

### Windows
```bash
dotnet publish okutangaPDF.csproj -c Release -f net10.0-windows10.0.19041.0 -r win-x64
```

### Android (APK)
```powershell
./build-android-release.ps1
```

## Documentação

- **Requisitos:** `doc/REQUISITOS.md`
- **MVP leitor PDF:** `doc/OKUTANGA-MVP-PDF.md`
- **Métricas:** `doc/METRICAS-DESEMPENHO.md`

## Identidade

- **Nome:** Okutanga PDF
- **Pacote:** `com.okutangapdf.app`
