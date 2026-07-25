using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("AttachAuxBook")]
public class AttachAuxBookRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";
}

public class AttachInMemoryDatabaseObjectTests
{
    [Fact]
    public void AttachInMemoryDatabaseObjectThrows()
    {
        using TestDatabase main = new();
        using TestDatabase aux = new();
        aux.Table<AttachAuxBookRow>().Schema.CreateTable();
        aux.Table<AttachAuxBookRow>().Add(new AttachAuxBookRow { Id = 1, Title = "aux" });

        Assert.Throws<NotSupportedException>(() => main.AttachDatabase(aux, "aux1"));
    }

    [Fact]
    public void AttachEmptyPathDatabaseObjectThrows()
    {
        using TestDatabase main = new();
        using SQLiteDatabase aux = new(new SQLiteOptionsBuilder("").Build());

        Assert.Throws<NotSupportedException>(() => main.AttachDatabase(aux, "aux2"));
    }

    [Fact]
    public void AttachFileDatabaseObjectExposesItsTables()
    {
        using TestDatabase main = new();
        using TestDatabase aux = new(useFile: true);
        aux.Table<AttachAuxBookRow>().Schema.CreateTable();
        aux.Table<AttachAuxBookRow>().Add(new AttachAuxBookRow { Id = 1, Title = "aux" });

        main.AttachDatabase(aux, "aux1");

        List<string> titles = main.Query<string>("SELECT \"Title\" FROM aux1.\"AttachAuxBook\"");
        Assert.Equal(["aux"], titles);
    }
}
