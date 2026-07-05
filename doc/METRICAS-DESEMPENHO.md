# Métricas de desempenho — OkutangaPDF (MVP notas)

Este documento define **o que medir**, **como medir** e **alvos (SLOs)** para garantir que a app se mantém rápida, estável e eficiente (rede, SQLite e UI).

---

## 1. Regras gerais de medição

- **Unidade**: sempre em milissegundos (ms) para duração; bytes/MB para tamanho; contagens para registos/itens.
- **Percentis**: reportar pelo menos **P50 / P90 / P95** (média pode enganar).
- **Ambientes**: medir separadamente:
  - **Emulador Android** (rede “boa” e “má”)
  - **Dispositivo low-end** (2–4 GB RAM, CPU fraco)
  - **Dispositivo mid-range**
- **Cenários**: medir em 3 cenários reais:
  - **Offline** (modo avião)
  - **Online estável**
  - **Online instável** (perda de rede/intermitência)

---

## 2. Métricas (KPIs) e alvos (SLOs)

### 2.1 Arranque e navegação (UI)

- **TTI Home/Notas (cold start)**: tempo entre abrir a app e ver o ecrã de notas renderizado com dados locais (ou estado vazio).
  - **Alvo**: P90 ≤ **2000 ms** (dispositivo mid), P90 ≤ **3500 ms** (low-end)
- **TTI Login (cold)**: abrir app → ecrã login pronto.
  - **Alvo**: P90 ≤ **1500 ms**
- **Mudança de filtros (matrícula/período) em modo offline**: tempo de interação até a tabela atualizar.
  - **Alvo**: P95 ≤ **200 ms**

### 2.2 Login (rede)

- **Latência do login (POST `/auth/student/login`)**: do clique “Entrar” até token guardado.
  - **Alvo**: P90 ≤ **2000 ms** em rede normal; timeout coerente (60–90s) em rede fraca.
- **Taxa de falhas do login** (HTTP != 2xx ou exceção):
  - **Alvo**: ≤ **1%** (excluindo credenciais inválidas)

### 2.3 Sincronização (rede + SQLite)

Pipeline da sync (executada por `GradesSyncService`):

1) **Check leve** `GET /student/grades/sync-status`
2) Se houver mudança → **pull completo** (enrolments + periods + grades)
3) Escrita em SQLite

Métricas:

- **Duração do check leve**:
  - **Alvo**: P95 ≤ **800 ms**
- **Frequência efetiva de sync**:
  - **Alvo**: **≤ 1** pipeline/24h (exceto “Atualizar agora”)
- **Taxa de “NoChange”**:
  - **Alvo**: alta (indicador de que não se descarrega payload pesado sem necessidade)
- **Duração do pull completo** (rede+parse+SQLite):
  - **Alvo**: P90 ≤ **6000 ms** para 1 matrícula, 3 períodos; P90 ≤ **12000 ms** para casos grandes
- **Tempo de escrita SQLite por (matrícula, período)**:
  - **Alvo**: P95 ≤ **500 ms**

### 2.4 SQLite / Cache local

- **Tamanho do DB** (`okutanga_pdf.db3`):
  - **Alvo**: ≤ **20 MB** típico; alertar acima de 50 MB (necessidade de limpeza/compactação)
- **Registos**:
  - **grade_lines**: contagem total
  - **local_periods**: por matrícula
  - **local_enrolments**: total
- **Erros de desserialização** (linhas corrompidas):
  - **Alvo**: 0; se >0, logar e remover as linhas afetadas em manutenção futura

### 2.5 Memória, bateria e rede

- **Pico de memória durante sync**:
  - **Alvo**: não exceder **+150 MB** acima do baseline (mid-range)
- **Consumo de dados por sync**:
  - **Alvo**: “check leve” ≈ insignificante; “pull completo” proporcional (monitorizar KB/MB por dia)
- **Bateria**:
  - **Alvo**: sync não deve acordar a app repetidamente; no MVP, deve correr apenas quando o utilizador abre a app (ou quando o timer interno corre na sessão)

---

## 3. Instrumentação mínima (recomendado)

### 3.1 Eventos de logging (chaves fixas)

Implementar logs estruturados (por ex. `ILogger`) com:

- **Evento** `grades_sync_pipeline`:
  - `status` (Completed/SkippedTooSoon/SkippedNoNetwork/SkippedNoChangeOnServer/Failed)
  - `force` (true/false)
  - `check_ms`
  - `pull_ms`
  - `sqlite_ms_total`
  - `enrolments_count`, `periods_count_total`, `grades_count_total`
  - `grades_version_old`, `grades_version_new`
- **Evento** `login_attempt`:
  - `success`
  - `http_status`
  - `duration_ms`

### 3.2 Cronometragem padrão

- Usar `Stopwatch` por etapa (check, pull, sqlite) e reportar no final (1 linha).
- Evitar logs por item (disciplina a disciplina), para não degradar performance.

---

## 4. Como medir (processo)

### 4.1 Medição manual (rápida)

- **Android Studio Profiler**:
  - CPU + Memory durante “Atualizar agora”
- **Logcat**:
  - filtrar por tag “okutangaPDF” e recolher durações

### 4.2 Medição por sessão (QA)

Checklist:
- [ ] Abrir app offline → navegar notas → medir tempo de troca de período
- [ ] Abrir app online → login → sync forçada → medir duração total
- [ ] Abrir app no dia seguinte → verificar que “check leve” não puxa payload se não houve mudança

---

## 5. Sinais de alerta (quando otimizar)

- “pull completo” ocorre todos os dias mesmo sem alterações → `sync-status` não está a refletir mudanças reais (ou versão não está a ser persistida).
- SQLite cresce sem limite → falta de limpeza ou duplicação de linhas.
- UI lenta a trocar período → índices/queries locais precisam ser otimizados (ou payload local demasiado grande).

