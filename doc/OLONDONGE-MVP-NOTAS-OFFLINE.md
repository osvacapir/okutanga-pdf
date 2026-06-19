# OlondongeApp — MVP: notas, offline (SQLite) e sincronização

Documento de **especificação e decisões** antes da implementação. Alinha com `.cursor/rules/olondonge-app.mdc`, `doc/REQUISITOS.md` e `../../.cursor/skills/med-ecosystem/reference-auth-requisitos.md`.

---

## 1. Avaliação do estado atual

| Área | Estado |
|------|--------|
| **OlondongeApp** | Template MAUI + Blazor (Home/Counter/Weather); sem `appsettings` de API, sem serviços HTTP, sem auth, sem SQLite. |
| **Contrato API “oficial”** | Docs referem `/api/student/v1` e `GET /api/v1/tenants`. |
| **api-med (estudante)** | Contrato oficial: `App\Http\Controllers\Api\V1\AuthStudentController` + `AuthStudentService` sob `/api/v1/auth/student/*` e `/api/v1/student/*`. Rotas legadas sem `v1` (`loginap`, `pegarAvaliacaoNew`, …) foram **removidas** da API. |
| **Requisito de auth (ecossistema)** | Estudante: **número de processo (matrícula)** OU **número do documento de identificação** + senha. |

**Conclusão:** a app e a API precisam evoluir em conjunto: **normalizar rotas sob `/api/v1/auth/student/*` e `/api/v1/student/*`**, endurecer com `auth:sanctum` onde aplicável, e **unificar o login estudante** com a mesma semântica do `LoginRequest` (processo ou documento + password). Sem isso, o MVP fica frágil ou inseguro.

---

## 2. Objetivo do MVP (produto)

1. **Autenticação** com identificador **matrícula/número de processo** ou **número do bilhete (documento)** + **senha** (não obrigatório email).
2. Após login, **listar notas** do estudante por **disciplina** e **período** (trimestre/periodo conforme dados da API v1 — grades + períodos por matrícula).
3. **Android** como alvo principal; outras plataformas MAUI podem seguir o mesmo código.
4. **SQLite** como cache local para **consulta offline** das últimas notas sincronizadas.
5. **Sincronização em segundo plano:** no máximo **uma vez por dia** quando houver rede; **só descarregar notas em profundidade** se a API indicar **atualização** (ver secção 5).
6. **Notificações** quando existirem notas atualizadas no servidor (ver secção 6).

---

## 3. Fluxos funcionais (app)

### 3.1 Login

- UI: um campo “Número de matrícula ou número do bilhete” + “Senha” + botão Entrar (e opcional lembrar tenant/escola se multi-tenant).
- Pedido HTTP: enviar credenciais para o endpoint acordado (ver secção 4).
- Sucesso: guardar **token Sanctum** em **SecureStorage** (nunca em SQLite em claro); opcionalmente metadados mínimos do utilizador para o ecrã inicial.
- Falha: mensagens claras (credenciais, rede, timeout) conforme `reference-auth-requisitos.md` (RNF-C1/C2).

### 3.2 Notas (online)

- Obter **matricula_id** (e contexto turma/ano) via endpoint dedicado (ex.: classe/curso ou perfil estudante) — hoje `pegarClasseCurso` usa input `email` de forma legada; o contrato novo deve usar **utilizador autenticado** ou parâmetros explícitos.
- Para cada combinação matrícula + período necessária, obter linhas de notas (estrutura atual de `pegarAvaliacaoNew`: MAC, CPP, CPT, MT, exames, etc.).
- UI: filtros por **período** e lista por **disciplina**; estados vazio / erro / carregamento.

### 3.3 Notas (offline)

- Ler **apenas** da base SQLite local (última sincronização bem-sucedida).
- Indicador visual “dados de” (data/hora da última sync) e, se possível, “sem ligação”.

### 3.4 Logout

- Revogar token no servidor (logout API) e apagar SecureStorage + **dados locais sensíveis** conforme política (opcional: manter só cache anónimo ou apagar tudo).

---

## 4. Contrato API (alvo para o MVP)

