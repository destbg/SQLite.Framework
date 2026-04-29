using System.ComponentModel.DataAnnotations;
using System.Reflection;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonFunctionsTests
{
    private static TestDatabase CreateDb(string? methodName = null)
    {
        TestDatabase db = new(b => { }, methodName);
        db.Schema.CreateTable<JsonRow>();
        return db;
    }

    private static void Seed(TestDatabase db, int id, string json)
    {
        db.Table<JsonRow>().Add(new JsonRow
        {
            Id = id,
            Data = json
        });
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

    private static TestDatabase CreateListDb(Action<SQLiteOptionsBuilder>? configure = null, string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
            configure?.Invoke(b);
        }, methodName);
        db.Schema.CreateTable<ListRow>();
        return db;
    }

    [Fact]
    public void List_Contains_MatchingItem_ReturnsRow()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["fiction", "bestseller"]
        });
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 2,
            Tags = ["non-fiction"]
        });

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
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["fiction"]
        });

        List<ListRow> results = db.Table<ListRow>()
            .Where(r => r.Tags.Contains("mystery"))
            .ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void List_Any_NonEmptyList_ReturnsRow()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["fiction"]
        });
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 2,
            Tags = []
        });

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
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = []
        });

        List<ListRow> results = db.Table<ListRow>()
            .Where(r => r.Tags.Any())
            .ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void List_Count_ReturnsItemCount()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.Count())
            .First();

        Assert.Equal(3, result);
    }

    [Fact]
    public void List_First_ReturnsFirstItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["x", "y", "z"]
        });

        string result = db.Table<ListRow>()
            .Select(r => r.Tags.First())
            .First();

        Assert.Equal("x", result);
    }

    [Fact]
    public void List_FirstOrDefault_ReturnsFirstItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["x", "y"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.FirstOrDefault())
            .First();

        Assert.Equal("x", result);
    }

    [Fact]
    public void List_Last_ReturnsLastItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["x", "y", "z"]
        });

        string result = db.Table<ListRow>()
            .Select(r => r.Tags.Last())
            .First();

        Assert.Equal("z", result);
    }

    [Fact]
    public void List_LastOrDefault_ReturnsLastItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["x", "y", "z"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.LastOrDefault())
            .First();

        Assert.Equal("z", result);
    }

    [Fact]
    public void List_ElementAt_ReturnsItemAtIndex()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

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
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["banana", "apple", "cherry"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Min())
            .First();

        Assert.Equal("apple", result);
    }

    [Fact]
    public void List_Max_ReturnsMaxItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["banana", "apple", "cherry"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Max())
            .First();

        Assert.Equal("cherry", result);
    }

    [Fact]
    public void List_Single_ReturnsSingleItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["only"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Single())
            .First();

        Assert.Equal("only", result);
    }

    [Fact]
    public void List_Single_MultipleItems_ReturnsNull()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Single())
            .First();

        Assert.Null(result);
    }

    [Fact]
    public void List_SingleOrDefault_ReturnsSingleItem()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["only"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.SingleOrDefault())
            .First();

        Assert.Equal("only", result);
    }

    [Fact]
    public void List_IndexOf_ReturnsIndex()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.IndexOf("b"))
            .First();

        Assert.Equal(1, result);
    }

    [Fact]
    public void List_IndexOf_NotFound_ReturnsMinusOne()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.IndexOf("z"))
            .First();

        Assert.Equal(-1, result);
    }

    [Fact]
    public void List_Any_WithPredicate_Match()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.Any(x => x == "b"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void List_Any_WithPredicate_NoMatch()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.Any(x => x == "z"))
            .First();

        Assert.False(result);
    }

    [Fact]
    public void List_All_AllMatch()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.All(x => x != "z"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void List_All_NotAllMatch()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.All(x => x != "a"))
            .First();

        Assert.False(result);
    }

    [Fact]
    public void List_Count_WithPredicate()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "a", "b"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.Count(x => x == "a"))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void List_First_WithPredicate()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.First(x => x != "a"))
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void List_Last_WithPredicate()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Last(x => x != "c"))
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void List_Where_ReturnsFilteredList()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "a"))
            .First();

        Assert.Equal(2, result.Count());
        Assert.DoesNotContain("a", result);
    }

    [Fact]
    public void List_OrderBy_ReturnsSortedFirst()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["c", "a", "b"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).First())
            .First();

        Assert.Equal("a", result);
    }

    [Fact]
    public void List_OrderByDescending_ReturnsSortedFirst()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["c", "a", "b"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderByDescending(x => x).First())
            .First();

        Assert.Equal("c", result);
    }

    [Fact]
    public void List_Distinct_RemovesDuplicates()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "a", "b"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Distinct())
            .First();

        Assert.Equal(2, result.Count());
        Assert.Contains("a", result);
        Assert.Contains("b", result);
    }

    [Fact]
    public void List_Skip_SkipsItems()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Skip(1))
            .First();

        Assert.Equal(["b", "c"], result);
    }

    [Fact]
    public void List_Take_TakesItems()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Take(2))
            .First();

        Assert.Equal(["a", "b"], result);
    }

    [Fact]
    public void List_Concat_CombinesLists()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 2,
            Tags = ["c", "d"]
        });

        List<string> other = ["c", "d"];
        IEnumerable<string> result = db.Table<ListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Tags.Concat(other))
            .First();

        Assert.Equal(4, result.Count());
        Assert.Contains("a", result);
        Assert.Contains("d", result);
    }

    private static TestDatabase CreateNumericListDb(string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32);
        }, methodName);
        db.Schema.CreateTable<NumericListRow>();
        return db;
    }

    [Fact]
    public void List_Sum_ReturnsSumOfItems()
    {
        using TestDatabase db = CreateNumericListDb();
        db.Table<NumericListRow>().Add(new NumericListRow
        {
            Id = 1,
            Numbers = [1, 2, 3, 4]
        });

        int result = db.Table<NumericListRow>()
            .Select(r => r.Numbers.Sum())
            .First();

        Assert.Equal(10, result);
    }

    [Fact]
    public void List_Average_ReturnsAverageOfItems()
    {
        using TestDatabase db = CreateNumericListDb();
        db.Table<NumericListRow>().Add(new NumericListRow
        {
            Id = 1,
            Numbers = [2, 4, 6]
        });

        double result = db.Table<NumericListRow>()
            .Select(r => r.Numbers.Average())
            .First();

        Assert.Equal(4.0, result);
    }

    [Fact]
    public void List_Reverse_ReturnsReversed()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => Enumerable.Reverse(r.Tags))
            .First();

        Assert.Equal(["c", "b", "a"], result);
    }

    [Fact]
    public void List_Union_CombinesDistinct()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });

        List<string> other = ["b", "c"];
        IEnumerable<string> result = db.Table<ListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Tags.Union(other))
            .First();

        Assert.Equal(3, result.Count());
        Assert.Contains("a", result);
        Assert.Contains("b", result);
        Assert.Contains("c", result);
    }

    [Fact]
    public void List_Intersect_ReturnsCommon()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        List<string> other = ["b", "c", "d"];
        IEnumerable<string> result = db.Table<ListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Tags.Intersect(other))
            .First();

        Assert.Equal(2, result.Count());
        Assert.Contains("b", result);
        Assert.Contains("c", result);
    }

    [Fact]
    public void List_Except_RemovesCommon()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        List<string> other = ["b", "c"];
        IEnumerable<string> result = db.Table<ListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Tags.Except(other))
            .First();

        Assert.Single(result);
        Assert.Contains("a", result);
    }

    [Fact]
    public void ComplexType_Min_WithSelector()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Zebra"
                },
                new Address
                {
                    Street = "Oak Ave",
                    City = "Alpha"
                }
            ]
        });

        string? result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Min(x => x.City))
            .First();

        Assert.Equal("Alpha", result);
    }

    [Fact]
    public void ComplexType_Max_WithSelector()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Zebra"
                },
                new Address
                {
                    Street = "Oak Ave",
                    City = "Alpha"
                }
            ]
        });

        string? result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Max(x => x.City))
            .First();

        Assert.Equal("Zebra", result);
    }

    [Fact]
    public void ComplexType_Sum_WithSelector()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "1",
                    City = "A"
                },
                new Address
                {
                    Street = "2",
                    City = "B"
                },
                new Address
                {
                    Street = "3",
                    City = "C"
                }
            ]
        });

        int result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Sum(x => x.Street.Length))
            .First();

        Assert.Equal(3, result);
    }

    [Fact]
    public void ComplexType_Average_WithSelector()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "1",
                    City = "A"
                },
                new Address
                {
                    Street = "22",
                    City = "B"
                },
                new Address
                {
                    Street = "333",
                    City = "C"
                }
            ]
        });

        double result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Average(x => x.Street.Length))
            .First();

        Assert.Equal(2.0, result);
    }

    [Fact]
    public void List_LastIndexOf_ReturnsLastIndex()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "a", "c"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.LastIndexOf("a"))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void List_GetRange_ReturnsSublist()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c", "d"]
        });

        List<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.GetRange(1, 2))
            .First();

        Assert.Equal(["b", "c"], result);
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void List_Slice_ReturnsSublist()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c", "d"]
        });

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.Slice(1, 2))
            .ToSqlCommand();

        Assert.Contains("LIMIT @", command.CommandText);
        Assert.Contains("OFFSET @", command.CommandText);
    }
