using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite.Framework.Extensions;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.PageModels;

public partial class MainPageModel : ObservableObject, IProjectTaskPageModel
{
    private bool _isNavigatedTo;
    private bool _dataLoaded;
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly AppDatabase _db;
    private readonly ModalErrorHandler _errorHandler;
    private readonly SeedDataService _seedDataService;

    [ObservableProperty]
    private List<CategoryChartData> _todoCategoryData = [];

    [ObservableProperty]
    private List<Brush> _todoCategoryColors = [];

    [ObservableProperty]
    private List<ProjectTask> _tasks = [];

    [ObservableProperty]
    private List<ProjectListItem> _projects = [];

    [ObservableProperty]
    bool _isBusy;

    [ObservableProperty]
    bool _isRefreshing;

    [ObservableProperty]
    private string _today = DateTime.Now.ToString("dddd, MMM d");

    [ObservableProperty]
    private ProjectListItem? selectedProject;

    public bool HasCompletedTasks
        => Tasks?.Any(t => t.IsCompleted) ?? false;

    public MainPageModel(SeedDataService seedDataService, ProjectRepository projectRepository,
        TaskRepository taskRepository, CategoryRepository categoryRepository, AppDatabase db, ModalErrorHandler errorHandler)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _categoryRepository = categoryRepository;
        _db = db;
        _errorHandler = errorHandler;
        _seedDataService = seedDataService;
    }

    private async Task LoadData()
    {
        try
        {
            IsBusy = true;

            Projects = await _projectRepository.ListAsync();

            var taskCountsByCategory = await (
                from t in _db.Tasks
                join p in _db.Projects on t.ProjectId equals p.Id
                group t by p.CategoryId into g
                select new { CategoryId = g.Key, Count = g.Count() }
            ).ToListAsync();

            var chartData = new List<CategoryChartData>();
            var chartColors = new List<Brush>();

            var categories = await _categoryRepository.ListAsync();
            foreach (var category in categories)
            {
                chartColors.Add(category.ColorBrush);

                int tasksCount = taskCountsByCategory
                    .FirstOrDefault(x => x.CategoryId == category.Id)?.Count ?? 0;

                chartData.Add(new(category.Title, tasksCount));
            }

            TodoCategoryData = chartData;
            TodoCategoryColors = chartColors;

            Tasks = await _taskRepository.ListAsync();
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasCompletedTasks));
        }
    }

    private async Task InitData(SeedDataService seedDataService)
    {
        bool isSeeded = Preferences.Default.ContainsKey("is_seeded");

        if (!isSeeded)
        {
            await seedDataService.LoadSeedDataAsync();
        }

        Preferences.Default.Set("is_seeded", true);
        await Refresh();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            await LoadData();
        }
        catch (Exception e)
        {
            _errorHandler.HandleError(e);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void NavigatedTo() =>
        _isNavigatedTo = true;

    [RelayCommand]
    private void NavigatedFrom() =>
        _isNavigatedTo = false;

    [RelayCommand]
    private async Task Appearing()
    {
        if (!_dataLoaded)
        {
            await InitData(_seedDataService);
            _dataLoaded = true;
            await Refresh();
        }
        // This means we are being navigated to
        else if (!_isNavigatedTo)
        {
            await Refresh();
        }
    }

    [RelayCommand]
    private Task TaskCompleted(ProjectTask task)
    {
        OnPropertyChanged(nameof(HasCompletedTasks));
        return _taskRepository.SaveItemAsync(task);
    }

    [RelayCommand]
    private Task AddTask()
        => Shell.Current.GoToAsync($"task");

    [RelayCommand]
    private Task? NavigateToProject(ProjectListItem item)
        => item is null ? null : Shell.Current.GoToAsync($"project?id={item.Project.Id}");

    [RelayCommand]
    private Task NavigateToTask(ProjectTask task)
        => Shell.Current.GoToAsync($"task?id={task.Id}");

    [RelayCommand]
    private async Task CleanTasks()
    {
        var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();
        foreach (var task in completedTasks)
        {
            await _taskRepository.DeleteItemAsync(task);
            Tasks.Remove(task);
        }

        OnPropertyChanged(nameof(HasCompletedTasks));
        Tasks = new(Tasks);
        await AppShell.DisplayToastAsync("All cleaned up!");
    }
}
