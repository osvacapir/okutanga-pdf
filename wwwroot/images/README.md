# Imagens (`wwwroot/images`)

Estes ficheiros são servidos pelo **Blazor WebView** (caminhos relativos ao `wwwroot`).

## Logo do sistema (recomendado)

Coloca o ficheiro **na pasta do projeto** `Resources/Images/` (não só em `wwwroot`):

| Ficheiro em `Resources/Images/` | Uso |
|----------------------------------|-----|
| `logo_system.png` | Preferido: ícone da app, splash nativo, WebView (copiado para aqui no build). |
| `logo.png` | Alternativa: se não existir `logo_system.png`, é usado como ícone/splash e copiado como `logo_system.png` para o WebView. |
| `logo_system.svg` | Vector: ícone/splash se não houver PNG; também copiado para o WebView. |
| `logo_system.jpg` / `.jpeg` / `.webp` | Opcional: copiados para o mesmo nome em `wwwroot/images/`. |

O alvo MSBuild **`CopyBrandingImagesToWwwRoot`** em `OlondongeApp.csproj` corre em **`PrepareForBuild`** e copia os ficheiros acima para `wwwroot/images/` antes dos static web assets.

A UI (Splash, Login, `index.html`) tenta carregar por esta ordem: PNG → WebP → JPG → SVG (`Branding/AppBranding.cs`).

**Ícone do instalador / launcher (Android, iOS):** depende de `MauiIcon` no `.csproj`, que só considera ficheiros em `Resources/Images/` (`logo_system.png`, `logo.png` ou `logo_system.svg`). Não basta ter a logo só em `wwwroot/` — o alvo `CopyBrandingImagesToWwwRoot` gera `wwwroot/images/` a partir de `Resources/Images/` em cada build.
