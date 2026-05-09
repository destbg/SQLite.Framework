using Microsoft.JSInterop;
using SQLite.Framework.Blazor.Models;
using SQLite.Framework.Extensions;
using SQLite.Framework.Generated;

namespace SQLite.Framework.Blazor.Services;

/// <summary>
/// Owns the SQLite database for the app and round-trips the file bytes
/// to <c>localStorage</c> so the data survives a page reload. The DB file
/// itself lives in the WASM in-memory filesystem.
/// </summary>
public sealed class TodoStore : IAsyncDisposable
{
    private const string DatabaseFileName = "todos.db";
    private const string LocalStorageKey = "sqlite-framework-blazor-todos";

    private readonly IJSRuntime js;
    private SQLiteDatabase? database;

    public TodoStore(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<SQLiteDatabase> GetDatabaseAsync()
    {
        if (database != null)
        {
            return database;
        }

        await RestoreFromLocalStorageAsync();

        SQLiteOptions options = new SQLiteOptionsBuilder(DatabaseFileName)
            .UseGeneratedMaterializers()
            .Build();

        database = new SQLiteDatabase(options);
        await database.Table<Todo>().Schema.CreateTableAsync();

        if (await database.Table<Todo>().CountAsync() == 0)
        {
            await SeedAsync(database);
            await PersistAsync();
        }

        return database;
    }

    public async Task PersistAsync()
    {
        if (database == null || !File.Exists(DatabaseFileName))
        {
            return;
        }

        byte[] bytes = await File.ReadAllBytesAsync(DatabaseFileName);
        string base64 = Convert.ToBase64String(bytes);
        await js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, base64);
    }

    public async Task ResetAsync()
    {
        if (database != null)
        {
            database.Dispose();
            database = null;
        }

        if (File.Exists(DatabaseFileName))
        {
            File.Delete(DatabaseFileName);
        }

        await js.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (database != null)
        {
            database.Dispose();
            database = null;
        }

        await Task.CompletedTask;
    }

    private async Task RestoreFromLocalStorageAsync()
    {
        if (File.Exists(DatabaseFileName))
        {
            return;
        }

        string? base64 = await js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
        if (string.IsNullOrEmpty(base64))
        {
            return;
        }

        byte[] bytes = Convert.FromBase64String(base64);
        await File.WriteAllBytesAsync(DatabaseFileName, bytes);
    }

    private static async Task SeedAsync(SQLiteDatabase db)
    {
        DateTime now = DateTime.UtcNow;
        await db.Table<Todo>().AddRangeAsync([
            new Todo { Title = "Try SQLite running in the browser", CreatedAt = now },
            new Todo { Title = "Open DevTools and inspect localStorage", CreatedAt = now, DueBy = DateOnly.FromDateTime(DateTime.Today) },
            new Todo { Title = "Reload the page to see data persist", CreatedAt = now, DueBy = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) }
        ]);
    }
}
