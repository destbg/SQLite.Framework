using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.PageModels;

public partial class ProjectDetailPageModel : ObservableObject, IQueryAttributable, IProjectTaskPageModel
{
    private ProjectDetail? _project;
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly TagRepository _tagRepository;
    private readonly ModalErrorHandler _errorHandler;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private List<ProjectTask> _tasks = [];

    [ObservableProperty]
    private List<Category> _categories = [];

    [ObservableProperty]
    private Category? _category;

    [ObservableProperty]
    private int _categoryIndex = -1;

    [ObservableProperty]
    private List<TagSelection> _allTags = [];

    public IList<object> SelectedTags { get; set; } = new List<object>();

    [ObservableProperty]
    private IconData _icon;

    [ObservableProperty]
    bool _isBusy;

    [ObservableProperty]
    private List<IconData> _icons = new List<IconData>
    {
        new IconData { Icon = FluentUI.ribbon_24_regular, Description = "Ribbon Icon" },
        new IconData { Icon = FluentUI.ribbon_star_24_regular, Description = "Ribbon Star Icon" },
        new IconData { Icon = FluentUI.trophy_24_regular, Description = "Trophy Icon" },
        new IconData { Icon = FluentUI.badge_24_regular, Description = "Badge Icon" },
        new IconData { Icon = FluentUI.book_24_regular, Description = "Book Icon" },
        new IconData { Icon = FluentUI.people_24_regular, Description = "People Icon" },
        new IconData { Icon = FluentUI.bot_24_regular, Description = "Bot Icon" }
    };

    private bool _canDelete;

    public bool CanDelete
    {
        get => _canDelete;
        set
        {
            _canDelete = value;
            DeleteCommand.NotifyCanExecuteChanged();
        }
    }

    public bool HasCompletedTasks
        => _project?.Tasks.Any(t => t.IsCompleted) ?? false;

