using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ModelValidationDriftParityTests
{
    [Fact]
    public void IndexWithWrongColumnsAndUniqueness_IsReported()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<Book>().Index(b => b.Title, name: "IX_Book_Title", unique: true));
        db.Schema.CreateTable<Book>();
        db.Schema.DropIndex("IX_Book_Title");
        db.Execute("CREATE INDEX \"IX_Book_Title\" ON \"Books\" (\"BookAuthorId\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<Book>();

        Assert.False(result.IsValid, string.Join("; ", result.Issues));
    }
}
