namespace SpecFlowConnector.Discovery;

public class SpecFlowDiscoverer {
    
    public DiscoveryResult Discover(
        IBindingRegistryFactory bindingRegistryFactory, 
        Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        var typeNames = ImmutableDictionary.CreateBuilder<string, string>();
   
        var stepDefinitions = new BindingRegistryAdapterAdapter(bindingRegistryFactory)
            .GetStepDefinitions(testAssembly, configFile)
            .Select(sdb => CreateStepDefinition(sdb, method => GetParamTypes(method, parameterTypeName => GetKey(typeNames, parameterTypeName))))
            .ToImmutableHashSet();

        return new DiscoveryResult(
            stepDefinitions,
            typeNames.ToImmutable(),
            null!
        );
    }

    private StepDefinition CreateStepDefinition(StepDefinitionBindingAdapter sdb, Func<BindingMethodAdapter, string?> getParameterTypes)
    {
        var stepDefinition = new StepDefinition
        (
            sdb.StepDefinitionType,
            sdb.Regex.Map(r=>r.ToString()).Reduce(()=>null!),
            sdb.Method.ToString(),
            getParameterTypes(sdb.Method)
        //Scope = GetScope(sdb),
        //SourceLocation = GetSourceLocation(sdb.Method, warningCollector),
        //Expression = GetSourceExpression(sdb),
        //Error = GetError(sdb)
        );

        return stepDefinition;
    }

    private string? GetParamTypes(BindingMethodAdapter bindingMethod, Func<string, string> getKey)
    {
        var paramTypes = string.Join("|", bindingMethod.ParameterTypeNames.Select(parameterTypeName => GetParamType(parameterTypeName, getKey)));
        return paramTypes.Length == 0 ? null : paramTypes;
    }

    private string GetParamType(string parameterTypeName, Func<string, string> getKey)
    {
        if (TypeShortcuts.FromType.TryGetValue(parameterTypeName, out var shortcut))
            return shortcut;

        var key = getKey(parameterTypeName);
        return $"#{key}";
    }

    private string GetKey(ImmutableDictionary<string, string>.Builder dictionary, string value)
    {
        if (!dictionary.TryGetValue(value, out var key))
        {
            key = dictionary.Count.ToString();
            dictionary.Add(value, key);
        }

        return key;
    }
}
