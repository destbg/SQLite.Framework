using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public class RebinderFieldDerived : RebinderFieldBase
{
    [Key]
    public int Id { get; set; }
}
