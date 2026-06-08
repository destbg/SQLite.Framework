using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SchemaMigrationTests
{
    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void MigrateSetWithDateTimeLiteralDoesNotCrash(MigrateMode mode)
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Author>().Add(new Author
        {
            Id = 1,
            Name = "a",
            Email = "e",
            BirthDate = DateTime.UnixEpoch
        });

        Exception? ex = Record.Exception(() =>
            db.Schema.Table<Author>().Migrate(mode, m => m.Set(a => a.BirthDate, new DateTime(2000, 1, 1))));

        Assert.Null(ex);
    }

    [Fact]
    public void InsertFromQueryMapsByMemberNotPosition()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<BookArchive>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 7, Price = 11 });

        db.Table<BookArchive>().InsertFromQuery(
            db.Table<Book>().Select(b => new BookArchive
            {
                Id = b.Id,
                Title = b.Title,
                Price = b.Price,
                AuthorId = b.AuthorId,
            }));

        BookArchive archived = db.Table<BookArchive>().First();

        Assert.Equal(7, archived.AuthorId);
        Assert.Equal(11, archived.Price);
    }
}
