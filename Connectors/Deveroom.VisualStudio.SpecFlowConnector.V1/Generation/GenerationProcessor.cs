using System;
using System.Diagnostics;
using System.IO;
using Deveroom.VisualStudio.SpecFlowConnector.AppDomainHelper;
using Deveroom.VisualStudio.SpecFlowConnector.Generation.V19;
using Deveroom.VisualStudio.SpecFlowConnector.Generation.V22;

namespace Deveroom.VisualStudio.SpecFlowConnector.Generation
{
    public class GenerationProcessor
    {
        private readonly GenerationOptions _options;

        public GenerationProcessor(GenerationOptions options)
        {
            _options = options;
        }

        public string Process()
        {
            var generatorAssemblyPath = Path.Combine(_options.SpecFlowToolsFolder, "TechTalk.SpecFlow.Generator.dll");
            using (AssemblyHelper.SubscribeResolveForAssembly(generatorAssemblyPath))
            {
                var specFlowAssemblyPath = Path.Combine(_options.SpecFlowToolsFolder, "TechTalk.SpecFlow.dll");
                var specFlowVersion = File.Exists(specFlowAssemblyPath) ? FileVersionInfo.GetVersionInfo(specFlowAssemblyPath) : null;

                var generatorType = typeof(SpecFlowV22Generator);
                if (specFlowVersion != null)
                {
                    var versionNumber =
                        ((specFlowVersion.FileMajorPart * 100) + specFlowVersion.FileMinorPart) * 1000 + specFlowVersion.FileBuildPart;

                    if (versionNumber >= 2_02_000)
                        generatorType = typeof(SpecFlowV22Generator);
                    else if (versionNumber >= 1_09_000)
                        generatorType = typeof(SpecFlowV19Generator);
                }

                var generator = (ISpecFlowGenerator)Activator.CreateInstance(generatorType);
                return generator.Generate(_options.ProjectFolder, _options.ConfigFilePath, _options.TargetExtension, _options.FeatureFilePath, _options.TargetNamespace, _options.ProjectDefaultNamespace, _options.SaveResultToFile);
            }
        }
    }
}
