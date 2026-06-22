using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SetExpressionConstantColumnParityTests
{
    [Fact]
    public void ExecuteUpdateSetExpressionConstant_PlainColumn_UpdatesValue()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "old", AuthorId = 1, Price = 1.0 });

        db.Table<Book>().Where(b => b.Id == 1).ExecuteUpdate(s => s.Set(b => b.Title, b => "renamed"));

        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title).First();

        Assert.Equal("renamed", actual);
    }
}
