namespace SQLite.Framework.Tests;

public class InheritedRequiredMemberTests
{
    [Fact]
    public void OfType_AbstractBase_RequiredMember_PopulatedFromInheritance()
    {
        List<InheritedRequiredBaseDto> nodes =
        [
            new InheritedRequiredInputDto { Id = "a", Position = 1, SubTriggerJs = "trig" },
            new InheritedRequiredOutputDto { Id = "b", Position = 2, ActionType = 5 }
        ];

        InheritedRequiredInputDto[] inputs = nodes.OfType<InheritedRequiredInputDto>().ToArray();
        InheritedRequiredOutputDto[] outputs = nodes.OfType<InheritedRequiredOutputDto>().ToArray();

        Assert.Single(inputs);
        Assert.Equal("a", inputs[0].Id);
        Assert.Equal(1, inputs[0].Position);
        Assert.Equal("trig", inputs[0].SubTriggerJs);

        Assert.Single(outputs);
        Assert.Equal("b", outputs[0].Id);
        Assert.Equal(2, outputs[0].Position);
        Assert.Equal(5, outputs[0].ActionType);
    }
}

public abstract class InheritedRequiredBaseDto
{
    public required string Id { get; set; }

    public int Position { get; set; }
}

public class InheritedRequiredInputDto : InheritedRequiredBaseDto
{
    public string? SubTriggerJs { get; set; }
}

public class InheritedRequiredOutputDto : InheritedRequiredBaseDto
{
    public int ActionType { get; set; }
}
