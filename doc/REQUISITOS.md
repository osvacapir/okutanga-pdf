# OlondongeApp – Requisitos e priorização

## Estado actual da app

- **Stack:** .NET MAUI + Blazor WebView (.NET 10).
- **Estrutura:** `Components` (Blazor), `Platforms`, `wwwroot` na raiz do repositório (solution + csproj no mesmo nível).
- **Integração API:** `appsettings.json` com `Api:BaseUrl`; autenticação Sanctum (`/api/v1/auth/student/*`); cliente `IStudentGradesApi` para `/api/v1/student/*`.
- **Funcionalidades entregues:** login/logout; **Início** (`/home`) com insights a partir de dados locais; **Notas** (ano lectivo actual); **Histórico académico**; SQLite + sincronização de notas; notificações locais (Android).
- **Em preparação (UI guia):** **Grelha curricular** (pré-lista a partir de notas em cache até existir API); **Horário semanal**; **Propinas** — páginas com descrição do contrato futuro.
- **API prevista:** consumir `/api/student/v1` (prefixo real `/api/v1/` no código), com headers `X-Tenant-Domain` / `X-Tenant-Slug` quando configurados.

---

## Requisitos funcionais (escopo doc)

| # | Requisito | Descrição | Estado |
|---|-----------|-----------|--------|
| **R1** | **Configuração da API** | Base URL em `appsettings.json`; `Api:BaseUrl`, tenant opcional. | ✅ Feito |
| **R2** | **Descoberta de tenant** | Headers `X-Tenant-Domain` e `X-Tenant-Slug` quando preenchidos em config (sem `GET /tenants` automático). | ⚠️ Parcial |
| **R3** | **Autenticação (Sanctum)** | Login/logout `auth/student/*`; token em SecureStorage; Bearer nas chamadas. | ✅ Feito |
| **R4** | **Dados académicos (notas)** | Matrículas, períodos e notas por matrícula/período (ecrã Notas + SQLite). | ✅ Feito |
| **R5** | **Propinas** | Consulta de propinas / estado de pagamento / histórico conforme API estudante. | 🔄 Planeado (UI + RF detalhado abaixo) |
| **R6** | **Horários** | Horário semanal do estudante (turma) via API estudante. | 🔄 Planeado (UI + RF detalhado abaixo) |
| **R7** | **Navegação e UI** | Menu: Início, Notas, Histórico académico, Grelha curricular, Horário, Propinas; Entrar/Sair. | ✅ Estrutura base; R13–R16 em evolução |
| **R8** | **Funcionamento offline** | SQLite com última sync de notas e matrículas. | ✅ Feito (âmbito notas) |
| **R9** | **Notas por disciplina e período** | Visualizar notas (avaliações por matrícula/período). | ✅ Feito |
| **R10** | **Offline SQLite** | Persistir última sincronização de notas em SQLite; leitura sem rede. | ✅ Feito |
| **R11** | **Sync controlada** | Máx. 1×/24h; `GET student/grades/sync-status` antes do pull completo; «Atualizar agora» força. | ✅ Feito |
| **R12** | **Notificações de notas** | Notificação local Android quando a versão de notas muda após sync. | ✅ Feito (Android) |
| **R13** | **Painel inicial (Home)** | Ecrã `/home` após login com: acesso rápido às áreas principais; **insights** imediatos a partir de dados locais (sincronização, matrícula no ano actual, disciplinas com indicadores abaixo do mínimo positivo assumido 10/20). | ✅ Parcial (insights locais); evolução com API agregada opcional |
| **R14** | **Grelha curricular** | Listar **todas** as disciplinas previstas no curso/plano para a **classe/turma** do estudante (oficial), não só disciplinas com avaliação já lançada. | 🔄 Aguarda API; pré-visualização por disciplinas nas notas sincronizadas |
| **R15** | **Horário semanal** | Apresentar grade semanal (dia × slot): disciplina, docente opcional, sala, duração; respeitar turma/ano lectivo actual. | 🔄 Aguarda API |
| **R16** | **Propinas (detalhe)** | Resumo por ano lectivo: valores em dívida, mês corrente, próximos vencimentos; lista de mensalidades com estado (pago / pendente / atraso); ligação a comprovativo quando existir na API. | 🔄 Aguarda API |

**Especificação detalhada (MVP notas + offline):** `doc/OLONDONGE-MVP-NOTAS-OFFLINE.md`.

---

## Melhoria de requisitos — Módulo «Painel + percurso + serviços escolares»

Esta secção consolida o pedido de **grelha curricular**, **homepage com insights**, **horário** e **propinas**, alinhando app e backend.

### R13 — Painel inicial (insights)

**Objectivo:** O estudante vê, sem navegar para vários ecrãs, o estado do seu percurso e os pontos que requerem atenção.

**Critérios de aceitação (app):**

1. Após autenticação, a rota por omissão deve levar ao painel (`/home`) em vez de saltar directamente para notas (o utilizador pode abrir Notas a partir do painel).
2. O painel deve mostrar blocos de **acesso rápido** (Notas, Histórico, Grelha, Horário, Propinas).
3. **Insights imediatos** (MVP actual): calculados só com dados do `IGradesLocalStore` — última sincronização completa, presença de matrícula no ano lectivo actual (`is_current_ano_lectivo`), contagem de disciplinas com indicador de risco (nota final trimestral `MT` ou, na ausência, `CPT` / `CPP` / `MAC` comparados a **10** na escala 0–20).
4. Evolução futura (opcional): endpoint `GET /api/v1/student/dashboard` com agregados calculados no servidor (menos tráfego, regras de negócio centralizadas).

