using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NamedIdEntry")]
public class NamedIdEntryRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class DelegateInvocationClientEvalTests
{
    [Fact]
    public void ADelegateThatThrowsSurfacesTheOriginalException()
    {
        using TestDatabase db = new();
        db.Table<NamedIdEntryRow>().Schema.CreateTable();
        db.Table<NamedIdEntryRow>().Add(new NamedIdEntryRow { Id = 1, Name = "a" });
        Func<int, string> f = _ => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(() => db.Table<NamedIdEntryRow>().Select(x => f(x.Id)).ToList());
    }

    [Fact]
    public void ADelegateOverTheWholeRowRunsInMemory()
    {
        using TestDatabase db = new();
        db.Table<NamedIdEntryRow>().Schema.CreateTable();
        List<NamedIdEntryRow> rows =
        [
            new NamedIdEntryRow { Id = 1, Name = "a" },
            new NamedIdEntryRow { Id = 2, Name = "b" },
        ];
        db.Table<NamedIdEntryRow>().AddRange(rows);
        Func<NamedIdEntryRow, string> g = r => r.Name + r.Id;

        List<string> expected = rows.Select(x => g(x)).OrderBy(x => x).ToList();
        List<string> actual = db.Table<NamedIdEntryRow>().Select(x => g(x)).ToList().OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }
}
