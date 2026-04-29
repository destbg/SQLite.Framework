using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CustomConverterTests
{
    [Fact]
    public void TypeConverter_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 1,
            Score = new Points(42)
        });

        ScoreEntity result = db.Table<ScoreEntity>().First();

        Assert.Equal(new Points(42), result.Score);
    }

    [Fact]
    public void TypeConverter_RoundTrip_Multiple()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 1,
            Score = new Points(10)
        });
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 2,
            Score = new Points(20)
        });
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 3,
            Score = new Points(30)
        });

        List<ScoreEntity> results = db.Table<ScoreEntity>().OrderBy(e => e.Id).ToList();

        Assert.Equal(new Points(10), results[0].Score);
        Assert.Equal(new Points(20), results[1].Score);
        Assert.Equal(new Points(30), results[2].Score);
    }

    [Fact]
    public void TypeConverter_NullValue_RoundTrip()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<NullableScoreEntity>().Add(new NullableScoreEntity
        {
            Id = 1,
            Score = null
        });

        NullableScoreEntity result = db.Table<NullableScoreEntity>().First();

        Assert.Null(result.Score);
    }

    [Fact]
    public void TypeConverter_Select_ProjectedColumn()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 1,
            Score = new Points(99)
        });

        Points result = db.Table<ScoreEntity>().Select(e => e.Score).First();

        Assert.Equal(new Points(99), result);
    }

    [Fact]
    public void TypeConverter_Select_MixedProjection()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 1,
            Score = new Points(55)
        });

        var result = db.Table<ScoreEntity>().Select(e => new
        {
            e.Id,
            e.Score
        }).First();

        Assert.Equal(1, result.Id);
        Assert.Equal(new Points(55), result.Score);
    }

    [Fact]
    public void TypeConverter_Where_EqualityFilter()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 1,
            Score = new Points(10)
        });
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 2,
            Score = new Points(20)
        });

        Points target = new(10);
        List<ScoreEntity> results = db.Table<ScoreEntity>().Where(e => e.Score == target).ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void PropertyTranslator_CustomTypeMember_RewritesSqlAndRoundTrips()
    {
        using TestDatabase db = SetupDatabase(b =>
            b.PropertyTranslators.Add((memberName, instanceSql) =>
                memberName == nameof(Points.Value) ? instanceSql : null));
        db.Table<ScoreEntity>().Add(new ScoreEntity { Id = 1, Score = new Points(10) });
        db.Table<ScoreEntity>().Add(new ScoreEntity { Id = 2, Score = new Points(20) });

        List<ScoreEntity> results = db.Table<ScoreEntity>()
            .Where(e => e.Score.Value > 15)
            .ToList();

        Assert.Single(results);
        Assert.Equal(2, results[0].Id);
    }

    [Fact]
    public void TypeConverter_OrderBy()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 1,
            Score = new Points(30)
        });
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 2,
            Score = new Points(10)
        });
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 3,
            Score = new Points(20)
        });

        List<ScoreEntity> results = db.Table<ScoreEntity>().OrderBy(e => e.Score).ToList();

        Assert.Equal(new Points(10), results[0].Score);
        Assert.Equal(new Points(20), results[1].Score);
        Assert.Equal(new Points(30), results[2].Score);
    }

    [Fact]
    public void TypeConverter_Select_BinaryOperator_UsesNodeMethod()
    {
        using TestDatabase db = SetupDatabase();
        db.Table<ScoreEntity>().Add(new ScoreEntity
        {
            Id = 1,
            Score = new Points(100)
        });

        Points bonus = new(50);
        var result = db.Table<ScoreEntity>().Select(e => new
        {
            Total = e.Score + bonus
        }).First();

        Assert.Equal(new Points(150), result.Total);
    }

    [Fact]
    public void MethodTranslator_StaticMethod_TranslatesToSql()
    {
        using TestDatabase db = SetupMethodTranslatorDatabase();
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 1,
            Name = "hello world"
        });
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 2,
            Name = "foo bar"
        });

        List<TagEntity> results = db.Table<TagEntity>()
            .Where(e => SqlFunctions.Upper(e.Name) == "HELLO WORLD")
            .ToList();

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    [Fact]
    public void MethodTranslator_StaticMethod_InSelect()
    {
        using TestDatabase db = SetupMethodTranslatorDatabase();
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 1,
            Name = "hello"
        });

        var result = db.Table<TagEntity>()
            .Select(e => new
            {
                Upper = SqlFunctions.Upper(e.Name)
            })
            .First();

        Assert.Equal("HELLO", result.Upper);
    }

    [Fact]
    public void MethodTranslator_StaticMethod_TwoArguments()
    {
        using TestDatabase db = SetupMethodTranslatorDatabase();
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 1,
            Name = "hello world"
        });

        var result = db.Table<TagEntity>()
            .Select(e => new
            {
                Replaced = SqlFunctions.Replace(e.Name, "world", "there")
            })
            .First();

        Assert.Equal("hello there", result.Replaced);
    }

    [Fact]
    public void MethodTranslator_StaticMethod_OrderBy()
    {
        using TestDatabase db = SetupMethodTranslatorDatabase();
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 1,
            Name = "banana"
        });
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 2,
            Name = "apple"
        });
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 3,
            Name = "cherry"
        });

        List<TagEntity> results = db.Table<TagEntity>()
            .OrderBy(e => SqlFunctions.Upper(e.Name))
            .ToList();

        Assert.Equal("apple", results[0].Name);
        Assert.Equal("banana", results[1].Name);
        Assert.Equal("cherry", results[2].Name);
    }

    [Fact]
    public void MethodTranslator_UnregisteredMethod_DoesNotTranslate()
    {
        using TestDatabase db = SetupMethodTranslatorDatabase();
        db.Table<TagEntity>().Add(new TagEntity
        {
            Id = 1,
            Name = "hello"
        });

        var result = db.Table<TagEntity>()
            .Select(e => new
            {
                Upper = e.Name.ToUpper()
            })
            .First();

        Assert.Equal("HELLO", result.Upper);
    }

    private static TestDatabase SetupDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(Points)] = new PointsConverter();
            configure?.Invoke(b);
        }, methodName);
        db.Schema.CreateTable<ScoreEntity>();
        db.Schema.CreateTable<NullableScoreEntity>();
        return db;
    }

    private static TestDatabase SetupMethodTranslatorDatabase(Action<SQLiteOptionsBuilder>? configure = null, [CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b =>
        {
            b.MemberTranslators[
                typeof(SqlFunctions).GetMethod(nameof(SqlFunctions.Upper))!
            ] = SimpleTranslator.AsSimple((_, args) => $"upper({args[0]})");
            b.MemberTranslators[
                typeof(SqlFunctions).GetMethod(nameof(SqlFunctions.Replace))!
            ] = SimpleTranslator.AsSimple((_, args) => $"replace({args[0]}, {args[1]}, {args[2]})");
            configure?.Invoke(b);
        }, methodName);
        db.Schema.CreateTable<TagEntity>();
        return db;
    }

    private readonly struct Points(int value) : IEquatable<Points>
    {
        public int Value { get; } = value;

        public static Points operator +(Points a, Points b)
        {
            return new Points(a.Value + b.Value);
        }

        public bool Equals(Points other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is Points p && Equals(p);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(Points a, Points b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(Points a, Points b)
        {
            return a.Value != b.Value;
        }
    }

    private class PointsConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

        public object? ToDatabase(object? value)
        {
            return value is Points p ? p.Value : null;
        }

        public object FromDatabase(object? value)
        {
            return value is long l ? new Points((int)l) : new Points(0);
        }
    }

    private class ScoreEntity
    {
        [Key]
        public required int Id { get; set; }

        public required Points Score { get; set; }
    }

    private class NullableScoreEntity
    {
        [Key]
        public required int Id { get; set; }

        public Points? Score { get; set; }
    }

    private class TagEntity
    {
        [Key]
        public required int Id { get; set; }

        public required string Name { get; set; }
    }

    private static class SqlFunctions
    {
        public static string Upper(string _)
        {
            throw new InvalidOperationException("This method can only be used inside a LINQ query.");
        }

        public static string Replace(string _, string __, string ___)
        {
            throw new InvalidOperationException("This method can only be used inside a LINQ query.");
        }
    }
}