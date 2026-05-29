import type { Walkthrough } from "./types";

export const aspnetWalkthrough: Walkthrough = {
    slug: "aspnet",
    title: "ASP.NET Per-User Walkthrough",
    subtitle: "Give every signed-in user their own SQLite file in a minimal-API service",
    steps: [
        {
            title: "What you will build",
            description:
                "A minimal-API ASP.NET service where each user owns a private SQLite file. The framework opens and migrates that file on first use and serves the request from it. No shared write contention, no schema-per-tenant gymnastics.",
        },
        {
            title: "Create the project",
            description:
                "A bare minimal API is enough. Add JWT auth (or any other scheme) so each request carries a user identity to key the database off of.",
            code: {
                language: "bash",
                text: `dotnet new webapi -n MyApi --use-minimal-apis
cd MyApi
dotnet add package SQLite.Framework
dotnet add package SQLite.Framework.DependencyInjection
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer`,
            },
        },
        {
            title: "Define a per-user model",
            description:
                "Every row lives inside the user's own file, so you do not need a UserId column. The schema stays small.",
            code: {
                language: "csharp",
                filename: "Models/Note.cs",
                text: `using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

public class Note
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Required]
    public required string Title { get; set; }

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}`,
            },
        },
        {
            title: "A factory keyed by user id",
            description:
                "Resolve the path from the user id, build SQLiteOptions on demand, and cache the database per user. The cache lives for the lifetime of the process, one connection per user.",
            code: {
                language: "csharp",
                filename: "Data/UserDatabaseFactory.cs",
                text: `using System.Collections.Concurrent;
using SQLite.Framework;

public sealed class UserDatabaseFactory
{
    private readonly string rootDir;
    private readonly ConcurrentDictionary<string, SQLiteDatabase> cache = new();

    public UserDatabaseFactory(IConfiguration config)
    {
        rootDir = config.GetValue("Storage:UserDataDir", "user-data")!;
        Directory.CreateDirectory(rootDir);
    }

    public SQLiteDatabase GetFor(string userId)
    {
        return cache.GetOrAdd(userId, id =>
        {
            string safeId = SanitiseId(id);
            string path = Path.Combine(rootDir, $"{safeId}.db");
            SQLiteOptions options = new SQLiteOptionsBuilder(path)
                .UseWalMode()
                .Build();
            return new SQLiteDatabase(options);
        });
    }

    private static string SanitiseId(string id)
    {
        return string.Concat(id.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
    }
}`,
            },
        },
        {
            title: "Wrap it in a scoped accessor",
            description:
                "The accessor reads the user id from HttpContext and hands back the right database. It also runs migrations once per user so first access never sees an empty schema.",
            code: {
                language: "csharp",
                filename: "Data/UserDatabaseAccessor.cs",
                text: `using System.Collections.Concurrent;
using System.Security.Claims;
using SQLite.Framework;

public sealed class UserDatabaseAccessor
{
    private static readonly ConcurrentDictionary<string, Task> migrated = new();

    private readonly UserDatabaseFactory factory;
    private readonly IHttpContextAccessor http;

    public UserDatabaseAccessor(UserDatabaseFactory factory, IHttpContextAccessor http)
    {
        this.factory = factory;
        this.http = http;
    }

    public async Task<SQLiteDatabase> GetAsync()
    {
        string userId = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("No user id on the request.");

        SQLiteDatabase db = factory.GetFor(userId);
        await migrated.GetOrAdd(userId, _ => MigrateAsync(db));
        return db;
    }

    private static async Task MigrateAsync(SQLiteDatabase db)
    {
        int version = await db.Pragmas.GetUserVersionAsync();
        if (version < 1)
        {
            await db.Schema.CreateTableAsync<Note>();
            await db.Pragmas.SetUserVersionAsync(1);
        }
    }
}`,
            },
        },
        {
            title: "Register the accessor",
            description:
                "The factory is a singleton (it owns the per-user cache). The accessor is scoped because it reads HttpContext. No global SQLiteDatabase registration is needed.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UserDatabaseFactory>();
builder.Services.AddScoped<UserDatabaseAccessor>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();`,
            },
        },
        {
            title: "Query inside a request",
            description:
                "Every endpoint asks the accessor for the current user's database. Each call hits the right file with no extra plumbing.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `var notes = app.MapGroup("/notes").RequireAuthorization();

notes.MapGet("/", async (UserDatabaseAccessor dbs, CancellationToken ct) =>
{
    SQLiteDatabase db = await dbs.GetAsync();
    return await db.Table<Note>()
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync(ct);
});

notes.MapPost("/", async (UserDatabaseAccessor dbs, Note note, CancellationToken ct) =>
{
    SQLiteDatabase db = await dbs.GetAsync();
    note.Id = 0;
    note.CreatedAt = DateTime.UtcNow;
    await db.Table<Note>().AddAsync(note, ct);
    return Results.Created($"/notes/{note.Id}", note);
});

notes.MapDelete("/{id:int}", async (UserDatabaseAccessor dbs, int id, CancellationToken ct) =>
{
    SQLiteDatabase db = await dbs.GetAsync();
    int rows = await db.Table<Note>()
        .Where(n => n.Id == id)
        .ExecuteDeleteAsync(ct);
    return rows == 0 ? Results.NotFound() : Results.NoContent();
});`,
            },
        },
        {
            title: "Why this works under load",
            description:
                "SQLite handles a single writer per file just fine. Spreading writers across many files removes contention entirely. WAL mode (enabled in the factory) lets each user's reads run concurrently with that user's writes.",
        },
        {
            title: "Disposing per-user databases",
            description:
                "The cache holds connections forever by default. If you expect many inactive users, evict idle entries on a timer and call Dispose() so the file handle is released.",
            code: {
                language: "csharp",
                filename: "Data/UserDatabaseFactory.cs",
                text: `public bool Evict(string userId)
{
    if (!cache.TryRemove(userId, out SQLiteDatabase? db)) return false;
    db.Dispose();
    return true;
}

public IReadOnlyCollection<string> ActiveUsers => cache.Keys.ToArray();`,
            },
        },
        {
            title: "Backups and exports",
            description:
                "Because each user is one file, backup is a file copy. The Backup API streams a snapshot you can hand back as a download.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `notes.MapGet("/export", async (UserDatabaseAccessor dbs) =>
{
    SQLiteDatabase db = await dbs.GetAsync();
    string tmp = Path.GetTempFileName();
    await db.BackupAsync(tmp);
    return Results.File(tmp, "application/x-sqlite3", "notes.db");
});`,
            },
        },
        {
            title: "You are done",
            description:
                "Each authenticated request now reads and writes its own SQLite file. To enforce limits per user, swap the factory's cache for a size-bounded one. To run the schema in parallel for many users, fan-out the migration loop on a background worker.",
        },
    ],
};
