using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("DisposedReaderItem")]
public class DisposedReaderItem
{
    [Key]
    public int Id { get; set; }
}

public class DisposedReaderPooledStatementTests
{
    [Fact]
    public void ReadAfterDisposeDoesNotDisturbNextReader()
    {
        using TestDatabase db = new();
        db.Table<DisposedReaderItem>().Schema.CreateTable();
        db.Table<DisposedReaderItem>().AddRange(
        [
            new DisposedReaderItem { Id = 1 },
            new DisposedReaderItem { Id = 2 },
            new DisposedReaderItem { Id = 3 },
        ]);

        SQLiteCommand first = db.CreateCommand("SELECT Id FROM DisposedReaderItem ORDER BY Id", []);
        SQLiteDataReader disposedReader = first.ExecuteReader();
        Assert.True(disposedReader.Read());
        disposedReader.Dispose();

        SQLiteCommand second = db.CreateCommand("SELECT Id FROM DisposedReaderItem ORDER BY Id", []);
        using SQLiteDataReader activeReader = second.ExecuteReader();
        Assert.True(activeReader.Read());
        Assert.Equal(1, activeReader.GetInt32(0));

        Record.Exception(() => disposedReader.Read());

        Assert.True(activeReader.Read());
        Assert.Equal(2, activeReader.GetInt32(0));
    }
}
