using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20AceRow")]
public class H20AceRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H20AceDto
{
    public int V { get; set; }
}

public class TopLevelArrayCapturedElementTests
{
    private static List<H20AceRow> Rows() =>
    [
        new H20AceRow { Id = 1, A = 10 },
        new H20AceRow { Id = 2, A = 20 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20AceRow>().Schema.CreateTable();
        db.Table<H20AceRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CapturedNullComplexElementMatchesLinq()
    {
        using TestDatabase db = Setup();
        H20AceDto? captured = null;

        List<object?> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new object?[] { captured })
            .Select(a => a[0]).ToList();

        List<object?> actual = db.Table<H20AceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new object?[] { captured })
            .ToList()
            .Select(a => a[0]).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedComplexElementThrowsUnsupportedType()
    {
        using TestDatabase db = Setup();
        H20AceDto captured = new() { V = 7 };

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => db.Table<H20AceRow>()
            .Select(r => new object[] { captured })
            .ToList());

        Assert.Equal("Type SQLite.Framework.Tests.H20AceDto is not supported.", exception.Message);
    }
}