Todas as rotas abaixo são **proposta** a implementar ou consolidar em `api-med`; prefixo base da app: `{BaseUrl}/api/v1/...` com headers de tenant `X-Tenant-Domain` e `X-Tenant-Slug` quando aplicável.

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/auth/student/login` | — | Body: `identifier` (processo ou doc), `password` (ou manter `email` como nome de campo para compatibilidade interna, mas documentar como “identificador”). Resposta: `access_token`, `token_type`, opcional `user` mínimo. |
| POST | `/auth/student/logout` | Bearer | Revogar token. |
| GET | `/auth/student/me` | Bearer | Perfil + escola. |
| GET | `/student/enrolments` ou `/student/class-course` | Bearer | Matrículas / turma / ano (substituir semântica de `pegarClasseCurso`). |
| GET | `/student/grades/periods?matricula_id=` | Bearer | Períodos disponíveis (substituir/ajustar `pegarTrimeste`). |
| GET | `/student/grades?matricula_id=&periodo_id=` | Bearer | Notas detalhadas (substituir `pegarAvaliacaoNew`). |
| GET | `/student/grades/sync-status` | Bearer | **Leve:** `last_modified` ou `version` ou hash agregado das notas do estudante — para decidir sync completo (ver secção 5). |

**Compatibilidade:** manter rotas antigas como deprecated durante uma versão, com redirecionamento interno para os novos handlers, conforme `DEPLOYMENT_VERSIONING.md` em api-med.

---

## 5. Sincronização e performance

### 5.1 Princípios

- **Não** sincronizar em cada mudança de rede; usar **agendamento** (máx. 1×/24h por defeito, configurável).
- **Primeiro passo leve:** `GET .../grades/sync-status` (ou `HEAD` com `ETag`). Se `version`/`last_modified` igual ao guardado localmente → **não** chamar endpoints pesados de notas.
- Se houver alteração → descarregar períodos + notas necessárias; **uma transação SQLite** por sync para consistência.
- **Login inicial** ou “pull manual” pode forçar sync completo independentemente do cron diário.

### 5.2 Dados locais (SQLite)

Tabelas conceituais (nomes finais na implementação):

- `sync_meta` — `key`, `last_sync_utc`, `server_grades_version` (ou `last_modified`).
- `periods` — `id`, `name`, `matricula_id`, …
- `grade_lines` — chave composta ou surrogate key; campos alinhados ao JSON da API (disciplina, período, MAC, CPP, …).
- Índices em `(matricula_id, periodo_id)` para leitura rápida na UI.

### 5.3 Threads e UI

- Sync em **background** (`Task`, não bloquear UI thread); serviços injectáveis (`IGradesSyncService`, `ILocalGradesStore`).

---

## 6. Notificações (notas atualizadas)

| Opção | Descrição |
|-------|-----------|
| **A — Push (recomendado para “há notas novas”)** | API ou job envia notificação FCM/APNs quando notas mudam. Requer registo do device token, tópico ou user-id no backend. |
| **B — Local após sync** | Após sync diário bem-sucedido, se `server_grades_version` mudou, mostrar **notificação local** (MAUI `LocalNotifications`). Não exige servidor de push; o utilizador só é avisado quando a app corre o sync. |
| **C — Híbrido** | Push para urgência; local como fallback. |

**MVP pragmático:** começar por **B** (notificação local quando a sync deteta alteração) + documentar **A** como fase 2 se o backend passar a emitir eventos/FCM.

**Permissões Android:** `POST_NOTIFICATIONS` (API 33+), canal de notificação com nome claro (“Notas atualizadas”).

---

## 7. Pacotes NuGet (referência)

- `sqlite-net-pcl` ou `Microsoft.Data.Sqlite` + Dapper (equipa decide; `sqlite-net-pcl` é comum em MAUI).
- Opcional: `Plugin.LocalNotification` ou API de notificações do MAUI conforme versão suportada.

---

## 8. Fases de implementação sugeridas

1. **API:** login estudante unificado + rotas v1 + `sync-status` + proteção Sanctum nas rotas de notas; testes de feature.
2. **App:** config + HttpClient + tenant + login + logout + ecrã notas online.
3. **App:** SQLite + repositório + UI offline + indicador “última atualização”.
4. **App:** worker/timer sync diário + comparação de versão + notificação local.
5. **Opcional:** FCM + registo token na API.

---

## 9. Riscos e dependências

- **API legada** sem versionamento claro: risco de inconsistência; mitigação = secção 4 e versionamento em api-med.
- **Login só por email** no `AuthStudentService` atual: **bloqueante** para o requisito “bilhete ou matrícula”; mitigação = reutilizar lógica de `LoginRequest` / busca por documento em `People` ou tabela `alunos` (confirmar coluna exacta do “bilhete” na BD).
- **Rotas sem middleware** nas rotas antigas de `pegar*`: risco de exposição de dados; as novas rotas devem exigir **estudante autenticado** e **autorização** (só as próprias matrículas).

---

## 10. Referências no repositório

- Auth requisitos: `.cursor/skills/med-ecosystem/reference-auth-requisitos.md`
- Requisitos app: `OlondongeApp/doc/REQUISITOS.md`
- Regras Cursor: `.cursor/rules/olondonge-app.mdc`, `.cursor/rules/olondonge-mvp-notas.mdc`
- API: `api-med/app/Services/Auth/AuthStudentService.php`, `api-med/routes/api.php`

---

## 11. Implementação (estado)

### api-med

- Rotas **v1** (prefixo `/api/v1/`):
  - **Auth:** `POST auth/student/login`, `POST auth/student/logout`, `GET auth/student/me`, `GET auth/student/profile/me` — `App\Http\Controllers\Api\V1\AuthStudentController`.
  - **Notas / matrículas:** `GET student/enrolments`, `GET student/grades/sync-status`, `GET student/grades/periods`, `GET student/grades` — `App\Http\Controllers\Api\V1\StudentAppController`.
- Login estudante: campo **`identifier`** (matrícula / `num_processo`, número do documento `peoples.num_doc_id`, ou email) + **`password`**; resposta `{ success, data: { access_token, token_type, user } }`.
- Rotas legadas sem `v1` foram **removidas**; equivalências v1: `api-med/docs/DEPLOYMENT_VERSIONING.md` §8.

### OlondongeApp

- Config: `appsettings.json` embutido (`Api:BaseUrl`, `TenantDomain`, `TenantSlug`).
- Serviços: `AuthService` (SecureStorage), `StudentGradesApi` + `AuthDelegatingHandler`, `SqliteGradesLocalStore`, `GradesSyncService` (24h + `sync-status` antes do pull completo), `LocalGradesUpdateNotifier` (Android: canal + `NotificationCompat`).
- UI: `Login.razor`, `Grades.razor` (matrícula/período, tabela de notas, «Atualizar agora»).
- Tema/estrutura: layout e sidebar base alinhados ao padrão da Ulongisi (`Header`, overlay sidebar, `content-wrapper`, `responsive.css`).

### Notas de configuração

- **Emulador Android:** `Api:BaseUrl` por defeito `http://10.0.2.2:8000/api/v1/` (anfitrião na porta 8000). Ajustar porta/HTTPS conforme o `php artisan serve` ou Docker.
- **Dispositivo físico:** usar IP da máquina ou URL pública da API no `appsettings.json` (rebuild).

