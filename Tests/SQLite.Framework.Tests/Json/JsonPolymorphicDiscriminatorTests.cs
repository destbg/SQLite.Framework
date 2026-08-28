using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(PolyBase))]
[JsonSerializable(typeof(PolyX))]
[JsonSerializable(typeof(PolyX2))]
[JsonSerializable(typeof(PolyY))]
[JsonSerializable(typeof(List<PolyBase>))]
[JsonSerializable(typeof(Dictionary<int, PolyBase>))]
internal partial class PolyJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(PolyBase))]
[JsonSerializable(typeof(List<PolyBase>))]
[JsonSerializable(typeof(Dictionary<int, PolyBase>))]
internal partial class PolyBaseOnlyJsonContext : JsonSerializerContext;

public class JsonPolymorphicDiscriminatorTests
{
    [Fact]
    public void Add_DirectDerivedX_StoresDiscriminator()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 } });

        Assert.Equal("""{"type":"x","X":11}""", StoredData(db, 1));
    }

    [Fact]
    public void Add_DirectDerivedX_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 } });

        PolyContainer actual = db.Table<PolyContainer>().Single();

        PolyX x = Assert.IsType<PolyX>(actual.Data);
        Assert.Equal(11, x.X);
    }

    [Fact]
    public void Add_DirectDerivedY_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyY { Y = 7 } });

        PolyContainer actual = db.Table<PolyContainer>().Single();

        PolyY y = Assert.IsType<PolyY>(actual.Data);
        Assert.Equal(7, y.Y);
    }

    [Fact]
    public void Add_DirectNull_StoresNullAndRoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = null });

        Assert.Null(StoredData(db, 1));
        Assert.Null(db.Table<PolyContainer>().Single().Data);
    }

    [Fact]
    public void Add_ListWithBothDerived_StoresDiscriminators()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer
        {
            Id = 1,
            Data2 = [new PolyX { X = 11 }, new PolyY { Y = 7 }]
        });

        Assert.Equal("""[{"type":"x","X":11},{"type":"y","Y":7}]""", StoredData2(db, 1));
    }

    [Fact]
    public void Add_ListWithBothDerived_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer
        {
            Id = 1,
            Data2 = [new PolyX { X = 11 }, new PolyY { Y = 7 }]
        });

        PolyContainer actual = db.Table<PolyContainer>().Single();

        Assert.Equal(2, actual.Data2.Count);
        Assert.Equal(11, Assert.IsType<PolyX>(actual.Data2[0]).X);
        Assert.Equal(7, Assert.IsType<PolyY>(actual.Data2[1]).Y);
    }

    [Fact]
    public void Add_ListWithNullElement_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer
        {
            Id = 1,
            Data2 = [new PolyX { X = 11 }, null!]
        });

        PolyContainer actual = db.Table<PolyContainer>().Single();

        Assert.Equal(2, actual.Data2.Count);
        Assert.Equal(11, Assert.IsType<PolyX>(actual.Data2[0]).X);
        Assert.Null(actual.Data2[1]);
    }

    [Fact]
    public void Add_DictionaryWithBothDerived_StoresDiscriminators()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer
        {
            Id = 1,
            Data3 = new Dictionary<int, PolyBase> { [1] = new PolyX { X = 11 }, [2] = new PolyY { Y = 7 } }
        });

        Assert.Equal("""{"1":{"type":"x","X":11},"2":{"type":"y","Y":7}}""", StoredData3(db, 1));
    }

    [Fact]
    public void Add_DictionaryWithBothDerived_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().Add(new PolyContainer
        {
            Id = 1,
            Data3 = new Dictionary<int, PolyBase> { [1] = new PolyX { X = 11 }, [2] = new PolyY { Y = 7 } }
        });

        PolyContainer actual = db.Table<PolyContainer>().Single();

        Assert.Equal(2, actual.Data3.Count);
        Assert.Equal(11, Assert.IsType<PolyX>(actual.Data3[1]).X);
        Assert.Equal(7, Assert.IsType<PolyY>(actual.Data3[2]).Y);
    }

    [Fact]
    public void Update_DirectDerived_StoresDiscriminator()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");

        db.Table<PolyContainer>().Update(new PolyContainer { Id = 1, Data = new PolyY { Y = 7 } });

        Assert.Equal("""{"type":"y","Y":7}""", StoredData(db, 1));
    }

    [Fact]
    public void Read_CorrectlyStoredDerived_WholeEntity_ReturnsRuntimeType()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");

        PolyContainer actual = db.Table<PolyContainer>().Single();

        Assert.Equal(11, Assert.IsType<PolyX>(actual.Data).X);
    }

    [Fact]
    public void AddOrUpdate_DirectDerived_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<PolyContainer>().AddOrUpdate(new PolyContainer { Id = 1, Data = new PolyX { X = 11 } });

        Assert.Equal("""{"type":"x","X":11}""", StoredData(db, 1));
        Assert.Equal(11, Assert.IsType<PolyX>(db.Table<PolyContainer>().Single().Data).X);
    }

    [Fact]
    public void ExecuteUpdate_SetDirectToConstant_StoresDiscriminator()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");

        db.Table<PolyContainer>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Data, new PolyY { Y = 22 }));

        Assert.Equal("""{"type":"y","Y":22}""", StoredData(db, 1));
        Assert.Equal(22, Assert.IsType<PolyY>(db.Table<PolyContainer>().Single().Data).Y);
    }

    [Fact]
    public void WithColumns_SetDirectToConstant_StoresDiscriminator()
    {
        using TestDatabase db = Db();

        db.Table<PolyContainer>()
            .WithColumns(c => c.Set(r => r.Data, new PolyX { X = 33 }))
            .Add(new PolyContainer { Id = 1 });

        Assert.Equal("""{"type":"x","X":33}""", StoredData(db, 1));
    }

    [Fact]
    public void Where_EqualsDerivedConstant_MatchesRow()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (2, '{"type":"y","Y":7}', '[]', '{}')""");

        PolyBase local = new PolyX { X = 11 };

        List<int> ids = db.Table<PolyContainer>().Where(c => c.Data == local).Select(c => c.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Execute_RawParameter_UsesRuntimeContract()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, @p, '[]', '{}')""",
            new SQLiteParameter { Name = "@p", Value = new PolyX { X = 11 } });

        Assert.Equal("""{"X":11}""", StoredData(db, 1));
    }

    [Fact]
    public void ExecuteUpdate_SetLambdaConstant_StoresDiscriminator()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");

        PolyY local = new() { Y = 5 };

        db.Table<PolyContainer>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Data, r => local));

        Assert.Equal("""{"type":"y","Y":5}""", StoredData(db, 1));
    }

    [Fact]
    public void Where_ListContainsDerivedConstant_MatchesRow()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (2, '{"type":"y","Y":7}', '[]', '{}')""");

        List<PolyBase> local = [new PolyX { X = 11 }];

        List<int> ids = db.Table<PolyContainer>().Where(c => local.Contains(c.Data!)).Select(c => c.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void Select_CorrectlyStoredDerived_ReturnsRuntimeType()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");

        PolyBase? data = db.Table<PolyContainer>().Select(c => c.Data).Single();

        Assert.Equal(11, Assert.IsType<PolyX>(data).X);
    }

    [Fact]
    public void Add_BaseOnlyContext_DirectDerived_StoresDiscriminator()
    {
        using TestDatabase db = new(b => b.AddJsonContext(PolyBaseOnlyJsonContext.Default));
        db.Table<PolyContainer>().Schema.CreateTable();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 } });

        Assert.Equal("""{"type":"x","X":11}""", StoredData(db, 1));
        Assert.Equal(11, Assert.IsType<PolyX>(db.Table<PolyContainer>().Single().Data).X);
    }

    [Fact]
    public void Add_ExplicitGenericConverter_DirectDerived_StoresDiscriminator()
    {
        using TestDatabase db = new(b =>
            b.AddTypeConverter<PolyBase>(new SQLiteJsonConverter<PolyBase>(PolyJsonContext.Default.PolyBase)));
        db.Table<PolyOnlyContainer>().Schema.CreateTable();
        db.Table<PolyOnlyContainer>().Add(new PolyOnlyContainer { Id = 1, Data = new PolyX { X = 11 } });

        Assert.Equal("""{"type":"x","X":11}""",
            db.ExecuteScalar<string>("""SELECT "Data" FROM "PolyOnlyContainers" WHERE "Id" = 1"""));
        Assert.Equal(11, Assert.IsType<PolyX>(db.Table<PolyOnlyContainer>().Single().Data).X);
    }

    [Fact]
    public void MigrationTableChanged_SetDirectToConstant_StoresDiscriminator()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, '{"type":"x","X":11}', '[]', '{}')""");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<PolyContainer>(s => s.Set(x => x.Data, new PolyY { Y = 3 }), rebuild: true))
            .Migrate();

        Assert.Equal("""{"type":"y","Y":3}""", StoredData(db, 1));
    }

    [Fact]
    public void ValuesRange_DerivedValues_RoundTrip()
    {
        using TestDatabase db = Db();
        List<PolyBase> local = [new PolyX { X = 11 }, new PolyY { Y = 7 }];

        List<PolyBase> actual = db.ValuesRange(local).ToList();

        Assert.Equal(2, actual.Count);
        Assert.Equal(11, Assert.IsType<PolyX>(actual[0]).X);
        Assert.Equal(7, Assert.IsType<PolyY>(actual[1]).Y);
    }

    [Fact]
    public void Values_DerivedValue_RoundTrips()
    {
        using TestDatabase db = Db();

        PolyBase actual = db.Values<PolyBase>(new PolyX { X = 11 }).Single();

        Assert.Equal(11, Assert.IsType<PolyX>(actual).X);
    }

    [Fact]
    public void AddJsonContext_BaseOnlyRegistration_RegistersDerivedTypeConverters()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.AddJsonContext(PolyBaseOnlyJsonContext.Default);

        SQLiteOptions options = builder.Build();

        Assert.True(options.TypeConverters.ContainsKey(typeof(PolyX)));
        Assert.True(options.TypeConverters.ContainsKey(typeof(PolyY)));
    }

