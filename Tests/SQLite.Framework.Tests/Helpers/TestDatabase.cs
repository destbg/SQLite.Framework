using System.Runtime.CompilerServices;
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
using SQLite.Framework.Generated;
#endif

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
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
        builder.UseGeneratedMaterializers();
        builder.DisableReflectionFallback();
#endif
        configure?.Invoke(builder);
        return builder.Build();
    }

    public override void Dispose()
    {
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
        long fallbacks = SelectCompilerFallbacks;
#endif

        base.Dispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
        if (fallbacks > 0)
        {
            throw new InvalidOperationException(
                $"Source-generator parity check: {fallbacks} Select projection(s) fell back to the runtime compiler. " +
                "With ReflectionFallbackDisabled the runtime should throw before reaching this state; a non-zero count means the strict check was bypassed.");
        }
#endif

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
