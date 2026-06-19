# OlondongeApp – Documentação de escopo

## O que é

- App **.NET MAUI + Blazor** para **estudantes**: acesso a dados académicos, propinas, horários, etc.
- Projeto principal: `OlondongeApp`. Consome a **API** em `/api/student/v1`.

## Stack

- .NET MAUI, Blazor WebView. Estrutura em `Components`, `Platforms`, `wwwroot` (pasta única: `OlondongeApp.sln` + `OlondongeApp.csproj` na raiz).

## Integração com a API

- **Base URL**: configurada em `appsettings*.json`. Em desenvolvimento: localhost ou URL do tenant.
- **Tenant**: descoberta `GET /api/v1/tenants`; headers `X-Tenant-Domain` / `X-Tenant-Slug` nas chamadas student.
- **Auth**: login compatível com API student (Sanctum).

## Documentação

- **OlondongeApp**: `doc` (este escopo). Documentação detalhada pode ser adicionada em `doc` ou `docs`.
- **MVP notas, SQLite offline, sync e notificações:** `doc/OLONDONGE-MVP-NOTAS-OFFLINE.md`.
- **Métricas e alvos de desempenho:** `doc/METRICAS-DESEMPENHO.md`.
- **Geral do ecossistema**: `../../docs` (raiz Sup).
- **API**: `../../Api/docs`, rotas em `Api/routes/api/student.php`.

## Regras Cursor

- Regra geral do ecossistema: `.cursor/rules/ecosystem.mdc` (na raiz do repositório).
- Regra OlondongeApp: `.cursor/rules/olondonge-app.mdc` (na raiz do ecossistema `med`).
