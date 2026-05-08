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
        : this(null, useFile: false, methodName)
    {
    }

    public TestDatabase(Action<SQLiteOptionsBuilder>? configure, [CallerMemberName] string? methodName = null)
        : this(configure, useFile: false, methodName)
    {
    }

    public TestDatabase(bool useFile, [CallerMemberName] string? methodName = null)
        : this(null, useFile, methodName)
    {
    }

    public TestDatabase(Action<SQLiteOptionsBuilder>? configure, bool useFile, [CallerMemberName] string? methodName = null)
        : base(BuildOptions(methodName, configure, useFile))
    {
        if (Options.DatabasePath != ":memory:")
        {
            TryDeleteFile(Options.DatabasePath);
        }
    }

    private static SQLiteOptions BuildOptions(string? methodName, Action<SQLiteOptionsBuilder>? configure, bool useFile)
    {
        string initialPath = useFile ? FilePath(methodName) : ":memory:";
        SQLiteOptionsBuilder builder = new(initialPath);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
        builder.UseGeneratedMaterializers();
        builder.DisableReflectionFallback();
#endif
        configure?.Invoke(builder);

        if (builder.IsWalMode && builder.DatabasePath == ":memory:")
        {
            builder.DatabasePath = FilePath(methodName);
        }

        return builder.Build();
    }

    private static string FilePath(string? methodName) => $"{methodName}_{Guid.NewGuid():N}.db3";

    public override void Dispose()
    {
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
        long fallbacks = SelectCompilerFallbacks;
#endif

        base.Dispose();

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
        if (fallbacks > 0)
        {
            throw new InvalidOperationException(
                $"Source-generator parity check: {fallbacks} Select projection(s) fell back to the runtime compiler. " +
                "With ReflectionFallbackDisabled the runtime should throw before reaching this state; a non-zero count means the strict check was bypassed.");
        }
#endif

        if (Options.DatabasePath == ":memory:")
        {
            return;
        }

        TryDeleteFile(Options.DatabasePath);
        TryDeleteFile(Options.DatabasePath + "-wal");
        TryDeleteFile(Options.DatabasePath + "-shm");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }
}
