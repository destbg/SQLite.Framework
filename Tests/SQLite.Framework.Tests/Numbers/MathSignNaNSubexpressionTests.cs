using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MathSignNaNSubexpressionTests
{
    internal sealed class SignRow
    {
        [Key]
        public int Id { get; set; }

        public double Value { get; set; }
    }

    [Fact]
    public void SignOfNaNSubexpressionReadsBackZero()
    {
        using TestDatabase db = new();
        db.Table<SignRow>().Schema.CreateTable();
        db.Table<SignRow>().Add(new SignRow { Id = 1, Value = -4.0 });

        List<SignRow> rows = [new SignRow { Id = 1, Value = -4.0 }];
        Assert.Throws<ArithmeticException>(() => rows.Select(r => Math.Sign(Math.Sqrt(r.Value))).ToList());

        int actual = db.Table<SignRow>()
            .Select(r => Math.Sign(Math.Sqrt(r.Value)))
            .First();

        Assert.Equal(0, actual);
    }
}
