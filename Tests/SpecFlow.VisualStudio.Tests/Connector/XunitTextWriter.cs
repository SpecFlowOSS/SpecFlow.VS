namespace SpecFlow.VisualStudio.Tests.Connector;

internal class XunitTextWriter : TextWriter
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XunitTextWriter(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string message)
    {
        _testOutputHelper.WriteLine(message);
    }

    public override void WriteLine(string format, params object[] args)
    {
        _testOutputHelper.WriteLine(format, args);
    }

    public override void Write(char value)
    {
        throw new InvalidOperationException($"{nameof(ITestOutputHelper)} doesn't support this method");
    }
}
