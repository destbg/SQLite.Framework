using SQLite.Framework.Extensions;
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