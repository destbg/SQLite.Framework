using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25pWriters")]
public class H25pWriter
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H25pTitles")]
public class H25pTitle
{
    [Key]
    public int Id { get; set; }

    public int WriterId { get; set; }

    public string Caption { get; set; } = "";
}


public class EntitySubqueryInProjectionTests
{
    [Fact]
    public void ProjectingAWholeEntityFromACorrelatedSubqueryReportsAClearMessage()
    {
        using TestDatabase db = Setup(nameof(ProjectingAWholeEntityFromACorrelatedSubqueryReportsAClearMessage));

        Assert.Throws<NotSupportedException>(() => db.Table<H25pWriter>()
            .OrderBy(w => w.Id)
            .Select(w => db.Table<H25pTitle>().Where(t => t.WriterId == w.Id).FirstOrDefault())
            .ToList());
    }

    private static List<H25pWriter> Writers()
    {
        return
        [
            new H25pWriter { Id = 1, Name = "ann" },
            new H25pWriter { Id = 2, Name = "bob" }
        ];
    }

    private static List<H25pTitle> Titles()
    {
        return
        [
            new H25pTitle { Id = 1, WriterId = 1, Caption = "first" },
            new H25pTitle { Id = 2, WriterId = 3, Caption = "orphan" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25pWriter>().Schema.CreateTable();
        db.Table<H25pTitle>().Schema.CreateTable();
        db.Table<H25pWriter>().AddRange(Writers());
        db.Table<H25pTitle>().AddRange(Titles());
        return db;
    }
}