#if !SQLITECIPHER
    [Fact]
    public void Jsonb_Add_DirectDerived_StoresDiscriminator()
    {
        using TestDatabase db = new(b => b.AddJsonbContext(PolyJsonContext.Default));
        db.Table<PolyContainer>().Schema.CreateTable();
        db.Table<PolyContainer>().Add(new PolyContainer { Id = 1, Data = new PolyX { X = 11 } });

        Assert.Equal("""{"type":"x","X":11}""",
            db.ExecuteScalar<string>("""SELECT json("Data") FROM "PolyContainers" WHERE "Id" = 1"""));
        Assert.Equal(11, Assert.IsType<PolyX>(db.Table<PolyContainer>().Single().Data).X);
    }

    [Fact]
    public void Jsonb_ExecuteUpdate_SetDirectToConstant_StoresDiscriminator()
    {
        using TestDatabase db = new(b => b.AddJsonbContext(PolyJsonContext.Default));
        db.Table<PolyContainer>().Schema.CreateTable();
        db.Execute("""INSERT INTO "PolyContainers" ("Id", "Data", "Data2", "Data3") VALUES (1, jsonb('{"type":"x","X":11}'), jsonb('[]'), jsonb('{}'))""");

        db.Table<PolyContainer>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Data, new PolyY { Y = 22 }));

        Assert.Equal("""{"type":"y","Y":22}""",
            db.ExecuteScalar<string>("""SELECT json("Data") FROM "PolyContainers" WHERE "Id" = 1"""));
    }
