using Microsoft.Data.Sqlite;
using OkutangaPDF.Helpers;
using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public sealed class SqliteRecentDocumentsStore : IRecentDocumentsStore
{
    private const string DatabaseFileName = "okutanga_pdf.db3";

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private bool _initialized;

    private static string ConnectionString =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName)}";

    public Task EnsureReadyAsync(CancellationToken cancellationToken = default)
        => InitializeIfNeededAsync(cancellationToken);

    public Task<IReadOnlyList<PdfDocumentInfo>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            var results = new List<PdfDocumentInfo>();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id, FileName, LocalPath, OpenedAtUtc, PageCount, LastPage, FileSizeBytes, LastZoom
                FROM recent_documents
                ORDER BY OpenedAtUtc DESC
                LIMIT $limit
                """;
            cmd.Parameters.AddWithValue("$limit", PdfReaderMath.ClampRecentLimit(limit));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(ReadDocument(reader));
            }

            return (IReadOnlyList<PdfDocumentInfo>)results;
        }, cancellationToken);

    public Task<PdfDocumentInfo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            return await ReadByIdAsync(conn, id, cancellationToken);
        }, cancellationToken);

    public Task<PdfDocumentInfo> UpsertAsync(PdfDocumentInfo document, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);

            var existingId = document.Id > 0
                ? document.Id
                : await FindIdByPathAsync(conn, document.LocalPath, cancellationToken);

            if (existingId > 0)
            {
                await using var update = conn.CreateCommand();
                update.CommandText = """
                    UPDATE recent_documents
                    SET FileName = $name, LocalPath = $path, OpenedAtUtc = $opened,
                        PageCount = $pages, LastPage = $lastPage, FileSizeBytes = $size, LastZoom = $zoom
                    WHERE Id = $id
                    """;
                BindDocument(update, document, existingId);
                await update.ExecuteNonQueryAsync(cancellationToken);
                return (await ReadByIdAsync(conn, existingId, cancellationToken))!;
            }

            await using var insert = conn.CreateCommand();
            insert.CommandText = """
                INSERT INTO recent_documents (FileName, LocalPath, OpenedAtUtc, PageCount, LastPage, FileSizeBytes, LastZoom)
                VALUES ($name, $path, $opened, $pages, $lastPage, $size, $zoom);
                SELECT last_insert_rowid();
                """;
            BindDocument(insert, document, 0);
            var newId = Convert.ToInt32(await insert.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
            return (await ReadByIdAsync(conn, newId, cancellationToken))!;
        }, cancellationToken);

    public Task UpdateLastPageAsync(int id, int lastPage, int pageCount = 0, double lastZoom = 0, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = pageCount > 0 && lastZoom > 0
                ? "UPDATE recent_documents SET LastPage = $page, PageCount = $count, LastZoom = $zoom WHERE Id = $id"
                : pageCount > 0
                    ? "UPDATE recent_documents SET LastPage = $page, PageCount = $count WHERE Id = $id"
                    : lastZoom > 0
                        ? "UPDATE recent_documents SET LastPage = $page, LastZoom = $zoom WHERE Id = $id"
                        : "UPDATE recent_documents SET LastPage = $page WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$page", Math.Max(1, lastPage));
            if (pageCount > 0) cmd.Parameters.AddWithValue("$count", pageCount);
            if (lastZoom > 0) cmd.Parameters.AddWithValue("$zoom", PdfReaderMath.ClampStoredZoom(lastZoom));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);

    public Task UpdateLastZoomAsync(int id, double lastZoom, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE recent_documents SET LastZoom = $zoom WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$zoom", PdfReaderMath.ClampStoredZoom(lastZoom));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);

    public Task RemoveAsync(int id, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM recent_documents WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM recent_documents";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
        }, cancellationToken);

    private async Task InitializeIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var conn = CreateConnection();
            await conn.OpenAsync(cancellationToken);
            await EnsureSchemaAsync(conn, cancellationToken);
            _initialized = true;
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <summary>Abre ligação e garante schema — só invocar com <see cref="_mutex"/> já adquirido.</summary>
    private async Task<SqliteConnection> OpenInitializedConnectionAsync(CancellationToken cancellationToken)
    {
        var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);
        if (!_initialized)
        {
            await EnsureSchemaAsync(conn, cancellationToken);
            _initialized = true;
        }

        return conn;
    }

    private static async Task<PdfDocumentInfo?> ReadByIdAsync(SqliteConnection conn, int id, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, FileName, LocalPath, OpenedAtUtc, PageCount, LastPage, FileSizeBytes, LastZoom
            FROM recent_documents WHERE Id = $id
            """;
        cmd.Parameters.AddWithValue("$id", id);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDocument(reader) : null;
    }

    private static SqliteConnection CreateConnection() => new(ConnectionString);

    private static async Task EnsureSchemaAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS recent_documents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                LocalPath TEXT NOT NULL,
                OpenedAtUtc TEXT NOT NULL,
                PageCount INTEGER NOT NULL DEFAULT 0,
                LastPage INTEGER NOT NULL DEFAULT 1,
                FileSizeBytes INTEGER NOT NULL DEFAULT 0,
                LastZoom REAL NOT NULL DEFAULT 1.0
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        if (!await ColumnExistsAsync(conn, "recent_documents", "LastZoom", cancellationToken))
        {
            cmd.CommandText = "ALTER TABLE recent_documents ADD COLUMN LastZoom REAL NOT NULL DEFAULT 1.0";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection conn,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        var safeTable = table.Replace("'", "''", StringComparison.Ordinal);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT 1 FROM pragma_table_info('{safeTable}') WHERE name = $column LIMIT 1";
        cmd.Parameters.AddWithValue("$column", column);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    private static async Task<int> FindIdByPathAsync(SqliteConnection conn, string path, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id FROM recent_documents WHERE LocalPath = $path LIMIT 1";
        cmd.Parameters.AddWithValue("$path", path);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void BindDocument(SqliteCommand cmd, PdfDocumentInfo document, int id)
    {
        if (id > 0)
        {
            cmd.Parameters.AddWithValue("$id", id);
        }

        cmd.Parameters.AddWithValue("$name", document.FileName);
        cmd.Parameters.AddWithValue("$path", document.LocalPath);
        cmd.Parameters.AddWithValue("$opened", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$pages", document.PageCount);
        cmd.Parameters.AddWithValue("$lastPage", document.LastPage > 0 ? document.LastPage : 1);
        cmd.Parameters.AddWithValue("$size", document.FileSizeBytes);
        cmd.Parameters.AddWithValue("$zoom", document.LastZoom > 0 ? document.LastZoom : 1.0);
    }

    private static PdfDocumentInfo ReadDocument(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        FileName = reader.GetString(1),
        LocalPath = reader.GetString(2),
        OpenedAtUtc = DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
        PageCount = reader.GetInt32(4),
        LastPage = reader.GetInt32(5),
        FileSizeBytes = reader.GetInt64(6),
        LastZoom = reader.GetDouble(7),
    };

    private async Task WithMutexAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<T> WithMutexAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            _mutex.Release();
        }
    }
}
