using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class SetterHelperInternalTests
{
    [Fact]
    public void SetWritesValueAndCoercesNullToDefault()
    {
        PropertyInfo prop = typeof(NullableStringEntity).GetProperty(nameof(NullableStringEntity.Name))!;
        SetterHelper<NullableStringEntity, string> helper = new(prop.SetMethod!);
        NullableStringEntity entity = new() { Id = 1, Name = "seed" };

        helper.Set(entity, "value");
        Assert.Equal("value", entity.Name);

        helper.Set(entity, null);
        Assert.Null(entity.Name);
    }
}
