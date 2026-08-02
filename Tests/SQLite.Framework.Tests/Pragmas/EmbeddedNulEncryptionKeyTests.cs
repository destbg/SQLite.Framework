#if SQLITECIPHER
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EmbeddedNulEncryptionKeyTests
{
    [Fact]
    public void KeyWithEmbeddedNulDoesNotSilentlyCreateAPlaintextDatabase()
    {
        string path = Path.Combine(Path.GetTempPath(), $"sece_nul_{Guid.NewGuid():N}.db3");
        try
        {
            try
            {
                SQLiteOptionsBuilder builder = new(path);
                builder.UseEncryptionKey("sece-key\0-truncated-tail");
                using SQLiteDatabase db = new(builder.Build());
                db.Execute("CREATE TABLE T (Id INTEGER)");
                db.Execute("INSERT INTO T (Id) VALUES (1)");
            }
            catch
            {
            }

            SQLiteOptionsBuilder plainBuilder = new(path);
            using SQLiteDatabase plain = new(plainBuilder.Build());
            Assert.ThrowsAny<Exception>(() => plain.ExecuteScalar<long>("SELECT COUNT(*) FROM T"));
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
    public void RekeyWithEmbeddedNulIsRejected()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"SecENulRekey\" (\"Id\" INTEGER)");

        Assert.Throws<ArgumentException>(() => db.Pragmas.Rekey("sece-key\0tail"));
    }

    [Fact]
    public void AttachWithEmbeddedNulKeyIsRejected()
    {
        using TestDatabase db = new(useFile: true);
        string auxPath = Path.Combine(Path.GetTempPath(), $"sece_nul_attach_{Guid.NewGuid():N}.db3");
        try
        {
            SQLiteOptionsBuilder auxBuilder = new(auxPath);
            auxBuilder.UseEncryptionKey("aux-key");
            using (SQLiteDatabase aux = new(auxBuilder.Build()))
            {
                aux.Execute("CREATE TABLE A (Id INTEGER)");
            }

            Assert.Throws<ArgumentException>(() => db.AttachDatabase(auxPath, "secenulaux", "sece-key\0tail"));
        }
        finally
        {
            if (File.Exists(auxPath))
            {
                File.Delete(auxPath);
            }
        }
    }
}
#endif
