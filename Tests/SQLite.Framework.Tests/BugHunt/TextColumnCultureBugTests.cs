using System.Globalization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class TextColumnCultureBugTests
{
    [Fact]
    public void ExecuteScalarDoubleFromTextRespectsInvariantCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            using TestDatabase db = new();

            double expected = double.Parse("3.14", CultureInfo.InvariantCulture);
            double actual = db.ExecuteScalar<double>("SELECT '3.14'");

            Assert.Equal(expected, actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void QueryDoubleFromTextRespectsInvariantCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            using TestDatabase db = new();

            double expected = double.Parse("3.14", CultureInfo.InvariantCulture);
            double actual = db.Query<double>("SELECT '3.14'")[0];

            Assert.Equal(expected, actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ExecuteScalarDoubleFromTextDoesNotThrowUnderFrench()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            using TestDatabase db = new();

            double expected = double.Parse("1234.5", CultureInfo.InvariantCulture);
            double actual = db.ExecuteScalar<double>("SELECT '1234.5'");

            Assert.Equal(expected, actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
