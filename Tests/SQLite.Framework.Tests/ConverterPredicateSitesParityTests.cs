using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public readonly struct CwPoints
{
    public CwPoints(int n)
    {
        N = n;
    }

    public int N { get; }

    public static bool operator ==(CwPoints a, CwPoints b) => a.N == b.N;

    public static bool operator !=(CwPoints a, CwPoints b) => a.N != b.N;

    public override bool Equals(object? obj) => obj is CwPoints p && p.N == N;

    public override int GetHashCode() => N;
}

public sealed class CwPointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public string ParameterSqlExpression => "(({0}) + 1000)";

    public string ColumnSqlExpression => "(({0}) - 1000)";

    public object? ToDatabase(object? value) => value is CwPoints p ? (long)p.N : null;

    public object? FromDatabase(object? value) => value is long l ? new CwPoints((int)l) : new CwPoints(0);
}

[Table("CwUpsRows")]
public class CwUpsRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public string Name { get; set; } = "";
}

[Table("CwTrigSrcRows")]
public class CwTrigSrcRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }
}

[Table("CwTrigLogRows")]
public class CwTrigLogRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int Tag { get; set; }
}

[Table("CwChkRows")]
public class CwChkRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }
}

[Table("CwIdxRows")]
public class CwIdxRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public string Name { get; set; } = "";
}

[Table("CwUpdTgtRows")]
public class CwUpdTgtRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public int Mark { get; set; }
}

[Table("CwCompRows")]
public class CwCompRow
{
    [Key]
    public int Id { get; set; }

    public CwPoints Pts { get; set; }

    public bool IsFive { get; set; }
}

#if !SQLITECIPHER
[Table("CwJsonbCfgRows")]
public class CwJsonbCfgRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();

    public string Name { get; set; } = "";
}
#endif

