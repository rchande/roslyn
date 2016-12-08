// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractRecommendationServiceBasedCompletionProvider : AbstractSymbolCompletionProvider
    {
        protected override Task<IEnumerable<ISymbol>> GetSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            return recommender.GetRecommendedSymbolsAtPositionAsync(context.Workspace, context.SemanticModel, position, options, cancellationToken);
        }

        protected override async Task<IEnumerable<ISymbol>> GetPreselectedSymbolsWorker(AbstractSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var recommender = context.GetLanguageService<IRecommendationService>();
            var typeInferrer = context.GetLanguageService<ITypeInferenceService>();

            var inferredTypes = typeInferrer.InferTypes(context.SemanticModel, position, cancellationToken)
                .Where(t => t.SpecialType != SpecialType.System_Void)
                .ToSet();
            if (inferredTypes.Count == 0)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var symbols = await recommender.GetRecommendedSymbolsAtPositionAsync(
                context.Workspace,
                context.SemanticModel,
                context.Position,
                options,
                cancellationToken).ConfigureAwait(false);

            // Don't preselect intrinsic type symbols so we can preselect their keywords instead.
            return symbols.Where(s => inferredTypes.Contains(GetSymbolType(s)) && !IsInstrinsic(s));
        }

        private ITypeSymbol GetSymbolType(ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                return ((IMethodSymbol)symbol).ReturnType;
            }

            return symbol.GetSymbolType();
        }

        protected override CompletionItem CreateItem(string displayText, string insertionText, int position, List<ISymbol> symbols, AbstractSyntaxContext context, TextSpan span, bool preselect, SupportedPlatformData supportedPlatformData)
        {
            var rules = GetCompletionItemRules(symbols, context, preselect);

            return SymbolCompletionItem.CreateWithNameAndKind(
                displayText: displayText,
                span: span,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0], displayText, context),
                contextPosition: context.Position,
                symbols: symbols,
                supportedPlatforms: supportedPlatformData,
                matchPriority: preselect ? MatchPriority.Preselect : MatchPriority.Default,
                rules: rules);
        }

        protected abstract CompletionItemRules GetCompletionItemRules(List<ISymbol> symbols, AbstractSyntaxContext context, bool preselect);

        protected abstract bool IsInstrinsic(ISymbol symbol);

        public override async Task<CompletionDescription> GetDescriptionAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var position = SymbolCompletionItem.GetContextPosition(item);
            var name = SymbolCompletionItem.GetSymbolName(item);
            var kind = SymbolCompletionItem.GetKind(item);
            var relatedDocumentIds = document.Project.Solution.GetRelatedDocumentIds(document.Id).Concat(document.Id);
            var options = document.Project.Solution.Workspace.Options;
            var totalSymbols = await base.GetPerContextSymbols(document, position, options, relatedDocumentIds, preselect: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var info in totalSymbols)
            {
                var bestSymbols = info.Item3.Where(s => kind != null && s.Kind == kind && s.Name == name).ToImmutableArray();
                if (bestSymbols.Any())
                {
                    return await SymbolCompletionItem.GetDescriptionAsync(item, bestSymbols, document, info.Item2.SemanticModel, cancellationToken).ConfigureAwait(false);
                }
            }

            return CompletionDescription.Empty;
        }
    }
}