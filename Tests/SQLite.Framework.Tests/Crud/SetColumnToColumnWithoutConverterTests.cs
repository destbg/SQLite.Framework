using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SetColumnToColumnRows")]
public class SetColumnToColumnRow
{
    [Key]
    public int Id { get; set; }

    public int Source { get; set; }

    public int Target { get; set; }

    public string Text { get; set; } = "";

    public string Backup { get; set; } = "";

    [NotMapped]
    public int Unmapped { get; set; }
}

public class SetColumnToColumnWithoutConverterTests
{
    [Fact]
    public void SetNumberColumnFromAnotherNumberColumnCopiesTheValue()
    {
        using TestDatabase db = Setup(nameof(SetNumberColumnFromAnotherNumberColumnCopiesTheValue));

        db.Table<SetColumnToColumnRow>().ExecuteUpdate(s => s.Set(r => r.Target, r => r.Source));

        List<(int Source, int Target)> actual = db.Table<SetColumnToColumnRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Source, r.Target })
            .ToList()
            .Select(r => (r.Source, r.Target))
            .ToList();

        Assert.Equal([(10, 10), (20, 20)], actual);
    }

    [Fact]
    public void SetTextColumnFromAnotherTextColumnCopiesTheValue()
    {
        using TestDatabase db = Setup(nameof(SetTextColumnFromAnotherTextColumnCopiesTheValue));

        db.Table<SetColumnToColumnRow>().ExecuteUpdate(s => s.Set(r => r.Backup, r => r.Text));

        List<(string Text, string Backup)> actual = db.Table<SetColumnToColumnRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Text, r.Backup })
            .ToList()
            .Select(r => (r.Text, r.Backup))
            .ToList();

        Assert.Equal([("a", "a"), ("b", "b")], actual);
    }

    [Fact]
    public void SetFromAnUnmappedPropertyIsNotTreatedAsAColumnCopy()
    {
        using TestDatabase db = Setup(nameof(SetFromAnUnmappedPropertyIsNotTreatedAsAColumnCopy));

        Assert.ThrowsAny<Exception>(() => db.Table<SetColumnToColumnRow>()
            .ExecuteUpdate(s => s.Set(r => r.Target, r => r.Unmapped)));
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<SetColumnToColumnRow>().Schema.CreateTable();
        db.Table<SetColumnToColumnRow>().AddRange(
        [
            new SetColumnToColumnRow { Id = 1, Source = 10, Text = "a" },
            new SetColumnToColumnRow { Id = 2, Source = 20, Text = "b" }
        ]);
        return db;
    }
}
