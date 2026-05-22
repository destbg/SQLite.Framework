using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class VacuumTests
{
    [Fact]
    public void Vacuum_OnEmptyDatabase_Runs()
    {
        using TestDatabase db = new(useFile: true);
        db.Vacuum();
    }

    [Fact]
    public void Vacuum_AfterDelete_ReclaimsFreePages()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 200; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = new string('x', 200), AuthorId = 1, Price = i });
        }
        db.Table<Book>().ExecuteDelete(b => b.Id > 50);

        long beforeFreelist = db.Pragmas.FreelistCount;
        db.Vacuum();
        long afterFreelist = db.Pragmas.FreelistCount;

        Assert.True(beforeFreelist > 0);
        Assert.Equal(0, afterFreelist);
    }

    [Fact]
    public void Vacuum_AttachedSchema_RunsAgainstThatSchema()
    {
        string mainPath = Path.Combine(Path.GetTempPath(), $"vac_main_{Guid.NewGuid():N}.db3");
        string auxPath = Path.Combine(Path.GetTempPath(), $"vac_aux_{Guid.NewGuid():N}.db3");
        try
        {
            using SQLiteDatabase aux = new(new SQLiteOptionsBuilder(auxPath).Build());
            aux.Table<Book>().Schema.CreateTable();
            aux.Dispose();

            using SQLiteDatabase main = new(new SQLiteOptionsBuilder(mainPath).Build());
            main.AttachDatabase(auxPath, "aux");
            main.Vacuum("aux");
        }
        finally
        {
            if (File.Exists(mainPath)) File.Delete(mainPath);
            if (File.Exists(auxPath)) File.Delete(auxPath);
        }
    }

    [Fact]
    public void Vacuum_InvalidSchemaName_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() => db.Vacuum("bad name"));
    }

    [Fact]
    public void VacuumInto_WritesCleanCopyToDestination()
    {
        string destPath = Path.Combine(Path.GetTempPath(), $"vac_into_{Guid.NewGuid():N}.db3");
        try
        {
            using TestDatabase db = new(useFile: true);
            db.Table<Book>().Schema.CreateTable();
            db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

            db.VacuumInto(destPath);

            SQLiteOptionsBuilder cloneBuilder = new(destPath);
#if SQLITECIPHER
            cloneBuilder.UseEncryptionKey("test-key");
#endif
            using SQLiteDatabase clone = new(cloneBuilder.Build());
            Book copied = clone.Table<Book>().First();
            Assert.Equal(1, copied.Id);
            Assert.Equal("x", copied.Title);
        }
        finally
        {
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }

    [Fact]
    public void VacuumInto_DestinationAlreadyExists_Throws()
    {
        string destPath = Path.Combine(Path.GetTempPath(), $"vac_dup_{Guid.NewGuid():N}.db3");
        try
        {
            File.WriteAllText(destPath, "not empty");
            using TestDatabase db = new(useFile: true);
            Assert.ThrowsAny<Exception>(() => db.VacuumInto(destPath));
        }
        finally
        {
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }

    [Fact]
    public void VacuumInto_NullPath_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentNullException>(() => db.VacuumInto(null!));
    }

    [Fact]
    public void VacuumInto_EmptyPath_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() => db.VacuumInto(""));
    }

    [Fact]
    public void VacuumInto_PathWithSingleQuote_QuotedSafely()
    {
        string destPath = Path.Combine(Path.GetTempPath(), $"vac'quote_{Guid.NewGuid():N}.db3");
        try
        {
            using TestDatabase db = new(useFile: true);
            db.Table<Book>().Schema.CreateTable();
            db.VacuumInto(destPath);
            Assert.True(File.Exists(destPath));
        }
        finally
        {
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }

    [Fact]
    public void VacuumInto_AttachedSchema_CopiesThatSchema()
    {
        string mainPath = Path.Combine(Path.GetTempPath(), $"vac_into_main_{Guid.NewGuid():N}.db3");
        string auxPath = Path.Combine(Path.GetTempPath(), $"vac_into_aux_{Guid.NewGuid():N}.db3");
        string destPath = Path.Combine(Path.GetTempPath(), $"vac_into_dest_{Guid.NewGuid():N}.db3");
        try
        {
            using SQLiteDatabase aux = new(new SQLiteOptionsBuilder(auxPath).Build());
            aux.Table<Book>().Schema.CreateTable();
            aux.Table<Book>().Add(new Book { Id = 7, Title = "aux-row", AuthorId = 1, Price = 1 });
            aux.Dispose();

            using SQLiteDatabase main = new(new SQLiteOptionsBuilder(mainPath).Build());
            main.AttachDatabase(auxPath, "aux");
            main.VacuumInto(destPath, "aux");

            SQLiteOptionsBuilder cloneBuilder = new(destPath);
            using SQLiteDatabase clone = new(cloneBuilder.Build());
            Book row = clone.Table<Book>().First();
            Assert.Equal(7, row.Id);
            Assert.Equal("aux-row", row.Title);
        }
        finally
        {
            if (File.Exists(mainPath)) File.Delete(mainPath);
            if (File.Exists(auxPath)) File.Delete(auxPath);
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }

    [Fact]
    public void VacuumInto_InvalidSchemaName_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() => db.VacuumInto("dest.db", "bad name"));
    }

    [Fact]
    public async Task VacuumAsync_RunsOnLockedConnection()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<Book>().Schema.CreateTable();
        await db.VacuumAsync();
    }

    [Fact]
    public async Task VacuumIntoAsync_WritesCleanCopy()
    {
        string destPath = Path.Combine(Path.GetTempPath(), $"vac_into_async_{Guid.NewGuid():N}.db3");
        try
        {
            using TestDatabase db = new(useFile: true);
            db.Table<Book>().Schema.CreateTable();
            db.Table<Book>().Add(new Book { Id = 1, Title = "async", AuthorId = 1, Price = 1 });

            await db.VacuumIntoAsync(destPath);

            SQLiteOptionsBuilder cloneBuilder = new(destPath);
#if SQLITECIPHER
            cloneBuilder.UseEncryptionKey("test-key");
#endif
            using SQLiteDatabase clone = new(cloneBuilder.Build());
            Book row = clone.Table<Book>().First();
            Assert.Equal("async", row.Title);
        }
        finally
        {
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }
}
