using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deveroom.VisualStudio.Monitoring;
using Gherkin;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Editor.Services.Parser
{
    public class DeveroomGherkinDocument : GherkinDocument
    {
        private readonly List<int> _statesForLines;
        public GherkinDialect GherkinDialect { get; }

        public DeveroomGherkinDocument(Feature feature, Comment[] comments, string sourceFilePath,
            GherkinDialect gherkinDialect, List<int> statesForLines) : base(feature, comments)
        {
            _statesForLines = statesForLines;
            GherkinDialect = gherkinDialect;
        }

        public TokenType[] GetExpectedTokens(int line, IMonitoringService monitoringService)
        {
            if (_statesForLines.Count <= line)
                return new TokenType[0];

            var state = _statesForLines[line];
            if (state < 0)
                return new TokenType[0];
            return DeveroomGherkinParser.GetExpectedTokens(state, monitoringService);
        }
    }
}
