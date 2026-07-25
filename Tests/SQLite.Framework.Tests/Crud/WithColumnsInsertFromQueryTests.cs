using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WithColumnsInsertFromQueryTests
{
    [Fact]
    public void InsertFromQueryWritesDeclaredShadowColumn()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Schema.CreateTable<WcItem>();
        db.Execute("CREATE TABLE \"WcSource\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Version\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"WcSource\" (\"Id\", \"Name\", \"Version\") VALUES (7, 'src', 3)");

        int count = db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<string>(x, "Tag"), "copied"))
            .InsertFromQuery(db.FromSql<WcItem>("SELECT \"Id\" AS \"Id\", \"Name\" AS \"Name\", \"Version\" AS \"Version\" FROM \"WcSource\"").Select(x => new WcItem { Id = x.Id, Name = x.Name, Version = x.Version }));

        Assert.Equal(1, count);
        Assert.Equal("copied", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"WcItem\""));
        Assert.Equal("src", db.Table<WcItem>().Single().Name);

        int second = db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<string>(x, "Tag"), "again"))
            .InsertFromQuery(db.FromSql<WcItem>("SELECT \"Id\" + 100 AS \"Id\", \"Name\" AS \"Name\", \"Version\" AS \"Version\" FROM \"WcSource\"").Select(x => new WcItem { Id = x.Id, Name = x.Name, Version = x.Version }));

        Assert.Equal(1, second);
        Assert.Equal("again", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"WcItem\" WHERE \"Id\" = 107"));
    }

    [Fact]
    public void InsertFromQueryWithRowReadingValueThrows()
    {
        using ModelTestDatabase db = new(m => m.Entity<WcItem>().Column("Stamp", SQLiteColumnType.Integer, nullable: true));
        db.Schema.CreateTable<WcItem>();
        db.Execute("CREATE TABLE \"WcSource\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Version\" INTEGER NOT NULL)");

        Exception? ex = Record.Exception(() => db.Table<WcItem>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<long>(x, "Stamp"), x => x.Version * 10))
            .InsertFromQuery(db.FromSql<WcItem>("SELECT \"Id\" AS \"Id\", \"Name\" AS \"Name\", \"Version\" AS \"Version\" FROM \"WcSource\"").Select(x => new WcItem { Id = x.Id, Name = x.Name, Version = x.Version })));

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void InsertFromQuerySkipsDeclaredColumnAlreadyInTheProjection()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<WcItem>();
        db.Execute("CREATE TABLE \"WcSource\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Version\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"WcSource\" (\"Id\", \"Name\", \"Version\") VALUES (7, 'src', 3)");

        int count = db.Table<WcItem>()
            .WithColumns(c => c.Set(x => x.Name, "ignored"))
            .InsertFromQuery(db.FromSql<WcItem>("SELECT \"Id\" AS \"Id\", \"Name\" AS \"Name\", \"Version\" AS \"Version\" FROM \"WcSource\"").Select(x => new WcItem { Id = x.Id, Name = x.Name, Version = x.Version }));

        Assert.Equal(1, count);
        Assert.Equal("src", db.Table<WcItem>().Single().Name);
    }
}
