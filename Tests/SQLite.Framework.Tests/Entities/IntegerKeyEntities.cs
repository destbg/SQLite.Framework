using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;

namespace SQLite.Framework.Tests.Entities;

[Table("LongKeyEntity")]
public class LongKeyEntity
{
    [Key, AutoIncrement]
    public long Id { get; set; }
    public required string Name { get; set; }
}

[Table("ShortKeyEntity")]
public class ShortKeyEntity
{
    [Key, AutoIncrement]
    public short Id { get; set; }
    public required string Name { get; set; }
}

[Table("ByteKeyEntity")]
public class ByteKeyEntity
{
    [Key, AutoIncrement]
    public byte Id { get; set; }
    public required string Name { get; set; }
}

[Table("SByteKeyEntity")]
public class SByteKeyEntity
{
    [Key, AutoIncrement]
    public sbyte Id { get; set; }
    public required string Name { get; set; }
}

[Table("UIntKeyEntity")]
public class UIntKeyEntity
{
    [Key, AutoIncrement]
    public uint Id { get; set; }
    public required string Name { get; set; }
}

[Table("ULongKeyEntity")]
public class ULongKeyEntity
{
    [Key, AutoIncrement]
    public ulong Id { get; set; }
    public required string Name { get; set; }
}

[Table("UShortKeyEntity")]
public class UShortKeyEntity
{
    [Key, AutoIncrement]
    public ushort Id { get; set; }
    public required string Name { get; set; }
}
