#if !SQLITECIPHER
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
internal sealed class JsonbHookRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ConverterHookColumnWriteParityTests
{
    [Fact]
    public void HookColumnJsonbValueOnAdd_StoresBlob()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<JsonbHookRow>().Column("Extra", SQLiteColumnType.Blob),
            options =>
            {
                options.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address);
                options.OnAdd<JsonbHookRow>((_, _, columns) =>
                {
                    columns["Extra"] = new Address { Street = "s", City = "c" };
                    return true;
                });
            });
        db.Schema.CreateTable<JsonbHookRow>();

        db.Table<JsonbHookRow>().Add(new JsonbHookRow { Id = 1, Name = "x" });

        string storageClass = db.QueryFirst<string>("SELECT typeof(\"Extra\") FROM \"JsonbHookRow\" WHERE \"Id\" = 1");
        Assert.Equal("blob", storageClass);
    }

    [Fact]
    public void HookColumnNullValueOnAdd_StoresNull()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<JsonbHookRow>().Column("Extra", SQLiteColumnType.Blob),
            options =>
            {
                options.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address);
                options.OnAdd<JsonbHookRow>((_, _, columns) =>
                {
                    columns["Extra"] = null;
                    return true;
                });
            });
        db.Schema.CreateTable<JsonbHookRow>();

        db.Table<JsonbHookRow>().Add(new JsonbHookRow { Id = 1, Name = "x" });

        string storageClass = db.QueryFirst<string>("SELECT typeof(\"Extra\") FROM \"JsonbHookRow\" WHERE \"Id\" = 1");
        Assert.Equal("null", storageClass);
    }

    [Fact]
    public void HookColumnNullValueOnUpdate_StoresNull()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<JsonbHookRow>().Column("Extra", SQLiteColumnType.Blob),
            options =>
            {
                options.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address);
                options.OnUpdate<JsonbHookRow>((_, _, columns) =>
                {
                    columns["Extra"] = null;
                    return true;
                });
            });
        db.Schema.CreateTable<JsonbHookRow>();
        db.Table<JsonbHookRow>().Add(new JsonbHookRow { Id = 1, Name = "x" });

        db.Table<JsonbHookRow>().Update(new JsonbHookRow { Id = 1, Name = "y" });

        string storageClass = db.QueryFirst<string>("SELECT typeof(\"Extra\") FROM \"JsonbHookRow\" WHERE \"Id\" = 1");
        Assert.Equal("null", storageClass);
    }

    [Fact]
    public void HookColumnJsonbValueOnUpdate_StoresBlob()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<JsonbHookRow>().Column("Extra", SQLiteColumnType.Blob),
            options =>
            {
                options.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address);
                options.OnUpdate<JsonbHookRow>((_, _, columns) =>
                {
                    columns["Extra"] = new Address { Street = "u", City = "v" };
                    return true;
                });
            });
        db.Schema.CreateTable<JsonbHookRow>();
        db.Table<JsonbHookRow>().Add(new JsonbHookRow { Id = 1, Name = "x" });

        db.Table<JsonbHookRow>().Update(new JsonbHookRow { Id = 1, Name = "y" });

        string storageClass = db.QueryFirst<string>("SELECT typeof(\"Extra\") FROM \"JsonbHookRow\" WHERE \"Id\" = 1");
        Assert.Equal("blob", storageClass);
    }
}
#endif
