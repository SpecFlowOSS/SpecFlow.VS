using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gherkin;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Editor.Services.Parser
{
    public class SemanticParserException : ParserException
    {
        public SemanticParserException(string message) : base(message)
        {
        }

        public SemanticParserException(string message, Location location) : base(message, location)
        {
        }
    }
}
