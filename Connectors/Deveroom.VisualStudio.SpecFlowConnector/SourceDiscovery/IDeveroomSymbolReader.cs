using System;

namespace Deveroom.VisualStudio.SpecFlowConnector.SourceDiscovery
{
    public class MethodSymbol
    {
        public int Token { get; }
        public SequencePoint[] SequencePoints { get; }

        public MethodSymbol(int token, SequencePoint[] sequencePoints)
        {
            Token = token;
            SequencePoints = sequencePoints;
        }
    }

    public interface IDeveroomSymbolReader : IDisposable
    {
        MethodSymbol ReadMethodSymbol(int token);
    }
}
