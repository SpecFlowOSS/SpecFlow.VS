namespace SpecFlow.VisualStudio.SpecFlowConnector;

public record CommandResult(string Json);

public interface ICommand
{
    CommandResult Execute(Func<string, TestAssemblyLoadContext> testAssemblyLoadContext);
}
