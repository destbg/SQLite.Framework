using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Tests;

public class ParameterHelpersInternalTests
{
    [Fact]
    public void CombineParametersFromModels_AllNullSqlExpression_ReturnsNull()
    {
        ResolvedModel a = MakeNullSqlModel();
        ResolvedModel b = MakeNullSqlModel();

        SQLiteParameter[]? result = ParameterHelpers.CombineParametersFromModels([a, b]);

        Assert.Null(result);
    }

    [Fact]
    public void CombineParametersFromModels_MixOfNullAndNonNull_OnlyTakesNonNull()
    {
        ResolvedModel withNullExpr = MakeNullSqlModel();
        ResolvedModel withParam = MakeModelWithParameter("@p0", 1);

        SQLiteParameter[]? result = ParameterHelpers.CombineParametersFromModels([withNullExpr, withParam, withNullExpr]);

        SQLiteParameter[] arr = Assert.IsType<SQLiteParameter[]>(result);
        Assert.Single(arr);
        Assert.Equal("@p0", arr[0].Name);
    }

    [Fact]
    public void CombineParameters_AllExpressionsNull_ReturnsNull()
    {
        SQLiteExpression a = new(typeof(int), 0, "1");
        SQLiteExpression b = new(typeof(int), 1, "2");

        SQLiteParameter[]? result = ParameterHelpers.CombineParameters(a, b, a);

        Assert.Null(result);
    }

    private static ResolvedModel MakeNullSqlModel()
    {
        return new ResolvedModel
        {
            IsConstant = false,
            Constant = null,
            SQLiteExpression = null,
            Expression = Expression.Constant(0),
        };
    }

    private static ResolvedModel MakeModelWithParameter(string paramName, object value)
    {
        SQLiteExpression expr = new(typeof(int), 0, paramName, value);
        return new ResolvedModel
        {
            IsConstant = true,
            Constant = value,
            SQLiteExpression = expr,
            Expression = Expression.Constant(value),
        };
    }
}
