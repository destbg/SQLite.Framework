using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonExceptSourceDuplicatesTests
{
    public sealed class ExceptRow
    {
        [Key]
        public int Id { get; set; }

        public List<string> Tags { get; set; } = [];
    }

    [Fact]
    public void ExceptWithDuplicateSourceElementsDeduplicatesLikeDotNet()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<ExceptRow>().Schema.CreateTable();
        db.Table<ExceptRow>().Add(new ExceptRow { Id = 1, Tags = ["a", "b", "a", "c"] });

        List<string> other = ["b"];

        List<string> oracle = new List<string> { "a", "b", "a", "c" }.Except(other).ToList();
        Assert.Equal(["a", "c"], oracle);

        List<string> actual = db.Table<ExceptRow>()
            .Select(r => r.Tags.Except(other).ToList())
            .First();

        Assert.Equal(oracle, actual);
    }
}