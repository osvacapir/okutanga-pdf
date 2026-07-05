# Okutanga PDF

Leitor PDF offline multiplataforma (.NET MAUI + Blazor WebView).

**Editor:** [Zetabyte.tech](https://zetabyte.tech) · **Desenvolvedor:** Osvaldo Capir · **Suporte:** osvacapir@gmail.com

## Funcionalidades

- Biblioteca e histórico de documentos recentes
- Leitura em scroll contínuo ou página a página
- Pesquisa de texto, marcadores, zoom
- «Abrir com» / partilha a partir de outras apps
- Tema claro, escuro ou automático
- Interface desktop e mobile (telefone)

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Windows: SDK 10.0.19041+ (para build Windows)
- Android: Android SDK (API 24+)

## Desenvolvimento

```bash
dotnet restore okutangaPDF.sln
dotnet build okutangaPDF.csproj -c Debug
```

Windows (Visual Studio): abrir `okutangaPDF.sln`.

## Testes

```bash
dotnet test OkutangaPDF.Tests/OkutangaPDF.Tests.csproj -c Release
```

Ou `./scripts/test-release.sh` (testes + builds Release).

## Publicação

| Plataforma | Comando |
|------------|---------|
| Android APK | `dotnet publish okutangaPDF.csproj -c Release -f net10.0-android -r android-arm64` |
| Play Store AAB | `./scripts/publish-playstore.sh` ou `-p:PublishForPlayStore=true` |
| Microsoft Store | `./scripts/publish-windows-store.ps1` |

Ver `store/CHECKLIST-PUBLICACAO.txt` e textos em `store/`.

## Identidade

| Campo | Valor |
|-------|-------|
| Nome | Okutanga PDF |
| ApplicationId | `com.okutangapdf.app` |
| Privacidade | https://zetabyte.tech/okutangapdf/privacidade |

## Licença

Copyright © 2026 Zetabyte.tech. Todos os direitos reservados.
