﻿using SpecFlowConnector.Tests;

namespace SpecFlowConnector.Discovery;

public class SpecFlowDiscoverer
{
    private readonly ILogger _log;
    private readonly SymbolReaderCache _symbolReaders;

    public SpecFlowDiscoverer() : this(new StringBuilderLogger())
    {
    }

    public SpecFlowDiscoverer(ILogger log)
    {
        _log = log;
        _symbolReaders = new SymbolReaderCache(log);
    }

    public DiscoveryResult Discover(IBindingRegistryFactory bindingRegistryFactory,
        AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly,
        Option<FileDetails> configFile)
    {
        var typeNames = ImmutableDictionary.CreateBuilder<string, string>();
        var sourcePaths = ImmutableDictionary.CreateBuilder<string, string>();

        var stepDefinitions = new BindingRegistryAdapterAdapter(bindingRegistryFactory)
            .GetStepDefinitions(assemblyLoadContext, configFile, testAssembly)
            .Select(sdb => CreateStepDefinition(sdb,
                method => GetParamTypes(method, parameterTypeName => GetKey(typeNames, parameterTypeName)),
                sourcePath => GetKey(sourcePaths, sourcePath))
            )
            .ToImmutableHashSet();

        return new DiscoveryResult(
            stepDefinitions,
            sourcePaths.ToImmutable(),
            typeNames.ToImmutable()
        );
    }

    private StepDefinition CreateStepDefinition(StepDefinitionBindingAdapter sdb,
        Func<BindingMethodAdapter, string?> getParameterTypes, Func<string, string> getSourcePathId)
    {
        var sourceLocation = GetSourceLocation(sdb.Method, getSourcePathId);
        var stepDefinition = new StepDefinition
        (
            sdb.StepDefinitionType,
            sdb.Regex.Map(r => r.ToString()).Reduce((string) null!),
            sdb.Method.ToString(),
            getParameterTypes(sdb.Method),
            GetScope(sdb),
            GetSourceExpression(sdb),
            GetError(sdb),
            sourceLocation.Reduce((string) null!)
        );

        return stepDefinition;
    }

    private string GetKey(ImmutableDictionary<string, string>.Builder dictionary, string value)
    {
        KeyValuePair<string, string> found = dictionary
            .FirstOrDefault(kvp => kvp.Value == value);
        if (found.Key is null)
        {
            found = new KeyValuePair<string, string>(dictionary.Count.ToString(), value);
            dictionary.Add(found);
        }

        return found.Key;
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
        => sdb.GetProperty<string>("SourceExpression").Reduce(() => GetSpecifiedExpressionFromRegex(sdb)!);

    private static string? GetSpecifiedExpressionFromRegex(StepDefinitionBindingAdapter sdb) =>
        sdb.Regex
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

    private static string? GetError(StepDefinitionBindingAdapter sdb) 
        => sdb.GetProperty<string>("Error").Reduce((string) null!);

    private Option<string> GetSourceLocation(BindingMethodAdapter bindingMethod, Func<string, string> getSourcePathId)
    {
        return bindingMethod.MethodInfo
            .Map(mi => (reader: mi.DeclaringType
                        .Map(declaringType => declaringType!.Assembly)
                        .Map(assembly => _symbolReaders[assembly].Reduce(() => default!)),
                    mi.MetadataToken
                ))
            .Map(x => x.reader.ReadMethodSymbol(x.MetadataToken))
            .Reduce(ImmutableArray<MethodSymbolSequencePoint>.Empty)
            .Map(sequencePoints => sequencePoints
                .Select(sp => (Option<MethodSymbolSequencePoint>) sp)
                .DefaultIfEmpty(None.Value)
                .Aggregate(
                    (startSequencePoint: None<MethodSymbolSequencePoint>.Value,
                        endSequencePoint: None<MethodSymbolSequencePoint>.Value),
                    (acc, cur) => acc.startSequencePoint is None<MethodSymbolSequencePoint>
                        ? (cur, cur)
                        : (acc.startSequencePoint, cur)
                )
                .Map(x => x.startSequencePoint is Some<MethodSymbolSequencePoint> some
                    ? (some.Content, ((Some<MethodSymbolSequencePoint>) x.endSequencePoint).Content)
                    : None<(MethodSymbolSequencePoint startSequencePoint, MethodSymbolSequencePoint endSequencePoint)>
                        .Value)
            )
            .Map(border =>
                $"#{getSourcePathId(border.startSequencePoint.SourcePath)}|{border.startSequencePoint.StartLine}|{border.startSequencePoint.StartColumn}|{border.endSequencePoint.EndLine}|{border.endSequencePoint.EndColumn}");
    }
}
