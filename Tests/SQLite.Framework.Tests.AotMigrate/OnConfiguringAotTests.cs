using SQLite.Framework;

namespace SQLite.Framework.Tests.AotMigrate;

public class OnConfiguringAotTests
{
    [Fact]
    public void OptionsConstructor_NotOverridden_DoesNotThrow()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();

        using NoOverrideDatabase db = new(options);

        Assert.False(db.Options.IsWalMode);
    }

    [Fact]
    public void OptionsConstructor_Overridden_AppliesBuilderChanges()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();

        using WalOverrideDatabase db = new(options);

        Assert.True(db.Options.IsWalMode);
    }

    [Fact]
    public void ParameterlessConstructor_NotOverridden_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new UnconfiguredAotDatabase());
    }

    [Fact]
    public void ParameterlessConstructor_Overridden_BuildsFromScratch()
    {
        using SelfConfiguringAotDatabase db = new();

        Assert.Equal(":memory:", db.Options.DatabasePath);
    }

    private sealed class NoOverrideDatabase(SQLiteOptions options) : SQLiteDatabase(options);

    private sealed class WalOverrideDatabase(SQLiteOptions options) : SQLiteDatabase(options)
    {
        protected override void OnConfiguring(SQLiteOptionsBuilder builder)
        {
            builder.UseWalMode();
        }
    }

    private sealed class UnconfiguredAotDatabase : SQLiteDatabase;

    private sealed class SelfConfiguringAotDatabase : SQLiteDatabase
    {
        protected override void OnConfiguring(SQLiteOptionsBuilder builder)
        {
            builder.DatabasePath = ":memory:";
        }
    }
}
