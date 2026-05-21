using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Internals.RTree;

/// <summary>
/// Reads <see cref="RTreeIndexAttribute" /> and the related per-property attributes off a CLR
/// type and produces an <see cref="RTreeTableInfo" />. Throws when the layout is invalid.
/// </summary>
internal static class RTreeMappingReader
{
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Caller passes a public-property-preserving type.")]
    public static RTreeTableInfo? TryRead([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        RTreeIndexAttribute? rtree = type.GetCustomAttribute<RTreeIndexAttribute>();
        if (rtree == null)
        {
            return null;
        }

        PropertyInfo[] properties = type.GetProperties();

        PropertyInfo? rowIdProperty = null;
        string? rowIdColumnName = null;

        List<(PropertyInfo Property, string ColumnName, string Dimension, bool IsMin)> mins = [];
        List<(PropertyInfo Property, string ColumnName, string Dimension, bool IsMin)> maxs = [];
        List<RTreeAuxiliaryColumn> auxiliaries = [];

        foreach (PropertyInfo property in properties)
        {
            if (property.GetCustomAttribute<NotMappedAttribute>() != null)
            {
                continue;
            }

            string columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;

            bool isKey = property.GetCustomAttribute<KeyAttribute>() != null;
            RTreeMinAttribute? minAttr = property.GetCustomAttribute<RTreeMinAttribute>();
            RTreeMaxAttribute? maxAttr = property.GetCustomAttribute<RTreeMaxAttribute>();
            RTreeAuxiliaryAttribute? auxAttr = property.GetCustomAttribute<RTreeAuxiliaryAttribute>();

            int marker = (isKey ? 1 : 0) + (minAttr != null ? 1 : 0) + (maxAttr != null ? 1 : 0) + (auxAttr != null ? 1 : 0);
            if (marker == 0)
            {
                throw new InvalidOperationException(
                    $"R-Tree entity '{type.Name}' has property '{property.Name}' with no R-Tree role. " +
                    "Mark it with [Key], [RTreeMin], [RTreeMax], [RTreeAuxiliary], or [NotMapped].");
            }
            if (marker > 1)
            {
                throw new InvalidOperationException(
                    $"R-Tree entity '{type.Name}' has property '{property.Name}' with more than one role attribute. " +
                    "[Key], [RTreeMin], [RTreeMax], and [RTreeAuxiliary] are mutually exclusive.");
            }

            if (isKey)
            {
                if (rowIdProperty != null)
                {
                    throw new InvalidOperationException(
                        $"R-Tree entity '{type.Name}' has more than one [Key] property. Exactly one is required.");
                }

                Type rowIdType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (rowIdType != typeof(int) && rowIdType != typeof(long))
                {
                    throw new InvalidOperationException(
                        $"R-Tree entity '{type.Name}' has a [Key] on '{property.Name}' which is not int or long.");
                }

                rowIdProperty = property;
                rowIdColumnName = columnName;
                continue;
            }

            if (minAttr != null)
            {
                ValidateCoordinateType(type, property, rtree.Storage);
                mins.Add((property, columnName, minAttr.Dimension, true));
                continue;
            }

            if (maxAttr != null)
            {
                ValidateCoordinateType(type, property, rtree.Storage);
                maxs.Add((property, columnName, maxAttr.Dimension, false));
                continue;
            }

            auxiliaries.Add(new RTreeAuxiliaryColumn(property, columnName));
        }

        if (rowIdProperty == null)
        {
            throw new InvalidOperationException(
                $"R-Tree entity '{type.Name}' must have one [Key] property of type int or long.");
        }

        if (mins.Count == 0)
        {
            throw new InvalidOperationException(
                $"R-Tree entity '{type.Name}' must declare at least one [RTreeMin]/[RTreeMax] dimension pair.");
        }

        if (mins.Count > 5)
        {
            throw new InvalidOperationException(
                $"R-Tree entity '{type.Name}' declares {mins.Count} dimensions. SQLite supports at most 5.");
        }

        List<RTreeBoundsColumn> bounds = [];
        foreach ((PropertyInfo Property, string ColumnName, string Dimension, bool IsMin) min in mins)
        {
            int matchIndex = maxs.FindIndex(m => m.Dimension == min.Dimension);
            if (matchIndex < 0)
            {
                throw new InvalidOperationException(
                    $"R-Tree entity '{type.Name}' has [RTreeMin(\"{min.Dimension}\")] on '{min.Property.Name}' but no matching [RTreeMax(\"{min.Dimension}\")] property.");
            }

            (PropertyInfo Property, string ColumnName, string Dimension, bool IsMin) max = maxs[matchIndex];
            maxs.RemoveAt(matchIndex);

            bounds.Add(new RTreeBoundsColumn(min.Property, min.ColumnName, min.Dimension, isMin: true));
            bounds.Add(new RTreeBoundsColumn(max.Property, max.ColumnName, max.Dimension, isMin: false));
        }

        if (maxs.Count > 0)
        {
            (PropertyInfo Property, string ColumnName, string Dimension, bool IsMin) orphan = maxs[0];
            throw new InvalidOperationException(
                $"R-Tree entity '{type.Name}' has [RTreeMax(\"{orphan.Dimension}\")] on '{orphan.Property.Name}' but no matching [RTreeMin(\"{orphan.Dimension}\")] property.");
        }

        return new RTreeTableInfo(rtree, rowIdProperty, rowIdColumnName!, bounds, auxiliaries);
    }

    private static void ValidateCoordinateType(Type entityType, PropertyInfo property, SQLiteRTreeStorage storage)
    {
        Type type = property.PropertyType;
        if (storage == SQLiteRTreeStorage.Int32)
        {
            if (type != typeof(int))
            {
                throw new InvalidOperationException(
                    $"R-Tree entity '{entityType.Name}' uses Int32 storage so property '{property.Name}' must be of type int.");
            }
            return;
        }

        if (type != typeof(float) && type != typeof(double) && type != typeof(int))
        {
            throw new InvalidOperationException(
                $"R-Tree entity '{entityType.Name}' has property '{property.Name}' of type {property.PropertyType.Name} which is not a supported R-Tree coordinate type. Use float, double, or int.");
        }
    }
}
