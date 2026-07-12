using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WalCheckpointBlockedReaderParityTests
{
    [Fact]
    public void PassiveCheckpointBlockedByReaderReportsNotFullyCheckpointed()
    {
        using TestDatabase main = new(b => b.UseWalMode(), useFile: true);
        main.Execute("CREATE TABLE \"CkpRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER NOT NULL)");
        main.Execute("INSERT INTO \"CkpRows\" (\"Id\", \"Value\") VALUES (1, 1)");

        using SQLiteDatabase reader = OpenSecondConnection(main.Options.DatabasePath);
        using (SQLiteTransaction snapshot = reader.BeginTransaction())
        {
            long seen = reader.ExecuteScalar<long>("SELECT COUNT(*) FROM \"CkpRows\"");
            Assert.Equal(1L, seen);

            for (int i = 2; i <= 40; i++)
            {
                main.Execute($"INSERT INTO \"CkpRows\" (\"Id\", \"Value\") VALUES ({i}, {i})");
            }

            Assert.False(main.Pragmas.WalCheckpoint(SQLiteWalCheckpointMode.Full));

            bool passive = main.Pragmas.WalCheckpoint(SQLiteWalCheckpointMode.Passive);

            Dictionary<string, object?> state = main.Query<Dictionary<string, object?>>("PRAGMA wal_checkpoint(PASSIVE)").First();
            long log = Convert.ToInt64(state["log"]);
            long checkpointed = Convert.ToInt64(state["checkpointed"]);
            Assert.True(checkpointed < log);

            Assert.False(passive);

            snapshot.Rollback();
        }

        Assert.True(main.Pragmas.WalCheckpoint(SQLiteWalCheckpointMode.Passive));
    }

    private static SQLiteDatabase OpenSecondConnection(string path)
    {
        SQLiteOptionsBuilder builder = new(path);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        return new SQLiteDatabase(builder.Build());
    }
}
