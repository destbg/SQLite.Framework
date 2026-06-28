using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Migrations;

/// <summary>
/// The first schema version. Creates every table the app uses. Each later schema change gets its
/// own file in this folder with the next version number.
/// </summary>
public sealed class M0001_InitialSchema : ISQLiteMigration
{
    public static int Version => 1;

    public void Apply(SQLiteMigrationStep step)
    {
        step.CreateTable<Category>()
            .CreateTable<Project>()
            .CreateTable<ProjectTask>()
            .CreateTable<Tag>()
            .CreateTable<ProjectsTags>();
    }
}
