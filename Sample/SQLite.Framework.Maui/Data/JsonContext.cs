using System.Text.Json.Serialization;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

[JsonSerializable(typeof(SeedDataDto))]
[JsonSerializable(typeof(ProjectSeedDto))]
[JsonSerializable(typeof(ProjectTask))]
[JsonSerializable(typeof(Category))]
[JsonSerializable(typeof(Tag))]
public partial class JsonContext : JsonSerializerContext;
