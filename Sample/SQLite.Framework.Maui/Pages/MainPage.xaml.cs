using SQLite.Framework.Maui.Models;
using SQLite.Framework.Maui.PageModels;

namespace SQLite.Framework.Maui.Pages;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageModel model)
	{
		InitializeComponent();
		BindingContext = model;
	}
}