public class ConverterPredicateSitesParityTests
{
    private static TestDatabase Db(string methodName)
    {
        return new TestDatabase(b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()), methodName);
    }

    [Fact]
    public void UpsertDoUpdateWhereConverterEqualityUpdatesMatchingRow()
    {
        using TestDatabase db = Db(nameof(UpsertDoUpdateWhereConverterEqualityUpdatesMatchingRow));
        db.Table<CwUpsRow>().Schema.CreateTable();
        db.Table<CwUpsRow>().Add(new CwUpsRow { Id = 1, Pts = new CwPoints(5), Name = "before" });
        CwPoints five = new(5);

        List<CwUpsRow> local = [new CwUpsRow { Id = 1, Pts = new CwPoints(5), Name = "before" }];
        CwUpsRow incoming = new() { Id = 1, Pts = new CwPoints(5), Name = "after" };
        CwUpsRow? conflict = local.FirstOrDefault(r => r.Id == incoming.Id);
        if (conflict != null && conflict.Pts == five)
        {
            conflict.Pts = incoming.Pts;
            conflict.Name = incoming.Name;
        }

        db.Table<CwUpsRow>().Upsert(incoming, c => c.OnConflict(b => b.Id).DoUpdateAll().Where(r => r.Pts == five));

        Assert.Equal(local[0].Name, db.Table<CwUpsRow>().Select(r => r.Name).Single());
    }

    [Fact]
    public void TriggerWhenConverterEqualityFires()
    {
        using TestDatabase db = Db(nameof(TriggerWhenConverterEqualityFires));
        db.Table<CwTrigSrcRow>().Schema.CreateTable();
        db.Table<CwTrigLogRow>().Schema.CreateTable();
        CwPoints five = new(5);

        db.Schema.CreateTrigger<CwTrigSrcRow>("cw_trg_when", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .When(() => t.New.Pts == five)
            .Insert(db.Table<CwTrigLogRow>(), s => s.Set(a => a.Tag, _ => 1)));

        List<CwTrigSrcRow> source = [new CwTrigSrcRow { Id = 1, Pts = new CwPoints(5) }];
        db.Table<CwTrigSrcRow>().Add(source[0]);

        int expected = source.Count(r => r.Pts == five);
        int actual = db.Table<CwTrigLogRow>().Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TriggerUpdatePredicateOnConverterColumnMatchesRows()
    {
        using TestDatabase db = Db(nameof(TriggerUpdatePredicateOnConverterColumnMatchesRows));
        db.Table<CwTrigSrcRow>().Schema.CreateTable();
        db.Table<CwUpdTgtRow>().Schema.CreateTable();
        CwPoints five = new(5);

        List<CwUpdTgtRow> targets =
        [
            new CwUpdTgtRow { Id = 1, Pts = new CwPoints(5), Mark = 0 },
            new CwUpdTgtRow { Id = 2, Pts = new CwPoints(7), Mark = 0 },
        ];
        db.Table<CwUpdTgtRow>().AddRange(targets);

        db.Schema.CreateTrigger<CwTrigSrcRow>("cw_trg_upd", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .Update(db.Table<CwUpdTgtRow>(), r => r.Pts == five, s => s.Set(a => a.Mark, _ => 1)));

        db.Table<CwTrigSrcRow>().Add(new CwTrigSrcRow { Id = 9, Pts = new CwPoints(1) });

        foreach (CwUpdTgtRow t in targets.Where(r => r.Pts == five))
        {
            t.Mark = 1;
        }
        List<int> expected = targets.OrderBy(r => r.Id).Select(r => r.Mark).ToList();
        List<int> actual = db.Table<CwUpdTgtRow>().OrderBy(r => r.Id).Select(r => r.Mark).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckConstraintOnConverterColumnIsEnforced()
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<CwChkRow>().Check(r => r.Pts != five),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CwChkRow>().Schema.CreateTable();

        db.Table<CwChkRow>().Add(new CwChkRow { Id = 1, Pts = new CwPoints(7) });
        Assert.ThrowsAny<Exception>(() => db.Table<CwChkRow>().Add(new CwChkRow { Id = 2, Pts = new CwPoints(5) }));

        Assert.Equal(1, db.Table<CwChkRow>().Count());
    }

    [Fact]
    public void UniqueIndexFilterOnConverterColumnIsEnforced()
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<CwIdxRow>().Index(r => r.Name, unique: true, filter: r => r.Pts == five),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CwIdxRow>().Schema.CreateTable();

        db.Table<CwIdxRow>().Add(new CwIdxRow { Id = 1, Pts = new CwPoints(5), Name = "x" });
        db.Table<CwIdxRow>().Add(new CwIdxRow { Id = 2, Pts = new CwPoints(7), Name = "x" });
        Assert.ThrowsAny<Exception>(() => db.Table<CwIdxRow>().Add(new CwIdxRow { Id = 3, Pts = new CwPoints(5), Name = "x" }));

        Assert.Equal(2, db.Table<CwIdxRow>().Count());
    }

    [Fact]
    public void ComputedColumnOverConverterEqualityMatchesLinq()
    {
        CwPoints five = new(5);
        using ModelTestDatabase db = new(
            mb => mb.Entity<CwCompRow>().Computed(r => r.IsFive, r => r.Pts == five),
            b => b.AddTypeConverter<CwPoints>(new CwPointsConverter()));
        db.Table<CwCompRow>().Schema.CreateTable();

        List<CwCompRow> rows =
        [
            new CwCompRow { Id = 1, Pts = new CwPoints(5) },
            new CwCompRow { Id = 2, Pts = new CwPoints(7) },
        ];
        db.Table<CwCompRow>().AddRange(rows);

        List<bool> expected = rows.OrderBy(r => r.Id).Select(r => r.Pts == five).ToList();
        List<bool> actual = db.Table<CwCompRow>().OrderBy(r => r.Id).Select(r => r.IsFive).ToList();

        Assert.Equal(expected, actual);
    }

#if !SQLITECIPHER
    [Fact]
    public void UpsertDoUpdateWhereJsonbEqualityUpdatesMatchingRow()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        db.Table<CwJsonbCfgRow>().Schema.CreateTable();
        Address addr = new() { Street = "1", City = "A" };
        db.Table<CwJsonbCfgRow>().Add(new CwJsonbCfgRow { Id = 1, Data = addr, Name = "before" });

        List<CwJsonbCfgRow> local = [new CwJsonbCfgRow { Id = 1, Data = addr, Name = "before" }];
        CwJsonbCfgRow incoming = new() { Id = 1, Data = addr, Name = "after" };
        CwJsonbCfgRow? conflict = local.FirstOrDefault(r => r.Id == incoming.Id);
        if (conflict != null && conflict.Data.Street == addr.Street && conflict.Data.City == addr.City)
        {
            conflict.Data = incoming.Data;
            conflict.Name = incoming.Name;
        }

        db.Table<CwJsonbCfgRow>().Upsert(incoming, c => c.OnConflict(b => b.Id).DoUpdateAll().Where(r => r.Data == addr));

        Assert.Equal(local[0].Name, db.Table<CwJsonbCfgRow>().Select(r => r.Name).Single());
    }
#endif
}
