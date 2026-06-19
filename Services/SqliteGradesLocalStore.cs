using System.Text.Json;
using System.Threading;
using OlondongeApp.Data;
using OlondongeApp.Models.Dtos;
using SQLite;

namespace OlondongeApp.Services;

public sealed class SqliteGradesLocalStore : IGradesLocalStore
{
    private const string DatabaseFileName = "olondonge_grades.db3";

    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _dbMutex = new(1, 1);

    private static string DatabaseFullPath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    private async Task WithMutexAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await _dbMutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _dbMutex.Release();
        }
    }

    private async Task<T> WithMutexAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _dbMutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            _dbMutex.Release();
        }
    }

    private async Task<SQLiteAsyncConnection> GetDbAsync(CancellationToken cancellationToken = default)
    {
        if (_db != null)
        {
            return _db;
        }

        var path = DatabaseFullPath;
        _db = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        await _db.CreateTableAsync<LocalGradeLineEntity>().ConfigureAwait(false);
        await _db.CreateTableAsync<LocalPeriodEntity>().ConfigureAwait(false);
        await _db.CreateTableAsync<SyncMetaEntity>().ConfigureAwait(false);
        await _db.CreateTableAsync<LocalEnrolmentEntity>().ConfigureAwait(false);
        await _db.CreateTableAsync<LocalHistorySummaryEntity>().ConfigureAwait(false);
        await MigrateEnrolmentColumnsIfNeededAsync(_db).ConfigureAwait(false);
        return _db;
    }

    public Task EnsureReadyAsync(CancellationToken cancellationToken = default)
        => WithMutexAsync(async () => await GetDbAsync(cancellationToken), cancellationToken);

    private static async Task MigrateEnrolmentColumnsIfNeededAsync(SQLiteAsyncConnection db)
    {
        foreach (var sql in new[]
                 {
                     "ALTER TABLE local_enrolments ADD COLUMN AnoLectivoId INTEGER NOT NULL DEFAULT 0",
                     "ALTER TABLE local_enrolments ADD COLUMN IsCurrentAnoLectivo INTEGER NOT NULL DEFAULT 0",
                     "ALTER TABLE local_enrolments ADD COLUMN CursoId INTEGER NOT NULL DEFAULT 0",
                     "ALTER TABLE local_enrolments ADD COLUMN TurnoName TEXT",
                     "ALTER TABLE local_enrolments ADD COLUMN CursoAbreviatura TEXT",
                 })
        {
            try
            {
                await db.ExecuteAsync(sql).ConfigureAwait(false);
            }
            catch (SQLiteException)
            {
                // coluna já existe
            }
        }
    }

    public Task ReplaceEnrolmentsAsync(IReadOnlyList<EnrolmentDto> enrolments, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            await db.DeleteAllAsync<LocalEnrolmentEntity>().ConfigureAwait(false);
            foreach (var e in enrolments)
            {
                await db.InsertAsync(new LocalEnrolmentEntity
                {
                    MatriculaId = e.MatriculaId,
                    TurmaId = e.TurmaId,
                    CursoId = e.CursoId,
                    AnoLectivoId = e.AnoLectivoId,
                    IsCurrentAnoLectivo = e.IsCurrentAnoLectivo ? 1 : 0,
                    ClasseName = e.ClasseName,
                    CursoName = e.CursoName,
                    CursoAbreviatura = e.CursoAbreviatura,
                    AnoLectivoName = e.AnoLectivoName,
                    TurnoName = e.TurnoName,
                }).ConfigureAwait(false);
            }

            // Remover órfãos: linhas/períodos/resumos de matrículas que já não existem no servidor.
            // Sem isto, antigos MatriculaId podiam reaparecer em queries amplas (ex.: GetGradesForMatriculasAsync).
            if (enrolments.Count == 0)
            {
                await db.ExecuteAsync("DELETE FROM local_periods").ConfigureAwait(false);
                await db.ExecuteAsync("DELETE FROM grade_lines").ConfigureAwait(false);
                await db.ExecuteAsync("DELETE FROM local_history_summaries").ConfigureAwait(false);
            }
            else
            {
                var ids = enrolments.Select(x => x.MatriculaId).Distinct().ToList();
                var placeholders = string.Join(',', Enumerable.Repeat("?", ids.Count));
                var args = ids.Cast<object>().ToArray();
                await db.ExecuteAsync($"DELETE FROM local_periods WHERE MatriculaId NOT IN ({placeholders})", args).ConfigureAwait(false);
                await db.ExecuteAsync($"DELETE FROM grade_lines WHERE MatriculaId NOT IN ({placeholders})", args).ConfigureAwait(false);
                await db.ExecuteAsync($"DELETE FROM local_history_summaries WHERE MatriculaId NOT IN ({placeholders})", args).ConfigureAwait(false);
            }
        }, cancellationToken);

    public Task<IReadOnlyList<EnrolmentDto>> GetEnrolmentsAsync(CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            var rows = await db.Table<LocalEnrolmentEntity>().OrderBy(x => x.MatriculaId).ToListAsync().ConfigureAwait(false);
            return (IReadOnlyList<EnrolmentDto>)rows
                .Select(x => new EnrolmentDto
                {
                    MatriculaId = x.MatriculaId,
                    TurmaId = x.TurmaId,
                    CursoId = x.CursoId,
                    AnoLectivoId = x.AnoLectivoId,
                    IsCurrentAnoLectivo = x.IsCurrentAnoLectivo != 0,
                    ClasseName = x.ClasseName,
                    CursoName = x.CursoName,
                    CursoAbreviatura = x.CursoAbreviatura,
                    AnoLectivoName = x.AnoLectivoName,
                    TurnoName = x.TurnoName,
                })
                .ToList();
        }, cancellationToken);

    public Task SetMetaAsync(string key, string value, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            await db.InsertOrReplaceAsync(new SyncMetaEntity { Key = key, Value = value }).ConfigureAwait(false);
        }, cancellationToken);

    public Task<string?> GetMetaAsync(string key, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            var row = await db.Table<SyncMetaEntity>().Where(x => x.Key == key).FirstOrDefaultAsync().ConfigureAwait(false);
            return row?.Value;
        }, cancellationToken);

    public Task ReplacePeriodsAsync(int matriculaId, IReadOnlyList<(int PeriodoId, string? Name)> periods, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            await db.ExecuteAsync("DELETE FROM local_periods WHERE MatriculaId = ?", matriculaId).ConfigureAwait(false);
            foreach (var p in periods)
            {
                await db.InsertAsync(new LocalPeriodEntity
                {
                    MatriculaId = matriculaId,
                    PeriodoId = p.PeriodoId,
                    PeriodoName = p.Name,
                }).ConfigureAwait(false);
            }

            // Remover linhas órfãs de períodos que desapareceram para esta matrícula
            // (ex.: 3º trimestre removido no servidor). Sem isto, ficariam grade_lines fantasma.
            if (periods.Count == 0)
            {
                await db.ExecuteAsync("DELETE FROM grade_lines WHERE MatriculaId = ?", matriculaId).ConfigureAwait(false);
            }
            else
            {
                var pids = periods.Select(p => p.PeriodoId).Distinct().ToList();
                var placeholders = string.Join(',', Enumerable.Repeat("?", pids.Count));
                var args = new List<object> { matriculaId };
                args.AddRange(pids.Cast<object>());
                await db.ExecuteAsync(
                        $"DELETE FROM grade_lines WHERE MatriculaId = ? AND PeriodoId NOT IN ({placeholders})",
                        args.ToArray())
                    .ConfigureAwait(false);
            }
        }, cancellationToken);

    public Task<IReadOnlyList<(int PeriodoId, string? Name)>> GetPeriodsAsync(int matriculaId, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            var rows = await db.Table<LocalPeriodEntity>()
                .Where(x => x.MatriculaId == matriculaId)
                .OrderBy(x => x.PeriodoId)
                .ToListAsync()
                .ConfigureAwait(false);
            return (IReadOnlyList<(int PeriodoId, string? Name)>)rows.Select(x => (x.PeriodoId, x.PeriodoName)).ToList();
        }, cancellationToken);

    public Task ReplaceGradesAsync(int matriculaId, int periodoId, IReadOnlyList<GradeLineDto> grades, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            await db.ExecuteAsync("DELETE FROM grade_lines WHERE MatriculaId = ? AND PeriodoId = ?", matriculaId, periodoId).ConfigureAwait(false);
            var sort = 0;
            foreach (var g in grades)
            {
                var json = JsonSerializer.Serialize(g, AppJson.Options);
                await db.InsertAsync(new LocalGradeLineEntity
                {
                    MatriculaId = matriculaId,
                    PeriodoId = periodoId,
                    AvaliacaoId = g.AvaliacaoId,
                    SortOrder = sort++,
                    Json = json,
                }).ConfigureAwait(false);
            }
        }, cancellationToken);

    public Task<IReadOnlyList<GradeLineDto>> GetGradesAsync(int matriculaId, int periodoId, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            var rows = await db.Table<LocalGradeLineEntity>()
                .Where(x => x.MatriculaId == matriculaId && x.PeriodoId == periodoId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync()
                .ConfigureAwait(false);
            var list = new List<GradeLineDto>();
            foreach (var row in rows)
            {
                try
                {
                    var g = JsonSerializer.Deserialize<GradeLineDto>(row.Json, AppJson.Options);
                    if (g != null)
                    {
                        list.Add(g);
                    }
                }
                catch
                {
                    // ignora linha corrompida
                }
            }

            return (IReadOnlyList<GradeLineDto>)list;
        }, cancellationToken);

    public Task<IReadOnlyList<GradeLineDto>> GetGradesForMatriculasAsync(IReadOnlyList<int> matriculaIds, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            if (matriculaIds.Count == 0)
            {
                return Array.Empty<GradeLineDto>();
            }

            var ids = matriculaIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
            {
                return Array.Empty<GradeLineDto>();
            }

            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            var placeholders = string.Join(',', Enumerable.Repeat("?", ids.Count));
            var sql = $"SELECT * FROM grade_lines WHERE MatriculaId IN ({placeholders}) ORDER BY MatriculaId, PeriodoId, SortOrder";
            var rows = await db.QueryAsync<LocalGradeLineEntity>(sql, ids.Cast<object>().ToArray()).ConfigureAwait(false);
            var list = new List<GradeLineDto>();
            foreach (var row in rows)
            {
                try
                {
                    var g = JsonSerializer.Deserialize<GradeLineDto>(row.Json, AppJson.Options);
                    if (g != null)
                    {
                        list.Add(g);
                    }
                }
                catch
                {
                    // ignora linha corrompida
                }
            }

            return (IReadOnlyList<GradeLineDto>)list;
        }, cancellationToken);

    public Task UpsertHistorySummaryAsync(HistorySummaryDto summary, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            await db.InsertOrReplaceAsync(new LocalHistorySummaryEntity
            {
                MatriculaId = summary.MatriculaId,
                ResultadoFinal = summary.ResultadoFinal,
                NegativasFinais = summary.NegativasFinais,
                MediaFinal = summary.MediaFinal,
                RankingPosicao = summary.RankingPosicao,
                RankingTotal = summary.RankingTotal,
                UpdatedAtUtc = DateTime.UtcNow.ToString("o"),
            }).ConfigureAwait(false);
        }, cancellationToken);

    public Task<HistorySummaryDto?> GetHistorySummaryAsync(int matriculaId, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            var db = await GetDbAsync(cancellationToken).ConfigureAwait(false);
            var row = await db.Table<LocalHistorySummaryEntity>()
                .Where(x => x.MatriculaId == matriculaId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if (row == null)
            {
                return null;
            }

            return new HistorySummaryDto
            {
                MatriculaId = row.MatriculaId,
                ResultadoFinal = row.ResultadoFinal,
                NegativasFinais = row.NegativasFinais,
                MediaFinal = row.MediaFinal,
                RankingPosicao = row.RankingPosicao,
                RankingTotal = row.RankingTotal,
            };
        }, cancellationToken);

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            if (_db != null)
            {
                try
                {
                    await _db.CloseAsync().ConfigureAwait(false);
                }
                catch
                {
                    // continua mesmo com ficheiros bloqueados momentaneamente
                }

                _db = null;
            }

            TryDeleteDatabaseFiles(DatabaseFullPath);
        }, cancellationToken);

    /// <summary>Remove o .db3 da app e WAL/SHM; a próxima abertura recria um ficheiro vazio.</summary>
    private static void TryDeleteDatabaseFiles(string dbPath)
    {
        static void SafeDelete(string p)
        {
            try
            {
                if (File.Exists(p))
                {
                    File.Delete(p);
                }
            }
            catch
            {
                // ignorar falhas isoladas por plataforma / ficheiros ainda locked
            }
        }

        SafeDelete(dbPath);
        SafeDelete(dbPath + "-wal");
        SafeDelete(dbPath + "-shm");
        SafeDelete(dbPath + "-journal");
    }
}
