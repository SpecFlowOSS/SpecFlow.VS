namespace SpecFlow.VisualStudio.Configuration
{
    public class EditorConfiguration
    {
        public bool ShowStepCompletionAfterStepKeywords { get; set; } = true;

        public void CheckConfiguration()
        {
            // nop
        }

        #region Equality

        protected bool Equals(EditorConfiguration other)
        {
            return ShowStepCompletionAfterStepKeywords == other.ShowStepCompletionAfterStepKeywords;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EditorConfiguration)obj);
        }

        public override int GetHashCode()
        {
            return ShowStepCompletionAfterStepKeywords.GetHashCode();
        }

        #endregion
    }
}