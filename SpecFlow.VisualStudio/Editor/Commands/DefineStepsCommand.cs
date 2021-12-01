using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SpecFlow.VisualStudio.Diagnostics;
using SpecFlow.VisualStudio.Discovery;
using SpecFlow.VisualStudio.Editor.Commands.Infrastructure;
using SpecFlow.VisualStudio.Editor.Services;
using SpecFlow.VisualStudio.Monitoring;
using SpecFlow.VisualStudio.ProjectSystem;
using SpecFlow.VisualStudio.ProjectSystem.Actions;
using SpecFlow.VisualStudio.ProjectSystem.Settings;
using SpecFlow.VisualStudio.Snippets.Fallback;
using SpecFlow.VisualStudio.UI.ViewModels;
using Gherkin.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SpecFlow.VisualStudio.Editor.Services.Parser;
using SpecFlow.VisualStudio.SpecFlowConnector.Models;

namespace SpecFlow.VisualStudio.Editor.Commands
{
    [Export(typeof(IDeveroomFeatureEditorCommand))]
    public class DefineStepsCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
    {
        [ImportingConstructor]
        public DefineStepsCommand(IIdeScope ideScope, IBufferTagAggregatorFactoryService aggregatorFactory, IMonitoringService monitoringService) : 
            base(ideScope, aggregatorFactory, monitoringService)
        {
        }

        public override DeveroomEditorCommandTargetKey[] Targets => new[]
        {
            new DeveroomEditorCommandTargetKey(SpecFlowVsCommands.DefaultCommandSet, SpecFlowVsCommands.DefineStepsCommandId)
        };

        public override DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey)
        {
            var projectScope = IdeScope.GetProject(textView.TextBuffer);
            var projectSettings = projectScope?.GetProjectSettings();
            if (projectScope == null || !projectSettings.IsSpecFlowProject)
            {
                return DeveroomEditorCommandStatus.Disabled;
            }
            return base.QueryStatus(textView, commandKey);
        }

        public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey, IntPtr inArgs = default(IntPtr))
        {
            Logger.LogVerbose("Create Step Definitions");

            var projectScope = IdeScope.GetProject(textView.TextBuffer);
            var projectSettings = projectScope?.GetProjectSettings();
            if (projectScope == null || !projectSettings.IsSpecFlowProject)
            {
                IdeScope.Actions.ShowProblem("Define steps command can only be invoked for feature files in SpecFlow projects");
                return true;
            }

            var featureTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.FeatureBlock);
            if (featureTag == null)
            {
                Logger.LogWarning("Define steps command called for a file without feature block");
                return true;
            }

            var snippetService = projectScope.GetSnippetService();
            if (snippetService == null)
                return true;

            var undefinedStepTags = featureTag.GetDescendantsOfType(DeveroomTagTypes.UndefinedStep).ToArray();
            if (undefinedStepTags.Length == 0)
            {
                IdeScope.Actions.ShowProblem("All steps have been defined in this file already.");
                return true;
            }

            const string indent = "    ";
            string newLine = Environment.NewLine;

            var feature = (Feature)featureTag.Data;
            var viewModel = new CreateStepDefinitionsDialogViewModel();
            viewModel.ClassName = feature.Name.ToIdentifier()+"StepDefinitions";
            viewModel.ExpressionStyle = snippetService.DefaultExpressionStyle;

            foreach (var undefinedStepTag in undefinedStepTags)
            {
                var matchResult = (MatchResult)undefinedStepTag.Data;
                foreach (var match in matchResult.Items.Where(mi => mi.Type == MatchResultType.Undefined))
                {
                    var snippet = snippetService.GetStepDefinitionSkeletonSnippet(match.UndefinedStep, viewModel.ExpressionStyle, indent, newLine);
                    if (viewModel.Items.Any(i => i.Snippet == snippet))
                        continue;

                    viewModel.Items.Add(new StepDefinitionSnippetItemViewModel {Snippet = snippet});
                }
            }

            IdeScope.WindowManager.ShowDialog(viewModel);

            if (viewModel.Result == CreateStepDefinitionsDialogResult.Cancel)
                return true;

            if (viewModel.Items.Count(i => i.IsSelected) == 0)
            {
                IdeScope.Actions.ShowProblem("No snippet was selected");
                return true;
            }

            var combinedSnippet = string.Join(newLine, viewModel.Items.Where(i => i.IsSelected).Select(i => i.Snippet.Indent(indent + indent)));

            MonitoringService.MonitorCommandDefineSteps(viewModel.Result, viewModel.Items.Count(i => i.IsSelected));

            switch (viewModel.Result)
            {
                case CreateStepDefinitionsDialogResult.Create:
                    SaveAsStepDefinitionClass(projectScope, combinedSnippet, viewModel.ClassName, indent, newLine);
                    break;
                case CreateStepDefinitionsDialogResult.CopyToClipboard:
                    Logger.LogVerbose($"Copy to clipboard: {combinedSnippet}");
                    IdeScope.Actions.SetClipboardText(combinedSnippet);
                    break;
            }

            return true;
        }

        private void SaveAsStepDefinitionClass(IProjectScope projectScope, string combinedSnippet, string className,
            string indent, string newLine)
        {
            string targetFolder = projectScope.ProjectFolder;
            var projectSettings = projectScope.GetProjectSettings();
            var defaultNamespace = projectSettings.DefaultNamespace ?? projectScope.ProjectName;
            var fileNamespace = defaultNamespace;
            var stepDefinitionsFolder = Path.Combine(targetFolder, "StepDefinitions");
            if (IdeScope.FileSystem.Directory.Exists(stepDefinitionsFolder))
            {
                targetFolder = stepDefinitionsFolder;
                fileNamespace = fileNamespace + ".StepDefinitions";
            }

            var targetFilePath = Path.Combine(targetFolder, className + ".cs");

            if (IdeScope.FileSystem.File.Exists(targetFilePath))
            {
                if (IdeScope.Actions.ShowSyncQuestion("Overwrite file?",
                    $"The selected step definition file '{targetFilePath}' already exists. By overwriting the existing file you might loose work. {Environment.NewLine}Do you want to overwrite the file?",
                    defaultButton: MessageBoxResult.No) != MessageBoxResult.Yes)
                    return;
            }

            var template = $"using System;" + newLine +
                           $"using TechTalk.SpecFlow;" + newLine +
                           newLine +
                           $"namespace {fileNamespace}" + newLine +
                           $"{{" + newLine +
                           $"{indent}[Binding]" + newLine +
                           $"{indent}public class {className}" + newLine +
                           $"{indent}{{" + newLine +
                           combinedSnippet +
                           $"{indent}}}" + newLine +
                           $"}}" + newLine;

            projectScope.AddFile(targetFilePath, template);
            projectScope.IdeScope.Actions.NavigateTo(new SourceLocation(targetFilePath, 9, 1));

            _ = projectScope.IdeScope.RunOnBackgroundThread(()=>RebuildBindingRegistry(projectScope, targetFilePath, template), _=>{ });
        }

        private static Task RebuildBindingRegistry(IProjectScope projectScope, string targetFilePath, string template)
        {
            var discoveryService = projectScope.GetDiscoveryService();
            CSharpStepDefinitionFile stepDefinitionFile = new CSharpStepDefinitionFile(targetFilePath, template);
            return discoveryService.ProcessAsync(stepDefinitionFile);
        }
    }
}
