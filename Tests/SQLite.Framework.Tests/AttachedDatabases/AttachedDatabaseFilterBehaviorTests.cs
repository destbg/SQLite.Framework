using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FeAttachRemote")]
public class FeAttachRemote
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";

    public bool IsDeleted { get; set; }
}

[Table("FeAttachLocal")]
public class FeAttachLocal
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class AttachedDatabaseFilterBehaviorTests
{
    [Fact]
    public void FilterRegisteredOnAttachedDatabaseAppliesWhenItsTableJoinsAMainQuery()
    {
        using TestDatabase main = new();
        main.Table<FeAttachLocal>().Schema.CreateTable();
        FeAttachLocal[] locals =
        [
            new() { Id = 1, Name = "l1" },
            new() { Id = 2, Name = "l2" },
        ];
        main.Table<FeAttachLocal>().AddRange(locals);

        string auxPath = TempPath();
        try
        {
            FeAttachRemote[] remotes =
            [
                new() { Id = 1, Label = "live", IsDeleted = false },
                new() { Id = 2, Label = "gone", IsDeleted = true },
            ];
            using SQLiteDatabase aux = OpenAux(auxPath, b => b.AddQueryFilter<FeAttachRemote>(r => !r.IsDeleted));
            aux.Table<FeAttachRemote>().Schema.CreateTable();
            aux.Table<FeAttachRemote>().AddRange(remotes);

            main.AttachDatabase(aux, "aux");

            FeAttachRemote[] visible = remotes.Where(r => !r.IsDeleted).ToArray();
            List<string> expected = (
                from l in locals
                join r in visible on l.Id equals r.Id
                select l.Name + ":" + r.Label)
                .OrderBy(x => x)
                .ToList();

            List<string> actual = (
                from l in main.Table<FeAttachLocal>()
                join r in aux.Table<FeAttachRemote>() on l.Id equals r.Id
                select l.Name + ":" + r.Label)
                .ToList()
                .OrderBy(x => x)
                .ToList();

            Assert.Equal(expected, actual);
        }
        finally
        {
            Delete(auxPath);
        }
    }

    private static string TempPath()
    {
        return Path.Combine(Path.GetTempPath(), $"filterattach_{Guid.NewGuid():N}.db3");
    }

    private static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static SQLiteDatabase OpenAux(string path, Action<SQLiteOptionsBuilder>? configure = null)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        configure?.Invoke(builder);
        return new SQLiteDatabase(builder.Build());
    }
}
