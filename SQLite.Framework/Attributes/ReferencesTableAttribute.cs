namespace SQLite.Framework.Attributes;

/// <summary>
/// Marks the property as a foreign key into another table-mapped entity. The framework emits
/// an inline <c>REFERENCES "Parent"("Col")</c> clause when the table is created.
/// </summary>
/// <remarks>
/// The framework also reads
/// <see cref="System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute" />. Prefer this
/// attribute when you need an action other than <c>NoAction</c>, deferred enforcement or
/// refactor-safe <c>typeof</c> targeting.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ReferencesTableAttribute : Attribute
{
    /// <summary>
    /// References the primary key of <paramref name="targetType" />. The target must have a
    /// single-column primary key.
    /// </summary>
    public ReferencesTableAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        TargetType = targetType;
    }

    /// <summary>
    /// References <paramref name="targetColumn" /> on <paramref name="targetType" />.
    /// </summary>
    public ReferencesTableAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type targetType, string targetColumn)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentException.ThrowIfNullOrEmpty(targetColumn);
        TargetType = targetType;
        TargetColumn = targetColumn;
    }

    /// <summary>
    /// The entity type that owns the parent column.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type TargetType { get; }

    /// <summary>
    /// The target property name or <see langword="null" /> to use the single primary key.
    /// </summary>
    public string? TargetColumn { get; }

    /// <summary>
    /// Action to take when the parent row is deleted.
    /// </summary>
    public SQLiteForeignKeyAction OnDelete { get; set; } = SQLiteForeignKeyAction.NoAction;

    /// <summary>
    /// Action to take when the parent row's referenced column is updated.
    /// </summary>
    public SQLiteForeignKeyAction OnUpdate { get; set; } = SQLiteForeignKeyAction.NoAction;

    /// <summary>
    /// Emits <c>DEFERRABLE INITIALLY DEFERRED</c>. The check runs at the end of the
    /// transaction instead of after each statement.
    /// </summary>
    public bool Deferred { get; set; }
}
