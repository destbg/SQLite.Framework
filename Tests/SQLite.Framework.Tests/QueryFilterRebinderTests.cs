using System.Linq.Expressions;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Interfaces;

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE && !SQLITE_FRAMEWORK_SOURCE_GENERATOR
namespace SQLite.Framework.Tests;

public class QueryFilterRebinderTests
{
    private static readonly SQLiteOptions Options = new SQLiteOptionsBuilder("rebinder-test.db3").Build();

    [Fact]
    public void Rebind_SameType_ReturnsSourceUnchanged()
    {
        Expression<Func<SoftDeletableBook, bool>> source = b => !b.IsDeleted;

        LambdaExpression result = QueryFilterRebinder.Rebind(source, typeof(SoftDeletableBook), Options);

        Assert.Same(source, result);
    }

    [Fact]
    public void Rebind_InterfaceToConcrete_RewritesParameterAndMember()
    {
        Expression<Func<ISoftDelete, bool>> source = e => !e.IsDeleted;

        LambdaExpression result = QueryFilterRebinder.Rebind(source, typeof(SoftDeletableBook), Options);

        Assert.NotSame(source, result);
        Assert.Equal(typeof(SoftDeletableBook), result.Parameters[0].Type);

        UnaryExpression notExpr = Assert.IsAssignableFrom<UnaryExpression>(result.Body);
        MemberExpression memberExpr = Assert.IsAssignableFrom<MemberExpression>(notExpr.Operand);
        Assert.Equal(typeof(SoftDeletableBook), memberExpr.Member.DeclaringType);
        Assert.Equal(nameof(SoftDeletableBook.IsDeleted), memberExpr.Member.Name);
        Assert.Same(result.Parameters[0], memberExpr.Expression);
    }

    [Fact]
    public void Rebind_InterfaceFilter_FuncTypeMatchesEntity()
    {
        Expression<Func<ISoftDelete, bool>> source = e => !e.IsDeleted;

        LambdaExpression result = QueryFilterRebinder.Rebind(source, typeof(SoftDeletableBook), Options);

        Assert.Equal(typeof(Func<SoftDeletableBook, bool>), result.Type);
    }

    [Fact]
    public void Rebind_StaticMemberAccess_PassesThrough()
    {
        Expression<Func<ISoftDelete, bool>> source = e => DateTime.UtcNow.Year > 2000 && !e.IsDeleted;

        LambdaExpression result = QueryFilterRebinder.Rebind(source, typeof(SoftDeletableBook), Options);

        BinaryExpression and = Assert.IsAssignableFrom<BinaryExpression>(result.Body);
        BinaryExpression yearGt = Assert.IsAssignableFrom<BinaryExpression>(and.Left);
        MemberExpression year = Assert.IsAssignableFrom<MemberExpression>(yearGt.Left);
        MemberExpression utcNow = Assert.IsAssignableFrom<MemberExpression>(year.Expression);
        Assert.Null(utcNow.Expression);
    }

    [Fact]
    public void Rebind_NestedLambdaParameter_LeavesItUnchanged()
    {
        Expression<Func<ISoftDelete, bool>> source = e =>
            new[] { 1, 2, 3 }.Any(i => i > 0) && !e.IsDeleted;

        LambdaExpression result = QueryFilterRebinder.Rebind(source, typeof(SoftDeletableBook), Options);

        Assert.Equal(typeof(SoftDeletableBook), result.Parameters[0].Type);
        BinaryExpression and = Assert.IsAssignableFrom<BinaryExpression>(result.Body);
        UnaryExpression notExpr = Assert.IsAssignableFrom<UnaryExpression>(and.Right);
        MemberExpression memberExpr = Assert.IsAssignableFrom<MemberExpression>(notExpr.Operand);
        Assert.Same(result.Parameters[0], memberExpr.Expression);
    }

    [Fact]
    public void Rebind_BaseClassFilterWithField_RewritesAccessForDerived()
    {
        Expression<Func<RebinderFieldBase, bool>> source = b => b.Flag;

        LambdaExpression result = QueryFilterRebinder.Rebind(source, typeof(RebinderFieldDerived), Options);

        Assert.Equal(typeof(RebinderFieldDerived), result.Parameters[0].Type);
        MemberExpression memberExpr = Assert.IsAssignableFrom<MemberExpression>(result.Body);
        Assert.IsAssignableFrom<System.Reflection.FieldInfo>(memberExpr.Member);
        Assert.Equal(nameof(RebinderFieldBase.Flag), memberExpr.Member.Name);
        Assert.Same(result.Parameters[0], memberExpr.Expression);
    }
}
#endif
