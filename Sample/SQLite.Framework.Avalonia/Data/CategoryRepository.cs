using SQLite.Framework.Avalonia.Models;
using SQLite.Framework.Extensions;

namespace SQLite.Framework.Avalonia.Data;

public class CategoryRepository
{
    private readonly AppDatabase _db;

    public CategoryRepository(AppDatabase db)
    {
        _db = db;
    }

    public Task<List<Category>> ListAsync()
    {
        return _db.Categories.OrderBy(c => c.Title).ToListAsync();
    }

    public Task<Category?> GetAsync(int id)
    {
        return _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> SaveAsync(Category item)
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

    public Task<int> RemoveAsync(Category item)
    {
        return _db.Categories.RemoveAsync(item);
    }
}
