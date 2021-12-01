using Location = Gherkin.Ast.Location;

namespace SpecFlow.VisualStudio.Editor.Services.Parser;

public class SemanticParserException : ParserException
{
    public SemanticParserException(string message) : base(message)
    {
    }

    public SemanticParserException(string message, Location location) : base(message, location)
    {
    }
}
