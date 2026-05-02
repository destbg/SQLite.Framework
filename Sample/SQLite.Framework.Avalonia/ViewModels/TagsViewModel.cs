using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite.Framework.Avalonia.Data;
using SQLite.Framework.Avalonia.Models;

namespace SQLite.Framework.Avalonia.ViewModels;

public partial class TagsViewModel : ViewModelBase
{
    private readonly TagRepository _tags;

    public ObservableCollection<Tag> Items { get; } = [];

    [ObservableProperty]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    private string _newColor = "#FF9900";

    [ObservableProperty]
    private bool _isLoading;

    public TagsViewModel(TagRepository tags)
    {
        _tags = tags;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            Items.Clear();
            foreach (Tag t in await _tags.ListAsync())
            {
                Items.Add(t);
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
        Tag t = new() { Title = title, Color = NewColor };
        await _tags.SaveAsync(t);
        NewTitle = string.Empty;
        Items.Add(t);
    }

    [RelayCommand]
    private async Task RemoveAsync(Tag? tag)
    {
        if (tag is null) return;
        await _tags.RemoveAsync(tag);
        Items.Remove(tag);
    }
}
