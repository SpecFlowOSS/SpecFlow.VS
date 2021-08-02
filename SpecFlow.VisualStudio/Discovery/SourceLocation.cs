using Microsoft.VisualStudio.Text;

namespace SpecFlow.VisualStudio.Discovery
{
    public class SourceLocation
    {
        public string SourceFile { get; }
        public int SourceFileLine { get; } // 1-based
        public int SourceFileColumn { get; } // 1-based
        public int? SourceFileEndLine { get; } // 1-based
        public int? SourceFileEndColumn { get; } // 1-based

        public IPersistentSpan SourceLocationSpan { get; set; }

        public bool HasEndPosition => SourceFileEndLine != null && SourceFileEndColumn != null;

        public SourceLocation(string sourceFile, int sourceFileLine, int sourceFileColumn, int? sourceFileEndLine = null, int? sourceFileEndColumn = null)
        {
            SourceFile = sourceFile;
            SourceFileLine = sourceFileLine;
            SourceFileColumn = sourceFileColumn;
            SourceFileEndLine = sourceFileEndLine;
            SourceFileEndColumn = sourceFileEndColumn;
        }

        public override string ToString()
        {
            return $"{SourceFile}({SourceFileLine},{SourceFileColumn})";
        }
    }
}