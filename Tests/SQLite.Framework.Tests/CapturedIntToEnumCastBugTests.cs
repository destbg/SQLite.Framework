using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class CapturedIntToEnumCastBugTests
{
    [Fact]
    public void CapturedIntCastToEnumInWhereDoesNotThrow()
    {
        using TestDatabase db = new();
        db.Table<helpersEnumCastRow>().Schema.CreateTable();
        db.Table<helpersEnumCastRow>().Add(new helpersEnumCastRow { Id = 1, Color = helpersEnumCastColor.Green });
        db.Table<helpersEnumCastRow>().Add(new helpersEnumCastRow { Id = 2, Color = helpersEnumCastColor.Blue });
        List<helpersEnumCastRow> data = new()
        {
            new helpersEnumCastRow { Id = 1, Color = helpersEnumCastColor.Green },
            new helpersEnumCastRow { Id = 2, Color = helpersEnumCastColor.Blue }
        };
        int code = 1;
        List<int> oracle = data.Where(x => x.Color == (helpersEnumCastColor)code).Select(x => x.Id).ToList();
        List<int> actual = db.Table<helpersEnumCastRow>().Where(x => x.Color == (helpersEnumCastColor)code).Select(x => x.Id).ToList();
        Assert.Equal(oracle, actual);
    }
}

public enum helpersEnumCastColor { Red = 0, Green = 1, Blue = 2 }

public class helpersEnumCastRow { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } public helpersEnumCastColor Color { get; set; } }
