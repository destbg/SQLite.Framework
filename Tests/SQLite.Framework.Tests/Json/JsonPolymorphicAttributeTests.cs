using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(AttrShape))]
[JsonSerializable(typeof(AttrCircle))]
[JsonSerializable(typeof(AttrSquare))]
[JsonSerializable(typeof(List<AttrShape>))]
internal partial class AttrShapeJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(SnakeShape))]
[JsonSerializable(typeof(SnakeRed))]
[JsonSerializable(typeof(SnakeBlue))]
internal partial class SnakeShapeJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(AbsShape))]
[JsonSerializable(typeof(AbsImpl))]
internal partial class AbsShapeJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(PolyNodeBase))]
[JsonSerializable(typeof(PolyNode))]
internal partial class PolyNodeJsonContext : JsonSerializerContext;

public class JsonPolymorphicAttributeTests
{
    [Fact]
    public void Add_DirectDerived_StoresCustomDiscriminatorAndRenamedMembers()
    {
        using TestDatabase db = Db();
        db.Table<AttrShapeRow>().Add(new AttrShapeRow
        {
            Id = 1,
            Shape = new AttrCircle { Radius = 2.5, CachedArea = 19 }
        });

        Assert.Equal("""{"kind":"circle","radius":2.5}""",
            db.ExecuteScalar<string>("""SELECT "Shape" FROM "AttrShapeRows" WHERE "Id" = 1"""));
    }

    [Fact]
    public void Add_DirectDerived_RoundTripsThroughWholeEntity()
    {
        using TestDatabase db = Db();
        db.Table<AttrShapeRow>().Add(new AttrShapeRow
        {
            Id = 1,
            Shape = new AttrCircle { Radius = 2.5, CachedArea = 19 }
        });

        AttrShapeRow actual = db.Table<AttrShapeRow>().Single();

        Assert.Equal(2.5, Assert.IsType<AttrCircle>(actual.Shape).Radius);
    }

    [Fact]
    public void Add_ConcreteDerivedProperty_StoresWithoutDiscriminator()
    {
        using TestDatabase db = Db();
        db.Table<ConcreteCircleRow>().Add(new ConcreteCircleRow
        {
            Id = 1,
            Circle = new AttrCircle { Radius = 3.5 }
        });

        Assert.Equal("""{"radius":3.5}""",
            db.ExecuteScalar<string>("""SELECT "Circle" FROM "ConcreteCircleRows" WHERE "Id" = 1"""));
        Assert.Equal(3.5, db.Table<ConcreteCircleRow>().Single().Circle.Radius);
    }

    [Fact]
    public void Add_ListWithBothDerived_RoundTripsRenamedMembers()
    {
        using TestDatabase db = Db();
        db.Table<AttrShapeRow>().Add(new AttrShapeRow
        {
            Id = 1,
            Shapes = [new AttrCircle { Radius = 1.5 }, new AttrSquare { Side = 4 }]
        });

        AttrShapeRow actual = db.Table<AttrShapeRow>().Single();

        Assert.Equal(2, actual.Shapes.Count);
        Assert.Equal(1.5, Assert.IsType<AttrCircle>(actual.Shapes[0]).Radius);
        Assert.Equal(4, Assert.IsType<AttrSquare>(actual.Shapes[1]).Side);
    }

    [Fact]
    public void Add_NamingPolicyContext_StoresSnakeCaseWithDiscriminator()
    {
        using TestDatabase db = new(b => b.AddJsonContext(SnakeShapeJsonContext.Default));
        db.Table<SnakeShapeRow>().Schema.CreateTable();
        db.Table<SnakeShapeRow>().Add(new SnakeShapeRow
        {
            Id = 1,
            Shape = new SnakeRed { ColorCode = 7 }
        });

        Assert.Equal("""{"shape_type":"red","color_code":7}""",
            db.ExecuteScalar<string>("""SELECT "Shape" FROM "SnakeShapeRows" WHERE "Id" = 1"""));
        Assert.Equal(7, Assert.IsType<SnakeRed>(db.Table<SnakeShapeRow>().Single().Shape).ColorCode);
    }

