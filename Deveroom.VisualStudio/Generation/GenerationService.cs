using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Deveroom.VisualStudio.Common;
using Deveroom.VisualStudio.Connectors;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Monitoring;
using Deveroom.VisualStudio.ProjectSystem;
using Deveroom.VisualStudio.ProjectSystem.Configuration;
using Deveroom.VisualStudio.ProjectSystem.Settings;
using Deveroom.VisualStudio.SpecFlowConnector.Models;

namespace Deveroom.VisualStudio.Generation
{
    public class GenerationService
    {
        private readonly IProjectScope _projectScope;
        private readonly IDeveroomLogger _logger;
        private IMonitoringService MonitoringService => _projectScope.IdeScope.MonitoringService;

        public GenerationService(IProjectScope projectScope)
        {
            _projectScope = projectScope;
            _logger = projectScope.IdeScope.Logger;
        }

        private string GetSpecFlowToolsFolderSafe(ProjectSettings projectSettings)
        {
            try
            {
                var specFlowToolsFolder = projectSettings.SpecFlowGeneratorFolder;
                if (specFlowToolsFolder == null)
                    throw new InvalidOperationException("Unable to generate feature-file code behind, the SpecFlow NuGet package folder could not be detected.");

                if (!_projectScope.IdeScope.FileSystem.Directory.Exists(specFlowToolsFolder))
                    _projectScope.IdeScope.Actions.ShowProblem($"Unable to find SpecFlow tools folder: '{specFlowToolsFolder}'. Build solution to ensure that all packages are restored. The feature file has to be re-generated (e.g. by saving) after the packages have been restored.");
                return specFlowToolsFolder;
            }
            catch (Exception ex)
            {
                _logger.LogException(MonitoringService, ex);
                return null;
            }
        }

        public GenerationResult GenerateFeatureFile(string featureFilePath, string targetExtension, string targetNamespace)
        {
            var projectSettings = _projectScope.GetProjectSettings();
            var specFlowToolsFolder = GetSpecFlowToolsFolderSafe(projectSettings);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var connector = new OutProcSpecFlowConnector(_projectScope.GetDeveroomConfiguration(), _logger, projectSettings.TargetFrameworkMoniker, _projectScope.IdeScope.GetExtensionFolder());

                var result = connector.RunGenerator(featureFilePath, projectSettings.SpecFlowConfigFilePath,
                    targetExtension, targetNamespace, _projectScope.ProjectFolder, specFlowToolsFolder);

                _projectScope.IdeScope.MonitoringService.MonitorSpecFlowGeneration(result.IsFailed, projectSettings);

                if (result.IsFailed)
                {
                    _logger.LogWarning(result.ErrorMessage);
                    result.FeatureFileCodeBehind = new FeatureFileCodeBehind()
                    {
                        FeatureFilePath = featureFilePath,
                        Content = GetErrorContent(result.ErrorMessage)
                    };
                    _logger.LogVerbose(() => result.FeatureFileCodeBehind.Content);
                }
                else
                {
                    _logger.LogInfo($"code-behind file generated for file {featureFilePath} in project {_projectScope.ProjectName}");
                    _logger.LogVerbose(() => result.FeatureFileCodeBehind.Content.Substring(0, Math.Min(450, result.FeatureFileCodeBehind.Content.Length)));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogException(MonitoringService, ex);
                return null;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogVerbose($"Generation: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        private string GetErrorContent(string resultErrorMessage)
        {
            var errorLines = resultErrorMessage.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return
                "#error " + errorLines[0] + Environment.NewLine +
                string.Join(Environment.NewLine,
                errorLines.Skip(1)
                    .Select(l => l.StartsWith("(") ? "#error " + l : "//" + l));
        }
    }
}
