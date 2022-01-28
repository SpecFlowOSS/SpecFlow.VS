namespace SpecFlow.VisualStudio.SpecFlowConnector;

public record CommandResult(int Code, string Json);

public interface ICommand
{
    CommandResult Execute();
}
