using SQLite.Framework.Avalonia.Models;

namespace SQLite.Framework.Avalonia.Data;

/// <summary>
/// Single shared database for the app. Registered as a singleton in <c>App</c>;
/// repositories take it via DI and call the typed table properties.
/// </summary>
public class AppDatabase : SQLiteDatabase
{
    public AppDatabase(SQLiteOptions options) : base(options)
    {
        EnsureSchema();
    }

    public SQLiteTable<Project> Projects => Table<Project>();
    public SQLiteTable<ProjectTask> Tasks => Table<ProjectTask>();
    public SQLiteTable<Tag> Tags => Table<Tag>();
    public SQLiteTable<Category> Categories => Table<Category>();
    public SQLiteTable<ProjectsTags> ProjectsTags => Table<ProjectsTags>();

    /// <summary>
    /// Creates every table the app uses. Safe to call again, it does nothing on existing tables.
    /// </summary>
    public void EnsureSchema()
    {
        Schema.CreateTable<Category>();
        Schema.CreateTable<Project>();
        Schema.CreateTable<ProjectTask>();
        Schema.CreateTable<Tag>();
        Schema.CreateTable<ProjectsTags>();
    }
}
