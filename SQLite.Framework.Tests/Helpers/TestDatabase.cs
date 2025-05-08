using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

public class TestDatabase : SQLiteDatabase
{
    public TestDatabase([CallerMemberName] string? methodName = null)
        : base($"{methodName}_{Guid.NewGuid():N}.db3")
    {
    }

    public override void Dispose()
    {
        base.Dispose();

        File.Delete(DatabasePath);
    }
}
