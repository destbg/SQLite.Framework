namespace SQLite.Framework.Avalonia.Data;

public static class Constants
{
    public const string DatabaseFilename = "AppSQLite.db3";

    /// <summary>
    /// Picks a per-platform writable folder for the database file. Avalonia does not
    /// expose a unified app-data API the way MAUI does, so we resolve the right
    /// location ourselves.
    /// </summary>
    public static string DatabasePath
    {
        get
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(folder))
            {
                folder = AppContext.BaseDirectory;
            }

            string appFolder = Path.Combine(folder, "SQLite.Framework.Avalonia");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, DatabaseFilename);
        }
    }
}
