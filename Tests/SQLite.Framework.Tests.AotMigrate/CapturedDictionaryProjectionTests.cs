using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Generated;

namespace SQLite.Framework.Tests.AotMigrate;

public sealed class QueuedMessageRow
{
    [Key]
    public int Id { get; set; }
    public required string ProjectId { get; set; }
    public required int WorkflowId { get; set; }
    public string? AdoEntityId { get; set; }
    public required DateTime ScheduledFor { get; set; }
    public required DateTime CreatedAt { get; set; }
    public string? LastError { get; set; }
    public int RetryCount { get; set; }
}

public sealed class QueuedItemDTO
{
    public int Id { get; set; }
    public required string WorkflowName { get; set; }
    public string? AdoEntityId { get; set; }
    public DateTime ScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Reason { get; set; }
    public int RetryCount { get; set; }
}

public class CapturedDictionaryProjectionTests
{
    [Fact]
    public async Task Select_CapturedDictionaryIntoDto_ReturnsOrderedQueue()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.UseGeneratedMaterializers();
        builder.DisableReflectionFallback();
        using SQLiteDatabase db = new(builder.Build());
        SQLiteTable<QueuedMessageRow> messages = db.Table<QueuedMessageRow>();
        messages.Schema.CreateTable();
        DateTime createdAt = new(2026, 11, 16, 10, 0, 0, DateTimeKind.Utc);
        messages.Add(new QueuedMessageRow
        {
            Id = 1, ProjectId = "project-1", WorkflowId = 10, AdoEntityId = "123",
            CreatedAt = createdAt, ScheduledFor = createdAt.AddMinutes(2), LastError = "Retry later", RetryCount = 2,
        });
        messages.Add(new QueuedMessageRow
        {
            Id = 2, ProjectId = "project-1", WorkflowId = 20,
            CreatedAt = createdAt, ScheduledFor = createdAt.AddMinutes(1),
        });
        messages.Add(new QueuedMessageRow
        {
            Id = 3, ProjectId = "other-project", WorkflowId = 10,
            CreatedAt = createdAt, ScheduledFor = createdAt,
        });
        Dictionary<int, string> workflowNames = new() { [10] = "Review" };
        string projectId = "project-1";

        List<QueuedItemDTO> queued = await messages
            .Where(q => q.ProjectId == projectId)
            .OrderBy(q => q.ScheduledFor)
            .Select(q => new QueuedItemDTO
            {
                Id = q.Id,
                WorkflowName = workflowNames.GetValueOrDefault(q.WorkflowId, "Unknown"),
                AdoEntityId = q.AdoEntityId,
                ScheduledFor = q.ScheduledFor,
                CreatedAt = q.CreatedAt,
                Reason = q.LastError,
                RetryCount = q.RetryCount,
            })
            .ToListAsync(ct: TestContext.Current.CancellationToken);

        Assert.Collection(queued,
            item =>
            {
                Assert.Equal(2, item.Id);
                Assert.Equal("Unknown", item.WorkflowName);
                Assert.Null(item.AdoEntityId);
                Assert.Equal(createdAt.AddMinutes(1), item.ScheduledFor);
                Assert.Equal(createdAt, item.CreatedAt);
                Assert.Null(item.Reason);
                Assert.Equal(0, item.RetryCount);
            },
            item =>
            {
                Assert.Equal(1, item.Id);
                Assert.Equal("Review", item.WorkflowName);
                Assert.Equal("123", item.AdoEntityId);
                Assert.Equal(createdAt.AddMinutes(2), item.ScheduledFor);
                Assert.Equal(createdAt, item.CreatedAt);
                Assert.Equal("Retry later", item.Reason);
                Assert.Equal(2, item.RetryCount);
            });
        Assert.True(db.SelectMaterializerHits > 0);
        Assert.Equal(0, db.SelectCompilerFallbacks);
    }
}
