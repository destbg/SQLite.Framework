using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Attributes;
using SQLite.Framework.Models;

namespace SQLite.Framework;

/// <summary>
/// Represents a base class for SQLite tables.
/// </summary>
public class SQLiteTable : BaseSQLiteTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTable{T}"/> class.
    /// </summary>
    public SQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database)
    {
        Table = table;
    }

    /// <summary>
    /// The mapping of the database table to the class.
    /// </summary>
    public TableMapping Table { get; }

    /// <inheritdoc />
    public override Type ElementType => Table.Type;

    /// <inheritdoc />
    public override Expression Expression => Expression.Constant(this);

    /// <inheritdoc />
    public override IQueryProvider Provider => Database;

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }

    /// <summary>
    /// Creates the table in the database if it does not exist.
    /// </summary>
    public int CreateTable()
    {
        string columns = string.Join(", ", Table.Columns.Select(c => c.GetCreateColumnSql()));

        string sql = $"CREATE TABLE IF NOT EXISTS \"{Table.TableName}\" ({columns})";

        if (Table.WithoutRowId)
        {
            sql += " WITHOUT ROWID";
        }

        int count = Database.CreateCommand(sql, []).ExecuteNonQuery();

        foreach (TableColumn tableColumn in Table.Columns)
        {
            foreach (IndexedAttribute index in tableColumn.Indices)
            {
                string indexName = index.Name ?? ("idx_" + tableColumn.Name + "_" + index.Order);

                if (index.IsUnique)
                {
                    string uniqueSql = $"CREATE UNIQUE INDEX IF NOT EXISTS \"{indexName}\" ON \"{Table.TableName}\" ({tableColumn.Name})";
                    count += Database.CreateCommand(uniqueSql, []).ExecuteNonQuery();
                }
                else
                {
                    string indexSql = $"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{Table.TableName}\" ({tableColumn.Name})";
                    count += Database.CreateCommand(indexSql, []).ExecuteNonQuery();
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Deletes the table from the database.
    /// </summary>
    public int DropTable()
    {
        string sql = $"DROP TABLE IF EXISTS \"{Table.TableName}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Performs a DELETE operation on the database table.
    /// </summary>
    /// <remarks>
    /// WARNING! This will delete all rows in the table.
    /// </remarks>
    public int Clear()
    {
        string sql = $"DELETE FROM \"{Table.TableName}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }
}

/// <summary>
/// Represents a table in the SQLite database.
/// </summary>
public class SQLiteTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : SQLiteTable, IQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTable{T}"/> class.
    /// </summary>
    public SQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    /// <summary>
    /// Wraps the provided SQL query and parameters into a queryable object.
    /// </summary>
    public IQueryable<T> FromSql(string sql, params SQLiteParameter[] parameters)
    {
        return Database.FromSql<T>(sql, parameters);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public int Add(T item)
    {
        (TableColumn[] columns, string sql) = GetAddInfo();

        return AddItem(columns, sql, item);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public int AddRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        (TableColumn[] columns, string sql) = GetAddInfo();

        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();

            foreach (T item in collection)
            {
                count += AddItem(columns, sql, item);
            }

            transaction.Commit();
        }
        else
        {
            foreach (T item in collection)
            {
                count += AddItem(columns, sql, item);
            }
        }

        return count;
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public int Update(T item)
    {
        (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();

        return UpdateItem(columns, primaryKeyColumns, sql, item);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public int UpdateRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();

        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();

            foreach (T item in collection)
            {
                count += UpdateItem(columns, primaryKeyColumns, sql, item);
            }

            transaction.Commit();
        }
        else
        {
            foreach (T item in collection)
            {
                count += UpdateItem(columns, primaryKeyColumns, sql, item);
            }
        }

        return count;
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public int Remove(T item)
    {
        (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();

        return RemoveItem(primaryKeyColumns, sql, item);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public int RemoveRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();

        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();

            foreach (T item in collection)
            {
                count += RemoveItem(primaryKeyColumns, sql, item);
            }

            transaction.Commit();
        }
        else
        {
            foreach (T item in collection)
            {
                count += RemoveItem(primaryKeyColumns, sql, item);
            }
        }

        return count;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    private (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        string columnsString = string.Join(", ", columns.Select(c => c.Name));
        string parametersString = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

        string sql = $"INSERT INTO \"{Table.TableName}\" ({columnsString}) VALUES ({parametersString})";

        return (columns, sql);
    }

    private (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        TableColumn[] primaryKeyColumns = Table.Columns
            .Where(f => f.IsPrimaryKey)
            .ToArray();

        string setClause = string.Join(", ", columns.Select((c, i) => $"{c.Name} = @p{i}"));
        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{c.Name} = @p{i + columns.Length}")
        );
        string sql = $"UPDATE \"{Table.TableName}\" SET {setClause} WHERE {primaryKeyClause}";

        return (columns, primaryKeyColumns, sql);
    }

    private (TableColumn[] PrimaryColumns, string Sql) GetRemoveInfo()
    {
        TableColumn[] primaryKeyColumns = Table.Columns
            .Where(f => f.IsPrimaryKey)
            .ToArray();

        if (primaryKeyColumns.Length == 0)
        {
            throw new Exception("Cannot perform a delete operation without a primary key.");
        }

        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{c.Name} = @p{i}")
        );
        string sql = $"DELETE FROM \"{Table.TableName}\" WHERE {primaryKeyClause}";

        return (primaryKeyColumns, sql);
    }

    private int AddItem(TableColumn[] columns, string sql, T item)
    {
        List<SQLiteParameter> parameters = columns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = c.PropertyInfo.GetValue(item)
            })
            .ToList();

        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }

    private int RemoveItem(TableColumn[] primaryColumns, string sql, T item)
    {
        List<SQLiteParameter> parameters = primaryColumns
            .Select((f, i) => new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = f.PropertyInfo.GetValue(item)
            })
            .ToList();

        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }

    private int UpdateItem(TableColumn[] columns, TableColumn[] primaryColumns, string sql, T item)
    {
        IEnumerable<SQLiteParameter> primaryParameters = primaryColumns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i + columns.Length}",
                Value = c.PropertyInfo.GetValue(item)
            });

        List<SQLiteParameter> parameters = columns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = c.PropertyInfo.GetValue(item)
            })
            .Concat(primaryParameters)
            .ToList();

        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }
}