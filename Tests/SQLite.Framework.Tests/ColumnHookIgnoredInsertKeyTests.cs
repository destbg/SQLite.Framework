using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ColumnHookIgnoredInsertKeyTests
{
    [Fact]
    public void OnAddColumnHook_TriggerIgnoredInsert_LeavesAutoIncrementKeyUnset()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<HookItem>().Column("CreatedAt", SQLiteColumnType.Integer, nullable: true),
            o => o.OnAdd<HookItem>((_, _, columns) =>
            {
                columns["CreatedAt"] = 1L;
                return true;
            }));
        db.Schema.CreateTable<HookItem>();
        db.Execute("CREATE TRIGGER \"trg_ignore\" BEFORE INSERT ON \"HookItem\" WHEN NEW.\"Name\" = 'skip' BEGIN SELECT RAISE(IGNORE); END");

        HookItem inserted = new() { Name = "a" };
        HookItem ignored = new() { Name = "skip" };
        db.Table<HookItem>().Add(inserted);
        db.Table<HookItem>().Add(ignored);

        long rowCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"HookItem\"");

        Assert.Equal(1, rowCount);
        Assert.Equal(0, ignored.Id);
    }
}
