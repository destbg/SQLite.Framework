using System.Globalization;
using SQLite.Framework;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ViewClientEvalProjectionTests
{
    [Fact]
    public void CreateViewWithClientEvalProjectionThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.Schema.CreateView<BookView>(() =>
                from b in db.Table<Book>()
                select new BookView { Id = b.Id, Title = b.Title.ToUpper(CultureInfo.InvariantCulture), Price = b.Price }));
    }
}
