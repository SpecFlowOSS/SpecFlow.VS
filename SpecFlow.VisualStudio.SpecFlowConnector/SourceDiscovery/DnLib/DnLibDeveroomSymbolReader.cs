#nullable disable
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using ILogger = SpecFlowConnector.Logging.ILogger;

namespace SpecFlowConnector.SourceDiscovery.DnLib;

public class DnLibDeveroomSymbolReader : DeveroomSymbolReader
{
    private readonly ModuleDefMD _moduleDefMd;

    public DnLibDeveroomSymbolReader(ModuleDefMD moduleDefMd)
    {
        _moduleDefMd = moduleDefMd;
    }
    
    public static  DeveroomSymbolReader Create(ILogger log, string assemblyPath)
    {
        log.Info($"Creating {nameof(DnLibDeveroomSymbolReader)}");
        var moduleDefMd = ModuleDefMD.Load(assemblyPath);
        return new DnLibDeveroomSymbolReader(moduleDefMd);
    }

    public override IEnumerable<MethodSymbolSequencePoint> ReadMethodSymbol(int token)
    {
        var method = _moduleDefMd.ResolveMethod((uint)(token & 0x00FFFFFF));

        var stateClassType = GetStateClassType(method);
        if (stateClassType != null)
        {
            var sequencePoints = new List<MethodSymbolSequencePoint>();
            if (stateClassType != null)
                foreach (var typeMethod in stateClassType.Methods)
                    sequencePoints.AddRange(GetSequencePointsFromMethodBody(typeMethod));

            sequencePoints.AddRange(GetSequencePointsFromMethodBody(method));
            sequencePoints.Sort((sp1, sp2) => Comparer<int>.Default.Compare(sp1.StartLine, sp2.StartLine));
            return sequencePoints;
        }

        return GetSequencePointsFromMethodBody(method);
    }

    private TypeDef GetStateClassType(MethodDef method)
    {
        var stateMachineDebugInfo =
            method.CustomDebugInfos?.OfType<PdbStateMachineTypeNameCustomDebugInfo>().FirstOrDefault();
        if (stateMachineDebugInfo != null)
            return stateMachineDebugInfo.Type;
        var stateMachineAttr = method.CustomAttributes.FirstOrDefault(ca =>
            ca.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");
        return stateMachineAttr?.ConstructorArguments.Select(ca => ca.Value).OfType<TypeDefOrRefSig>().FirstOrDefault()
            ?.TypeDef;
    }

    private IEnumerable<MethodSymbolSequencePoint> GetSequencePointsFromMethodBody(MethodDef methodDef)
    {
        var methodBody = methodDef?.MethodBody as CilBody;
        if (methodBody == null)
            yield break;

        var relevantInstructions = methodBody.Instructions.Where(i => i.SequencePoint != null);
        var sequencePoints = relevantInstructions.Select(i => new MethodSymbolSequencePoint((int)i.Offset,
            GetSourcePath(i.SequencePoint.Document), i.SequencePoint.StartLine,
            i.SequencePoint.EndLine, i.SequencePoint.StartColumn, i.SequencePoint.EndColumn));
        foreach (var sequencePoint in sequencePoints)
            yield return sequencePoint;
    }

    private string GetSourcePath(PdbDocument document) => document.Url;

    public override string ToString() => $"{nameof(DnLibDeveroomSymbolReader)}({_moduleDefMd})";
}
