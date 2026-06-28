using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonElementAtColumnIndexParityTests
{
    public class JeRow
    {
        [Key]
        public int Id { get; set; }
        public int Pick { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private static readonly JeRow[] Seed =
    [
        new JeRow { Id = 1, Pick = -1, Tags = ["a", "b"] },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<string>)] = new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString));
        db.Table<JeRow>().Schema.CreateTable();
        foreach (JeRow r in Seed)
        {
            db.Table<JeRow>().Add(r);
        }
        return db;
    }

    [Fact]
    public void ElementAtOrDefaultWithNegativeColumnIndex_ErrorsInsteadOfReturningDefault()
    {
        using TestDatabase db = Create();

        string? oracle = Seed.Select(r => r.Tags.ElementAtOrDefault(r.Pick)).First();
        Assert.Null(oracle);

        Assert.Throws<SQLiteException>(() => db.Table<JeRow>().Select(r => r.Tags.ElementAtOrDefault(r.Pick)).First());
    }
}
