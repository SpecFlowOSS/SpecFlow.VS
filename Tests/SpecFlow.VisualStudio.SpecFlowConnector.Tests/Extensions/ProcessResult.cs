namespace SpecFlow.VisualStudio.SpecFlowConnector.Tests.Extensions;

public record ProcessResult(
    int ExitCode, 
    string StdOutput, 
    string StdError,
    TimeSpan ExecutionTime);