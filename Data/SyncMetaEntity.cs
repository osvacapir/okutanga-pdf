using SQLite;

namespace OlondongeApp.Data;

[Table("sync_meta")]
public sealed class SyncMetaEntity
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