    [Fact]
    public void Add_AbstractBase_RoundTrips()
    {
        using TestDatabase db = new(b => b.AddJsonContext(AbsShapeJsonContext.Default));
        db.Table<AbsShapeRow>().Schema.CreateTable();
        db.Table<AbsShapeRow>().Add(new AbsShapeRow
        {
            Id = 1,
            Shape = new AbsImpl { A = 1, B = 2 }
        });

        Assert.Equal("""{"t":"impl","B":2,"A":1}""",
            db.ExecuteScalar<string>("""SELECT "Shape" FROM "AbsShapeRows" WHERE "Id" = 1"""));

        AbsImpl actual = Assert.IsType<AbsImpl>(db.Table<AbsShapeRow>().Single().Shape);
        Assert.Equal(1, actual.A);
        Assert.Equal(2, actual.B);
    }

    [Fact]
    public void Add_RecursiveDerivedType_StoresNestedDiscriminators()
    {
        using TestDatabase db = new(b => b.AddJsonContext(PolyNodeJsonContext.Default));
        db.Table<PolyNodeRow>().Schema.CreateTable();
        db.Table<PolyNodeRow>().Add(new PolyNodeRow
        {
            Id = 1,
            Node = new PolyNode { Child = new PolyNode { Child = null } }
        });

        Assert.Equal("""{"type":"node","Child":{"type":"node","Child":null}}""",
            db.ExecuteScalar<string>("""SELECT "Node" FROM "PolyNodeRows" WHERE "Id" = 1"""));

        PolyNode actual = Assert.IsType<PolyNode>(db.Table<PolyNodeRow>().Single().Node);
        Assert.IsType<PolyNode>(actual.Child);
        Assert.Null(Assert.IsType<PolyNode>(actual.Child).Child);
    }

    [Fact]
    public void AddRange_DirectDerived_StoresDiscriminatorPerRow()
    {
        using TestDatabase db = Db();
        db.Table<AttrShapeRow>().AddRange(
        [
            new AttrShapeRow { Id = 1, Shape = new AttrCircle { Radius = 1 } },
            new AttrShapeRow { Id = 2, Shape = new AttrSquare { Side = 2 } }
        ]);

        Assert.Equal("""{"kind":"circle","radius":1}""",
            db.ExecuteScalar<string>("""SELECT "Shape" FROM "AttrShapeRows" WHERE "Id" = 1"""));
        Assert.Equal("""{"kind":"square","Side":2}""",
            db.ExecuteScalar<string>("""SELECT "Shape" FROM "AttrShapeRows" WHERE "Id" = 2"""));
    }

    [Fact]
    public void UpdateRange_DirectDerived_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<AttrShapeRow>().AddRange(
        [
            new AttrShapeRow { Id = 1, Shape = new AttrCircle { Radius = 1 } },
            new AttrShapeRow { Id = 2, Shape = new AttrSquare { Side = 2 } }
        ]);

        db.Table<AttrShapeRow>().UpdateRange(
        [
            new AttrShapeRow { Id = 1, Shape = new AttrSquare { Side = 9 } },
            new AttrShapeRow { Id = 2, Shape = new AttrCircle { Radius = 8 } }
        ]);

        List<AttrShapeRow> actual = db.Table<AttrShapeRow>().OrderBy(r => r.Id).ToList();

