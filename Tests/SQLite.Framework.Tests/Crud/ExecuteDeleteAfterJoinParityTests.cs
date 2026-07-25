using System;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class DeleteJoinBook
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }
}

internal sealed class DeleteJoinAuthor
{
    [Key]
    public int Id { get; set; }
}

public class ExecuteDeleteAfterJoinParityTests
{
    [Fact]
    public void ExecuteDeleteAfterJoin_ThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<DeleteJoinBook>().Schema.CreateTable();
        db.Table<DeleteJoinAuthor>().Schema.CreateTable();

        db.Table<DeleteJoinAuthor>().Add(new DeleteJoinAuthor { Id = 1 });
        db.Table<DeleteJoinBook>().Add(new DeleteJoinBook { Id = 1, AuthorId = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<DeleteJoinBook>()
                .Join(db.Table<DeleteJoinAuthor>(), b => b.AuthorId, a => a.Id, (b, a) => b)
                .ExecuteDelete());
    }
}
