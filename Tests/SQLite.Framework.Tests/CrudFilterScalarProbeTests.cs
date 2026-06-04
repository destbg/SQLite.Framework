using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NoKeyLogRows")]
file sealed class NoKeyLogRow
{
    public required string Message { get; set; }

    public int Level { get; set; }
}

public class CrudFilterScalarProbeTests
{
    [Fact]
    public void UpdateWithoutPrimaryKeyThrowsClearError()
    {
        using TestDatabase db = new();
        db.Table<NoKeyLogRow>().Schema.CreateTable();
        db.Table<NoKeyLogRow>().Add(new NoKeyLogRow { Message = "hi", Level = 1 });

        NoKeyLogRow row = db.Table<NoKeyLogRow>().First();
        row.Level = 2;

        Exception? ex = Record.Exception(() => db.Table<NoKeyLogRow>().Update(row));

        Assert.NotNull(ex);
        Assert.Contains("primary key", ex!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IgnoreQueryFiltersIsNoOpWhenNoFiltersRegistered()
    {
        using TestDatabase db = new();
        db.Table<SoftDeletableBook>().Schema.CreateTable();
        db.Table<SoftDeletableBook>().AddRange(new[]
        {
            new SoftDeletableBook { Id = 1, Title = "a", IsDeleted = false },
            new SoftDeletableBook { Id = 2, Title = "b", IsDeleted = true },
        });

        List<SoftDeletableBook> rows = db.Table<SoftDeletableBook>().IgnoreQueryFilters().ToList();

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void SecureDeleteGetterReportsFastMode()
    {
        using TestDatabase db = new();
        db.ExecuteScalar<int>("PRAGMA secure_delete = FAST");

        Assert.Equal(2, db.ExecuteScalar<int>("PRAGMA secure_delete"));
        Assert.True(db.Pragmas.SecureDelete);
    }

    [Fact]
    public void ExecuteScalarValueTypeReturnsDefaultForNullAggregate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        double max = db.ExecuteScalar<double>(
            "SELECT MAX(\"BookPrice\") FROM \"Books\" WHERE \"BookAuthorId\" = @a",
            new { a = 999 });

        Assert.Equal(0.0, max);
    }

    [Fact]
    public void TimeOnlyAddHoursWrapsAroundMidnight()
    {
        using TestDatabase db = new();
        db.Table<TimeOnlyMethodEntity>().Schema.CreateTable();
        db.Table<TimeOnlyMethodEntity>().Add(new TimeOnlyMethodEntity { Id = 1, Time = new TimeOnly(3, 4, 5) });

        TimeOnly r = db.Table<TimeOnlyMethodEntity>()
            .Where(a => a.Id == 1)
            .Select(a => a.Time.AddHours(23))
            .First();

        Assert.Equal(new TimeOnly(2, 4, 5), r);
    }
}
