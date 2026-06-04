using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
using SQLite.Framework.Tests.Entities;
using System.Globalization;

namespace SQLite.Framework.Tests;

public class UpsertOnConflictAutoIncrementBugTests
{
    [Fact]
    public void UpsertOnAutoIncrementPrimaryKeyConflictDoesNotInsertDuplicate()
    {
        using TestDatabase db = new();
                db.Table<crudUpsertAutoPkRow>().Schema.CreateTable();
                db.Table<crudUpsertAutoPkRow>().Add(new crudUpsertAutoPkRow { Value = 10 });

                crudUpsertAutoPkRow incoming = new() { Id = 1, Value = 999 };
                db.Table<crudUpsertAutoPkRow>().Upsert(incoming, c => c.OnConflict(x => x.Id).DoNothing());

                List<crudUpsertAutoPkRow> oracle = new() { new crudUpsertAutoPkRow { Id = 1, Value = 10 } };
                crudUpsertAutoPkRow? existing = oracle.FirstOrDefault(o => o.Id == 1);
                if (existing == null)
                {
                    oracle.Add(new crudUpsertAutoPkRow { Id = 1, Value = 999 });
                }

                List<int> oracleValues = oracle.OrderBy(x => x.Id).Select(x => x.Value).ToList();
                List<int> actualValues = db.Table<crudUpsertAutoPkRow>().OrderBy(x => x.Id).Select(x => x.Value).ToList();

                Assert.Equal(oracle.Count, db.Table<crudUpsertAutoPkRow>().Count());
                Assert.Equal(oracleValues, actualValues);
    }
}

[System.ComponentModel.DataAnnotations.Schema.Table("crudUpsertAutoPkRows")]
public class crudUpsertAutoPkRow
{
    [System.ComponentModel.DataAnnotations.Key]
    [SQLite.Framework.Attributes.AutoIncrement]
    public int Id { get; set; }
    public int Value { get; set; }
}
