using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24qFojBooks")]
public class H24qFojBook
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; } = "";
}

[Table("H24qFojAuthors")]
public class H24qFojAuthor
{
    [Key]
    public int Id { get; set; }
}

public class ExecuteUpdateOuterJoinGuardTests
{
    [Fact]
    public void ExecuteUpdateOverAFullOuterJoinIsRejected()
    {
        using TestDatabase db = new();
        db.Table<H24qFojBook>().Schema.CreateTable();
        db.Table<H24qFojAuthor>().Schema.CreateTable();

        db.Table<H24qFojAuthor>().Add(new H24qFojAuthor { Id = 1 });
        db.Table<H24qFojBook>().Add(new H24qFojBook { Id = 1, AuthorId = 1, Title = "old" });
        db.Table<H24qFojBook>().Add(new H24qFojBook { Id = 2, AuthorId = 99, Title = "old" });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<H24qFojBook>()
                .FullOuterJoin(
                    db.Table<H24qFojAuthor>(),
                    b => b.AuthorId,
                    a => a.Id,
                    (b, a) => new { b, a })
                .ExecuteUpdate(s => s.Set(x => x.b!.Title, "new")));
    }
}