---

## 12. MVP — Pré-requisitos de dados (base de dados)

Para o ecrã de notas, sync e `grades/sync-status` funcionarem de ponta a ponta, a BD deve conter dados coerentes com a lógica de `AuthStudentService`:

| Necessidade | Tabelas / dados |
|-------------|-----------------|
| Login por matrícula | `students.num_processo`, `users` com `belongable_type` = estudante e password válida |
| Login por bilhete / documento | `peoples.num_doc_id` ligado ao `students.people_id` |
| Matrículas no ecrã «Notas» | `matriculas` com `aluno_id` do estudante; joins com `turmas`, `classes`, `cursos`, `ano_lectivos` |
| Períodos | `avaliacao_cabs` + `avaliacao_dets` **ou** linhas em `avaliacaos` com `periodos`; `getTrimestres` prefere o primeiro conjunto se existir |
| Linhas de notas | `avaliacao_cabs`, `avaliacao_dets`, `modulo_turma`, `modulos`, `disciplinas`, `periodos`; exames em `exame_dets` (relacionados a `avaliacao_cab_id`) |
| Versão para sync | `updated_at` em `avaliacao_cabs`, `avaliacao_dets` e `avaliacaos` — o `sync-status` usa o **máximo** destes timestamps (unix em `grades_version`, ISO em `grades_version_iso`). Sem matrículas devolve versão `"0"`. |

**Checklist rápido antes de demo/release:** existe pelo menos um utilizador estudante com token; pelo menos uma `matricula` desse aluno; para essa matrícula existem períodos e cabeçalhos de avaliação (`avaliacao_cabs`) ou avaliações antigas (`avaliacaos`), conforme o ano lectivo em uso.

---

## 13. MVP — Infraestrutura e segurança (deploy)

| Tema | Recomendação |
|------|----------------|
| **URL da API** | HTTPS em produção; `APP_URL` e `SANCTUM_STATEFUL_DOMAINS` / config Sanctum alinhados aos domínios reais |
| **OlondongeApp** | `Api:BaseUrl` a terminar em `/api/v1/` (com barra final aceite pelo `HttpClient`); em Android físico, IP ou hostname acessível a partir do dispositivo |
| **Health** | `GET /api/health` acessível (liveness); a app usa esta rota para o banner de estado, **não** `/api/v1/health` |
| **Docker / VPS** | Fluxo de migrações e variáveis em `api-med/docs/DEPLOYMENT_VERSIONING.md` |
| **CORS** | Chamadas da app são sobretudo **HttpClient nativo** (MAUI); se houver cliente browser, validar `config/cors.php` |
| **Credenciais de teste** | Utilizador com papel estudante ou `belongable` estudante; senha em conformidade com a política da API (ex.: mínimo 6 caracteres no `LoginStudentRequest`) |

---

## 14. MVP — Contrato oficial do cliente

- **OlondongeApp (release MVP):** consome **exclusivamente** `/api/v1/auth/student/*` e `/api/v1/student/*`.
- **Rotas estudante sem prefixo v1** (`/api/loginap`, `pegarTrimeste`, …) **deixaram de existir** na API; clientes antigos devem migrar para v1 (tabela histórica em `api-med/docs/DEPLOYMENT_VERSIONING.md` §8).
- **Evolução:** novas funcionalidades estudante só em rotas v1.
