import type { Walkthrough } from "./types";

export const mauiWalkthrough: Walkthrough = {
    slug: "maui",
    title: "MAUI Walkthrough",
    subtitle: "Build a MAUI app with SQLite.Framework end to end",
    steps: [
        {
            title: "What you will build",
            description:
                "A fresh .NET MAUI app that opens on a loading page, runs schema migrations, sends you to login, and after sign-in returns to the loading page to download the lookup data before landing on the main screen.",
        },
        {
            title: "Create the project",
            description:
                "Start from the MAUI template. Pick any project name you like and adjust the namespaces in the following code if you change it.",
            code: {
                language: "bash",
                text: `dotnet new maui -n MyApp
cd MyApp`,
            },
        },
        {
            title: "Install the packages",
            description:
                "SQLite.Framework provides the database. The DependencyInjection package adds AddSQLiteDatabase. CommunityToolkit.Mvvm keeps page models short.",
            code: {
                language: "bash",
                text: `dotnet add package SQLite.Framework
dotnet add package SQLite.Framework.DependencyInjection
dotnet add package CommunityToolkit.Mvvm`,
            },
        },
        {
            title: "Define a user model",
            description:
                "The User table stores who is signed in. Use the attributes from SQLite.Framework to mark the primary key and required fields.",
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
}`,
            },
        },
        {
            title: "Define a lookup table",
            description:
                "A lookup table is a small reference list you download from the backend.",
            code: {
                language: "csharp",
                filename: "Models/Country.cs",
                text: `using System.ComponentModel.DataAnnotations;

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
                "A small subclass keeps every table on one type. Callsites get db.Users and db.Countries instead of db.Table<User>().",
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
            title: "Register everything in MauiProgram",
            description:
                "The database is a singleton. Pages, page models, and services are added so MAUI's DI can hand them out.",
            code: {
                language: "csharp",
                filename: "MauiProgram.cs",
                text: `var builder = MauiApp.CreateBuilder();
builder.UseMauiApp<App>();

string dbPath = Path.Combine(FileSystem.AppDataDirectory, "app.db");
builder.Services.AddSQLiteDatabase<AppDatabase>(
    b =>
    {
        b.DatabasePath = dbPath;
        b.MinimumSqliteVersion = SQLiteMinimumVersion.V3_36;
    },
    ServiceLifetime.Singleton);

builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<MigrationService>();
builder.Services.AddSingleton<LookupService>();

builder.Services.AddTransient<LoadingPage>();
builder.Services.AddTransient<LoadingPageModel>();
builder.Services.AddTransient<LoginPage>();
builder.Services.AddTransient<LoginPageModel>();
builder.Services.AddTransient<MainPage>();
builder.Services.AddTransient<MainPageModel>();

return builder.Build();`,
            },
        },
        {
            title: "Write the migration service",
            description:
                "All schema changes live in one method. Each version runs once. The runner records the version it reached, so the next launch skips it.",
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
                "Replace the body with real HTTP later. For now it just flips a flag so the loading page can branch on it.",
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
            title: "Build the loading page",
            description:
                "A simple page with a spinner and a status label. The page model decides what to do on appear.",
            code: {
                language: "xml",
                filename: "Pages/LoadingPage.xaml",
                text: `<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.Pages.LoadingPage"
             Shell.NavBarIsVisible="False">
    <VerticalStackLayout VerticalOptions="Center"
                         HorizontalOptions="Center"
                         Spacing="16">
        <ActivityIndicator IsRunning="True" />
        <Label Text="{Binding Status}" />
    </VerticalStackLayout>
</ContentPage>`,
            },
        },
        {
            title: "Branch on sign-in state",
            description:
                "The first pass migrates and sends the user to login. After sign-in the loading page reappears, sees IsLoggedIn = true, and syncs lookups before going to main.",
            code: {
                language: "csharp",
                filename: "PageModels/LoadingPageModel.cs",
                text: `public partial class LoadingPageModel : ObservableObject
{
    private readonly MigrationService migrations;
    private readonly LookupService lookups;
    private readonly AuthService auth;

    [ObservableProperty]
    private string status = "Loading...";

    public LoadingPageModel(
        MigrationService migrations,
        LookupService lookups,
        AuthService auth)
    {
        this.migrations = migrations;
        this.lookups = lookups;
        this.auth = auth;
    }

    public async Task RunAsync()
    {
        if (!auth.IsLoggedIn)
        {
            Status = "Updating database...";
            await migrations.RunAsync();
            await Shell.Current.GoToAsync("//login");
        }
        else
        {
            Status = "Downloading data...";
            await lookups.SyncAsync();
            await Shell.Current.GoToAsync("//main");
        }
    }
}`,
            },
        },
        {
            title: "Wire the login page",
            description:
                "After a successful sign-in, navigate back to //loading. The same loading page now runs its second pass.",
            code: {
                language: "csharp",
                filename: "PageModels/LoginPageModel.cs",
                text: `public partial class LoginPageModel : ObservableObject
{
    private readonly AuthService auth;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";

    public LoginPageModel(AuthService auth)
    {
        this.auth = auth;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (await auth.SignInAsync(Email, Password))
        {
            await Shell.Current.GoToAsync("//loading");
        }
    }
}`,
            },
        },
        {
            title: "Read from the main page",
            description:
                "The main page just reads from the local database. After the sync the data is already there.",
            code: {
                language: "csharp",
                filename: "PageModels/MainPageModel.cs",
                text: `public partial class MainPageModel : ObservableObject
{
    private readonly AppDatabase db;

    [ObservableProperty]
    private List<Country> countries = new();

    public MainPageModel(AppDatabase db)
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
            title: "Hook up the AppShell routes",
            description:
                "Three top-level routes, one per page. The first ShellContent is the starting page, so the app boots straight into the loading flow.",
            code: {
                language: "xml",
                filename: "AppShell.xaml",
                text: `<Shell xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:pages="clr-namespace:MyApp.Pages"
       x:Class="MyApp.AppShell"
       FlyoutBehavior="Disabled">
    <ShellContent Route="loading"
                  ContentTemplate="{DataTemplate pages:LoadingPage}" />
    <ShellContent Route="login"
                  ContentTemplate="{DataTemplate pages:LoginPage}" />
    <ShellContent Route="main"
                  ContentTemplate="{DataTemplate pages:MainPage}" />
</Shell>`,
            },
        },
        {
            title: "You are done",
            description:
                "Run the app. You should land on the loading page, then login, then back to loading, then the main page. From here, plug in real HTTP calls and lean on the docs for the deeper topics.",
        },
    ],
};
