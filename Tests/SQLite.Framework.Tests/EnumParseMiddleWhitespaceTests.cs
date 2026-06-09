using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumParseMiddleWhitespaceTests
{
    [Fact]
    public void EnumParse_EmbeddedAsciiWhitespace_MatchesNameWhereDotNetThrows()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = "News\tpaper" });

        bool dotNetThrows = false;
        try
        {
            _ = Enum.Parse<PublisherType>("News\tpaper");
        }
        catch (ArgumentException)
        {
            dotNetThrows = true;
        }

        Assert.True(dotNetThrows);

        PublisherType actual = db.Table<NullableStringEntity>()
            .Where(x => x.Id == 1)
            .Select(x => Enum.Parse<PublisherType>(x.Name!))
            .First();

        Assert.Equal(PublisherType.Newspaper, actual);
    }

    [Fact]
    public void EnumParse_SpacedFlagsForm_RoundTripsLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = "Read, Write" });

        FilePermission oracle = Enum.Parse<FilePermission>("Read, Write");

        FilePermission actual = db.Table<NullableStringEntity>()
            .Where(x => x.Id == 1)
            .Select(x => Enum.Parse<FilePermission>(x.Name!))
            .First();

        Assert.Equal(oracle, actual);
        Assert.Equal(FilePermission.Read | FilePermission.Write, actual);
    }

    [Flags]
    private enum FilePermission
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }
}