**Não objectivo (MVP insights locais):** substituir o parecer pedagógico oficial; os limiares na app são indicativos.

### R14 — Grelha curricular do curso (por classe)

**Objectivo:** Lista **completa** das disciplinas do plano curricular da turma/classe (independentemente de já haver notas lançadas).

**Critérios de aceitação:**

1. Dados devem refletir a turma/matricula do **ano lectivo actual** (ou selector explícito se multi-turma).
2. Por disciplina: nome, abreviatura opcional, carga horária opcional, tipo (geral/específica) se existir no modelo de dados.
3. Offline: após primeira sincronização bem-sucedida, permitir leitura em cache (SQLite) com TTL ou invalidação na próxima sync.

**Contrato API sugerido (api-med, a implementar):**

- `GET /api/v1/student/curriculum?matricula_id={id}`  
  Resposta: `{ success, data: [ { "disciplina_id", "name", "abreviatura", "carga_horaria", "tipo" } ] }`  
  Autorização: matrícula pertence ao `Student` autenticado.

**Estado actual na app:** ecrã `/curriculo` com lista **provisória** derivada das disciplinas presentes nas notas em cache, até o endpoint existir.

### R15 — Horário semanal

**Objectivo:** Visualizar o horário da turma do estudante (semana lectiva).

**Critérios de aceitação:**

1. Vista semanal (ex.: Seg–Sáb) com slots ordenados por hora.
2. Cada célula: disciplina (e opcionalmente docente, sala).
3. Suportar alterações de horário após sync (substituir plano local).

**Contrato API sugerido (api-med):**

- `GET /api/v1/student/schedule?matricula_id={id}`  
  Resposta: `{ success, data: { "slots": [ { "weekday", "start_time", "end_time", "disciplina_name", "sala", "employer_name" } ] } }`  
  Reutilizar modelos de `plano-horario` / `horario-slots` já existentes no domínio professor, filtrados por turma do estudante.

**Estado actual na app:** ecrã `/horario` informativo (aguarda API).

### R16 — Propinas

**Objectivo:** Transparência financeira do estudante face à escola.

**Critérios de aceitação:**

1. Resumo: total em dívida (se aplicável), mês actual, quantidade de meses em atraso.
2. Lista paginável ou por ano lectivo: mês/ano, valor devido, valor pago, estado.
3. Nunca expor dados de outros estudantes; validar sempre `matricula`/aluno na API.

**Contrato API sugerido (api-med):**

- `GET /api/v1/student/fees/summary?matricula_id={id}` — totais e alertas.
- `GET /api/v1/student/fees/installments?matricula_id={id}&ano_lectivo_id={optional}` — linhas de mensalidade.

Reutilizar entidades em `App\Models\Propinas\*` já usadas no backoffice, com vista filtrada ao aluno autenticado.

**Estado actual na app:** ecrã `/propinas` informativo (aguarda API).

### Priorização sugerida (backend + app)

| Fase | Entrega | Notas |
|------|---------|--------|
| **F1** | R14 curriculum + R15 schedule read API | Maior valor percebido no dia-a-dia do estudante; depende de dados de turma já na BD. |
| **F2** | R16 propinas read API | Regras de negócio e permissões financeiras; pode exigir alinhamento com contabilidade escolar. |
| **F3** | Cache offline SQLite para R14–R16 + sync | Paridade com o módulo de notas (R8/R11). |
| **F4** | `GET student/dashboard` opcional | Só se os insights locais forem insuficientes ou a payload for grande. |

### Dependências transversais

- **Autenticação:** todos os novos endpoints sob `Route::middleware('auth:sanctum')->prefix('student')`.
- **Multi-tenant:** mesmos headers `X-Tenant-*` que o resto da API v1.
- **Versionamento:** novos contratos em `/api/v1/student/…`; manter `DEPLOYMENT_VERSIONING.md` actualizado ao publicar.

---

## Requisitos técnicos

| # | Requisito | Descrição |
|---|-----------|-----------|
| **T1** | **HttpClient configurado** | `HttpClient` nomeado via factory (`IStudentGradesApi`), BaseAddress e timeout a partir de `ApiOptions`. |
| **T2** | **Serviços de API** | `IAuthService`, `IStudentGradesApi`, `IGradesLocalStore`, `IGradesSyncService`, `IGradesUpdateNotifier`. |
| **T3** | **Segurança do token** | Token em `SecureStorage`; não persistido em SQLite. |
| **T4** | **Tratamento de erros** | Login com mensagem; sync e lista local com estados «sem rede» / mensagens na UI. |
| **T5** | **Resumo académico local** | `IStudentAcademicOverviewService` — agrega dados já em SQLite para o painel (insights e lista provisória de disciplinas). |

---

## Proposta de priorização histórica (referência)

1. **Fundação:** R1, R3, T1–T3.  
2. **Primeiro valor:** R4, R7, R9–R12 (notas + offline).  
3. **Extensão actual:** R13 (painel), R14–R16 com API + cache conforme tabela F1–F3 acima.

---

## Como usar este documento

- **Requisitos:** tabela R1–R16 e secção «Melhoria» para novos módulos.  
- **Implementação API:** seguir contratos sugeridos ou ajustá-los em `api-med` com documentação OpenAPI e testes de feature.  
- **Implementação app:** ao surgirem endpoints, adicionar serviços C# (padrão `IStudentGradesApi`), DTOs em `Models/DTOs`, actualizar `REQUISITOS.md` (coluna Estado) e `OLONDONGE-MVP-NOTAS-OFFLINE.md` se a sync offline for alargada.
