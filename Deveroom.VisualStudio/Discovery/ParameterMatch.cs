using System;
using System.Linq;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Discovery
{
    public class ParameterMatch
    {
        public static readonly ParameterMatch Empty = new ParameterMatch(new MatchedStepTextParameter[0], null, new string[0]);

        public MatchedStepTextParameter[] StepTextParameters { get; }
        public StepArgument StepArgument { get; }
        public string[] ParameterTypes { get; }
        public string Error { get; }

        public bool HasError => Error != null;
        public bool MatchedDataTable => StepArgument is DataTable;
        public string DataTableParameterType => MatchedDataTable ? ParameterTypes.LastOrDefault() : null;
        public bool MatchedDocString => StepArgument is DocString;
        public string DocStringParameterType => MatchedDocString ? ParameterTypes.LastOrDefault() : null;

        public ParameterMatch(MatchedStepTextParameter[] stepTextParameters, StepArgument stepArgument, string[] parameterTypes, string error = null)
        {
            StepTextParameters = stepTextParameters ?? throw new ArgumentNullException(nameof(stepTextParameters));
            StepArgument = stepArgument;
            ParameterTypes = parameterTypes ?? throw new ArgumentNullException(nameof(parameterTypes));
            Error = error;
        }
    }
}