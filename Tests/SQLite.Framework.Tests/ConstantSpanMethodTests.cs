using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SpanMethodRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ConstantSpanMethodTests
{
    [Fact]
    public void SpanIndexOfOverConstantCollectionThrows()
    {
        using TestDatabase db = new();
        db.Table<SpanMethodRow>().Schema.CreateTable();
        db.Table<SpanMethodRow>().Add(new SpanMethodRow { Id = 1, Name = "a" });

        string[] captured = ["a", "b"];

        Assert.Throws<NotSupportedException>(() =>
            db.Table<SpanMethodRow>()
                .Where(r => r.Id > 0 && captured.IndexOf("a") >= 0)
                .ToList());
    }
}
