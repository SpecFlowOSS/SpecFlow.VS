#if NETFRAMEWORK
// ReSharper disable once CheckNamespace
namespace System.Reflection.Metadata;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public readonly struct SequencePoint
{
    public const int HiddenLine = 0xfeefee;

    public SequencePoint(int ilOffset, string sourcePath, int startLine, int endLine, int startColumn, int endColumn)
    {
        IlOffset = ilOffset;
        SourcePath = sourcePath;
        StartLine = startLine;
        EndLine = endLine;
        StartColumn = startColumn;
        EndColumn = endColumn;
    }

    public bool IsHidden => StartLine == HiddenLine;

    public int IlOffset { get; }

    public string SourcePath { get; }

    public int StartLine { get; }

    public int EndLine { get; }

    public int StartColumn { get; }

    public int EndColumn { get; }
}
#endif