#if SQLITECIPHER
using SQLite.Framework;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EncryptionKeyLogRedactionTests
{
    [Fact]
    public void RekeyDoesNotWriteTheNewKeyIntoTheCommandLog()
    {
        string path = Path.Combine(Path.GetTempPath(), $"sece_rekey_{Guid.NewGuid():N}.db3");
        List<string> log = [];
        try
        {
            SQLiteOptionsBuilder builder = new(path);
            builder.UseEncryptionKey("old-key");
            builder.LogCommands(log.Add);
            using (SQLiteDatabase db = new(builder.Build()))
            {
                db.Execute("CREATE TABLE T (Id INTEGER)");
                db.Pragmas.Rekey("sece-new-secret");
            }

            Assert.DoesNotContain(log, line => line.Contains("sece-new-secret", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void AttachWithKeyDoesNotWriteTheKeyIntoTheCommandLog()
    {
        string auxPath = Path.Combine(Path.GetTempPath(), $"sece_attach_{Guid.NewGuid():N}.db3");
        List<string> log = [];
        try
        {
            SQLiteOptionsBuilder auxBuilder = new(auxPath);
            auxBuilder.UseEncryptionKey("sece-aux-secret");
            using (SQLiteDatabase aux = new(auxBuilder.Build()))
            {
                aux.Execute("CREATE TABLE A (Id INTEGER)");
            }

            SQLiteOptionsBuilder mainBuilder = new(":memory:");
            mainBuilder.UseEncryptionKey("main-key");
            mainBuilder.LogCommands(log.Add);
            using SQLiteDatabase main = new(mainBuilder.Build());
            main.AttachDatabase(auxPath, "seceaux", "sece-aux-secret");

            Assert.DoesNotContain(log, line => line.Contains("sece-aux-secret", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(auxPath))
            {
                File.Delete(auxPath);
            }
        }
    }
    [Fact]
    public void AttachFailureKeepsTheKeyOutOfTheException()
    {
        using TestDatabase db = new();
        string badPath = Path.Combine(Path.GetTempPath(), $"sece_missing_{Guid.NewGuid():N}", "aux.db3");

        SQLiteException ex = Assert.Throws<SQLiteException>(() => db.AttachDatabase(badPath, "secebad", "sece-hidden-key"));

        Assert.True(ex.Sql == null || !ex.Sql.Contains("sece-hidden-key", StringComparison.Ordinal));
    }
}
#endif
