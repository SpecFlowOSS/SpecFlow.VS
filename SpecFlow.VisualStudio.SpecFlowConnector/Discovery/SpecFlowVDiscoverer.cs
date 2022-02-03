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
            sdb.Regex.Map(r=>r.ToString()).Reduce((string)null!),
            sdb.Method.ToString(),
            getParameterTypes(sdb.Method),
            GetScope(sdb),
            GetSourceExpression(sdb)
            //Error = GetError(sdb)
            //SourceLocation = GetSourceLocation(sdb.Method, warningCollector),
        );

        return stepDefinition;
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

    private static StepScope? GetScope(StepDefinitionBindingAdapter stepDefinitionBinding)
    {
        if (!stepDefinitionBinding.IsScoped)
            return null;

        return new StepScope(
            stepDefinitionBinding.BindingScopeTag.Map(tag=>$"@{tag}").Reduce((string)null!),
            stepDefinitionBinding.BindingScopeFeatureTitle,
            stepDefinitionBinding.BindingScopeScenarioTitle
        );
    }

    private static string? GetSourceExpression(StepDefinitionBindingAdapter sdb)
    {
        const string propertyName = "SourceExpression";
        return sdb.GetProperty<string>(propertyName).Reduce(()=>GetSpecifiedExpressionFromRegex(sdb)!);
    }

    private static string? GetSpecifiedExpressionFromRegex(StepDefinitionBindingAdapter sdb)
    {
        return sdb.Regex
            .Map(regex => regex.ToString())
            .Map(regexString =>
            {
                if (regexString.StartsWith("^"))
                    regexString = regexString.Substring(1);
                if (regexString.EndsWith("$"))
                    regexString = regexString.Substring(0, regexString.Length - 1);
                return regexString;
            })
            .Reduce((string) null!);
    }
}
