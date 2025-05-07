using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
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
        throw new Exception($"Cannot enumerate over the non-generic {nameof(SQLiteTable)} class.");
    }

    /// <summary>
    /// Creates the table in the database if it does not exist.
    /// </summary>
    public int CreateTable()
    {
        string columns = string.Join(", ", Table.Columns.Select(c => c.GetCreateColumnSql()));

        string sql = $"CREATE TABLE IF NOT EXISTS \"{Table.TableName}\" ({columns})";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes the table from the database.
    /// </summary>
    public int DropTable()
    {
        string sql = $"DROP TABLE IF EXISTS \"{Table.TableName}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }
}

/// <summary>
/// Represents a table in the SQLite database.
/// </summary>
public class SQLiteTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : SQLiteTable, IQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTable{T}"/> class.
    /// </summary>
    public SQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table) { }

    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public int Add(T item)
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        object[] values = columns
            .Select(c => c.PropertyInfo.GetValue(item))
            .ToArray()!;

        string columnsString = string.Join(", ", columns.Select(c => c.Name));
        string parametersString = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

        Dictionary<string, object?> parametersDict = [];

        for (int i = 0; i < values.Length; i++)
        {
            parametersDict.Add($"@p{i}", values[i]);
        }

        string sql = $"INSERT INTO \"{Table.TableName}\" ({columnsString}) VALUES ({parametersString})";
        return Database.CreateCommand(sql, parametersDict).ExecuteNonQuery();
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public int AddRange(IEnumerable<T> collection)
    {
        int count = 0;

        foreach (T item in collection)
        {
            count += Add(item);
        }

        return count;
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public int Update(T item)
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        object[] values = columns
            .Select(c => c.PropertyInfo.GetValue(item))
            .ToArray()!;

        string setClause = string.Join(", ", columns.Select((c, i) => $"{c.Name} = @p{i + 1}"));
        string sql = $"UPDATE \"{Table.TableName}\" SET {setClause} WHERE {Table.PrimaryKey.Name} = @p0";

        Dictionary<string, object?> parametersDict = [];

        for (int i = 0; i < values.Length; i++)
        {
            parametersDict.Add($"@p{i + 1}", values[i]);
        }

        parametersDict.Add("@p0", Table.PrimaryKey.PropertyInfo.GetValue(item));

        return Database.CreateCommand(sql, parametersDict).ExecuteNonQuery();
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public int UpdateRange(IEnumerable<T> collection)
    {
        int count = 0;

        foreach (T item in collection)
        {
            count += Update(item);
        }

        return count;
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public int Remove(T item)
    {
        return Remove(Table.PrimaryKey.PropertyInfo.GetValue(item)!);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the primary key.
    /// </summary>
    public int Remove(object primaryKey)
    {
        string sql = $"DELETE FROM \"{Table.TableName}\" WHERE {Table.PrimaryKey.Name} = @p0";
        return Database.CreateCommand(sql, new() { ["@p0"] = primaryKey }).ExecuteNonQuery();
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public int RemoveRange(IEnumerable<T> collection)
    {
        int count = 0;

        foreach (T item in collection)
        {
            count += Remove(Table.PrimaryKey.PropertyInfo.GetValue(item)!);
        }

        return count;
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

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }
}