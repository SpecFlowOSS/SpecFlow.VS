namespace SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery
{
    class NullDeveroomSymbolReader : IDeveroomSymbolReader
    {
        public void Dispose()
        {
            //nop
        }

        public MethodSymbol ReadMethodSymbol(int token)
        {
            return null;
        }
    }
}