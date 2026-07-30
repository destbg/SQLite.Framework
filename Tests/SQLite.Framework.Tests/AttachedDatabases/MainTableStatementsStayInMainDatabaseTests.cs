using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25kLedgers")]
public class H25kLedger
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MainTableStatementsStayInMainDatabaseTests
{
    [Fact]
    public void AddingThroughTheMainTableLeavesTheAttachedRowsUnchanged()
    {
        string auxPath = AuxPath();
        try
        {
            SeedAux(auxPath);

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h25kaux", AuxKey);

            Record.Exception(() =>
            {
                main.Table<H25kLedger>().Add(new H25kLedger { Id = 3, Name = "gamma" });
            });

            Assert.Equal(SeededNames(), AttachedNames(main));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void ClearingTheMainTableLeavesTheAttachedRowsUnchanged()
    {
        string auxPath = AuxPath();
        try
        {
            SeedAux(auxPath);

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h25kaux", AuxKey);

            Record.Exception(() =>
            {
                main.Table<H25kLedger>().Clear();
            });

            Assert.Equal(SeededNames(), AttachedNames(main));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void ReadingTheMainTableReturnsNoAttachedRows()
    {
        string auxPath = AuxPath();
        try
        {
            SeedAux(auxPath);

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h25kaux", AuxKey);

            List<H25kLedger> actual = [];
            Record.Exception(() =>
            {
                actual = main.Table<H25kLedger>().ToList();
            });

            Assert.Empty(actual);
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    [Fact]
    public void UpsertingThroughTheMainTableLeavesTheAttachedRowsUnchanged()
    {
        string auxPath = AuxPath();
        try
        {
            SeedAux(auxPath);

            using TestDatabase main = new();
            main.AttachDatabase(auxPath, "h25kaux", AuxKey);

            Record.Exception(() =>
            {
                main.Table<H25kLedger>().Schema.CreateTable();
                main.Table<H25kLedger>().Upsert(
                    new H25kLedger { Id = 1, Name = "gamma" },
                    c => c.OnConflict(x => x.Id).DoUpdate(x => x.Name));
            });

            Assert.Equal(SeededNames(), AttachedNames(main));
        }
        finally
        {
            Cleanup(auxPath);
        }
    }

    private static List<string> SeededNames()
    {
        return ["alpha", "beta"];
    }

    private static List<string> AttachedNames(SQLiteDatabase main)
    {
        return main.Query<string>("SELECT \"Name\" FROM h25kaux.\"H25kLedgers\" ORDER BY \"Id\"");
    }

    private static void SeedAux(string path)
    {
        using SQLiteDatabase aux = OpenAux(path);
        aux.Table<H25kLedger>().Schema.CreateTable();
        aux.Table<H25kLedger>().AddRange([
            new H25kLedger { Id = 1, Name = "alpha" },
            new H25kLedger { Id = 2, Name = "beta" }
        ]);
    }

    private static SQLiteDatabase OpenAux(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }

    private static string? AuxKey =>
#if SQLITECIPHER
        "test-key";
#else
        null;
#endif

    private static string AuxPath()
    {
        return Path.Combine(Path.GetTempPath(), $"h25kaux_{Guid.NewGuid():N}.db3");
    }

    private static void Cleanup(string auxPath)
    {
        if (File.Exists(auxPath))
        {
            File.Delete(auxPath);
        }
    }
}
