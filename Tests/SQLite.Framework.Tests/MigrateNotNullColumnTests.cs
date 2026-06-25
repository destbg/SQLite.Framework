using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MigrateNotNullColumnTests
{
    [Table("MigNn")]
    public class Nullable
    {
        [Key] public int Id { get; set; }
        public int? Flag { get; set; }
    }

    [Table("MigNn")]
    public class NotNullWithDefault
    {
        [Key] public int Id { get; set; }
        [DefaultValue(7)] public int Flag { get; set; }
    }

    [Table("MigNn")]
    public class NotNullWithoutDefault
    {
        [Key] public int Id { get; set; }
        public int Flag { get; set; }
    }

    [Fact]
    public void NullableToNotNullWithDefault_BackfillsExistingNullRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<Nullable>().Schema.CreateTable();
        db.Table<Nullable>().Add(new Nullable { Id = 1, Flag = null });
        db.Table<Nullable>().Add(new Nullable { Id = 2, Flag = 3 });

        db.Table<NotNullWithDefault>().Schema.Migrate();

        var rows = db.Table<NotNullWithDefault>().OrderBy(r => r.Id).Select(r => r.Flag).ToList();
        Assert.Equal(new[] { 7, 3 }, rows);
    }

    [Fact]
    public void NullableToNotNullWithSetValue_FillsExistingNullRows()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<Nullable>().Schema.CreateTable();
        db.Table<Nullable>().Add(new Nullable { Id = 1, Flag = null });

        db.Table<NotNullWithoutDefault>().Schema.Migrate(m => m.Set(r => r.Flag, 9));

        int flag = db.Table<NotNullWithoutDefault>().Select(r => r.Flag).First();
        Assert.Equal(9, flag);
    }

    [Fact]
    public void NullableToNotNullWithoutDefault_ThrowsWhenNullRowsExist()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<Nullable>().Schema.CreateTable();
        db.Table<Nullable>().Add(new Nullable { Id = 1, Flag = null });

        Assert.ThrowsAny<SQLiteException>(() => db.Table<NotNullWithoutDefault>().Schema.Migrate());
    }
}
