using System.Linq;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ModelValidationComputedColumnParityTests
{
    [Fact]
    public void MissingComputedColumn_IsReported()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<ProductLine>().Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Execute("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL, \"Quantity\" INTEGER NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<ProductLine>();

        Assert.False(result.IsValid, string.Join("; ", result.Issues));
        Assert.Contains(result.Issues, i => i.Contains("Total"));
    }

    [Fact]
    public void MissingRegularColumn_IsReported_Control()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<ProductLine>().Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Execute("CREATE TABLE \"ProductLines\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" REAL NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<ProductLine>();

        Assert.Contains(result.Issues, i => i.Contains("Quantity"));
    }
}
