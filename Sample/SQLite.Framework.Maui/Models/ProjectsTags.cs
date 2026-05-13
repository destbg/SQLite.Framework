using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Maui.Models;

[Table("ProjectsTags")]
public class ProjectsTags
{
	[Key]
	[ReferencesTable(typeof(Project), OnDelete = SQLiteForeignKeyAction.Cascade)]
	public int ProjectId { get; set; }

	[Key]
	[ReferencesTable(typeof(Tag), OnDelete = SQLiteForeignKeyAction.Cascade)]
	public int TagId { get; set; }
}
