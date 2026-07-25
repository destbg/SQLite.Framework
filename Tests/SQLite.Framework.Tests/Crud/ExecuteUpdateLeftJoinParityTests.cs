using System;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class UpdateLeftJoinBook
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; } = "";
}

internal sealed class UpdateLeftJoinAuthor
{
    [Key]
    public int Id { get; set; }
}

public class ExecuteUpdateLeftJoinParityTests
{
    [Fact]
    public void ExecuteUpdateOverLeftJoin_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<UpdateLeftJoinBook>().Schema.CreateTable();
        db.Table<UpdateLeftJoinAuthor>().Schema.CreateTable();

        db.Table<UpdateLeftJoinAuthor>().Add(new UpdateLeftJoinAuthor { Id = 1 });
        db.Table<UpdateLeftJoinBook>().Add(new UpdateLeftJoinBook { Id = 1, AuthorId = 1, Title = "old" });
        db.Table<UpdateLeftJoinBook>().Add(new UpdateLeftJoinBook { Id = 2, AuthorId = 99, Title = "old" });

        Assert.Throws<NotSupportedException>(() =>
            (from b in db.Table<UpdateLeftJoinBook>()
             join a in db.Table<UpdateLeftJoinAuthor>() on b.AuthorId equals a.Id into g
             from a in g.DefaultIfEmpty()
             select new { b, a })
                .ExecuteUpdate(s => s.Set(x => x.b.Title, "new")));
    }
}
