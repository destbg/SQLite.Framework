using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23qDocumentRows")]
public class H23qDocumentRow
{
    [Key]
    public int Id { get; set; }

    public string? Data { get; set; }
}

public class JsonValidNullDocumentTests
{
    [Fact]
    public void JsonValidAndItsNegationTogetherCoverEveryRow()
    {
        using TestDatabase db = Setup(nameof(JsonValidAndItsNegationTogetherCoverEveryRow));

        List<int> matching = db.Table<H23qDocumentRow>()
            .Where(r => SQLiteJsonFunctions.Valid(r.Data!))
            .Select(r => r.Id)
            .ToList();

        List<int> notMatching = db.Table<H23qDocumentRow>()
            .Where(r => !SQLiteJsonFunctions.Valid(r.Data!))
            .Select(r => r.Id)
            .ToList();

        List<int> expected = Rows().Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = matching.Concat(notMatching).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GlobAndItsNegationTogetherCoverEveryRow()
    {
        using TestDatabase db = Setup(nameof(GlobAndItsNegationTogetherCoverEveryRow));

        List<int> matching = db.Table<H23qDocumentRow>()
            .Where(r => SQLiteFunctions.Glob("{*", r.Data!))
            .Select(r => r.Id)
            .ToList();

        List<int> notMatching = db.Table<H23qDocumentRow>()
            .Where(r => !SQLiteFunctions.Glob("{*", r.Data!))
            .Select(r => r.Id)
            .ToList();

        List<int> expected = Rows().Select(r => r.Id).OrderBy(id => id).ToList();
        List<int> actual = matching.Concat(notMatching).OrderBy(id => id).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23qDocumentRow> Rows()
    {
        return
        [
            new H23qDocumentRow { Id = 1, Data = "{\"a\":1}" },
            new H23qDocumentRow { Id = 2, Data = "not json" },
            new H23qDocumentRow { Id = 3, Data = null }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23qDocumentRow>().Schema.CreateTable();
        db.Table<H23qDocumentRow>().AddRange(Rows());
        return db;
    }
}
