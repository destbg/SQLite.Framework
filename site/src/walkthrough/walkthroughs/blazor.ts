import type { Walkthrough } from "./types";

export const blazorWalkthrough: Walkthrough = {
    slug: "blazor",
    title: "Blazor WebAssembly Walkthrough",
    subtitle: "Run SQLite entirely in the browser, with the data persisted to localStorage",
    steps: [
        {
            title: "What you will build",
            description:
                "A Blazor WebAssembly app that ships SQLite inside the WASM bundle, holds the database in the in-memory filesystem, and round-trips the file bytes to localStorage so the data survives a page reload. No backend required.",
        },
        {
            title: "Create the project",
            description:
                "A vanilla Blazor WebAssembly template. The bundled SQLite provider is what makes this work in the browser.",
            code: {
                language: "bash",
                text: `dotnet new blazorwasm -n MyBlazor
cd MyBlazor
dotnet add package SQLite.Framework.Bundled`,
            },
        },
        {
            title: "Define your model",
            description:
                "A small Todo type is enough to see the round trip.",
            code: {
                language: "csharp",
                filename: "Models/Todo.cs",
                text: `using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

public class Todo
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Required]
    public required string Title { get; set; }

    public bool IsComplete { get; set; }
}`,
            },
        },
        {
            title: "Own the database in one service",
            description:
                "TodoStore lazily opens the database on first use. The file path is a name inside the WASM in-memory filesystem, so it lives only for the page lifetime.",
            code: {
                language: "csharp",
                filename: "Services/TodoStore.cs",
                text: `using Microsoft.JSInterop;
using SQLite.Framework;

public sealed class TodoStore
{
    private const string FileName = "todos.db";
    private const string StorageKey = "blazor-todos";

    private readonly IJSRuntime js;
    private SQLiteDatabase? db;

    public TodoStore(IJSRuntime js)
    {
        this.js = js;
    }
}`,
            },
        },
        {
            title: "Restore from localStorage",
            description:
                "On first access, pull the base64 bytes out of localStorage and write them to the in-memory filesystem at the path SQLite expects. If nothing was stored yet, SQLite just creates a fresh file.",
            code: {
                language: "csharp",
                filename: "Services/TodoStore.cs",
                text: `public async Task<SQLiteDatabase> GetAsync()
{
    if (db != null) return db;

    string? cached = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
    if (!string.IsNullOrEmpty(cached))
    {
        byte[] bytes = Convert.FromBase64String(cached);
        await File.WriteAllBytesAsync(FileName, bytes);
    }

    SQLiteOptions options = new SQLiteOptionsBuilder(FileName).Build();
    db = new SQLiteDatabase(options);
    await db.Schema.CreateTableAsync<Todo>();
    return db;
}`,
            },
        },
        {
            title: "Persist after every change",
            description:
                "Read the file bytes back out and stash them as base64. Call this after every insert, update, or delete.",
            code: {
                language: "csharp",
                filename: "Services/TodoStore.cs",
                text: `public async Task PersistAsync()
{
    if (db == null || !File.Exists(FileName)) return;

    byte[] bytes = await File.ReadAllBytesAsync(FileName);
    string base64 = Convert.ToBase64String(bytes);
    await js.InvokeVoidAsync("localStorage.setItem", StorageKey, base64);
}`,
            },
        },
        {
            title: "Expose the operations",
            description:
                "Wrap the database calls in small methods so the UI never touches the SQLiteDatabase directly.",
            code: {
                language: "csharp",
                filename: "Services/TodoStore.cs",
                text: `public async Task<List<Todo>> ListAsync()
{
    SQLiteDatabase db = await GetAsync();
    return await db.Table<Todo>()
        .OrderBy(t => t.Id)
        .ToListAsync();
}

public async Task AddAsync(string title)
{
    SQLiteDatabase db = await GetAsync();
    await db.Table<Todo>().AddAsync(new Todo { Title = title });
    await PersistAsync();
}

public async Task ToggleAsync(int id)
{
    SQLiteDatabase db = await GetAsync();
    await db.Table<Todo>()
        .Where(t => t.Id == id)
        .ExecuteUpdateAsync(s => s.Set(t => t.IsComplete, t => !t.IsComplete));
    await PersistAsync();
}`,
            },
        },
        {
            title: "Register the store",
            description:
                "A single scoped registration. Scoped on a Blazor WebAssembly app means one instance per browser tab.",
            code: {
                language: "csharp",
                filename: "Program.cs",
                text: `using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});
builder.Services.AddSingleton<TodoStore>();

await builder.Build().RunAsync();`,
            },
        },
        {
            title: "Use it from a page",
            description:
                "Inject the store, load on initialise, render the list. Each action ends with a refresh.",
            code: {
                language: "csharp",
                filename: "Pages/Home.razor",
                text: `@page "/"
@inject TodoStore Store

<h1>Todos</h1>

<input @bind="newTitle" placeholder="What needs doing?" />
<button @onclick="AddAsync">Add</button>

<ul>
    @foreach (Todo t in todos)
    {
        <li @onclick="() => ToggleAsync(t.Id)">
            <span style="text-decoration:@(t.IsComplete ? "line-through" : "none")">
                @t.Title
            </span>
        </li>
    }
</ul>

@code {
    private List<Todo> todos = new();
    private string newTitle = "";

    protected override async Task OnInitializedAsync()
        => todos = await Store.ListAsync();

    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return;
        await Store.AddAsync(newTitle);
        newTitle = "";
        todos = await Store.ListAsync();
    }

    private async Task ToggleAsync(int id)
    {
        await Store.ToggleAsync(id);
        todos = await Store.ListAsync();
    }
}`,
            },
        },
        {
            title: "Trade-offs to know",
            description:
                "localStorage caps at around 5 MB per origin, so this approach fits small datasets. For larger stores, swap localStorage for IndexedDB. The in-memory filesystem is per page load, so PersistAsync after every write is non-negotiable.",
        },
        {
            title: "You are done",
            description:
                "Reload the page. Your todos are still there. The database lives entirely client-side, ships in the WASM bundle, and survives reloads thanks to one base64 round trip per write.",
        },
    ],
};
