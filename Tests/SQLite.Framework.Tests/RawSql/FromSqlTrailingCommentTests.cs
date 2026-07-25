using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CommentSqlRows")]
public class CommentSqlRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class FromSqlTrailingCommentTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<CommentSqlRow>().Schema.CreateTable();
        db.Table<CommentSqlRow>().Add(new CommentSqlRow { Id = 1, Name = ";" });
        db.Table<CommentSqlRow>().Add(new CommentSqlRow { Id = 2, Name = "b" });
        return db;
    }

    [Fact]
    public void TrailingSemicolonAndLineCommentStillComposes()
    {
        using TestDatabase db = Seed();

        List<int> ids = db.FromSql<CommentSqlRow>("SELECT * FROM CommentSqlRows; -- note")
            .Select(r => r.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([1, 2], ids);
    }

    [Fact]
    public void TrailingSemicolonAndBlockCommentStillComposes()
    {
        using TestDatabase db = Seed();

        List<int> ids = db.FromSql<CommentSqlRow>("SELECT * FROM CommentSqlRows; /* note */")
            .Where(r => r.Id > 1)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([2], ids);
    }

    [Fact]
    public void TrailingSemicolonWithoutCompositionStillRuns()
    {
        using TestDatabase db = Seed();

        List<CommentSqlRow> rows = db.FromSql<CommentSqlRow>("SELECT * FROM CommentSqlRows;").ToList();

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void CteFragmentWithTrailingSemicolonStillComposes()
    {
        using TestDatabase db = Seed();

        List<string> names = db.FromSql<CommentSqlRow>(
                "WITH big AS (SELECT * FROM CommentSqlRows WHERE Id > 1) SELECT * FROM big;")
            .Select(r => r.Name)
            .ToList();

        Assert.Equal(["b"], names);
    }

    [Fact]
    public void SemicolonInsideStringLiteralIsPreserved()
    {
        using TestDatabase db = Seed();

        List<int> ids = db.FromSql<CommentSqlRow>("SELECT * FROM CommentSqlRows WHERE Name = ';'")
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void CommentMarkerInsideStringLiteralIsPreserved()
    {
        using TestDatabase db = Seed();

        int count = db.FromSql<CommentSqlRow>("SELECT * FROM CommentSqlRows WHERE Name <> '--';")
            .Count();

        Assert.Equal(2, count);
    }
}
