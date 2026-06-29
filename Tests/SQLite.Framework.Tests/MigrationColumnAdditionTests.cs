using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MigAgentSessions")]
public class MigAgentSession
{
    [Key]
    public required string Id { get; set; }
    public required string Directory { get; set; }
    public required string Model { get; set; }
    public string? Backend { get; set; }
    public string? Title { get; set; }
    public int TurnActive { get; set; }
    public required DateTime CreatedAt { get; set; }
    public long PromptTokens { get; set; }
}

[Table("MigAgentMessages")]
public class MigAgentMessage
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public required string SessionId { get; set; }
    public required string Role { get; set; }
    public string? SenderName { get; set; }
    public long PromptTokens { get; set; }
    public required DateTime CreatedAt { get; set; }
}

[Table("MigWorkflowExecutions")]
public class MigWorkflowExecution
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }
    public int WorkflowId { get; set; }
    public string? CheckpointJson { get; set; }
}

[Table("MigModelAliases")]
public class MigModelAlias
{
    [Key]
    public required string Alias { get; set; }
    public required string Backend { get; set; }
}

public class MigrationColumnAdditionTests
{
    private static void MigrateAgent(TestDatabase db) =>
        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<MigAgentSession>()
                .CreateTable<MigAgentMessage>())
            .Version(6, m => m
                .TableChanged<MigAgentMessage>()
                .TableChanged<MigAgentSession>(b => b.Set(s => s.TurnActive, 0)))
            .Migrate();

    private static void MigrateProject(TestDatabase db) =>
        db.Schema.Migrations()
            .Version(6, m => m.CreateTable<MigWorkflowExecution>())
            .Version(8, m => m.TableChanged<MigWorkflowExecution>())
            .Migrate();

    private static void MigrateMain(TestDatabase db) =>
        db.Schema.Migrations()
            .Version(2, m => m.CreateTable<MigModelAlias>())
            .Version(3, m => m.TableChanged<MigModelAlias>(b => b.Set(m => m.Backend, "ollama")))
            .Migrate();

    [Fact]
    public void AgentMigration_FreshDatabase_HasEveryModelColumn()
    {
        using TestDatabase db = new(useFile: true);
        MigrateAgent(db);

        List<string> session = db.Schema.ListColumns<MigAgentSession>().Select(c => c.Name).ToList();
        Assert.Contains("TurnActive", session);
        Assert.Contains("Backend", session);
        Assert.Contains("PromptTokens", session);
        Assert.Contains(db.Schema.ListColumns<MigAgentMessage>(), c => c.Name == "SenderName");

        db.Table<MigAgentSession>().Add(new MigAgentSession { Id = "s", Directory = "/", Model = "m", CreatedAt = DateTime.UtcNow });
        Assert.Equal(0, db.Table<MigAgentSession>().Single().TurnActive);
        Assert.Equal(6, db.Pragmas.UserVersion);
    }

    [Fact]
    public void AgentMigration_UpgradeFromOldSchema_BackfillsFilledColumn()
    {
        using TestDatabase db = new(useFile: true);
        MigrateAgent(db);
        db.Table<MigAgentSession>().Add(new MigAgentSession { Id = "old", Directory = "/", Model = "m", TurnActive = 7, CreatedAt = DateTime.UtcNow });

        db.Execute("ALTER TABLE \"MigAgentSessions\" DROP COLUMN \"TurnActive\"");
        db.Pragmas.UserVersion = 5;

        MigrateAgent(db);

        Assert.Contains(db.Schema.ListColumns<MigAgentSession>(), c => c.Name == "TurnActive");
        Assert.Equal(0, db.Table<MigAgentSession>().Single(s => s.Id == "old").TurnActive);
        Assert.Equal(6, db.Pragmas.UserVersion);
    }

    [Fact]
    public void AgentMigration_UpgradeFromOldSchema_AddsNullableColumnInPlace()
    {
        using TestDatabase db = new(useFile: true);
        MigrateAgent(db);

        db.Execute("ALTER TABLE \"MigAgentMessages\" DROP COLUMN \"SenderName\"");
        db.Pragmas.UserVersion = 5;
        Assert.DoesNotContain(db.Schema.ListColumns<MigAgentMessage>(), c => c.Name == "SenderName");

        MigrateAgent(db);

        Assert.Contains(db.Schema.ListColumns<MigAgentMessage>(), c => c.Name == "SenderName");
    }

    [Fact]
    public void AgentMigration_UpgradeAddsNotNullColumnToRows_ThrowsWithColumnName()
    {
        using TestDatabase db = new(useFile: true);
        MigrateAgent(db);
        db.Table<MigAgentMessage>().Add(new MigAgentMessage { SessionId = "s", Role = "user", PromptTokens = 5, CreatedAt = DateTime.UtcNow });

        db.Execute("ALTER TABLE \"MigAgentMessages\" DROP COLUMN \"PromptTokens\"");
        db.Pragmas.UserVersion = 5;

        Exception ex = Assert.ThrowsAny<Exception>(() => MigrateAgent(db));
        Assert.Contains("PromptTokens", ex.Message);
    }

    [Fact]
    public void ProjectMigration_UpgradeReaddsDroppedColumn()
    {
        using TestDatabase db = new(useFile: true);
        MigrateProject(db);

        db.Execute("ALTER TABLE \"MigWorkflowExecutions\" DROP COLUMN \"CheckpointJson\"");
        db.Pragmas.UserVersion = 7;
        Assert.DoesNotContain(db.Schema.ListColumns<MigWorkflowExecution>(), c => c.Name == "CheckpointJson");

        MigrateProject(db);

        Assert.Contains(db.Schema.ListColumns<MigWorkflowExecution>(), c => c.Name == "CheckpointJson");
        Assert.True(db.Pragmas.UserVersion >= 8);
    }

    [Fact]
    public void AgentMigration_UpgradeWithFill_ReaddsEveryMissingColumn()
    {
        using TestDatabase db = new(useFile: true);
        MigrateAgent(db);
        db.Table<MigAgentSession>().Add(new MigAgentSession { Id = "old", Directory = "/", Model = "m", Backend = "x", Title = "y", TurnActive = 7, CreatedAt = DateTime.UtcNow });

        db.Execute("ALTER TABLE \"MigAgentSessions\" DROP COLUMN \"TurnActive\"");
        db.Execute("ALTER TABLE \"MigAgentSessions\" DROP COLUMN \"Backend\"");
        db.Execute("ALTER TABLE \"MigAgentSessions\" DROP COLUMN \"Title\"");
        db.Pragmas.UserVersion = 5;

        MigrateAgent(db);

        List<string> columns = db.Schema.ListColumns<MigAgentSession>().Select(c => c.Name).ToList();
        Assert.Contains("TurnActive", columns);
        Assert.Contains("Backend", columns);
        Assert.Contains("Title", columns);

        MigAgentSession row = db.Table<MigAgentSession>().Single(s => s.Id == "old");
        Assert.Equal(0, row.TurnActive);
        Assert.Null(row.Backend);
        Assert.Null(row.Title);
    }

    [Fact]
    public void AgentMigration_UnfilledNotNullColumnWithRows_RollsBackWholeMigration()
    {
        using TestDatabase db = new(useFile: true);
        MigrateAgent(db);
        db.Table<MigAgentMessage>().Add(new MigAgentMessage { SessionId = "s", Role = "user", PromptTokens = 5, CreatedAt = DateTime.UtcNow });

        db.Execute("ALTER TABLE \"MigAgentMessages\" DROP COLUMN \"PromptTokens\"");
        db.Execute("ALTER TABLE \"MigAgentSessions\" DROP COLUMN \"Backend\"");
        db.Pragmas.UserVersion = 5;

        Assert.ThrowsAny<Exception>(() => MigrateAgent(db));

        Assert.DoesNotContain(db.Schema.ListColumns<MigAgentSession>(), c => c.Name == "Backend");
        Assert.Equal(5, db.Pragmas.UserVersion);
    }

    [Fact]
    public void MainMigration_UpgradeBackfillsColumnWithLiteral()
    {
        using TestDatabase db = new(useFile: true);
        MigrateMain(db);
        db.Table<MigModelAlias>().Add(new MigModelAlias { Alias = "gpt", Backend = "openai" });

        db.Execute("ALTER TABLE \"MigModelAliases\" DROP COLUMN \"Backend\"");
        db.Pragmas.UserVersion = 2;

        MigrateMain(db);

        Assert.Equal("ollama", db.Table<MigModelAlias>().Single().Backend);
    }
}
