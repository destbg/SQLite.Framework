import type { Walkthrough } from "./types";

export const avaloniaWalkthrough: Walkthrough = {
    slug: "avalonia",
    title: "Avalonia Walkthrough",
    subtitle: "Wire SQLite.Framework into an Avalonia app, shared across desktop and mobile",
    steps: [
        {
            title: "What you will build",
            description:
                "A cross-platform Avalonia app that uses a single SQLite database for desktop, Android, and iOS. The flow is loading, login, sync, main, with the same database layer shared across every target.",
        },
        {
            title: "Create the project",
            description:
                "Install the Avalonia templates if you have not already, then scaffold a cross-platform app. Pick any project name you like and adjust the namespaces in the following code if you change it.",
            code: {
                language: "bash",
                text: `dotnet new install Avalonia.Templates
dotnet new avalonia.xplat -n MyApp
cd MyApp`,
            },
        },
        {
            title: "Install the packages",
            description:
                "Add the packages to the shared head project. Avalonia.Hosting.Microsoft.Extensions.DependencyInjection wires Microsoft.Extensions.DependencyInjection into the Avalonia host.",
            code: {
                language: "bash",
                text: `dotnet add MyApp package SQLite.Framework
dotnet add MyApp package SQLite.Framework.DependencyInjection
dotnet add MyApp package CommunityToolkit.Mvvm
dotnet add MyApp package Microsoft.Extensions.DependencyInjection
dotnet add MyApp package Microsoft.Extensions.Hosting`,
            },
        },
        {
            title: "Define the models",
            description:
                "Same models as any other app. User identifies the signed-in account, Country is a typical lookup table you download from your backend.",
            code: {
                language: "csharp",
                filename: "Models/User.cs",
                text: `using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;

public class User
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    [Required]
    public required string Email { get; set; }

    [Required]
    public required string DisplayName { get; set; }
}

public class Country
{
    [Key]
    public required string Code { get; set; }

    [Required]
    public required string Name { get; set; }
}`,
            },
        },
        {
            title: "Subclass SQLiteDatabase",
            description:
                "One subclass holds the tables and gives callsites short names.",
            code: {
                language: "csharp",
                filename: "Data/AppDatabase.cs",
                text: `using SQLite.Framework;

public class AppDatabase : SQLiteDatabase
{
    public AppDatabase(SQLiteOptions options) : base(options)
    {
    }

    public SQLiteTable<User> Users => Table<User>();

    public SQLiteTable<Country> Countries => Table<Country>();
}`,
            },
        },
        {
            title: "Pick a cross-platform path",
            description:
                "Use Environment.SpecialFolder.LocalApplicationData to resolve a writable per-user directory. It returns the right per-platform spot on every target Avalonia ships on.",
            code: {
                language: "csharp",
                filename: "Data/DatabasePaths.cs",
                text: `public static class DatabasePaths
{
    public static string Resolve(string fileName)
    {
        string baseDir = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        string appDir = Path.Combine(baseDir, "MyApp");
        Directory.CreateDirectory(appDir);
        return Path.Combine(appDir, fileName);
    }
}`,
            },
        },
        {
            title: "Register the database and services",
            description:
                "Build an IServiceProvider in your App.axaml.cs (or wherever the app initialises) and register everything. The same registration runs on every target.",
            code: {
                language: "csharp",
                filename: "App.axaml.cs",
                text: `var services = new ServiceCollection();

string dbPath = DatabasePaths.Resolve("app.db");
services.AddSQLiteDatabase<AppDatabase>(
    b =>
    {
        b.DatabasePath = dbPath;
        b.MinimumSqliteVersion = SQLiteMinimumVersion.V3_36;
    },
    ServiceLifetime.Singleton);

services.AddSingleton<AuthService>();
services.AddSingleton<MigrationService>();
services.AddSingleton<LookupService>();

services.AddTransient<LoadingViewModel>();
services.AddTransient<LoginViewModel>();
services.AddTransient<MainViewModel>();

services.AddTransient<LoadingPage>();
services.AddTransient<LoginPage>();
services.AddTransient<MainPage>();

Services = services.BuildServiceProvider();`,
            },
        },
        {
            title: "Migration service",
            description:
                "All schema work in one method. The migration runner records the version it reached, so each version runs once per file.",
            code: {
                language: "csharp",
                filename: "Services/MigrationService.cs",
                text: `public class MigrationService
{
    private readonly AppDatabase db;

    public MigrationService(AppDatabase db)
    {
        this.db = db;
    }

    public async Task RunAsync()
    {
        await db.Schema.Migrations()
            .Version(1, m => m
                .TableChanged<User>()
                .TableChanged<Country>())
            .MigrateAsync();
    }
}`,
            },
        },
        {
            title: "Stub the auth service",
            description:
                "Replace the body with real HTTP later. For now it just flips a flag so the loading view model can branch on it.",
            code: {
                language: "csharp",
                filename: "Services/AuthService.cs",
                text: `public class AuthService
{
    public bool IsLoggedIn { get; private set; }

    public Task<bool> SignInAsync(string email, string password)
    {
        IsLoggedIn = true;
        return Task.FromResult(true);
    }
}`,
            },
        },
        {
            title: "Download the lookup data",
            description:
                "Clear the table with one ExecuteDeleteAsync, then insert the new rows with AddRangeAsync. Both calls go through a single transaction.",
            code: {
                language: "csharp",
                filename: "Services/LookupService.cs",
                text: `public class LookupService
{
    private readonly AppDatabase db;

    public LookupService(AppDatabase db)
    {
        this.db = db;
    }

    public async Task SyncAsync()
    {
        var countries = new[]
        {
            new Country { Code = "US", Name = "United States" },
            new Country { Code = "BG", Name = "Bulgaria" },
            new Country { Code = "DE", Name = "Germany" },
        };

        await db.Countries.ExecuteDeleteAsync();
        await db.Countries.AddRangeAsync(countries);
    }
}`,
            },
        },
        {
            title: "Loading view model",
            description:
                "The view model holds the status text and exposes a method that does the work. It does not navigate. Navigation is the page's job, so the view model stays unit-testable.",
            code: {
                language: "csharp",
                filename: "ViewModels/LoadingViewModel.cs",
                text: `public enum LoadingOutcome { Login, Main }

public partial class LoadingViewModel : ObservableObject
{
    private readonly MigrationService migrations;
    private readonly LookupService lookups;
    private readonly AuthService auth;

    [ObservableProperty]
    private string status = "Loading...";

    public LoadingViewModel(
        MigrationService migrations,
        LookupService lookups,
        AuthService auth)
    {
        this.migrations = migrations;
        this.lookups = lookups;
        this.auth = auth;
    }

    public async Task<LoadingOutcome> RunAsync()
    {
        if (!auth.IsLoggedIn)
        {
            Status = "Updating database...";
            await migrations.RunAsync();
            return LoadingOutcome.Login;
        }

        Status = "Downloading data...";
        await lookups.SyncAsync();
        return LoadingOutcome.Main;
    }
}`,
            },
        },
        {
            title: "Build the loading page",
            description:
                "A ContentPage with a spinner and the status label. The code-behind runs the view model in OnNavigatedTo, then calls Navigation.ReplaceAsync to swap to the next page resolved from DI.",
            code: {
                language: "xml",
                filename: "Pages/LoadingPage.axaml",
                text: `<ContentPage xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.Pages.LoadingPage"
             HasNavigationBar="False">
    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Spacing="16">
        <ProgressBar IsIndeterminate="True" Width="200" />
        <TextBlock Text="{Binding Status}" HorizontalAlignment="Center" />
    </StackPanel>
</ContentPage>`,
            },
        },
        {
            title: "Branch in the loading page code-behind",
            description:
                "Resolve the next page from the service provider and replace the navigation stack with it. ReplaceAsync swaps the current page so the back gesture cannot return to loading.",
            code: {
                language: "csharp",
                filename: "Pages/LoadingPage.axaml.cs",
                text: `public partial class LoadingPage : ContentPage
{
    private readonly LoadingViewModel vm;
    private readonly IServiceProvider services;

    public LoadingPage(LoadingViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        this.vm = vm;
        this.services = services;
        DataContext = vm;
    }

    protected override async void OnNavigatedTo()
    {
        LoadingOutcome outcome = await vm.RunAsync();
        ContentPage next = outcome switch
        {
            LoadingOutcome.Login => services.GetRequiredService<LoginPage>(),
            LoadingOutcome.Main => services.GetRequiredService<MainPage>(),
            _ => throw new InvalidOperationException(),
        };
        await Navigation.ReplaceAsync(next);
    }
}`,
            },
        },
        {
            title: "Build the login page",
            description:
                "Email, password, and a sign-in button. The view model raises an event on success, the page reacts by navigating back to loading. The second loading pass then syncs and lands on main.",
            code: {
                language: "xml",
                filename: "Pages/LoginPage.axaml",
                text: `<ContentPage xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.Pages.LoginPage"
             Title="Sign in">
    <StackPanel Margin="24" Spacing="12">
        <TextBox Text="{Binding Email}" Watermark="Email" />
        <TextBox Text="{Binding Password}" Watermark="Password" PasswordChar="*" />
        <Button Content="Sign in" Command="{Binding SignInCommand}" />
    </StackPanel>
</ContentPage>`,
            },
        },
        {
            title: "Login page model",
            description:
                "Standard MVVM with [ObservableProperty] and [RelayCommand]. Successful sign-in raises an event the page subscribes to.",
            code: {
                language: "csharp",
                filename: "ViewModels/LoginViewModel.cs",
                text: `public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService auth;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";

    public event EventHandler? SignedIn;

    public LoginViewModel(AuthService auth)
    {
        this.auth = auth;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (await auth.SignInAsync(Email, Password))
        {
            SignedIn?.Invoke(this, EventArgs.Empty);
        }
    }
}`,
            },
        },
        {
            title: "Wire the login page code-behind",
            description:
                "The page owns the navigation call. When the view model raises SignedIn, send the user back to the loading page.",
            code: {
                language: "csharp",
                filename: "Pages/LoginPage.axaml.cs",
                text: `public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        DataContext = vm;
        vm.SignedIn += async (_, _) =>
            await Navigation.ReplaceAsync(services.GetRequiredService<LoadingPage>());
    }
}`,
            },
        },
        {
            title: "Build the main page",
            description:
                "Reads the local database and renders the list. After the second loading pass, the data is already there.",
            code: {
                language: "csharp",
                filename: "ViewModels/MainViewModel.cs",
                text: `public partial class MainViewModel : ObservableObject
{
    private readonly AppDatabase db;

    [ObservableProperty]
    private List<Country> countries = new();

    public MainViewModel(AppDatabase db)
    {
        this.db = db;
    }

    public async Task LoadAsync()
    {
        Countries = await db.Countries
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}`,
            },
        },
        {
            title: "Main page XAML",
            description:
                "The page hosts the list and triggers the load in OnNavigatedTo so the data is fresh every time the user returns to main.",
            code: {
                language: "xml",
                filename: "Pages/MainPage.axaml",
                text: `<ContentPage xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.Pages.MainPage"
             Title="Countries">
    <ListBox ItemsSource="{Binding Countries}"
             Margin="16">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding Name}" />
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</ContentPage>`,
            },
        },
        {
            title: "Wire the navigation root",
            description:
                "MainWindow hosts a NavigationPage whose first child is the LoadingPage resolved from DI. From there the loading page swaps to login or main as it sees fit.",
            code: {
                language: "csharp",
                filename: "Views/MainWindow.axaml.cs",
                text: `public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Content = new NavigationPage
        {
            Content = App.Services.GetRequiredService<LoadingPage>(),
        };
    }
}`,
            },
        },
        {
            title: "You are done",
            description:
                "Run any of the heads, desktop, Android, or iOS. The first launch migrates, sends you to login, then back to loading to sync, then to main. The whole DB layer is shared across every target.",
        },
    ],
};
