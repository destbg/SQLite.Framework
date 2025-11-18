using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests.Entities;

[Table("NumericTypes")]
public class NumericType : IEntity
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("IntValue")]
    public int IntValue { get; set; }

    [Column("LongValue")]
    public long LongValue { get; set; }

    [Column("ShortValue")]
    public short ShortValue { get; set; }

    [Column("ByteValue")]
    public byte ByteValue { get; set; }

    [Column("SByteValue")]
    public sbyte SByteValue { get; set; }

    [Column("UIntValue")]
    public uint UIntValue { get; set; }

    [Column("ULongValue")]
    public ulong ULongValue { get; set; }

    [Column("UShortValue")]
    public ushort UShortValue { get; set; }

    [Column("DoubleValue")]
    public double DoubleValue { get; set; }

    [Column("FloatValue")]
    public float FloatValue { get; set; }

    [Column("DecimalValue")]
    public decimal DecimalValue { get; set; }

    [Column("CharValue")]
    public char CharValue { get; set; }

    [Column("BlobValue")]
    public byte[]? BlobValue { get; set; }
}
