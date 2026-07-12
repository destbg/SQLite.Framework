using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PlanExecRows")]
public class PlanExecutedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationPlanExecutedOperationParityTests
{
    [Fact]
    public void PlanListsExactlyTheOperationsMigrateExecutes()
    {
        using TestDatabase db = new(useFile: true);
        List<string> executed = [];

        SQLiteMigrationRunner Build() => db.Schema.Migrations()
            .Progress(p => executed.Add(p.Description))
            .Version(1, m => m
                .CreateTable<PlanExecutedRow>()
                .Insert(new PlanExecutedRow { Id = 1, Name = "old" }))
            .Version(2, m => m.DropTable<PlanExecutedRow>())
            .Version(3, m => m.CreateTable<PlanExecutedRow>());

        SQLiteMigrationPlan plan = Build().Plan();
        Build().Migrate();

        Assert.Equal(
            executed.OrderBy(s => s, StringComparer.Ordinal),
            plan.Operations.OrderBy(s => s, StringComparer.Ordinal));
    }
}
