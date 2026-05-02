using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Utilities;

public static class ProjectExtensions
{
    public static bool IsNullOrNew([NotNullWhen(false)] this Project? project)
    {
        return project is null || project.Id == 0;
    }

    public static bool IsNullOrNew([NotNullWhen(false)] this ProjectDetail? detail)
    {
        return detail is null || detail.Project.Id == 0;
    }
}
