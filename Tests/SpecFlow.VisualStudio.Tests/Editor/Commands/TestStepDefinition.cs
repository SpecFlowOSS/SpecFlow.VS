using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class TestStepDefinition : StepDefinition
    {
        private string _testExpression;
        private SourceLocation _testSourceLocation;

        public string TestExpression
        {
            get => _testExpression;
            set
            {
                _testExpression = value;
                Regex = $"^{_testExpression}$";
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