    public ProjectDetailPageModel(ProjectRepository projectRepository, TaskRepository taskRepository, CategoryRepository categoryRepository, TagRepository tagRepository, ModalErrorHandler errorHandler)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _errorHandler = errorHandler;
        _icon = _icons.First();
        Tasks = [];
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("id"))
        {
            int id = Convert.ToInt32(query["id"]);
            LoadData(id).FireAndForgetSafeAsync(_errorHandler);
        }
        else if (query.ContainsKey("refresh"))
        {
            RefreshData().FireAndForgetSafeAsync(_errorHandler);
        }
        else
        {
            Task.WhenAll(LoadCategories(), LoadTags()).FireAndForgetSafeAsync(_errorHandler);
            _project = new()
            {
                Project = new()
                {
                    Name = string.Empty,
                    Description = string.Empty,
                    Icon = string.Empty,
                },
                Tasks = [],
                Tags = [],
            };
            Tasks = _project.Tasks;
        }
    }

    private async Task LoadCategories() =>
        Categories = await _categoryRepository.ListAsync();

    private async Task LoadTags()
    {
        var tags = await _tagRepository.ListAsync();
        AllTags = tags.Select(t => new TagSelection { Tag = t }).ToList();
    }

    private async Task RefreshData()
    {
        if (_project.IsNullOrNew())
        {
            if (_project is not null)
                Tasks = new(_project.Tasks);

            return;
        }

        Tasks = await _taskRepository.ListAsync(_project.Project.Id);
        _project.Tasks = Tasks;
    }

    private async Task LoadData(int id)
    {
        try
        {
            IsBusy = true;

            _project = await _projectRepository.GetAsync(id);

            if (_project.IsNullOrNew())
            {
                _errorHandler.HandleError(new Exception($"Project with id {id} could not be found."));
                return;
            }

            Name = _project.Project.Name;
            Description = _project.Project.Description;
            Tasks = _project.Tasks;

            foreach (var icon in Icons)
            {
                if (icon.Icon == _project.Project.Icon)
                {
                    Icon = icon;
                    break;
                }
            }

            Categories = await _categoryRepository.ListAsync();
            Category = Categories?.FirstOrDefault(c => c.Id == _project.Project.CategoryId);
            CategoryIndex = Categories?.FindIndex(c => c.Id == _project.Project.CategoryId) ?? -1;

            var tags = await _tagRepository.ListAsync();
            var selections = new List<TagSelection>(tags.Count);
            foreach (var tag in tags)
            {
                bool isSelected = _project.Tags.Any(t => t.Id == tag.Id);
                var selection = new TagSelection { Tag = tag, IsSelected = isSelected };
                selections.Add(selection);
                if (isSelected)
                {
                    SelectedTags.Add(selection);
                }
            }
            AllTags = selections;
        }
        catch (Exception e)
        {
            _errorHandler.HandleError(e);
        }
        finally
        {
            IsBusy = false;
            CanDelete = !_project.IsNullOrNew();
            OnPropertyChanged(nameof(HasCompletedTasks));
        }
    }

    [RelayCommand]
    private async Task TaskCompleted(ProjectTask task)
    {
        await _taskRepository.SaveItemAsync(task);
        OnPropertyChanged(nameof(HasCompletedTasks));
    }

    [RelayCommand]
    private async Task Save()
    {
        if (_project is null)
        {
            _errorHandler.HandleError(
                new Exception("Project is null. Cannot Save."));

            return;
        }

        _project.Project.Name = Name;
        _project.Project.Description = Description;
        _project.Project.CategoryId = Category?.Id ?? 0;
        _project.Project.Icon = Icon.Icon ?? FluentUI.ribbon_24_regular;
        await _projectRepository.SaveItemAsync(_project.Project);

        foreach (var selection in AllTags)
        {
            if (selection.IsSelected)
            {
                await _tagRepository.SaveItemAsync(selection.Tag, _project.Project.Id);
            }
        }

        foreach (var task in _project.Tasks)
        {
            if (task.Id == 0)
            {
                task.ProjectId = _project.Project.Id;
                await _taskRepository.SaveItemAsync(task);
            }
        }

        await Shell.Current.GoToAsync("..");
        await AppShell.DisplayToastAsync("Project saved");
    }

    [RelayCommand]
    private async Task AddTask()
    {
        if (_project is null)
        {
            _errorHandler.HandleError(
                new Exception("Project is null. Cannot navigate to task."));

            return;
        }

        // Pass the project so if this is a new project we can just add
        // the tasks to the project and then save them all from here.
        await Shell.Current.GoToAsync($"task",
            new ShellNavigationQueryParameters(){
                {TaskDetailPageModel.ProjectQueryKey, _project}
            });
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete()
    {
        if (_project.IsNullOrNew())
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await _projectRepository.DeleteItemAsync(_project.Project);
        await Shell.Current.GoToAsync("..");
        await AppShell.DisplayToastAsync("Project deleted");
    }

    [RelayCommand]
    private Task NavigateToTask(ProjectTask task) =>
        Shell.Current.GoToAsync($"task?id={task.Id}");

    [RelayCommand]
    internal async Task ToggleTag(TagSelection selection)
    {
        selection.IsSelected = !selection.IsSelected;

        if (!_project.IsNullOrNew())
        {
            if (selection.IsSelected)
            {
                await _tagRepository.SaveItemAsync(selection.Tag, _project.Project.Id);
            }
            else
            {
                await _tagRepository.DeleteItemAsync(selection.Tag, _project.Project.Id);
            }
        }

        AllTags = new(AllTags);
        SemanticScreenReader.Announce($"{selection.Tag.Title} {(selection.IsSelected ? "selected" : "unselected")}");
    }

    [RelayCommand]
    private void IconSelected(IconData icon)
    {
        SemanticScreenReader.Announce($"{icon.Description} selected");
    }

    [RelayCommand]
    private async Task CleanTasks()
    {
        var completedTasks = Tasks.Where(t => t.IsCompleted).ToArray();
        foreach (var task in completedTasks)
        {
            await _taskRepository.DeleteItemAsync(task);
            Tasks.Remove(task);
        }

        Tasks = new(Tasks);
        OnPropertyChanged(nameof(HasCompletedTasks));
        await AppShell.DisplayToastAsync("All cleaned up!");
    }

    [RelayCommand]
    private async Task SelectionChanged(object parameter)
    {
        if (parameter is IEnumerable<object> enumerableParameter)
        {
            var currentSelection = enumerableParameter.OfType<TagSelection>().ToList();
            var previousSelection = AllTags.Where(s => s.IsSelected).ToList();

            // Handle newly selected tags
            foreach (var selection in currentSelection.Except(previousSelection))
            {
                selection.IsSelected = true;
                if (!_project.IsNullOrNew())
                {
                    await _tagRepository.SaveItemAsync(selection.Tag, _project.Project.Id);
                }
            }

            // Handle deselected tags
            foreach (var selection in previousSelection.Except(currentSelection))
            {
                selection.IsSelected = false;
                if (!_project.IsNullOrNew())
                {
                    await _tagRepository.DeleteItemAsync(selection.Tag, _project.Project.Id);
                }
            }
        }
    }
}
