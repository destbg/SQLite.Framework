using System.ComponentModel.DataAnnotations;
using SQLite.Framework.JsonB;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonFunctionsTests
{
    private static TestDatabase CreateDb(string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.AddJson();
        db.Table<JsonRow>().CreateTable();
        return db;
    }

    private static void Seed(TestDatabase db, int id, string json)
    {
        db.Table<JsonRow>().Add(new JsonRow { Id = id, Data = json });
    }

    [Fact]
    public void Extract_ReturnsValueAtPath()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice","age":30}""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Extract<string>(r.Data, "$.name"))
            .First();

        Assert.Equal("Alice", result);
    }

    [Fact]
    public void Set_UpdatesValueAtPath()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice"}""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Set(r.Data, "$.name", "Bob"))
            .First();

        Assert.Contains("Bob", result);
    }

    [Fact]
    public void Insert_AddsNewKey()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice"}""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Insert(r.Data, "$.age", 25))
            .First();

        Assert.Contains("25", result);
    }

    [Fact]
    public void Replace_UpdatesExistingKey()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice"}""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Replace(r.Data, "$.name", "Carol"))
            .First();

        Assert.Contains("Carol", result);
    }

    [Fact]
    public void Remove_DeletesKey()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice","age":30}""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Remove(r.Data, "$.age"))
            .First();

        Assert.DoesNotContain("age", result);
    }

    [Fact]
    public void Type_ReturnsTypeAtPath()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice","age":30}""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Type(r.Data, "$.name"))
            .First();

        Assert.Equal("text", result);
    }

    [Fact]
    public void Valid_ReturnsTrueForValidJson()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice"}""");

        bool result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Valid(r.Data))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Patch_MergesJsonObjects()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice","age":30}""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Patch(r.Data, """{"age":31}"""))
            .First();

        Assert.Contains("31", result);
        Assert.Contains("Alice", result);
    }

    [Fact]
    public void ArrayLength_NoPath_ReturnsTopLevelCount()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """["a","b","c"]""");

        int result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.ArrayLength(r.Data))
            .First();

        Assert.Equal(3, result);
    }

    [Fact]
    public void ArrayLength_WithPath_ReturnsNestedCount()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"items":["x","y"]}""");

        int result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.ArrayLength(r.Data, "$.items"))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void Minify_RemovesWhitespace()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{ "name" : "Alice" }""");

        string result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.Minify(r.Data))
            .First();

        Assert.Equal("""{"name":"Alice"}""", result);
    }

#if !SQLITECIPHER
    [Fact]
    public void ToJsonb_ConvertsToBlob()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice"}""");

        byte[] result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.ToJsonb(r.Data))
            .First();

        Assert.NotEmpty(result);
    }

    [Fact]
    public void ExtractJsonb_ReturnsValueFromBinaryJson()
    {
        using TestDatabase db = CreateDb();
        Seed(db, 1, """{"name":"Alice","score":42}""");

        int result = db.Table<JsonRow>()
            .Select(r => SQLiteJsonFunctions.ExtractJsonb<int>(SQLiteJsonFunctions.ToJsonb(r.Data), "$.score"))
            .First();

        Assert.Equal(42, result);
    }
#endif

    private static TestDatabase CreateListDb(string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.AddJson();
        db.StorageOptions.TypeConverters[typeof(List<string>)] =
            new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
        db.Table<ListRow>().CreateTable();
        return db;
    }

    [Fact]
    public void List_Contains_MatchingItem_ReturnsRow()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["fiction", "bestseller"] });
        db.Table<ListRow>().Add(new ListRow { Id = 2, Tags = ["non-fiction"] });

        List<ListRow> results = db.Table<ListRow>()
            .Where(r => r.Tags.Contains("fiction"))
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void List_Contains_NoMatch_ReturnsEmpty()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["fiction"] });

        List<ListRow> results = db.Table<ListRow>()
            .Where(r => r.Tags.Contains("mystery"))
            .ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void List_Any_NonEmptyList_ReturnsRow()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["fiction"] });
        db.Table<ListRow>().Add(new ListRow { Id = 2, Tags = [] });

        List<ListRow> results = db.Table<ListRow>()
            .Where(r => r.Tags.Any())
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void List_Any_AllEmpty_ReturnsEmpty()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = [] });

        List<ListRow> results = db.Table<ListRow>()
            .Where(r => r.Tags.Any())
            .ToList();

        Assert.Empty(results);
    }

    private class JsonRow
    {
        [Key]
        public int Id { get; set; }
        public string Data { get; set; } = "";
    }

    private class ListRow
    {
        [Key]
        public int Id { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
