#nullable enable
namespace SpecFlow.VisualStudio.Tests.Discovery;

public sealed record StubGherkinDocument : IGherkinDocumentContext
{
    private StubGherkinDocument()
    {
    }

    public static StubGherkinDocument Instance { get; } = new();

    public IGherkinDocumentContext Parent => null!;
    public object Node => null!;
}
