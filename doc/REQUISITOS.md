# Okutanga PDF – Requisitos e priorização

## Estado actual da app

- **Stack:** .NET MAUI + Blazor WebView (.NET 10).
- **Plataformas:** Android, iOS, Mac Catalyst, Windows.
- **Estrutura:** `Components` (Blazor), `Platforms`, `wwwroot` na raiz do repositório (`okutangaPDF.sln` + `okutangaPDF.csproj`).
- **Propósito:** leitor de PDF multiplataforma — abrir, navegar e gerir documentos localmente.
- **Funcionalidades entregues (MVP identidade):** arranque, navegação principal, tema claro/escuro, layout responsivo (telefone/tablet).
- **Em preparação:** abrir ficheiro PDF, biblioteca de recentes, leitor com paginação e zoom.

---

## Requisitos funcionais

| # | Requisito | Descrição | Estado |
|---|-----------|-----------|--------|
| **R1** | **Abrir PDF** | Seleccionar ficheiro `.pdf` do dispositivo (FilePicker) ou receber via partilha/intent. | ✅ Feito |
| **R2** | **Visualização** | Renderizar páginas com scroll vertical ou horizontal; suportar zoom (pinch / botões). | ✅ Parcial (página a página + zoom) |
| **R3** | **Navegação por páginas** | Ir para página N, miniatura ou índice; indicador «página X de Y». | ✅ Parcial (anterior/seguinte + indicador) |
| **R4** | **Biblioteca / recentes** | Listar documentos abertos recentemente com nome, data e caminho; reabrir com um toque. | ✅ Feito |
| **R5** | **Persistência local** | Guardar metadados dos recentes em SQLite ou Preferences (sem enviar conteúdo para servidor). | ✅ Feito |
| **R6** | **Marcadores** | Guardar posição ou páginas favoritas por documento. | ✅ Feito |
| **R7** | **Pesquisa no texto** | Localizar termos dentro do PDF (quando o documento tiver camada de texto). | ✅ Feito |
| **R8** | **Modo escuro / claro** | Tema automático ou manual; conforto de leitura nocturno. | ✅ Feito |
| **R9** | **Layout adaptativo** | Sidebar em tablet/desktop; dock inferior em telefone; orientação portrait/landscape. | ✅ Feito |
| **R10** | **Funcionamento offline** | Leitura de PDFs já abertos ou guardados localmente sem rede. | ✅ Feito |
| **R11** | **Partilha / exportar** | Partilhar ou «Abrir com» a partir de outras apps (Android intent, iOS share extension). | ✅ Parcial (Android VIEW/SEND + Share; Windows arranque com .pdf) |
| **R12** | **Definições** | Tema, comportamento de scroll, manter ecrã ligado durante leitura. | ✅ Feito |

**Especificação detalhada (MVP leitor):** `doc/OKUTANGA-MVP-PDF.md`.

---

## Requisitos técnicos

| # | Requisito | Descrição |
|---|-----------|-----------|
| **T1** | **Renderização PDF** | Biblioteca nativa ou multiplataforma (ex.: PDFium, SkiaSharp, ou WebView + pdf.js para MVP web). |
| **T2** | **Armazenamento local** | SQLite ou `Preferences` para recentes e marcadores; ficheiros PDF no sistema de ficheiros do utilizador. |
| **T3** | **FilePicker MAUI** | `FilePicker.PickAsync` com filtro `application/pdf`; permissões de armazenamento por plataforma. |
| **T4** | **Performance** | Carregar páginas sob demanda (virtualização); cache de bitmaps/rasters limitado em memória. |
| **T5** | **Tratamento de erros** | PDF corrompido, sem permissão ou demasiado grande — mensagens claras na UI. |
| **T6** | **Identidade** | Nome **Okutanga PDF**, `ApplicationId` `com.okutangapdf.app`, tema vermelho/coral de marca. |

---

## Priorização sugerida

| Fase | Entrega | Notas |
|------|---------|--------|
| **F1** | R1 + R2 + R8–R9 | Valor mínimo: abrir e ler um PDF com boa UX. |
| **F2** | R3 + R4 + R5 | Recentes e navegação — uso diário. |
| **F3** | R10 + R12 | Offline robusto e definições. |
| **F4** | R6, R7, R11 | Marcadores, pesquisa, partilha. |

---

## Como usar este documento

- **Requisitos:** tabela R1–R12 para funcionalidades do leitor.
- **Implementação:** serviços em `Services/`, páginas Blazor em `Components/Pages/`, actualizar coluna **Estado** ao concluir cada item.
- **Identidade:** marca **Okutanga PDF** — leitor PDF multiplataforma; não depende de API externa nem autenticação.
