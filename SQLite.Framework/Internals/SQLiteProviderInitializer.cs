namespace SQLite.Framework.Internals;

internal static class SQLiteProviderInitializer
{
    public static void Initialize()
    {
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
#if ANDROID
        raw.SetProvider(new SQLite3Provider_e_sqlite3());
#else
        raw.SetProvider(OsProvider(OperatingSystem.IsWindows()));
#endif
#elif !NO_SQLITEPCL_RAW_BATTERIES
        Batteries_V2.Init();
#endif
    }

#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE && !ANDROID
    public static ISQLite3Provider OsProvider(bool isWindows)
    {
        if (isWindows)
        {
            return new SQLite3Provider_winsqlite3();
        }

        return new SQLite3Provider_sqlite3();
    }
#endif
}
