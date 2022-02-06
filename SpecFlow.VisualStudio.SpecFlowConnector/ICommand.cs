namespace SpecFlowConnector;

public record CommandResult(string Json);

public interface ICommand
{
    CommandResult Execute(AssemblyLoadContext assemblyLoadContext);
}
