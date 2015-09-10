using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class CompletionListSelectedEventArgs
    {
        private CompletionSet newValue;
        private CompletionSet oldValue;

        public CompletionListSelectedEventArgs(CompletionSet oldValue, CompletionSet newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }
    }
}