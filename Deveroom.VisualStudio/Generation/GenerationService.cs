using System;
using System.Diagnostics;
using System.Linq;
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

        public static bool CheckSpecFlowToolsFolder(IProjectScope projectScope)
        {
            var toolsFolder = GetSpecFlowToolsFolderSafe(projectScope, projectScope.GetProjectSettings(), out _);
            return toolsFolder != null;
        }

        private static string GetSpecFlowToolsFolderSafe(IProjectScope projectScope, ProjectSettings projectSettings, out string toolsFolderErrorMessage)
        {
            toolsFolderErrorMessage = null;
            try
            {
                var specFlowToolsFolder = projectSettings.SpecFlowGeneratorFolder;
                if (string.IsNullOrEmpty(specFlowToolsFolder))
                {
                    projectScope.IdeScope.Actions.ShowProblem($"Unable to generate feature-file code behind, because SpecFlow NuGet package folder could not be detected. For configuring SpecFlow tools folder manually, check http://speclink.me/devrsftools.");
                    toolsFolderErrorMessage = "Folder is not configured. See http://speclink.me/devrsftools for details.";
                    return null;
                }

                if (!projectScope.IdeScope.FileSystem.Directory.Exists(specFlowToolsFolder))
                {
                    projectScope.IdeScope.Actions.ShowProblem($"Unable to find SpecFlow tools folder: '{specFlowToolsFolder}'. Build solution to ensure that all packages are restored. The feature file has to be re-generated (e.g. by saving) after the packages have been restored.");
                    toolsFolderErrorMessage = "Folder does not exist";
                    return null;
                }

                return specFlowToolsFolder;
            }
            catch (Exception ex)
            {
                projectScope.IdeScope.Logger.LogException(projectScope.IdeScope.MonitoringService, ex);
                toolsFolderErrorMessage = ex.Message;
                return null;
            }
        }

        public GenerationResult GenerateFeatureFile(string featureFilePath, string targetExtension, string targetNamespace)
        {
            var projectSettings = _projectScope.GetProjectSettings();
            var specFlowToolsFolder = GetSpecFlowToolsFolderSafe(_projectScope, projectSettings, out var toolsFolderErrorMessage);
            if (specFlowToolsFolder == null)
                return CreateErrorResult(featureFilePath, $"Unable to use SpecFlow tools folder '{projectSettings.SpecFlowGeneratorFolder}': {toolsFolderErrorMessage}");

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
                    SetErrorContent(featureFilePath, result);
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
                return CreateErrorResult(featureFilePath, ex.Message);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogVerbose($"Generation: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        private GenerationResult CreateErrorResult(string featureFilePath, string errorMessage)
        {
            var result = new GenerationResult
            {
                ErrorMessage = errorMessage
            };
            SetErrorContent(featureFilePath, result);
            return result;
        }

        private void SetErrorContent(string featureFilePath, GenerationResult result)
        {
            result.FeatureFileCodeBehind = new FeatureFileCodeBehind()
            {
                FeatureFilePath = featureFilePath,
                Content = GetErrorContent(result.ErrorMessage)
            };
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
