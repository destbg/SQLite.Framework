using CommunityToolkit.Mvvm.ComponentModel;

namespace SQLite.Framework.Maui.Models;

public partial class TagSelection : ObservableObject
{
    public required Tag Tag { get; init; }

    [ObservableProperty]
    private bool _isSelected;
}
