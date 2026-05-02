using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite.Framework.Avalonia.Data;
using SQLite.Framework.Avalonia.Models;

namespace SQLite.Framework.Avalonia.ViewModels;

public partial class ProjectsViewModel : ViewModelBase
{
    private readonly ProjectRepository _projects;
    private readonly TaskRepository _tasks;
    private readonly CategoryRepository _categories;

    public ObservableCollection<ProjectListItem> Items { get; } = [];
    public ObservableCollection<ProjectTask> SelectedTasks { get; } = [];
    public ObservableCollection<Category> AvailableCategories { get; } = [];

    [ObservableProperty]
    private ProjectListItem? _selected;

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ProjectsViewModel(
        ProjectRepository projects,
        TaskRepository tasks,
        CategoryRepository categories)
    {
        _projects = projects;
        _tasks = tasks;
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
            foreach (ProjectListItem p in await _projects.ListWithCategoriesAsync())
            {
                Items.Add(p);
            }

            AvailableCategories.Clear();
            foreach (Category c in await _categories.ListAsync())
            {
                AvailableCategories.Add(c);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedChanged(ProjectListItem? value)
    {
        _ = ReloadTasksAsync();
    }

    private async Task ReloadTasksAsync()
    {
        SelectedTasks.Clear();
        if (Selected is null) return;
        foreach (ProjectTask t in await _tasks.ListForProjectAsync(Selected.Project.Id))
        {
            SelectedTasks.Add(t);
        }
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        string name = NewProjectName.Trim();
        if (name.Length == 0) return;

        Project p = new()
        {
            Name = name,
            Description = string.Empty,
            CategoryId = AvailableCategories.FirstOrDefault()?.Id ?? 0,
        };
        await _projects.SaveAsync(p);

        NewProjectName = string.Empty;
        await LoadAsync();
        Selected = Items.FirstOrDefault(x => x.Project.Id == p.Id);
    }

    [RelayCommand]
    private async Task RemoveProjectAsync(ProjectListItem? item)
    {
        if (item is null) return;
        await _tasks.RemoveByProjectAsync(item.Project.Id);
        await _projects.RemoveAsync(item.Project);
        if (Selected?.Project.Id == item.Project.Id) Selected = null;
        Items.Remove(item);
    }

    [RelayCommand]
    private async Task AddTaskAsync()
    {
        if (Selected is null) return;
        string title = NewTaskTitle.Trim();
        if (title.Length == 0) return;

        ProjectTask task = new()
        {
            Title = title,
            ProjectId = Selected.Project.Id,
        };
        await _tasks.SaveAsync(task);
        NewTaskTitle = string.Empty;
        await ReloadTasksAsync();
    }

    [RelayCommand]
    private async Task RemoveTaskAsync(ProjectTask? task)
    {
        if (task is null) return;
        await _tasks.RemoveAsync(task);
        SelectedTasks.Remove(task);
    }

    [RelayCommand]
    private async Task ToggleTaskAsync(ProjectTask? task)
    {
        if (task is null) return;
        task.IsCompleted = !task.IsCompleted;
        await _tasks.SaveAsync(task);
        await ReloadTasksAsync();
    }
}
