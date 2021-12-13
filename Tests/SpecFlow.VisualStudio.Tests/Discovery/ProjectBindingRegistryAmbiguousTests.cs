﻿namespace SpecFlow.VisualStudio.Tests.Discovery;

/*
* Ambiguous
    * Multiple matching step => err,list
    * Multiple step, some with binding/parameter errors => err,list
    * SO, but all ambiguous => err,list (of merged candidates)

*/
public class ProjectBindingRegistryAmbiguousTests : ProjectBindingRegistryTestsBase
{
    // Multiple matching step => err,list

    [Fact]
    public void Matches_ambiguous()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my .* step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my .*"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("other step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my cool step"), StubGherkinDocument.Instance);
        result.HasAmbiguous.Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(m => m.ToLowerInvariant().Contains("ambiguous"));
    }

    [Fact]
    public void Error_contains_method_names()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my .* step", methodName: "Method1"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my .*", methodName: "Method2"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my cool step"), StubGherkinDocument.Instance);
        result.HasAmbiguous.Should().BeTrue();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(m => m.Contains("Method1"));
        result.Errors.Should().Contain(m => m.Contains("Method2"));
    }

    // Multiple step, some with binding/parameter errors => err,list


    [Fact]
    public void Contains_invalid_parameter_count()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my .*"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding(@"my invalid (\d+) with (.*) step",
            parameterTypes: GetParameterTypes("string")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my invalid 50 with extras step"), StubGherkinDocument.Instance);
        result.HasAmbiguous.Should().BeTrue();
        result.Items.Should().HaveCount(2);

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(m => m.Contains("parameter"));
    }

    // SO, but all ambiguous => err,list (of merged candidates)

    [Fact]
    public void Matches_multiple_stepdefs_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding(".* step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my .*"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other"}));
        result.Items.Should().HaveCount(2);
        result.Items.All(r => r.Type == MatchResultType.Ambiguous).Should().BeTrue();
    }
}
