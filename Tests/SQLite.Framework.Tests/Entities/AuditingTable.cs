using SQLite.Framework.Models;

namespace SQLite.Framework.Tests.Entities;

public class AuditingTable : SQLiteTable<SubclassedTableEntity>
{
    public AuditingTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    public int AddCallCount { get; private set; }

    protected override int AddOrRemoveItem(TableColumn[] columns, string sql, SubclassedTableEntity item)
    {
        AddCallCount++;
        return base.AddOrRemoveItem(columns, sql, item);
    }
}
