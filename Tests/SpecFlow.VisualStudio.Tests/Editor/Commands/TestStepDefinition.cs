using Microsoft.CodeAnalysis;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class TestStepDefinition : StepDefinition
    {
        private SourceLocation _testSourceLocation;
        private SyntaxToken _testExpression;

        public SyntaxToken TestExpression
        {
            get => _testExpression;
            set { _testExpression = value;
                Regex = $"^{_testExpression.ValueText}$";
            }
        }

        public SourceLocation TestSourceLocation
        {
            get => _testSourceLocation;
            set
            {
                _testSourceLocation = value;
                SourceLocation =
                    $"{_testSourceLocation.SourceFile}|{_testSourceLocation.SourceFileLine}|{_testSourceLocation.SourceFileColumn}";
            }
        }
    }
}