using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21zCasedRows")]
public class H21zCasedRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class BackupDistinctFileCasingTests
{
    [Fact]
    public void BackupBetweenPathsDifferingOnlyByCaseCopiesRows()
    {
        string dir = CreateCaseSensitiveDirectory();
        if (dir.Length == 0)
        {
            return;
        }

        string lower = Path.Combine(dir, "casedb.db3");
        string upper = Path.Combine(dir, "CASEDB.db3");

        try
        {
            using SQLiteDatabase source = Open(lower);
            using SQLiteDatabase destination = Open(upper);

            source.Table<H21zCasedRow>().Schema.CreateTable();
            source.Table<H21zCasedRow>().AddRange(Rows());
            destination.Execute("CREATE TABLE \"H21zWarm\" (\"Id\" INTEGER)");

            source.BackupTo(destination);

            long count = destination.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21zCasedRows\"");
            Assert.Equal(Rows().Count, count);
            long sum = destination.ExecuteScalar<long>("SELECT SUM(\"Value\") FROM \"H21zCasedRows\"");
            Assert.Equal(Rows().Sum(r => r.Value), sum);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task BackupAsyncBetweenPathsDifferingOnlyByCaseCopiesRows()
    {
        string dir = CreateCaseSensitiveDirectory();
        if (dir.Length == 0)
        {
            return;
        }

        string lower = Path.Combine(dir, "casedb.db3");
        string upper = Path.Combine(dir, "CASEDB.db3");

        try
        {
            using SQLiteDatabase source = Open(lower);
            using SQLiteDatabase destination = Open(upper);

            source.Table<H21zCasedRow>().Schema.CreateTable();
            source.Table<H21zCasedRow>().AddRange(Rows());
            destination.Execute("CREATE TABLE \"H21zWarm\" (\"Id\" INTEGER)");

            await source.BackupToAsync(destination);

            long count = destination.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H21zCasedRows\"");
            Assert.Equal(Rows().Count, count);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static List<H21zCasedRow> Rows()
    {
        return
        [
            new H21zCasedRow { Id = 1, Value = 10 },
            new H21zCasedRow { Id = 2, Value = 20 },
            new H21zCasedRow { Id = 3, Value = 30 }
        ];
    }

    private static SQLiteDatabase Open(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }

    private static string CreateCaseSensitiveDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "h21zcase_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string lower = Path.Combine(dir, "casedb.db3");
        string upper = Path.Combine(dir, "CASEDB.db3");
        File.WriteAllText(lower, "a");
        bool caseSensitive = !File.Exists(upper);
        File.Delete(lower);
        if (caseSensitive)
        {
            return dir;
        }

        Directory.Delete(dir, recursive: true);
        return string.Empty;
    }
}
