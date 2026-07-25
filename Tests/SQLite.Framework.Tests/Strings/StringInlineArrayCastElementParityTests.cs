using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21dCastElemRows")]
public class H21dCastElemRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringInlineArrayCastElementParityTests
{
    private static List<H21dCastElemRow> Rows()
    {
        return
        [
            new H21dCastElemRow { Id = 1, Name = "a" },
            new H21dCastElemRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21dCastElemRow>().Schema.CreateTable();
        db.Table<H21dCastElemRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void JoinIntArrayWithTruncatingCastElementMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new int[] { (int)Math.Sqrt(10.0), 7 }))
            .ToList();

        List<string> actual = db.Table<H21dCastElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new int[] { (int)Math.Sqrt(10.0), 7 }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatIntArrayWithTruncatingCastElementMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new int[] { (int)Math.Sqrt(10.0) }))
            .ToList();

        List<string> actual = db.Table<H21dCastElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new int[] { (int)Math.Sqrt(10.0) }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinByteArrayWithWrappingCastElementMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new byte[] { (byte)int.Parse("300") }))
            .ToList();

        List<string> actual = db.Table<H21dCastElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new byte[] { (byte)int.Parse("300") }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinFloatArrayWithNarrowingCastElementMatchesDotNet()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new float[] { (float)Math.Sqrt(2.0) }))
            .ToList();

        List<string> actual = db.Table<H21dCastElemRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join(",", new float[] { (float)Math.Sqrt(2.0) }))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
