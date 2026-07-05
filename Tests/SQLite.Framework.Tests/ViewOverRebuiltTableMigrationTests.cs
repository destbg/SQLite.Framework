using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ViewOverRebuiltTableMigrationTests
{
    [Fact]
    public void RebuildSucceedsWithViewOverTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 5 });
        db.Schema.CreateView<BookView>(() =>
            from b in db.Table<Book>()
            select new BookView { Id = b.Id, Title = b.Title, Price = b.Price });

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<Book>(s => s.Set(b => b.Price, 9.0), rebuild: true))
            .Migrate();

        Assert.Single(db.ReadOnlyTable<BookView>().ToList());
    }
}
