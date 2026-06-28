using SQLite.Framework.Maui.Migrations;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

/// <summary>
/// Single shared database for the app. One instance is registered as a singleton
/// in <see cref="MauiProgram" />; repositories take it via DI and call
/// <see cref="SQLiteDatabase.Table{T}()" /> to obtain a typed queryable.
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
    /// Brings the database up to the model by running every migration in the Migrations folder.
    /// Safe to call again, a version that already ran is skipped and is never constructed.
    /// </summary>
    public void EnsureSchema()
    {
        Schema.Migrations()
            .Add<M0001_InitialSchema>()
            .Migrate();
    }
}