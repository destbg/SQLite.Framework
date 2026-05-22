using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Internals.Geopoly;

/// <summary>
/// Reads <see cref="GeopolyIndexAttribute" /> and the related per-property attributes off a CLR
/// type and produces a <see cref="GeopolyTableInfo" />. Throws when the layout is invalid.
/// </summary>
internal static class GeopolyMappingReader
{
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Caller passes a public-property-preserving type.")]
    public static GeopolyTableInfo? TryRead([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (type.GetCustomAttribute<GeopolyIndexAttribute>() == null)
        {
            return null;
        }

        PropertyInfo[] properties = type.GetProperties();

        PropertyInfo? rowIdProperty = null;
        PropertyInfo? shapeProperty = null;
        List<GeopolyAuxiliaryColumn> auxiliaries = [];

        foreach (PropertyInfo property in properties)
        {
            if (property.GetCustomAttribute<NotMappedAttribute>() != null)
            {
                continue;
            }

            string columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;

            bool isKey = property.GetCustomAttribute<KeyAttribute>() != null;
            bool isShape = property.GetCustomAttribute<GeopolyShapeAttribute>() != null;

            if (isKey && isShape)
            {
                throw new InvalidOperationException(
                    $"Geopoly entity '{type.Name}' has property '{property.Name}' marked with both [Key] and [GeopolyShape]. These are mutually exclusive.");
            }

            if (isKey)
            {
                if (rowIdProperty != null)
                {
                    throw new InvalidOperationException(
                        $"Geopoly entity '{type.Name}' has more than one [Key] property. Exactly one is required.");
                }

                Type rowIdType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (rowIdType != typeof(int) && rowIdType != typeof(long))
                {
                    throw new InvalidOperationException(
                        $"Geopoly entity '{type.Name}' has a [Key] on '{property.Name}' which is not int or long.");
                }

                rowIdProperty = property;
                continue;
            }

            if (isShape)
            {
                if (shapeProperty != null)
                {
                    throw new InvalidOperationException(
                        $"Geopoly entity '{type.Name}' has more than one [GeopolyShape] property. Exactly one is required.");
                }

                if (property.PropertyType != typeof(string) && property.PropertyType != typeof(byte[]))
                {
                    throw new InvalidOperationException(
                        $"Geopoly entity '{type.Name}' has [GeopolyShape] on '{property.Name}' which is {property.PropertyType.Name}. The shape property must be string (GeoJSON) or byte[] (binary polygon).");
                }

                shapeProperty = property;
                continue;
            }

            auxiliaries.Add(new GeopolyAuxiliaryColumn(property, columnName));
        }

        if (rowIdProperty == null)
        {
            throw new InvalidOperationException(
                $"Geopoly entity '{type.Name}' must have one [Key] property of type int or long.");
        }

        if (shapeProperty == null)
        {
            throw new InvalidOperationException(
                $"Geopoly entity '{type.Name}' must have one property marked with [GeopolyShape].");
        }

        return new GeopolyTableInfo(rowIdProperty, shapeProperty, auxiliaries);
    }
}
