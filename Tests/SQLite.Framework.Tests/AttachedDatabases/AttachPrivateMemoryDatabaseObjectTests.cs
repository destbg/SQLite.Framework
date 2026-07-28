using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23rAttachedMemoryRows")]
public class H23rAttachedMemoryRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

public class AttachPrivateMemoryDatabaseObjectTests
{
    private const string PrivateMemoryName = "H23rPrivateMemoryAttach.db3";

    [Fact]
    public void APrivateMemoryDatabaseObjectIsRefusedTheSameWayAsAMemoryPath()
    {
        using TestDatabase main = new(useFile: true);
        using TestDatabase aux = new(UsePrivateMemory);
        aux.Table<H23rAttachedMemoryRow>().Schema.CreateTable();
        aux.Table<H23rAttachedMemoryRow>().Add(new H23rAttachedMemoryRow { Id = 1, Title = "aux" });
        Assert.False(File.Exists(PrivateMemoryName));

        Assert.Throws<NotSupportedException>(() => main.AttachDatabase(aux, "auxmem"));
    }

    private static void UsePrivateMemory(SQLiteOptionsBuilder builder)
    {
        builder.DatabasePath = PrivateMemoryName;
        builder.UseOpenFlags(SQLiteOpenFlags.Memory | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
    }
}