        Assert.Equal(9, Assert.IsType<AttrSquare>(actual[0].Shape).Side);
        Assert.Equal(8, Assert.IsType<AttrCircle>(actual[1].Shape).Radius);
    }

    [Fact]
    public void Upsert_DirectDerived_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<AttrShapeRow>().Upsert(
            new AttrShapeRow { Id = 1, Shape = new AttrCircle { Radius = 5 } },
            c => c.OnConflict(r => r.Id).DoUpdateAll());

        Assert.Equal("""{"kind":"circle","radius":5}""",
            db.ExecuteScalar<string>("""SELECT "Shape" FROM "AttrShapeRows" WHERE "Id" = 1"""));
        Assert.Equal(5, Assert.IsType<AttrCircle>(db.Table<AttrShapeRow>().Single().Shape).Radius);
    }

    [Fact]
    public void FromSql_WholeEntityRead_ReturnsRuntimeType()
    {
        using TestDatabase db = Db();
        db.Execute("""INSERT INTO "AttrShapeRows" ("Id", "Shape", "Shapes") VALUES (1, '{"kind":"square","Side":6}', '[]')""");

        AttrShapeRow actual = db.FromSql<AttrShapeRow>("""SELECT * FROM "AttrShapeRows" """).Single();

        Assert.Equal(6, Assert.IsType<AttrSquare>(actual.Shape).Side);
    }

    [Fact]
    public void Select_DtoProjection_ReturnsRuntimeType()
    {
        using TestDatabase db = Db();
        db.Table<AttrShapeRow>().Add(new AttrShapeRow
        {
            Id = 1,
            Shape = new AttrCircle { Radius = 4.5 }
        });

        AttrShapeDto actual = db.Table<AttrShapeRow>()
            .Select(r => new AttrShapeDto { Id = r.Id, Shape = r.Shape })
            .Single();

        Assert.Equal(4.5, Assert.IsType<AttrCircle>(actual.Shape).Radius);
    }

#if !SQLITECIPHER
    [Fact]
    public void Jsonb_WholeEntityRead_ReturnsRuntimeType()
    {
        using TestDatabase db = new(b => b.AddJsonbContext(AttrShapeJsonContext.Default));
        db.Table<AttrShapeRow>().Schema.CreateTable();
        db.Table<AttrShapeRow>().Add(new AttrShapeRow
        {
            Id = 1,
            Shape = new AttrSquare { Side = 3 }
        });

        AttrShapeRow actual = db.Table<AttrShapeRow>().Single();

        Assert.Equal(3, Assert.IsType<AttrSquare>(actual.Shape).Side);
    }
#endif

    private static TestDatabase Db()
    {
        TestDatabase db = new(b => b.AddJsonContext(AttrShapeJsonContext.Default));
        db.Table<AttrShapeRow>().Schema.CreateTable();
        db.Table<ConcreteCircleRow>().Schema.CreateTable();
        return db;
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AttrCircle), "circle")]
[JsonDerivedType(typeof(AttrSquare), "square")]
public class AttrShape
{
}

public class AttrCircle : AttrShape
{
    [JsonPropertyName("radius")]
    public double Radius { get; set; }

    [JsonIgnore]
    public int CachedArea { get; set; }
}

public class AttrSquare : AttrShape
{
    public double Side { get; set; }
}

[Table("AttrShapeRows")]
public class AttrShapeRow
{
    [Key]
    public int Id { get; set; }

    public AttrShape? Shape { get; set; }

    public List<AttrShape> Shapes { get; set; } = [];
}

[Table("ConcreteCircleRows")]
public class ConcreteCircleRow
{
    [Key]
    public int Id { get; set; }

    public AttrCircle Circle { get; set; } = new();
}

public class AttrShapeDto
{
    public int Id { get; set; }

    public AttrShape? Shape { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "shape_type")]
[JsonDerivedType(typeof(SnakeRed), "red")]
[JsonDerivedType(typeof(SnakeBlue), "blue")]
public class SnakeShape
{
}

public class SnakeRed : SnakeShape
{
    public int ColorCode { get; set; }
}

public class SnakeBlue : SnakeShape
{
    public int ShadeDepth { get; set; }
}

[Table("SnakeShapeRows")]
public class SnakeShapeRow
{
    [Key]
    public int Id { get; set; }

    public SnakeShape? Shape { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(AbsImpl), "impl")]
public abstract class AbsShape
{
    public int A { get; set; }
}

public class AbsImpl : AbsShape
{
    public int B { get; set; }
}

[Table("AbsShapeRows")]
public class AbsShapeRow
{
    [Key]
    public int Id { get; set; }

    public AbsShape? Shape { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PolyNode), "node")]
public class PolyNodeBase
{
}

public class PolyNode : PolyNodeBase
{
    public PolyNodeBase? Child { get; set; }
}

[Table("PolyNodeRows")]
public class PolyNodeRow
{
    [Key]
    public int Id { get; set; }

    public PolyNodeBase? Node { get; set; }
}
