namespace SpecFlow.VisualStudio.Editor.Commands
{
    public record Issue
    {
        public enum IssueKind
        {
            Problem, Notification
        }

        public Issue(IssueKind kind, string description)
        {
            Kind = kind;
            Description = description;
        }

        public IssueKind Kind { get; }
        public string Description { get; }

        public override string ToString()
        {
            return $"{Kind,10} {Description}";
        }
    }
}