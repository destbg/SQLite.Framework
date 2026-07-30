using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25kRepoOwners")]
public class H25kRepoOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H25kRepoNotes")]
public class H25kRepoNote
{
    [Key]
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public bool Archived { get; set; }
}

public class H25kNoteRepository
{
    private readonly SQLiteDatabase database;

    public H25kNoteRepository(SQLiteDatabase database)
    {
        this.database = database;
    }

    public SQLiteTable<H25kRepoNote> Notes()
    {
        return database.Table<H25kRepoNote>();
    }
}

public class AttachedTableThroughRepositoryMethodFilterTests
{
    [Fact]
    public void AttachedTableReachedThroughARepositoryMethodKeepsItsOwnDatabaseFilter()
    {
        using TestDatabase main = new();
        main.Table<H25kRepoOwner>().Schema.CreateTable();
        main.Table<H25kRepoOwner>().AddRange(Owners());

        string auxPath = AuxPath();
        try
        {
            using SQLiteDatabase aux = OpenAux(auxPath);
            aux.Table<H25kRepoNote>().Schema.CreateTable();
            aux.Table<H25kRepoNote>().AddRange(Notes());

            main.AttachDatabase(aux, "h25krepoaux");

            H25kNoteRepository repository = new(aux);

            List<int> expected = Owners()
                .Where(o => Notes().Where(n => !n.Archived).Any(n => n.OwnerId == o.Id))
                .Select(o => o.Id)
                .OrderBy(id => id)
                .ToList();

            List<int> actual = main.Table<H25kRepoOwner>()
                .Where(o => repository.Notes().Any(n => n.OwnerId == o.Id))
                .Select(o => o.Id)
                .OrderBy(id => id)
                .ToList();

            Assert.Equal(expected, actual);
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    private static List<H25kRepoOwner> Owners()
    {
        return
        [
            new H25kRepoOwner { Id = 1, Name = "a" },
            new H25kRepoOwner { Id = 2, Name = "b" },
            new H25kRepoOwner { Id = 3, Name = "c" }
        ];
    }

    private static List<H25kRepoNote> Notes()
    {
        return
        [
            new H25kRepoNote { Id = 1, OwnerId = 1, Archived = false },
            new H25kRepoNote { Id = 2, OwnerId = 2, Archived = true },
            new H25kRepoNote { Id = 3, OwnerId = 3, Archived = true }
        ];
    }

    private static SQLiteDatabase OpenAux(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        builder.AddQueryFilter<H25kRepoNote>(n => !n.Archived);
        return new SQLiteDatabase(builder.Build());
    }

    private static string AuxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h25krepo_{Guid.NewGuid():N}.db3");
    }

    private static void Cleanup(string auxPath)
    {
        if (File.Exists(auxPath))
        {
            File.Delete(auxPath);
        }
    }
}
