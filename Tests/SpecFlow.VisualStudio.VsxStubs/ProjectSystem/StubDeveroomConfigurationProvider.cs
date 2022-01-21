namespace SpecFlow.VisualStudio.VsxStubs.ProjectSystem;

public class StubDeveroomConfigurationProvider : IDeveroomConfigurationProvider
{
    private readonly DeveroomConfiguration _configuration;

    public StubDeveroomConfigurationProvider(DeveroomConfiguration configuration)
    {
        _configuration = configuration;
    }

    public event EventHandler<EventArgs>? WeakConfigurationChanged;

    public DeveroomConfiguration GetConfiguration() => _configuration;

    public void InvokeWeakConfigurationChanged()
    {
        WeakConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }
}
