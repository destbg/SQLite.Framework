using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25mGuardRows")]
public class H25mGuardRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class H25mGuardedBox
{
    public H25mGuardedBox(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public int Value { get; }
}

public class H25mGuardedHolder
{
    public int Raw { get; set; }

    public int Guarded => Raw >= 0 ? Raw : throw new InvalidOperationException("Raw is negative.");
}

public class H25mGuardedDto
{
    private int stored;

    public int Value
    {
        get => stored;
        set => stored = value >= 0 ? value : throw new ArgumentException("Value is negative.");
    }
}

public static class H25mGuardHelpers
{
    public static int Passthrough(int value)
    {
        return value;
    }

    public static H25mGuardedHolder Wrap(int value)
    {
        return new H25mGuardedHolder { Raw = value };
    }
}

public class ClientProjectionUserErrorSurfaceTests
{
    [Fact]
    public void AConstructorCheckReachesTheCallerAsItsOwnError()
    {
        using TestDatabase db = Setup(nameof(AConstructorCheckReachesTheCallerAsItsOwnError));

        Assert.Throws<ArgumentOutOfRangeException>(() => Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H25mGuardedBox(H25mGuardHelpers.Passthrough(r.Value)))
            .ToList());

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Table<H25mGuardRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H25mGuardedBox(H25mGuardHelpers.Passthrough(r.Value)))
            .ToList());
    }

    [Fact]
    public void APropertyCheckReachesTheCallerAsItsOwnError()
    {
        using TestDatabase db = Setup(nameof(APropertyCheckReachesTheCallerAsItsOwnError));

        Assert.Throws<InvalidOperationException>(() => Rows()
            .OrderBy(r => r.Id)
            .Select(r => H25mGuardHelpers.Wrap(r.Value).Guarded)
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H25mGuardRow>()
            .OrderBy(r => r.Id)
            .Select(r => H25mGuardHelpers.Wrap(r.Value).Guarded)
            .ToList());
    }

    [Fact]
    public void APropertySetterCheckReachesTheCallerAsItsOwnError()
    {
        using TestDatabase db = Setup(nameof(APropertySetterCheckReachesTheCallerAsItsOwnError));

        Assert.Throws<ArgumentException>(() => Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H25mGuardedDto { Value = H25mGuardHelpers.Passthrough(r.Value) })
            .ToList());

        Assert.Throws<ArgumentException>(() => db.Table<H25mGuardRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H25mGuardedDto { Value = H25mGuardHelpers.Passthrough(r.Value) })
            .ToList());
    }

    private static List<H25mGuardRow> Rows()
    {
        return
        [
            new H25mGuardRow { Id = 1, Value = 5 },
            new H25mGuardRow { Id = 2, Value = -1 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(o =>
        {
            o.SelectMaterializers.Clear();
            o.ReflectionFallbackDisabled = false;
        }, methodName);
        db.Table<H25mGuardRow>().Schema.CreateTable();
        db.Table<H25mGuardRow>().AddRange(Rows());
        return db;
    }
}
