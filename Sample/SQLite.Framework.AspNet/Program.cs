using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.DependencyInjection;
using SQLite.Framework.Extensions;
using SQLite.Framework.Generated;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddOpenApi();

builder.Services.AddSQLiteDatabase(b =>
{
    b.DatabasePath = Path.Combine(AppContext.BaseDirectory, "todos.db");
    b.UseWalMode()
        .DisableReflectionFallback()
        .UseGeneratedMaterializers();
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (IServiceScope scope = app.Services.CreateScope())
{
    SQLiteDatabase db = scope.ServiceProvider.GetRequiredService<SQLiteDatabase>();
    await db.Table<Todo>().Schema.CreateTableAsync();

    if (await db.Table<Todo>().CountAsync() == 0)
    {
        await db.Table<Todo>().AddRangeAsync([
            new Todo { Title = "Walk the dog" },
            new Todo { Title = "Do the dishes", DueBy = DateOnly.FromDateTime(DateTime.Now) },
            new Todo { Title = "Do the laundry", DueBy = DateOnly.FromDateTime(DateTime.Now.AddDays(1)) },
            new Todo { Title = "Clean the bathroom" },
            new Todo { Title = "Clean the car", DueBy = DateOnly.FromDateTime(DateTime.Now.AddDays(2)) }
        ]);
    }
}

RouteGroupBuilder todosApi = app.MapGroup("/todos");

todosApi.MapGet("/", async (SQLiteDatabase db, CancellationToken ct) =>
        await db.Table<Todo>().OrderBy(t => t.Id).ToListAsync(ct))
    .WithName("GetTodos");

todosApi.MapGet("/{id:int}", async Task<Results<Ok<Todo>, NotFound>> (SQLiteDatabase db, int id, CancellationToken ct) =>
    {
        Todo? todo = await db.Table<Todo>().FirstOrDefaultAsync(t => t.Id == id, ct);
        return todo is null ? TypedResults.NotFound() : TypedResults.Ok(todo);
    })
    .WithName("GetTodoById");

todosApi.MapPost("/", async Task<Created<Todo>> (SQLiteDatabase db, Todo todo, CancellationToken ct) =>
    {
        todo.Id = 0;
        await db.Table<Todo>().AddAsync(todo, ct);
        return TypedResults.Created($"/todos/{todo.Id}", todo);
    })
    .WithName("CreateTodo");

todosApi.MapPut("/{id:int}", async Task<Results<NoContent, NotFound>> (SQLiteDatabase db, int id, Todo update, CancellationToken ct) =>
    {
        int rows = await db.Table<Todo>()
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .Set(t => t.Title, update.Title)
                .Set(t => t.DueBy, update.DueBy)
                .Set(t => t.IsComplete, update.IsComplete), ct);

        return rows == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    })
    .WithName("UpdateTodo");

todosApi.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (SQLiteDatabase db, int id, CancellationToken ct) =>
    {
        int rows = await db.Table<Todo>()
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync(ct);

        return rows == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    })
    .WithName("DeleteTodo");

await app.RunAsync();

[Table("Todos")]
public class Todo
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Title { get; set; }

    public DateOnly? DueBy { get; set; }

    public bool IsComplete { get; set; }
}

[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(List<Todo>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
