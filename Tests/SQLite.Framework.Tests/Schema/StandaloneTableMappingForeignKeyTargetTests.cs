using SQLite.Framework;
using SQLite.Framework.Models;

namespace SQLite.Framework.Tests;

public class StandaloneTableMappingForeignKeyTargetTests
{
    [Fact]
    public void ATypedForeignKeyOnAStandaloneMappingResolvesTheAttributeTargetTable()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("standalone-typed-fk.db3").Build();
        TableMapping mapping = new(typeof(FkBook), options);
        TableMapping parent = new(typeof(FkAuthor), options);

        ForeignKeyInfo foreignKey = mapping.Columns.First(c => c.Name == "AuthorId").ForeignKey!;

        Assert.Equal(parent.TableName, foreignKey.TargetTable);
    }

    [Fact]
    public void ADataAnnotationsForeignKeyOnAStandaloneMappingResolvesTheNamedTargetTable()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("standalone-ef-fk.db3").Build();
        TableMapping mapping = new(typeof(FkBookEf), options);
        TableMapping parent = new(typeof(FkAuthor), options);

        ForeignKeyInfo foreignKey = mapping.Columns.First(c => c.Name == "AuthorId").ForeignKey!;

        Assert.Equal(parent.TableName, foreignKey.TargetTable);
    }
}
