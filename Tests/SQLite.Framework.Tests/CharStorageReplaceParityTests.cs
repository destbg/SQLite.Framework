using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CharStorageReplaceParityTests
{
    [Fact]
    public void ReplaceCharWithChar_IntegerCharStorage_MatchesDotNet()
    {
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello", AuthorId = 1, Price = 1 });

        string expected = "hello".Replace('l', 'L');
        string actual = db.Table<Book>().Where(b => b.Id == 1).Select(b => b.Title.Replace('l', 'L')).First();

        Assert.Equal(expected, actual);
    }
}
