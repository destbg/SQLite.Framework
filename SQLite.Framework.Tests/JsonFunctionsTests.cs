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

    [Fact]
    public void List_Count_ReturnsItemCount()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b", "c"] });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.Count())
            .First();

        Assert.Equal(3, result);
    }

    [Fact]
    public void List_First_ReturnsFirstItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["x", "y", "z"] });

        string result = db.Table<ListRow>()
            .Select(r => r.Tags.First())
            .First();

        Assert.Equal("x", result);
    }

    [Fact]
    public void List_FirstOrDefault_ReturnsFirstItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["x", "y"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.FirstOrDefault())
            .First();

        Assert.Equal("x", result);
    }

    [Fact]
    public void List_Last_ReturnsLastItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["x", "y", "z"] });

        string result = db.Table<ListRow>()
            .Select(r => r.Tags.Last())
            .First();

        Assert.Equal("z", result);
    }

    [Fact]
    public void List_LastOrDefault_ReturnsLastItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["x", "y", "z"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.LastOrDefault())
            .First();

        Assert.Equal("z", result);
    }

    [Fact]
    public void List_ElementAt_ReturnsItemAtIndex()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b", "c"] });

        string result = db.Table<ListRow>()
            .Select(r => r.Tags.ElementAt(1))
            .First();

        Assert.Equal("b", result);
    }

    private class JsonRow
    {
        [Key]
        public int Id { get; set; }
        public string Data { get; set; } = "";
    }

    [Fact]
    public void List_Min_ReturnsMinItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["banana", "apple", "cherry"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Min())
            .First();

        Assert.Equal("apple", result);
    }

    [Fact]
    public void List_Max_ReturnsMaxItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["banana", "apple", "cherry"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Max())
            .First();

        Assert.Equal("cherry", result);
    }

    [Fact]
    public void List_Single_ReturnsSingleItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["only"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Single())
            .First();

        Assert.Equal("only", result);
    }

    [Fact]
    public void List_Single_MultipleItems_ReturnsNull()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Single())
            .First();

        Assert.Null(result);
    }

    [Fact]
    public void List_SingleOrDefault_ReturnsSingleItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["only"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.SingleOrDefault())
            .First();

        Assert.Equal("only", result);
    }

    [Fact]
    public void List_IndexOf_ReturnsIndex()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b", "c"] });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.IndexOf("b"))
            .First();

        Assert.Equal(1, result);
    }

    [Fact]
    public void List_IndexOf_NotFound_ReturnsMinusOne()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b"] });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.IndexOf("z"))
            .First();

        Assert.Equal(-1, result);
    }

    private static TestDatabase CreateNumericListDb(string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.AddJson();
        db.StorageOptions.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32);
        db.Table<NumericListRow>().CreateTable();
        return db;
    }

    [Fact]
    public void List_Sum_ReturnsSumOfItems()
    {
        using TestDatabase db = CreateNumericListDb();
        db.Table<NumericListRow>().Add(new NumericListRow { Id = 1, Numbers = [1, 2, 3, 4] });

        int result = db.Table<NumericListRow>()
            .Select(r => r.Numbers.Sum())
            .First();

        Assert.Equal(10, result);
    }

    [Fact]
    public void List_Average_ReturnsAverageOfItems()
    {
        using TestDatabase db = CreateNumericListDb();
        db.Table<NumericListRow>().Add(new NumericListRow { Id = 1, Numbers = [2, 4, 6] });

        double result = db.Table<NumericListRow>()
            .Select(r => r.Numbers.Average())
            .First();

        Assert.Equal(4.0, result);
    }

    private class ListRow
    {
        [Key]
        public int Id { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private class NumericListRow
    {
        [Key]
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = [];
    }
}
