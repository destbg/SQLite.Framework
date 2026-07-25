using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("HgwGrowRows")]
public class HgwGrowRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class AddRangeLiveSourceParityTests
{
    [Fact]
    public void AddRangeFromQueryOverSameTableMatchesSnapshotSemantics()
    {
        using TestDatabase db = new();
        db.Table<HgwGrowRow>().Schema.CreateTable();
        db.Table<HgwGrowRow>().Add(new HgwGrowRow { Value = 1 });

        List<HgwGrowRow> snapshot = db.Table<HgwGrowRow>().Where(x => x.Value < 3).ToList();
        List<int> oracle = snapshot.Select(x => x.Value)
            .Concat(snapshot.Select(x => x.Value + 1))
            .OrderBy(v => v)
            .ToList();

        db.Table<HgwGrowRow>().AddRange(
            db.Table<HgwGrowRow>().Where(x => x.Value < 3).Select(x => new HgwGrowRow { Value = x.Value + 1 }));

        List<int> stored = db.Table<HgwGrowRow>().Select(x => x.Value).OrderBy(v => v).ToList();
        Assert.Equal(oracle, stored);
    }

    [Fact]
    public void AddRangeFromQueryOverSameTableMatchesSnapshotSemanticsWithInterceptor()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new HgwPassiveInterceptor()));
        db.Table<HgwGrowRow>().Schema.CreateTable();
        db.Table<HgwGrowRow>().Add(new HgwGrowRow { Value = 1 });

        List<HgwGrowRow> snapshot = db.Table<HgwGrowRow>().Where(x => x.Value < 3).ToList();
        List<int> oracle = snapshot.Select(x => x.Value)
            .Concat(snapshot.Select(x => x.Value + 1))
            .OrderBy(v => v)
            .ToList();

        db.Table<HgwGrowRow>().AddRange(
            db.Table<HgwGrowRow>().Where(x => x.Value < 3).Select(x => new HgwGrowRow { Value = x.Value + 1 }));

        List<int> stored = db.Table<HgwGrowRow>().Select(x => x.Value).OrderBy(v => v).ToList();
        Assert.Equal(oracle, stored);
    }

    [Fact]
    public void ReturningAddRangeFromQueryOverSameTableMatchesSnapshotSemantics()
    {
        using TestDatabase db = new();
        db.Table<HgwGrowRow>().Schema.CreateTable();
        db.Table<HgwGrowRow>().Add(new HgwGrowRow { Value = 1 });

        List<HgwGrowRow> snapshot = db.Table<HgwGrowRow>().Where(x => x.Value < 3).ToList();
        List<int> oracle = snapshot.Select(x => x.Value + 1).OrderBy(v => v).ToList();

        List<int> returned = db.Table<HgwGrowRow>()
            .Returning(x => x.Value)
            .AddRange(db.Table<HgwGrowRow>().Where(x => x.Value < 3).Select(x => new HgwGrowRow { Value = x.Value + 1 }));

        Assert.Equal(oracle, returned.OrderBy(v => v).ToList());
    }

    private sealed class HgwPassiveInterceptor : ISQLiteCommandInterceptor
    {
        public void OnExecuting(SQLiteCommand command)
        {
        }

        public void OnExecuted(SQLiteCommand command, int? rowsAffected)
        {
        }

        public void OnFailed(SQLiteCommand command, Exception exception)
        {
        }

        public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
        {
        }

        public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
        {
        }
    }
}
