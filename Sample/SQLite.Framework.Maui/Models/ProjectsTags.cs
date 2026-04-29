using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Maui.Models;

[Table("ProjectsTags")]
public class ProjectsTags
{
	[Key]
	public int ProjectId { get; set; }

	[Key]
	public int TagId { get; set; }
}