#endif

    private static TestDatabase Db()
    {
        TestDatabase db = new(b => b.AddJsonContext(PolyJsonContext.Default));
        db.Table<PolyContainer>().Schema.CreateTable();
        return db;
    }

    private static string? StoredData(TestDatabase db, int id)
    {
        return db.ExecuteScalar<string>($"""SELECT "Data" FROM "PolyContainers" WHERE "Id" = {id}""");
    }

    private static string? StoredData2(TestDatabase db, int id)
    {
        return db.ExecuteScalar<string>($"""SELECT "Data2" FROM "PolyContainers" WHERE "Id" = {id}""");
    }

    private static string? StoredData3(TestDatabase db, int id)
    {
        return db.ExecuteScalar<string>($"""SELECT "Data3" FROM "PolyContainers" WHERE "Id" = {id}""");
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PolyX), "x")]
[JsonDerivedType(typeof(PolyX2), "x2")]
[JsonDerivedType(typeof(PolyY), "y")]
public class PolyBase
{
}

public class PolyX : PolyBase
{
    public int X { get; set; }
}

public class PolyX2 : PolyX
{
    public int X2 { get; set; }
}

public interface IPolyMark
{
}

public class PolyY : PolyBase, IPolyMark
{
    public int Y { get; set; }
}

[Table("PolyContainers")]
public class PolyContainer
{
    [Key]
    public int Id { get; set; }

    public PolyBase? Data { get; set; }

    public List<PolyBase> Data2 { get; set; } = [];

    public Dictionary<int, PolyBase> Data3 { get; set; } = [];
}

[Table("PolyOnlyContainers")]
public class PolyOnlyContainer
{
    [Key]
    public int Id { get; set; }

    public PolyBase? Data { get; set; }
}
