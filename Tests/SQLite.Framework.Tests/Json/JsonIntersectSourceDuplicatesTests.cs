using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonIntersectSourceDuplicatesTests
{
    public sealed class IntersectRow
    {
        [Key]
        public int Id { get; set; }

        public List<string> Tags { get; set; } = [];
    }

    [Fact]
    public void IntersectWithDuplicateSourceElementsDeduplicatesLikeDotNet()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<IntersectRow>().Schema.CreateTable();
        db.Table<IntersectRow>().Add(new IntersectRow { Id = 1, Tags = ["a", "b", "a", "c"] });

        List<string> other = ["a", "b"];

        List<string> oracle = new List<string> { "a", "b", "a", "c" }.Intersect(other).ToList();
        Assert.Equal(["a", "b"], oracle);

        List<string> actual = db.Table<IntersectRow>()
            .Select(r => r.Tags.Intersect(other).ToList())
            .First();

        Assert.Equal(oracle, actual);
    }
}