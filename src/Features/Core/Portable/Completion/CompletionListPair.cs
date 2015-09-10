using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    ///  Contains the primary completion list to present (and optional) the specialized list.
    /// </summary>
    internal class CompletionListPair
    {
        public CompletionList PrimaryList { get; }
        public CompletionList SpecializedList { get; }
        public string SpecializedListTitle { get; }

        public CompletionListPair(CompletionList primaryList, CompletionList specializedList, string specializedListTitle)
        {
            this.PrimaryList = primaryList;
            this.SpecializedList = specializedList;
            this.SpecializedListTitle = specializedListTitle;
        }
    }
}
