using Microsoft.Data.Sqlite;
using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public sealed class SqlitePdfBookmarkStore : IPdfBookmarkStore
{
    private readonly IRecentDocumentsStore _recentStore;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private bool _initialized;

    private static string ConnectionString =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, "okutanga_pdf.db3")}";

    public SqlitePdfBookmarkStore(IRecentDocumentsStore recentStore)
    {
        _recentStore = recentStore;
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        await _recentStore.EnsureReadyAsync(cancellationToken);
        await InitializeIfNeededAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PdfBookmark>> GetForDocumentAsync(int documentId, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            var results = new List<PdfBookmark>();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id, DocumentId, PageNumber, Label, CreatedAtUtc
                FROM pdf_bookmarks WHERE DocumentId = $docId ORDER BY PageNumber
                """;
            cmd.Parameters.AddWithValue("$docId", documentId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(ReadBookmark(reader));
            }

            return (IReadOnlyList<PdfBookmark>)results;
        }, cancellationToken);

    public Task<PdfBookmark> AddAsync(int documentId, int pageNumber, string? label = null, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);

            await using var find = conn.CreateCommand();
            find.CommandText = "SELECT Id, DocumentId, PageNumber, Label, CreatedAtUtc FROM pdf_bookmarks WHERE DocumentId = $docId AND PageNumber = $page LIMIT 1";
            find.Parameters.AddWithValue("$docId", documentId);
            find.Parameters.AddWithValue("$page", pageNumber);
            await using (var reader = await find.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    return ReadBookmark(reader);
                }
            }

            await using var insert = conn.CreateCommand();
            insert.CommandText = """
                INSERT INTO pdf_bookmarks (DocumentId, PageNumber, Label, CreatedAtUtc)
                VALUES ($docId, $page, $label, $created);
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$docId", documentId);
            insert.Parameters.AddWithValue("$page", pageNumber);
            insert.Parameters.AddWithValue("$label", (object?)label ?? DBNull.Value);
            insert.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
            var id = Convert.ToInt32(await insert.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);

            return new PdfBookmark
            {
                Id = id,
                DocumentId = documentId,
                PageNumber = pageNumber,
                Label = label,
                CreatedAtUtc = DateTime.UtcNow,
            };
        }, cancellationToken);

    public Task RemoveAsync(int bookmarkId, CancellationToken cancellationToken = default)
        => WithMutexAsync(async () =>
        {
            await using var conn = await OpenInitializedConnectionAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM pdf_bookmarks WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", bookmarkId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
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

    private static async Task EnsureSchemaAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS pdf_bookmarks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentId INTEGER NOT NULL,
                PageNumber INTEGER NOT NULL,
                Label TEXT,
                CreatedAtUtc TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqliteConnection CreateConnection() => new(ConnectionString);

    private static PdfBookmark ReadBookmark(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        DocumentId = reader.GetInt32(1),
        PageNumber = reader.GetInt32(2),
        Label = reader.IsDBNull(3) ? null : reader.GetString(3),
        CreatedAtUtc = DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
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
