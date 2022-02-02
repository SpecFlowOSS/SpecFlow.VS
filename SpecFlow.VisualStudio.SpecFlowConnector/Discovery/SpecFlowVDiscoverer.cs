using System.Text.RegularExpressions;
using TechTalk.SpecFlow.Bindings;

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

        string specFlowVersion = typeof(IStepDefinitionBinding).Assembly.Location;
        
        return new DiscoveryResult(
            stepDefinitions,
            specFlowVersion,
            null!,
            ImmutableArray<string>.Empty
        );
    }

    private StepDefinition CreateStepDefinition(StepDefinitionBindingProxy sdb)
    {
        var stepDefinition = new StepDefinition
        (
            sdb.StepDefinitionType,
            sdb.Regex.Map(r=>r.ToString()).Reduce(()=>null!)
            //Method = sdb.Method.ToString(),
            //ParamTypes = GetParamTypes(sdb.Method),
            //Scope = GetScope(sdb),
            //SourceLocation = GetSourceLocation(sdb.Method, warningCollector),
            //Expression = GetSourceExpression(sdb),
            //Error = GetError(sdb)
        );

        return stepDefinition;
    }

}
