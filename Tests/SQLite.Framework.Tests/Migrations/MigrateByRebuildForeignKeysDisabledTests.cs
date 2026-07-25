using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FkOffRebuildTable")]
internal sealed class FkOffRebuildV1
{
    [Key] public int Id { get; set; }
    public string Keep { get; set; } = "";
    [Column("Old")] public string? Moved { get; set; }
}

[Table("FkOffRebuildTable")]
internal sealed class FkOffRebuildV2
{
    [Key] public int Id { get; set; }
    public string Keep { get; set; } = "";
    [Column("New")] public string? Moved { get; set; }
}

public class MigrateByRebuildForeignKeysDisabledTests
{
    [Fact]
    public void MigrateByRebuild_WithForeignKeysDisabled_PreservesUnchangedColumns()
    {
        using TestDatabase db = new(b => b.UseForeignKeys(false));
        db.Schema.CreateTable<FkOffRebuildV1>();
        db.Table<FkOffRebuildV1>().Add(new FkOffRebuildV1 { Id = 1, Keep = "alive", Moved = "x" });

        db.Schema.MigrateByRebuild<FkOffRebuildV2>();

        FkOffRebuildV2 row = db.Table<FkOffRebuildV2>().Single();
        Assert.Equal(1, row.Id);
        Assert.Equal("alive", row.Keep);
    }
}
