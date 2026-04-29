namespace SQLite.Framework.Maui.Data;

public static class Constants
{
	public const string DatabaseFilename = "AppSQLite.db3";

	public static string DatabasePath =>
		Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
}