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
        : this(null, methodName)
    {
    }

    public TestDatabase(Action<SQLiteOptionsBuilder>? configure, [CallerMemberName] string? methodName = null)
        : base(BuildOptions(methodName, configure))
    {
        File.Delete(Options.DatabasePath);
    }

    private static SQLiteOptions BuildOptions(string? methodName, Action<SQLiteOptionsBuilder>? configure)
    {
        SQLiteOptionsBuilder builder = new($"{methodName}_{Guid.NewGuid():N}.db3");
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        configure?.Invoke(builder);
        return builder.Build();
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
                if (File.Exists(Options.DatabasePath))
                {
                    File.Delete(Options.DatabasePath);
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
            string sidecar = Options.DatabasePath + suffix;
            if (File.Exists(sidecar))
            {
                File.Delete(sidecar);
            }
        }
    }
}
