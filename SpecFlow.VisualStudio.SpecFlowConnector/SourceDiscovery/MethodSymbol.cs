namespace SpecFlowConnector.SourceDiscovery;

#if NETFRAMEWORK
struct SequencePoint
{
    public const int HiddenLine = 1;
}
#else
    using System.Reflection.Metadata;
#endif

public record MethodSymbolSequencePoint(
    int IlOffset, 
    string SourcePath,
    int StartLine,
    int EndLine,
    int StartColumn,
    int EndColumn)
{
    public bool IsHidden => StartLine == SequencePoint.HiddenLine;
}