#endif

    [Fact]
    public void Enumerable_OneArgUnknownMethod_FallsThroughToDefault()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b"] });

        List<List<string>> result = db.Table<ListRow>()
            .Select(r => r.Tags.ToList())
            .ToList();

        Assert.Single(result);
        Assert.Equal(["a", "b"], result[0]);
    }

    [Fact]
    public void Array_TwoArgUnknownMethod_FallsThroughToDefault()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["a"] });

        SQLiteCommand command = db.Table<ArrayRow>()
            .Select(r => Array.AsReadOnly(r.Tags).Count)
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void Enumerable_TwoArgUnknownMethod_FallsThroughToDefault()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a"] });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<ListRow>()
                .Where(r => r.Tags.SequenceEqual(new[] { "a" }))
                .ToSqlCommand());
    }

    [Fact]
    public void Enumerable_ChainedSourceWithNonJsonResultType_FallsThrough()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b"] });

        List<string> other = ["c"];
        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => Enumerable.Reverse(r.Tags).Concat(other))
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void List_Exists_Match()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.Exists(x => x == "b"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void List_Find_ReturnsFirstMatch()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["apple", "banana", "cherry"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Find(x => x != "apple"))
            .First();

        Assert.Equal("banana", result);
    }

    [Fact]
    public void List_FindAll_ReturnsAllMatches()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "bb", "ccc", "dd"]
        });

        List<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.FindAll(x => x.Length > 1))
            .First();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void List_FindIndex_ReturnsIndex()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.FindIndex(x => x == "b"))
            .First();

        Assert.Equal(1, result);
    }

    [Fact]
    public void List_FindLast_ReturnsLastMatch()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "a"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.FindLast(x => x == "a"))
            .First();

        Assert.Equal("a", result);
    }

    [Fact]
    public void List_FindLastIndex_ReturnsLastIndex()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "a", "c"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.FindLastIndex(x => x == "a"))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void List_TrueForAll_AllMatch()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.TrueForAll(x => x.Length == 1))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Array_IndexOf_ReturnsIndex()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        int result = db.Table<ArrayRow>()
            .Select(r => Array.IndexOf(r.Tags, "b"))
            .First();

        Assert.Equal(1, result);
    }

    [Fact]
    public void Array_Exists_Match()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        bool result = db.Table<ArrayRow>()
            .Select(r => Array.Exists(r.Tags, x => x == "b"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Array_FindIndex_ReturnsIndex()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        int result = db.Table<ArrayRow>()
            .Select(r => Array.FindIndex(r.Tags, x => x != "a"))
            .First();

        Assert.Equal(1, result);
    }

    [Fact]
    public void Array_TrueForAll_AllMatch()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow
        {
            Id = 1,
            Tags = ["a", "b"]
        });

        bool result = db.Table<ArrayRow>()
            .Select(r => Array.TrueForAll(r.Tags, x => x.Length == 1))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Array_LastIndexOf_ReturnsIndex()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["a", "b", "a"] });

        int result = db.Table<ArrayRow>()
            .Select(r => Array.LastIndexOf(r.Tags, "a"))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void Array_Find_ReturnsFirstMatch()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["apple", "banana"] });

        string? result = db.Table<ArrayRow>()
            .Select(r => Array.Find(r.Tags, x => x.StartsWith("b")))
            .First();

        Assert.Equal("banana", result);
    }

    [Fact]
    public void Array_FindAll_ReturnsAllMatches()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["a", "bb", "ccc"] });

        string[] result = db.Table<ArrayRow>()
            .Select(r => Array.FindAll(r.Tags, x => x.Length > 1))
            .First();

        Assert.Equal(["bb", "ccc"], result);
    }

    [Fact]
    public void Array_FindLast_ReturnsLastMatch()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["a", "b", "a"] });

        string? result = db.Table<ArrayRow>()
            .Select(r => Array.FindLast(r.Tags, x => x == "a"))
            .First();

        Assert.Equal("a", result);
    }

    [Fact]
    public void Array_FindLastIndex_ReturnsLastIndex()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["a", "b", "a"] });

        int result = db.Table<ArrayRow>()
            .Select(r => Array.FindLastIndex(r.Tags, x => x == "a"))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void Array_ConvertAll_ProjectsEachElement()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["a", "bb"] });

        SQLiteCommand command = db.Table<ArrayRow>()
            .Select(r => Array.ConvertAll(r.Tags, x => x + "!"))
            .ToSqlCommand();

        Assert.Contains("json_group_array", command.CommandText);
    }

    private static TestDatabase CreateArrayDb(string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(string[])] =
                new SQLiteJsonConverter<string[]>(TestJsonContext.Default.StringArray);
        }, methodName);
        db.Schema.CreateTable<ArrayRow>();
        return db;
    }

    private class ArrayRow
    {
        [Key]
        public int Id { get; set; }

        public string[] Tags { get; set; } = [];
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

    [Fact]
    public void Chain_OrderBy_ThenBy_SortsMultipleKeys()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "B",
                    City = "Z"
                },
                new Address
                {
                    Street = "A",
                    City = "Z"
                },
                new Address
                {
                    Street = "A",
                    City = "A"
                }
            ]
        });

        SQLiteCommand command1 = db.Table<AddressListRow>()
            .Select(r => r.Addresses.OrderBy(x => x.City).ThenBy(x => x.Street).First().Street)
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT json_extract((
                         SELECT value
                         FROM json_each(a0.Addresses)
                         ORDER BY json_extract(value, '$.City') ASC, json_extract(value, '$.Street') ASC
                         LIMIT 1
                     ), '$.Street') AS "Street"
                     FROM "AddressListRow" AS a0
                     """.Replace("\r\n", "\n"), command1.CommandText.Replace("\r\n", "\n"));

        string? result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.OrderBy(x => x.City).ThenBy(x => x.Street).First().Street)
            .First();

        Assert.Equal("A", result);
    }

    [Fact]
    public void Chain_OrderBy_ThenByDescending_MixedSort()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "A",
                    City = "Z"
                },
                new Address
                {
                    Street = "B",
                    City = "Z"
                },
                new Address
                {
                    Street = "C",
                    City = "A"
                }
            ]
        });

        SQLiteCommand command = db.Table<AddressListRow>()
            .Select(r => r.Addresses.OrderBy(x => x.City).ThenByDescending(x => x.Street).First().Street)
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT json_extract((
                         SELECT value
                         FROM json_each(a0.Addresses)
                         ORDER BY json_extract(value, '$.City') ASC, json_extract(value, '$.Street') DESC
                         LIMIT 1
                     ), '$.Street') AS "Street"
                     FROM "AddressListRow" AS a0
                     """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));

        string? result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.OrderBy(x => x.City).ThenByDescending(x => x.Street).First().Street)
            .First();

        Assert.Equal("C", result);
    }

    [Fact]
    public void Chain_Where_OrderBy_ThenBy_Combined()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "X",
                    City = "Remove"
                },
                new Address
                {
                    Street = "B",
                    City = "Keep"
                },
                new Address
                {
                    Street = "A",
                    City = "Keep"
                }
            ]
        });

        string? result = db.Table<AddressListRow>()
            .Select(r => r.Addresses
                .Where(x => x.City == "Keep")
                .OrderBy(x => x.City).ThenBy(x => x.Street)
                .First().Street)
            .First();

        Assert.Equal("A", result);
    }

    [Fact]
    public void Chain_Where_Count()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "bb", "ccc", "dd"]
        });

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x.Length > 1).Count())
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT (
                         SELECT COUNT(*)
                         FROM json_each(l0.Tags)
                         WHERE LENGTH(value) > @p0
                     ) AS "6"
                     FROM "ListRow" AS l0
                     """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x.Length > 1).Count())
            .First();

        Assert.Equal(3, result);
    }

    [Fact]
    public void Chain_OrderBy_Take()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["c", "a", "b"]
        });

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Take(2))
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT (
                         SELECT json_group_array(value)
                         FROM (
                             SELECT value
                             FROM json_each(l0.Tags)
                             ORDER BY value ASC
                             LIMIT @p0
                         )
                     ) AS "4"
                     FROM "ListRow" AS l0
                     """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Take(2))
            .First();

        Assert.Equal(["a", "b"], result);
    }

    [Fact]
    public void Chain_SelectMany_FlattensNested()
    {
        using TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<PersonWithTags>)] =
                new SQLiteJsonConverter<List<PersonWithTags>>(TestJsonContext.Default.ListPersonWithTags);
            b.TypeConverters[typeof(List<string>)] =
                new SQLiteJsonConverter<List<string>>(TestJsonContext.Default.ListString);
        });
        db.Schema.CreateTable<PersonWithTagsRow>();
        db.Table<PersonWithTagsRow>().Add(new PersonWithTagsRow
        {
            Id = 1,
            People =
            [
                new PersonWithTags
                {
                    Name = "Alice",
                    Tags = ["a", "b"]
                },
                new PersonWithTags
                {
                    Name = "Bob",
                    Tags = ["c"]
                }
            ]
        });

        SQLiteCommand command = db.Table<PersonWithTagsRow>()
            .Select(r => r.People.SelectMany(p => p.Tags))
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT (
                         SELECT json_group_array(n.value)
                         FROM json_each(p0.People) e, json_each(json_extract(e.value, '$.Tags')) n
                     ) AS "3"
                     FROM "PersonWithTagsRow" AS p0
                     """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));

        IEnumerable<string> result = db.Table<PersonWithTagsRow>()
            .Select(r => r.People.SelectMany(p => p.Tags))
            .First();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public void Chain_GroupBy_Count()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "a", "c", "a"]
        });

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.GroupBy(x => x).Count())
            .ToSqlCommand();

        Assert.Equal("""
                     SELECT (
                         SELECT COUNT(*)
                         FROM json_each(l0.Tags)
                         GROUP BY value
                     ) AS "3"
                     FROM "ListRow" AS l0
                     """.Replace("\r\n", "\n"), command.CommandText.Replace("\r\n", "\n"));

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.GroupBy(x => x).Count())
            .First();

        Assert.Equal(3, result);
    }

    [Fact]
    public void Chain_OrderByDescending_First()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderByDescending(x => x).First())
            .First();

        Assert.Equal("c", result);
    }

    [Fact]
    public void Chain_Where_Select()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["apple", "banana", "cherry"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x.Length > 5).Select(x => x.Length).Count())
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void Chain_Where_Skip()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c", "d"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "a").Skip(1))
            .First();

        Assert.Equal(["c", "d"], result);
    }

    [Fact]
    public void Chain_Where_Last()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "c").Last())
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void Chain_OrderBy_Last_ReversesOrder()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["c", "a", "b"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Last())
            .First();

        Assert.Equal("c", result);
    }

    [Fact]
    public void Chain_Where_Single()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x == "b").Single())
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void Chain_Where_Any()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "bb", "ccc"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x.Length > 2).Any())
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Chain_Where_All()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["aa", "bb", "cc"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x.Length == 2).All(x => x.Length == 2))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Chain_Where_Min()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["c", "a", "b"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "a").Min())
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void Chain_Where_Max()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["c", "a", "b"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "c").Max())
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void Chain_Where_Distinct()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "a", "b", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "c").Distinct())
            .First();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Chain_OrderBy_Reverse()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["c", "a", "b"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Reverse())
            .First();

        Assert.Equal(["c", "b", "a"], result);
    }

    [Fact]
    public void Chain_Where_ElementAt()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c", "d"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "a").ElementAt(1))
            .First();

        Assert.Equal("c", result);
    }

    [Fact]
    public void Chain_Where_Contains()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "c").Contains("b"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Chain_First_WithPredicate()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "bb", "ccc"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).First(x => x.Length > 1))
            .First();

        Assert.Equal("bb", result);
    }

    [Fact]
    public void Chain_Last_WithPredicate()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "bb", "ccc"]
        });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Last(x => x.Length > 1))
            .First();

        Assert.Equal("ccc", result);
    }

    [Fact]
    public void Chain_Count_WithPredicate()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "bb", "ccc"]
        });

        int result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Count(x => x.Length > 1))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void Chain_Any_WithPredicate()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "bb", "ccc"]
        });

        bool result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x.Length > 0).Any(x => x.Length > 2))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void Chain_Min_WithSelector()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Long Street",
                    City = "A"
                },
                new Address
                {
                    Street = "Short",
                    City = "B"
                }
            ]
        });

        string? result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Where(x => x.City != "").Min(x => x.Street))
            .First();

        Assert.Equal("Long Street", result);
    }

    [Fact]
    public void Chain_Where_Sum()
    {
        using TestDatabase db = CreateNumericListDb();
        db.Table<NumericListRow>().Add(new NumericListRow
        {
            Id = 1,
            Numbers = [1, 2, 3, 4, 5]
        });

        int result = db.Table<NumericListRow>()
            .Select(r => r.Numbers.Where(x => x > 2).Sum())
            .First();

        Assert.Equal(12, result);
    }

    [Fact]
    public void Chain_Where_Average()
    {
        using TestDatabase db = CreateNumericListDb();
        db.Table<NumericListRow>().Add(new NumericListRow
        {
            Id = 1,
            Numbers = [2, 4, 6]
        });

        double result = db.Table<NumericListRow>()
            .Select(r => r.Numbers.Where(x => x > 2).Average())
            .First();

        Assert.Equal(5.0, result);
    }

    [Fact]
    public void Chain_Skip_Take()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c", "d", "e"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Skip(1).Take(2))
            .First();

        Assert.Equal(["b", "c"], result);
    }

    [Fact]
    public void Chain_OrderBy_Distinct()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["b", "a", "b", "a", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x).Distinct())
            .First();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public void Chain_Where_SimpleCollection()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "a").Distinct())
            .First();

        Assert.Equal(2, result.Count());
        Assert.DoesNotContain("a", result);
    }

    [Fact]
    public void Chain_Reverse_NoExistingOrder()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        IEnumerable<string> result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x != "z").Reverse())
            .First();

        Assert.Equal(["c", "b", "a"], result);
    }

    private class PersonWithTagsRow
    {
        [Key]
        public int Id { get; set; }

        public List<PersonWithTags> People { get; set; } = [];
    }

    private static TestDatabase CreateAddressListDb(string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<Address>)] =
                new SQLiteJsonConverter<List<Address>>(TestJsonContext.Default.ListAddress);
        }, methodName);
        db.Schema.CreateTable<AddressListRow>();
        return db;
    }

    [Fact]
    public void List_NonOneArgMethod_FallsThroughToDefault()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a"] });

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.AsReadOnly().Count)
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void List_OneArgNonLambda_FallsThroughToDefault()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b"] });

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.BinarySearch("a"))
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void Chain_SinglePredicate_ReturnsValue()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b", "c"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Single(x => x == "b"))
            .First();

        Assert.Equal("b", result);
    }


    [Fact]
    public void Chain_NonJsonEnumerableSource_FallsThrough()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a"] });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<ListRow>()
                .Select(r => Enumerable.Range(0, 10).Where(x => x > 5).Any())
                .ToSqlCommand());
    }

    [Fact]
    public void Chain_WhereOnConcat_NonJsonResultType_FallsThrough()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b"] });
        List<string> other = ["x"];

        Assert.ThrowsAny<Exception>(() =>
            db.Table<ListRow>()
                .Select(r => r.Tags.Concat(other).Where(x => x == "a"))
                .ToSqlCommand());
    }

    [Fact]
    public void Chain_OrderBy_StandaloneProjection_CoercesToSourceType()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["b", "a"] });

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.OrderBy(x => x))
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void Chain_SelectMany_ParameterBearingProjection()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["ab", "cd"] });
        string suffix = "!";

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.SelectMany(t => new[] { t + suffix }))
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void Chain_Where_UntranslatablePredicate_Throws()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a"] });

        Assert.ThrowsAny<NotSupportedException>(() =>
            db.Table<ListRow>()
                .Select(r => r.Tags.Where(x => x.GetHashCode() == r.Id).First())
                .ToSqlCommand());
    }

    [Fact]
    public void Chain_SelectMany_UntranslatableProjection_Throws()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a"] });

        Assert.ThrowsAny<NotSupportedException>(() =>
            db.Table<ListRow>()
                .Select(r => r.Tags.SelectMany(t => new[] { t.GetHashCode().ToString() }).First())
                .ToSqlCommand());
    }

    [Fact]
    public void Chain_Where_Single_MultipleMatches_ReturnsNull()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "a", "b"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x == "a").Single())
            .First();

        Assert.Null(result);
    }

    [Fact]
    public void Chain_Where_Single_SingleMatch_ReturnsValue()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b", "c"] });

        string? result = db.Table<ListRow>()
            .Select(r => r.Tags.Where(x => x == "b").Single())
            .First();

        Assert.Equal("b", result);
    }

    [Fact]
    public void List_Exists_NonLambdaPredicate_FallsThroughToDefault()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a"] });
        Predicate<string> pred = x => x == "a";

        SQLiteCommand command = db.Table<ListRow>()
            .Select(r => r.Tags.Exists(pred))
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void List_RemoveAll_FallsThroughToDefault()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a", "b"] });

        Assert.ThrowsAny<Exception>(() =>
            db.Table<ListRow>()
                .Select(r => r.Tags.RemoveAll(x => x == "a"))
                .ToSqlCommand());
    }

    [Fact]
    public void Array_BinarySearch_FallsThroughToDefault()
    {
        using TestDatabase db = CreateArrayDb();
        db.Table<ArrayRow>().Add(new ArrayRow { Id = 1, Tags = ["a", "b"] });

        SQLiteCommand command = db.Table<ArrayRow>()
            .Select(r => Array.BinarySearch(r.Tags, "a"))
            .ToSqlCommand();

        Assert.NotNull(command.CommandText);
    }

    [Fact]
    public void List_Find_UntranslatablePredicate_Throws()
    {
        using TestDatabase db = CreateListDb();
        db.Table<ListRow>().Add(new ListRow { Id = 1, Tags = ["a"] });

        Assert.ThrowsAny<NotSupportedException>(() =>
            db.Table<ListRow>()
                .Select(r => r.Tags.Find(x => x.GetHashCode() == r.Id))
                .ToSqlCommand());
    }

    [Fact]
    public void List_FindAll_ComplexElement_BindsProperties()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address { Street = "A", City = "X" },
                new Address { Street = "B", City = "Y" },
            ],
        });

        SQLiteCommand command = db.Table<AddressListRow>()
            .Select(r => r.Addresses.FindAll(a => a.City == "Y"))
            .ToSqlCommand();

        Assert.Contains("json_extract(value, '$.City')", command.CommandText);
    }

    [Fact]
    public void ComplexType_Any_WithPredicate_Match()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                },
                new Address
                {
                    Street = "Oak Ave",
                    City = "Shelbyville"
                }
            ]
        });

        bool result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Any(x => x.City == "Springfield"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void ComplexType_Any_WithPredicate_NoMatch()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                }
            ]
        });

        bool result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Any(x => x.City == "nowhere"))
            .First();

        Assert.False(result);
    }

    [Fact]
    public void ComplexType_All_AllMatch()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                },
                new Address
                {
                    Street = "Oak Ave",
                    City = "Shelbyville"
                }
            ]
        });

        bool result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.All(x => x.City != ""))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void ComplexType_Count_WithPredicate()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                },
                new Address
                {
                    Street = "Elm St",
                    City = "Springfield"
                },
                new Address
                {
                    Street = "Oak Ave",
                    City = "Shelbyville"
                }
            ]
        });

        int result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Count(x => x.City == "Springfield"))
            .First();

        Assert.Equal(2, result);
    }

    [Fact]
    public void ComplexType_Any_InWhereClause()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                }
            ]
        });
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 2,
            Addresses =
            [
                new Address
                {
                    Street = "Oak Ave",
                    City = "Shelbyville"
                }
            ]
        });

        List<AddressListRow> result = db.Table<AddressListRow>()
            .Where(r => r.Addresses.Any(x => x.City == "Springfield"))
            .ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ComplexType_Any_CompoundPredicate()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                },
                new Address
                {
                    Street = "Oak Ave",
                    City = "Springfield"
                }
            ]
        });

        bool result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Any(x => x.City == "Springfield" && x.Street == "Oak Ave"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void ComplexType_Any_WithChainedMethods()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                }
            ]
        });

        bool result = db.Table<AddressListRow>()
            .Where(r => r.Addresses.Any(x => x.City.StartsWith("Spring")))
            .Any();

        Assert.True(result);
    }

    [Fact]
    public void ComplexType_Where_WithPredicate()
    {
        using TestDatabase db = CreateAddressListDb();
        db.Table<AddressListRow>().Add(new AddressListRow
        {
            Id = 1,
            Addresses =
            [
                new Address
                {
                    Street = "Main St",
                    City = "Springfield"
                },
                new Address
                {
                    Street = "Oak Ave",
                    City = "Shelbyville"
                },
                new Address
                {
                    Street = "Elm St",
                    City = "Springfield"
                }
            ]
        });

        IEnumerable<Address> result = db.Table<AddressListRow>()
            .Select(r => r.Addresses.Where(x => x.City == "Springfield"))
            .First();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void PredicateMethodTranslator_ExactMethodMatch()
    {
        MethodInfo method = typeof(JsonFunctionsTests)
            .GetMethod(nameof(CustomPredicate), BindingFlags.NonPublic | BindingFlags.Static)!;
        using TestDatabase db = CreateListDb(b =>
        {
            b.MemberTranslators[method] = SimpleTranslator.AsPredicate(
                (instance, predicate) => $"(SELECT COUNT(*) FROM json_each({instance}) WHERE {predicate})");
        });
        db.Table<ListRow>().Add(new ListRow
        {
            Id = 1,
            Tags = ["a", "b", "c"]
        });

        int result = db.Table<ListRow>()
            .Select(r => CustomPredicate(r.Tags, x => x != "a"))
            .First();

        Assert.Equal(2, result);
    }

    private static int CustomPredicate(List<string> source, Func<string, bool> predicate)
    {
        throw new NotSupportedException();
    }

    [Fact]
    public void ComplexType_Any_NestedProperty()
    {
        using TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<Person>)] =
                new SQLiteJsonConverter<List<Person>>(TestJsonContext.Default.ListPerson);
        });
        db.Schema.CreateTable<PersonListRow>();
        db.Table<PersonListRow>().Add(new PersonListRow
        {
            Id = 1,
            People =
            [
                new Person
                {
                    Name = "Alice",
                    Home = new Address
                    {
                        Street = "Main St",
                        City = "Springfield"
                    }
                },
                new Person
                {
                    Name = "Bob",
                    Home = new Address
                    {
                        Street = "Oak Ave",
                        City = "Shelbyville"
                    }
                }
            ]
        });

        bool result = db.Table<PersonListRow>()
            .Select(r => r.People.Any(x => x.Home.City == "Springfield"))
            .First();

        Assert.True(result);
    }

    [Fact]
    public void PropertyTranslator_MemberAccessOnJsonColumn()
    {
        using TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(Address)] =
                new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address);
        });
        db.Schema.CreateTable<SingleAddressRow>();
        db.Table<SingleAddressRow>().Add(new SingleAddressRow
        {
            Id = 1,
            Address = new Address
            {
                Street = "Main St",
                City = "Springfield"
            }
        });

        string? result = db.Table<SingleAddressRow>()
            .Select(r => r.Address.City)
            .First();

        Assert.Equal("Springfield", result);
    }

    private class SingleAddressRow
    {
        [Key]
        public int Id { get; set; }

        public Address Address { get; set; } = new();
    }

    private class PersonListRow
    {
        [Key]
        public int Id { get; set; }

        public List<Person> People { get; set; } = [];
    }

    private class AddressListRow
    {
        [Key]
        public int Id { get; set; }

        public List<Address> Addresses { get; set; } = [];
    }
}