using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

public class TestDatabase : SQLiteDatabase
{
#if NO_SQLITEPCL_RAW_BATTERIES
    static TestDatabase()
    {
        SQLitePCL.Batteries_V2.Init();
    }
#endif

    public TestDatabase([CallerMemberName] string? methodName = null)
        : base($"{methodName}_{Guid.NewGuid():N}.db3")
    {
        File.Delete(DatabasePath);
#if SQLITECIPHER
        Key = "test-key";
#endif
    }

    public override void Dispose()
    {
        base.Dispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        for (int i = 0; i < 10; i++)
        {
            try
            {
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                }
                break;
            }
            catch (IOException)
            {
                if (i == 9)
                {
                    return;
                }
                Thread.Sleep(100);
            }
        }

        foreach (string suffix in new[] { "-wal", "-shm" })
        {
            string sidecar = DatabasePath + suffix;
            if (File.Exists(sidecar))
            {
                File.Delete(sidecar);
            }
        }
    }
}
