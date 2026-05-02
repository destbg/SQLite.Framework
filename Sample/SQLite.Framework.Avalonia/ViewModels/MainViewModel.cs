using CommunityToolkit.Mvvm.ComponentModel;

namespace SQLite.Framework.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ProjectsViewModel Projects { get; }
    public CategoriesViewModel Categories { get; }
    public TagsViewModel Tags { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainViewModel(
        ProjectsViewModel projects,
        CategoriesViewModel categories,
        TagsViewModel tags)
    {
        Projects = projects;
        Categories = categories;
        Tags = tags;
    }
}
