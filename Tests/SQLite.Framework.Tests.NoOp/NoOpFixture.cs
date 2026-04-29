using System.Runtime.CompilerServices;
using SQLitePCL;

namespace SQLite.Framework.Tests.NoOp;

/// <summary>
/// Boots the NoOp SQLite provider once per test process so SQLitePCL.raw doesn't
/// fall back to the bundled provider when SQLiteDatabase calls Batteries_V2.Init.
/// </summary>
public static class NoOpFixture
{
    private static readonly NoOpSQLite Provider = new();

    public static void Init()
    {
        RuntimeHelpers.RunClassConstructor(typeof(SQLiteDatabase).TypeHandle);
        raw.SetProvider(Provider);

        NoOpSQLite.BackupInitReturnsNull = false;
        NoOpSQLite.BackupStepReturnCode = 101;
        NoOpSQLite.ErrCode = 0;
        NoOpSQLite.BeginStepReturnCode = 101;
    }
}
