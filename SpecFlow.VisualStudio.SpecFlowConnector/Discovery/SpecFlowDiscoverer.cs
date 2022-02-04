namespace SpecFlowConnector.Discovery;

public class SpecFlowDiscoverer
{
    private readonly ILogger _log;
    private readonly SymbolReaderCache _symbolReaders;

    public SpecFlowDiscoverer(ILogger log)
    {
        _log = log;
        _symbolReaders = new SymbolReaderCache(log);
    }

    public DiscoveryResult Discover(
        IBindingRegistryFactory bindingRegistryFactory,
        Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        var typeNames = ImmutableDictionary.CreateBuilder<string, string>();

        var stepDefinitions = new BindingRegistryAdapterAdapter(bindingRegistryFactory)
            .GetStepDefinitions(testAssembly, configFile)
            .Select(sdb => CreateStepDefinition(sdb,
                method => GetParamTypes(method, parameterTypeName => GetKey(typeNames, parameterTypeName))))
            .ToImmutableHashSet();

        return new DiscoveryResult(
            stepDefinitions,
            typeNames.ToImmutable(),
            null!
        );
    }

    private StepDefinition CreateStepDefinition(StepDefinitionBindingAdapter sdb,
        Func<BindingMethodAdapter, string?> getParameterTypes)
    {
        var sourceLocation = GetSourceLocation(sdb.Method);
        var stepDefinition = new StepDefinition
        (
            sdb.StepDefinitionType,
            sdb.Regex.Map(r => r.ToString()).Reduce((string) null!),
            sdb.Method.ToString(),
            getParameterTypes(sdb.Method),
            GetScope(sdb),
            GetSourceExpression(sdb),
            GetError(sdb),
            sourceLocation.Reduce((string)null!)
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
        var paramTypes = string.Join("|",
            bindingMethod.ParameterTypeNames.Select(parameterTypeName => GetParamType(parameterTypeName, getKey)));
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
            stepDefinitionBinding.BindingScopeTag.Map(tag => $"@{tag}").Reduce((string) null!),
            stepDefinitionBinding.BindingScopeFeatureTitle,
            stepDefinitionBinding.BindingScopeScenarioTitle
        );
    }

    private static string? GetSourceExpression(StepDefinitionBindingAdapter sdb)
    {
        const string propertyName = "SourceExpression";
        return sdb.GetProperty<string>(propertyName).Reduce(() => GetSpecifiedExpressionFromRegex(sdb)!);
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

    private static string? GetError(StepDefinitionBindingAdapter sdb)
    {
        const string propertyName = "Error";
        return sdb.GetProperty<string>(propertyName).Reduce((string) null!);
    }


    private Option<string> GetSourceLocation(BindingMethodAdapter bindingMethod)
    {
        //if (bindingMethod is not RuntimeBindingMethod runtimeBindingMethod ||
        //    runtimeBindingMethod.MethodInfo.DeclaringType == null) return null;

        var runtimeBindingMethod = bindingMethod.MethodInfo
            .Map(mi => mi.DeclaringType)
            .Map(t => _symbolReaders[t.Assembly].Reduce(()=>default!))
            
            ;

        //.Reduce((Type) null!)
        //.Map(t => GetOrCreateSymbolReader(t.Assembly));
        return runtimeBindingMethod.ToString();

        //try
        //{
        //    var symbolReader = GetOrCreateSymbolReader(runtimeBindingMethod.MethodInfo.DeclaringType.Assembly,
        //        warningCollector);
        //    var methodSymbol = symbolReader.ReadMethodSymbol(runtimeBindingMethod.MethodInfo.MetadataToken);
        //    var startSequencePoint = methodSymbol?.SequencePoints?.FirstOrDefault(sp => !sp.IsHidden);
        //    if (startSequencePoint == null)
        //        return null;
        //    var sourceKey = GetKey(_sourceFiles, startSequencePoint.SourcePath);
        //    var sourceLocation = $"#{sourceKey}|{startSequencePoint.StartLine}|{startSequencePoint.StartColumn}";
        //    var endSequencePoint = methodSymbol.SequencePoints.LastOrDefault(sp => !sp.IsHidden);
        //    if (endSequencePoint != null)
        //        sourceLocation = sourceLocation + $"|{endSequencePoint.EndLine}|{endSequencePoint.EndColumn}";

        //    return sourceLocation;
        //}
        //catch (Exception ex)
        //{
        //    return ex;
        //}
    }

}
