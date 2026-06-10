using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public record PositionalKeyBase([property: Key] int Id);

public record InheritedPositionalRow(int Id, string Name) : PositionalKeyBase(Id);

public class InheritedPositionalRecordEntityTests
{
    [Fact]
    public void Entity_PositionalRecordWithInheritedKeyProperty_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<InheritedPositionalRow>().Schema.CreateTable();

        db.Table<InheritedPositionalRow>().Add(new InheritedPositionalRow(1, "alpha"));
        db.Table<InheritedPositionalRow>().Add(new InheritedPositionalRow(2, "beta"));

        List<InheritedPositionalRow> rows = db.Table<InheritedPositionalRow>().OrderBy(r => r.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(new InheritedPositionalRow(1, "alpha"), rows[0]);
        Assert.Equal(new InheritedPositionalRow(2, "beta"), rows[1]);
    }
}
