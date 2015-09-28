﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            public void ComputeModel(
                ICompletionService completionService,
                CompletionTriggerInfo triggerInfo,
                OptionSet options,
                IEnumerable<CompletionListProvider> completionProviders)
            {
                AssertIsForeground();

                // If we've already computed a model then we can just ignore this request and not
                // generate any tasks.
                if (this.Computation.InitialUnfilteredModels != null)
                {
                    return;
                }

                new ModelComputer(this, completionService, triggerInfo, options, completionProviders).Do();
            }

            private class ModelComputer : ForegroundThreadAffinitizedObject
            {
                private static readonly Func<string, List<CompletionItem>> s_createList = _ => new List<CompletionItem>();

                private readonly Session _session;
                private readonly ICompletionService _completionService;
                private readonly OptionSet _options;
                private readonly CompletionTriggerInfo _triggerInfo;
                private readonly SnapshotPoint _subjectBufferCaretPosition;
                private readonly SourceText _text;
                private readonly IEnumerable<CompletionListProvider> _completionProviders;

                private Document _documentOpt;
                private bool _includeBuilder;
                private readonly DisconnectedBufferGraph _disconnectedBufferGraph;

                public ModelComputer(
                    Session session,
                    ICompletionService completionService,
                    CompletionTriggerInfo triggerInfo,
                    OptionSet options,
                    IEnumerable<CompletionListProvider> completionProviders)
                {
                    _session = session;
                    _completionService = completionService;
                    _options = options;
                    _triggerInfo = triggerInfo;
                    _subjectBufferCaretPosition = session.Controller.TextView.GetCaretPoint(session.Controller.SubjectBuffer).Value;
                    _completionProviders = completionProviders;

                    _text = _subjectBufferCaretPosition.Snapshot.AsText();

                    _includeBuilder = session.Controller.SubjectBuffer.GetOption(Options.EditorCompletionOptions.UseSuggestionMode);

                    _disconnectedBufferGraph = new DisconnectedBufferGraph(session.Controller.SubjectBuffer, session.Controller.TextView.TextBuffer);
                }

                public void Do()
                {
                    AssertIsForeground();
                    _session.Computation.ChainTaskAndNotifyControllerWhenFinished(
                        (models, cancellationToken) => models != default(ImmutableArray<Model>) ? Task.FromResult(models) : DoInBackgroundAsync(cancellationToken));
                }

                private async Task<ImmutableArray<Model>> DoInBackgroundAsync(CancellationToken cancellationToken)
                {
                    using (Logger.LogBlock(FunctionId.Completion_ModelComputer_DoInBackground, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (_completionService == null || _options == null)
                        {
                            // both completionService and options can be null if given buffer is not registered to workspace yet.
                            // could happen in razor more frequently
                            return default(ImmutableArray<Model>);
                        }

                        // get partial solution from background thread.
                        _documentOpt = await _text.GetDocumentWithFrozenPartialSemanticsAsync(cancellationToken).ConfigureAwait(false);

                        // TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
                        // to the extension crashing.
                        var completionList = await GetCompletionListsAsync(_completionService, _triggerInfo, cancellationToken).ConfigureAwait(false);
                        if (completionList == null)
                        {
                            return default(ImmutableArray<Model>);
                        }

                        var trackingSpan = await _completionService.GetDefaultTrackingSpanAsync(_documentOpt, _subjectBufferCaretPosition, cancellationToken).ConfigureAwait(false);

                        var primaryListModel = Model.CreateModel(
                            _disconnectedBufferGraph,
                            trackingSpan,
                            completionList.Items,
                            selectedItem: completionList.Items.First(),
                            isHardSelection: false,
                            isUnique: false,
                            useSuggestionCompletionMode: _includeBuilder,
                            builder: completionList.Builder,
                            triggerInfo: _triggerInfo,
                            completionService: _completionService,
                            workspace: _documentOpt != null ? _documentOpt.Project.Solution.Workspace : null,
                            title: "All",
                            isSelected: false,
                            neverDismissIfEmpty: false);

                        var taggedLists = ExtractTaggedLists(completionList);

                        if (!taggedLists.Any())
                        {
                            // Go ahead and mark the sole model as selected
                            return new[] { primaryListModel.WithIsSelected(true) }.ToImmutableArray();
                        }

                        var otherLists = taggedLists.Select(kvp => Model.CreateModel(_disconnectedBufferGraph, trackingSpan, kvp.Value.ToImmutableArrayOrEmpty(), kvp.Value.First(),
                            isHardSelection: false, isUnique: false, useSuggestionCompletionMode: _includeBuilder, builder: completionList.Builder, triggerInfo: _triggerInfo,
                            completionService: _completionService, workspace: _documentOpt != null ? _documentOpt.Project.Solution.Workspace : null, title: kvp.Key,
                            isSelected: true, neverDismissIfEmpty: true));

                        return otherLists.Concat(primaryListModel).ToImmutableArray();
                    }
                }

                private Dictionary<string, List<CompletionItem>> ExtractTaggedLists(CompletionList completionList)
                {
                    var result = new Dictionary<string, List<CompletionItem>>();

                    foreach (var item in completionList.Items)
                    {
                        foreach (var tag in item.Tags)
                        {
                            var list = result.GetOrAdd(tag, s => new List<CompletionItem>());
                            list.Add(item);
                        }
                    }

                    return result;
                }

                private async Task<CompletionList> GetCompletionListsAsync(ICompletionService completionService, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
                {
                    return _documentOpt != null
                        ? await completionService.GetCompletionListAsync(_documentOpt, _subjectBufferCaretPosition, triggerInfo, _options, _completionProviders, cancellationToken).ConfigureAwait(false)
                        : null;
                }
            }
        }
    }
}
