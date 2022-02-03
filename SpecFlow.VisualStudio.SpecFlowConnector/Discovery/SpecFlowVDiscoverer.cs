namespace SpecFlowConnector.Discovery;

public class SpecFlowDiscoverer {
    
    public DiscoveryResult Discover(
        IBindingRegistryProxy bindingRegistry, 
        Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        var stepDefinitions = bindingRegistry
            .GetStepDefinitions(testAssembly, configFile)
            .Select(CreateStepDefinition)
            .ToImmutableHashSet();


        return new DiscoveryResult(
            stepDefinitions,
            null!
        );
    }

    private StepDefinition CreateStepDefinition(StepDefinitionBindingProxy sdb)
    {
        var stepDefinition = new StepDefinition
        (
            sdb.StepDefinitionType,
            sdb.Regex.Map(r=>r.ToString()).Reduce(()=>null!),
            sdb.Method
            //ParamTypes = GetParamTypes(sdb.Method),
            //Scope = GetScope(sdb),
            //SourceLocation = GetSourceLocation(sdb.Method, warningCollector),
            //Expression = GetSourceExpression(sdb),
            //Error = GetError(sdb)
        );

        return stepDefinition;
    }

}
