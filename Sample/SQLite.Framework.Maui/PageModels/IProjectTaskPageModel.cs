using CommunityToolkit.Mvvm.Input;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	bool IsBusy { get; }
}