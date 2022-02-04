namespace SpecFlowConnector.SourceDiscovery;

public abstract class DeveroomSymbolReader
{
    public abstract MethodSymbol ReadMethodSymbol(int token);
}
