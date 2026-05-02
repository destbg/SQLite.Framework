using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite.Framework.Maui.Data;
using SQLite.Framework.Maui.Models;
using SQLite.Framework.Maui.Services;

namespace SQLite.Framework.Maui.PageModels;

public partial class ProjectListPageModel : ObservableObject
{
    private readonly ProjectRepository _projectRepository;

    [ObservableProperty]
    private List<ProjectListItem> _projects = [];

    [ObservableProperty]
    private ProjectListItem? selectedProject;

    public ProjectListPageModel(ProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    [RelayCommand]
    private async Task Appearing()
    {
        Projects = await _projectRepository.ListAsync();
    }

    [RelayCommand]
    Task? NavigateToProject(ProjectListItem item)
        => item is null ? Task.CompletedTask : Shell.Current.GoToAsync($"project?id={item.Project.Id}");

    [RelayCommand]
    async Task AddProject()
    {
        await Shell.Current.GoToAsync($"project");
    }
}
