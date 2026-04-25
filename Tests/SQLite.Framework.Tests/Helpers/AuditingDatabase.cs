using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests.Helpers;

public class AuditingDatabase : TestDatabase
{
    private AuditingTable? items;

    public AuditingDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public AuditingTable Items => items ??= new AuditingTable(this, TableMapping(typeof(SubclassedTableEntity)));
}
