using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.Avalonia.Data;
using SQLite.Framework.Avalonia.ViewModels;
using SQLite.Framework.Avalonia.Views;
using SQLite.Framework.DependencyInjection;
using SQLite.Framework.Generated;

namespace SQLite.Framework.Avalonia;

public class App : global::Avalonia.Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        SeedDataService seed = Services.GetRequiredService<SeedDataService>();
        // Wrap in Task.Run so the awaits resume on the thread pool, not on
        // the framework's UI thread. Calling GetResult() on the UI thread
        // directly would deadlock when the inner awaits try to come back.
        Task.Run(seed.SeedIfEmptyAsync).GetAwaiter().GetResult();

        MainViewModel mainVm = Services.GetRequiredService<MainViewModel>();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow { DataContext = mainVm };
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView { DataContext = mainVm };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSQLiteDatabase<AppDatabase>(b =>
        {
            b.DatabasePath = Constants.DatabasePath;
            b.UseWalMode()
                .DisableReflectionFallback()
                .UseGeneratedMaterializers();
        });

        services.AddSingleton<ProjectRepository>();
        services.AddSingleton<TaskRepository>();
        services.AddSingleton<TagRepository>();
        services.AddSingleton<CategoryRepository>();
        services.AddSingleton<SeedDataService>();

        services.AddSingleton<ProjectsViewModel>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<TagsViewModel>();
        services.AddSingleton<MainViewModel>();
    }
}
