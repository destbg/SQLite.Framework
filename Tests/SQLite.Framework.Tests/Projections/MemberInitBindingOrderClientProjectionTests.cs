using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25mBindingOrderRows")]
public class H25mBindingOrderRow
{
    [Key]
    public int Id { get; set; }

    public int Number { get; set; }

    public string Text { get; set; } = "";
}

public class H25mBindingOrderDto
{
    public int Number { get; set; }

    public string Label { get; set; } = "";
}

public class MemberInitBindingOrderClientProjectionTests
{
    [Fact]
    public void KeepsEachValueOnItsOwnMemberWhenTheInitializerOrderDiffersFromTheDeclarationOrder()
    {
        using TestDatabase db = Setup(nameof(KeepsEachValueOnItsOwnMemberWhenTheInitializerOrderDiffersFromTheDeclarationOrder));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H25mBindingOrderDto
            {
                Label = r.Text.Normalize(NormalizationForm.FormD),
                Number = r.Number
            })
            .Select(d => d.Number + "|" + d.Label)
            .ToList();

        List<string> actual = db.Table<H25mBindingOrderRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H25mBindingOrderDto
            {
                Label = r.Text.Normalize(NormalizationForm.FormD),
                Number = r.Number
            })
            .ToList()
            .Select(d => d.Number + "|" + d.Label)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25mBindingOrderRow> Rows()
    {
        return
        [
            new H25mBindingOrderRow { Id = 1, Number = 11, Text = "alpha" },
            new H25mBindingOrderRow { Id = 2, Number = 22, Text = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25mBindingOrderRow>().Schema.CreateTable();
        db.Table<H25mBindingOrderRow>().AddRange(Rows());
        return db;
    }
}
