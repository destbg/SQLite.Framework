using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite.Framework.Avalonia.Data;
using SQLite.Framework.Avalonia.Models;

namespace SQLite.Framework.Avalonia.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    private readonly CategoryRepository _categories;

    public ObservableCollection<Category> Items { get; } = [];

    [ObservableProperty]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    private string _newColor = "#3068DF";

    [ObservableProperty]
    private bool _isLoading;

    public CategoriesViewModel(CategoryRepository categories)
    {
        _categories = categories;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            Items.Clear();
            foreach (Category c in await _categories.ListAsync())
            {
                Items.Add(c);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        string title = NewTitle.Trim();
        if (title.Length == 0) return;
        Category c = new() { Title = title, Color = NewColor };
        await _categories.SaveAsync(c);
        NewTitle = string.Empty;
        Items.Add(c);
    }

    [RelayCommand]
    private async Task RemoveAsync(Category? category)
    {
        if (category is null) return;
        await _categories.RemoveAsync(category);
        Items.Remove(category);
    }
}
