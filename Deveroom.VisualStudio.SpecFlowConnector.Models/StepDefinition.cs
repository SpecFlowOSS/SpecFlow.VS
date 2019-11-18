namespace Deveroom.VisualStudio.SpecFlowConnector.Models
{
    public class StepDefinition
    {
        public string Type { get; set; }
        public string Regex { get; set; }
        public string Method { get; set; }
        public string ParamTypes { get; set; }
        public StepScope Scope { get; set; }

        public string SourceLocation { get; set; }

        #region Equality
        protected bool Equals(StepDefinition other)
        {
            return string.Equals(Type, other.Type) && string.Equals(Regex, other.Regex) && string.Equals(Method, other.Method) && string.Equals(Scope, other.Scope) && string.Equals(SourceLocation, other.SourceLocation);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StepDefinition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Regex != null ? Regex.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Method != null ? Method.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Scope != null ? Scope.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SourceLocation != null ? SourceLocation.GetHashCode() : 0);
                return hashCode;
            }
        }
        #endregion
    }
}
