using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct H25eScore
{
    public H25eScore(int n)
    {
        N = n;
    }

    public int N { get; }

    public static bool operator ==(H25eScore a, H25eScore b) => a.N == b.N;

    public static bool operator !=(H25eScore a, H25eScore b) => a.N != b.N;

    public override bool Equals(object? obj) => obj is H25eScore s && s.N == N;

    public override int GetHashCode() => N;
}

public sealed class H25eScoreConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value) => value is H25eScore s ? (long)s.N : null;

    public object? FromDatabase(object? value) => value is long l ? new H25eScore((int)l) : new H25eScore(0);
}

[Table("H25eScoreRows")]
public class H25eScoreRow
{
    [Key]
    public int Id { get; set; }

    public H25eScore Score { get; set; }

    public bool IsTarget { get; set; }
}

[Table("H25eUpsertScoreRows")]
public class H25eUpsertScoreRow
{
    [Key]
    public int Id { get; set; }

    public H25eScore Score { get; set; }

    public bool IsTarget { get; set; }
}

[Table("H25eTriggerScoreSources")]
public class H25eTriggerScoreSource
{
    [Key]
    public int Id { get; set; }
}

[Table("H25eTriggerScoreTargets")]
public class H25eTriggerScoreTarget
{
    [Key]
    public int Id { get; set; }

    public H25eScore Score { get; set; }

    public bool IsTarget { get; set; }
}

[Table("H25eMigrationScoreRows")]
public class H25eMigrationScoreRow
{
    [Key]
    public int Id { get; set; }

    public H25eScore Score { get; set; }

    public bool IsTarget { get; set; }
}

public class ConverterColumnReadInsideWrittenValueTests
{
    [Fact]
    public void WithColumnsWritingAConverterComparisonStoresTheSameFlagAsLinq()
    {
        using TestDatabase db = new(
            b => b.AddTypeConverter<H25eScore>(new H25eScoreConverter()),
            nameof(WithColumnsWritingAConverterComparisonStoresTheSameFlagAsLinq));
        db.Table<H25eScoreRow>().Schema.CreateTable();

        List<H25eScoreRow> rows =
        [
            new H25eScoreRow { Id = 1, Score = new H25eScore(5) },
            new H25eScoreRow { Id = 2, Score = new H25eScore(7) }
        ];
        db.Table<H25eScoreRow>().AddRange(rows);
        H25eScore five = new(5);

        foreach (H25eScoreRow row in rows)
        {
            db.Table<H25eScoreRow>()
                .WithColumns(c => c.Set(r => r.IsTarget, r => r.Score == five))
                .Update(row);
        }

        List<bool> expected = rows.OrderBy(r => r.Id).Select(r => r.Score == five).ToList();
        List<bool> actual = db.Table<H25eScoreRow>().OrderBy(r => r.Id).Select(r => r.IsTarget).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UpsertSetterWritingAConverterComparisonStoresTheSameFlagAsLinq()
    {
        using TestDatabase db = new(
            b => b.AddTypeConverter<H25eScore>(new H25eScoreConverter()),
            nameof(UpsertSetterWritingAConverterComparisonStoresTheSameFlagAsLinq));
        db.Table<H25eUpsertScoreRow>().Schema.CreateTable();
        db.Table<H25eUpsertScoreRow>().Add(new H25eUpsertScoreRow { Id = 1, Score = new H25eScore(5) });
        H25eScore five = new(5);

        List<H25eUpsertScoreRow> stored = [new H25eUpsertScoreRow { Id = 1, Score = new H25eScore(5) }];
        stored[0].IsTarget = stored[0].Score == five;
        bool expected = stored[0].IsTarget;

        db.Table<H25eUpsertScoreRow>().Upsert(
            new H25eUpsertScoreRow { Id = 1, Score = new H25eScore(5) },
            c => c.OnConflict(r => r.Id).DoUpdate(s => s.Set(r => r.IsTarget, r => r.Score == five)));

        bool actual = db.Table<H25eUpsertScoreRow>().Select(r => r.IsTarget).Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TriggerSetterWritingAConverterComparisonStoresTheSameFlagAsLinq()
    {
        using TestDatabase db = new(
            b => b.AddTypeConverter<H25eScore>(new H25eScoreConverter()),
            nameof(TriggerSetterWritingAConverterComparisonStoresTheSameFlagAsLinq));
        db.Table<H25eTriggerScoreSource>().Schema.CreateTable();
        db.Table<H25eTriggerScoreTarget>().Schema.CreateTable();

        List<H25eTriggerScoreTarget> targets =
        [
            new H25eTriggerScoreTarget { Id = 1, Score = new H25eScore(5) },
            new H25eTriggerScoreTarget { Id = 2, Score = new H25eScore(7) }
        ];
        db.Table<H25eTriggerScoreTarget>().AddRange(targets);
        H25eScore five = new(5);

        db.Schema.CreateTrigger<H25eTriggerScoreSource>(
            "h25e_score_flag_trigger",
            SQLiteTriggerTiming.After,
            SQLiteTriggerEvent.Insert,
            t => t.Update(
                db.Table<H25eTriggerScoreTarget>(),
                r => r.Id > 0,
                s => s.Set(a => a.IsTarget, a => a.Score == five)));

        db.Table<H25eTriggerScoreSource>().Add(new H25eTriggerScoreSource { Id = 1 });

        foreach (H25eTriggerScoreTarget target in targets)
        {
            target.IsTarget = target.Score == five;
        }

        List<bool> expected = targets.OrderBy(t => t.Id).Select(t => t.IsTarget).ToList();
        List<bool> actual = db.Table<H25eTriggerScoreTarget>().OrderBy(t => t.Id).Select(t => t.IsTarget).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MigrationFillWritingAConverterComparisonStoresTheSameFlagAsLinq()
    {
        using TestDatabase db = new(
            b => b.AddTypeConverter<H25eScore>(new H25eScoreConverter()),
            nameof(MigrationFillWritingAConverterComparisonStoresTheSameFlagAsLinq));
        db.Table<H25eMigrationScoreRow>().Schema.CreateTable();

        List<H25eMigrationScoreRow> rows =
        [
            new H25eMigrationScoreRow { Id = 1, Score = new H25eScore(5) },
            new H25eMigrationScoreRow { Id = 2, Score = new H25eScore(7) }
        ];
        db.Table<H25eMigrationScoreRow>().AddRange(rows);
        H25eScore five = new(5);

        db.Table<H25eMigrationScoreRow>().Schema.Migrate(m => m.Set(x => x.IsTarget, x => x.Score == five));

        foreach (H25eMigrationScoreRow row in rows)
        {
            row.IsTarget = row.Score == five;
        }

        List<bool> expected = rows.OrderBy(r => r.Id).Select(r => r.IsTarget).ToList();
        List<bool> actual = db.Table<H25eMigrationScoreRow>().OrderBy(r => r.Id).Select(r => r.IsTarget).ToList();

        Assert.Equal(expected, actual);
    }
}
