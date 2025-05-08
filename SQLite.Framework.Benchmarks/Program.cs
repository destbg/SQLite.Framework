using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using SQLite.Framework;

// 20 seconds for 1000 inserts using Microsoft.Data.Sqlite !!!!

File.Delete("test.db3");

SQLiteDatabase db = new("test.db3");
db.Table<TestEntity>().CreateTable();

List<TestEntity> entities = [];

for (int i = 0; i < 1000; i++)
{
    entities.Add(new TestEntity
    {
        Id = i,
        Name = $"Name {i}"
    });
}

Stopwatch sw = Stopwatch.StartNew();
db.Table<TestEntity>().AddRange(entities);
sw.Stop();

Console.WriteLine($"Insert 1000 entities took {sw.Elapsed.TotalSeconds} seconds");

class TestEntity
{
    [Key]
    public required int Id { get; set; }

    public required string Name { get; set; }
}