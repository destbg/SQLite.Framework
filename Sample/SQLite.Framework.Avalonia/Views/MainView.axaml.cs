using Avalonia.Controls;
using Avalonia.Interactivity;
using SQLite.Framework.Avalonia.ViewModels;

namespace SQLite.Framework.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is MainViewModel vm)
        {
            await vm.Categories.LoadAsync();
            await vm.Tags.LoadAsync();
            await vm.Projects.LoadAsync();
        }
    }
}
