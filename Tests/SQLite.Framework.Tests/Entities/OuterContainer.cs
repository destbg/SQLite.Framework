using System.ComponentModel.DataAnnotations;

namespace SQLite.Framework.Tests.Entities;

public static class OuterContainer
{
    public class NestedEntity
    {
        [Key]
        public int Id { get; set; }

        public string Label { get; set; } = string.Empty;
    }
}
