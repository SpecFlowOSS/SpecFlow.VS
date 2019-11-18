using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Deveroom.VisualStudio.Diagonostics;
using Deveroom.VisualStudio.Discovery.TagExpressions;
using Deveroom.VisualStudio.Editor.Services.Parser;
using Deveroom.VisualStudio.SpecFlowConnector.Models;

namespace Deveroom.VisualStudio.Discovery
{
    public class BindingImporter
    {
        private static readonly string[] EmptyParameterTypes = new string[0];
        private static readonly string[] SingleStringParameterTypes = { TypeShortcuts.StringType };
        private static readonly string[] DoubleStringParameterTypes = { TypeShortcuts.StringType, TypeShortcuts.StringType };
        private static readonly string[] SingleIntParameterTypes = { TypeShortcuts.Int32Type };
        private static readonly string[] SingleDataTableParameterTypes = { TypeShortcuts.SpecFlowTableType };

        private readonly IDeveroomLogger _logger;
        private readonly Dictionary<string, string> _sourceFiles;
        private readonly Dictionary<string, string> _typeNames;
        private readonly TagExpressionParser _tagExpressionParser = new TagExpressionParser();
        private readonly Dictionary<string, ProjectStepDefinitionImplementation> _implementations = new Dictionary<string, ProjectStepDefinitionImplementation>();

        public BindingImporter(Dictionary<string, string> sourceFiles, Dictionary<string, string> typeNames, IDeveroomLogger logger)
        {
            _sourceFiles = sourceFiles;
            _typeNames = typeNames;
            _logger = logger;
        }

        public ProjectStepDefinitionBinding ImportStepDefinition(StepDefinition stepDefinition)
        {
            try
            {
                var stepDefinitionType = (ScenarioBlock)Enum.Parse(typeof(ScenarioBlock), stepDefinition.Type);
                var regex = new Regex(stepDefinition.Regex, RegexOptions.CultureInvariant);
                var sourceLocation = ParseSourceLocation(stepDefinition.SourceLocation);
                var scope = ParseScope(stepDefinition.Scope);
                var parameterTypes = ParseParameterTypes(stepDefinition.ParamTypes);

                if (!_implementations.TryGetValue(stepDefinition.Method, out var implementation))
                {
                    implementation = new ProjectStepDefinitionImplementation(stepDefinition.Method, parameterTypes, sourceLocation);
                    _implementations.Add(stepDefinition.Method, implementation);
                }
                

                return new ProjectStepDefinitionBinding(stepDefinitionType, regex, scope, implementation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Invalid binding: {ex.Message}");
                return null;
            }
        }

        private string[] ParseParameterTypes(string paramTypes)
        {
            if (string.IsNullOrWhiteSpace(paramTypes))
                return EmptyParameterTypes;
            switch (paramTypes)
            {
                case "s":
                    return SingleStringParameterTypes;
                case "i":
                    return SingleIntParameterTypes;
                case "s|s":
                    return DoubleStringParameterTypes;
                case "st":
                    return SingleDataTableParameterTypes;
            }

            var parts = paramTypes.Split('|');
            return parts.Select(ParseParameterType).ToArray();
        }

        private string ParseParameterType(string paramType)
        {
            paramType = paramType.Trim();

            if (TypeShortcuts.FromShortcut.TryGetValue(paramType, out var shortcutTypeName))
                return shortcutTypeName;

            if (paramType.StartsWith("#") && _typeNames != null)
            {
                if (_typeNames.TryGetValue(paramType.Substring(1), out var typeNameAtIndex))
                    paramType = typeNameAtIndex;
            }

            return paramType;
        }

        private SourceLocation ParseSourceLocation(string sourceLocation)
        {
            if (string.IsNullOrWhiteSpace(sourceLocation))
                return null;
            var parts = sourceLocation.Split('|');
            if (parts.Length <= 1 || !int.TryParse(parts[1], out var line))
                line = 1;
            if (parts.Length <= 2 || !int.TryParse(parts[2], out var column))
                column = 1;
            int? endLineOrNull = null;
            if (parts.Length > 3 && int.TryParse(parts[3], out var endLine))
                endLineOrNull = endLine;
            int? endColumnOrNull = null;
            if (parts.Length > 4 && int.TryParse(parts[4], out var endColumn))
                endColumnOrNull = endColumn;

            string sourceFile = parts[0];
            if (sourceFile.StartsWith("#") && _sourceFiles != null)
            {
                if (_sourceFiles.TryGetValue(sourceFile.Substring(1), out var sourceFileAtIndex))
                    sourceFile = sourceFileAtIndex;
            }

            return new SourceLocation(sourceFile, line, column, endLineOrNull, endColumnOrNull);
        }

        private Scope ParseScope(StepScope bindingScope)
        {
            if (bindingScope == null)
                return null;

            return new Scope
            {
                FeatureTitle = bindingScope.FeatureTitle,
                ScenarioTitle = bindingScope.ScenarioTitle,
                Tag = string.IsNullOrWhiteSpace(bindingScope.Tag)
                    ? null
                    : _tagExpressionParser.Parse(bindingScope.Tag)
            };
        }
    }
}
