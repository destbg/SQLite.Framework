using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class NullableIntListRow
{
    [Key]
    public int Id { get; set; }

    public List<int?> Values { get; set; } = [];
}

public class JsonTrueForAllNullElementParityTests
{
    private static TestDatabase Seed(List<int?> values)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int?>)] =
                new SQLiteJsonConverter<List<int?>>(TestJsonContext.Default.ListNullableInt32));
        db.Table<NullableIntListRow>().Schema.CreateTable();
        db.Table<NullableIntListRow>().Add(new NullableIntListRow { Id = 1, Values = values });
        return db;
    }

    [Fact]
    public void TrueForAll_NullElement_RelationalPredicate_MatchesDotNet()
    {
        List<int?> values = [null, 7];
        using TestDatabase db = Seed(values);

        bool expected = values.TrueForAll(x => x > 5);
        bool actual = db.Table<NullableIntListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Values.TrueForAll(x => x > 5))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrueForAll_NullElement_AllNonNull_MatchesDotNet()
    {
        List<int?> values = [6, 7, 8];
        using TestDatabase db = Seed(values);

        bool expected = values.TrueForAll(x => x > 5);
        bool actual = db.Table<NullableIntListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Values.TrueForAll(x => x > 5))
            .First();

        Assert.Equal(expected, actual);
    }
}
