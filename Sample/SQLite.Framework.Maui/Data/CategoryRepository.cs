using SQLite.Framework.Extensions;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

/// <summary>
/// Repository for categories.
/// </summary>
public class CategoryRepository
{
    private readonly AppDatabase _db;

    public CategoryRepository(AppDatabase db)
    {
        _db = db;
    }

    public Task<List<Category>> ListAsync()
    {
        return _db.Categories.ToListAsync();
    }

    public Task<Category?> GetAsync(int id)
    {
        return _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> SaveItemAsync(Category item)
    {
        if (item.Id == 0)
        {
            await _db.Categories.AddAsync(item);
        }
        else
        {
            await _db.Categories.UpdateAsync(item);
        }

        return item.Id;
    }

    public async Task<int> DeleteItemAsync(Category item)
    {
        int affected = await _db.Categories.RemoveAsync(item);
        return affected;
    }

    public async Task DropTableAsync()
    {
        await _db.Schema.DropTableAsync<Category>();
        await _db.Schema.CreateTableAsync<Category>();
    }
}