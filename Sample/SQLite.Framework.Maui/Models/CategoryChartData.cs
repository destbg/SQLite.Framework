namespace SQLite.Framework.Maui.Models;

public class CategoryChartData
{
	public string Title { get; set; }
	public int Count { get; set; }

	public CategoryChartData(string title, int count)
	{
		Title = title;
		Count = count;
	}
}