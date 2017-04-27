﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using EditorCommanding = Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField
{
    internal abstract class AbstractEncapsulateFieldCommandHandler : VisualStudio.Text.UI.Commanding.ICommandHandler<EncapsulateFieldCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly AggregateAsynchronousOperationListener _listener;

        public bool InterestedInReadOnlyBuffer => false;

        public AbstractEncapsulateFieldCommandHandler(
            IWaitIndicator waitIndicator,
            ITextBufferUndoManagerProvider undoManager,
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _waitIndicator = waitIndicator;
            _undoManager = undoManager;
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.EncapsulateField);
        }

        private bool Execute(EncapsulateFieldCommandArgs args, IWaitContext waitContext)
        {
            using (var token = _listener.BeginAsyncOperation("EncapsulateField"))
            {
                var text = args.TextView.TextBuffer.CurrentSnapshot.AsText();
                var cancellationToken = waitContext.CancellationToken;
                if (!Workspace.TryGetWorkspace(text.Container, out var workspace))
                {
                    return false;
                }

                var documentId = workspace.GetDocumentIdInCurrentContext(text.Container);
                if (documentId == null)
                {
                    return false;
                }

                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    return false;
                }

                var spans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

                var service = document.GetLanguageService<AbstractEncapsulateFieldService>();

                var result = service.EncapsulateFieldAsync(document, spans.First().Span.ToTextSpan(), true, cancellationToken).WaitAndGetResult(cancellationToken);

                if (result == null)
                {
                    var notificationService = workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(EditorFeaturesResources.Please_select_the_definition_of_the_field_to_encapsulate, severity: NotificationSeverity.Error);
                    return false;
                }

                waitContext.AllowCancel = false;

                var finalSolution = result.GetSolutionAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                var previewService = workspace.Services.GetService<IPreviewDialogService>();
                if (previewService != null)
                {
                    finalSolution = previewService.PreviewChanges(
                        string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Encapsulate_Field),
                         "vs.csharp.refactoring.preview",
                        EditorFeaturesResources.Encapsulate_Field_colon,
                        result.GetNameAsync(cancellationToken).WaitAndGetResult(cancellationToken),
                        result.GetGlyphAsync(cancellationToken).WaitAndGetResult(cancellationToken),
                        finalSolution,
                        document.Project.Solution);
                }

                if (finalSolution == null)
                {
                    // User clicked cancel.
                    return true;
                }

                using (var undoTransaction = _undoManager.GetTextBufferUndoManager(args.SubjectBuffer).TextBufferUndoHistory.CreateTransaction(EditorFeaturesResources.Encapsulate_Field))
                {
                    if (!workspace.TryApplyChanges(finalSolution))
                    {
                        undoTransaction.Cancel();
                        return false;
                    }

                    undoTransaction.Complete();
                }

                return true;
            }
        }

        public VisualStudio.Text.UI.Commanding.CommandState GetCommandState(EncapsulateFieldCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return EditorCommanding.CommandState.CommandIsUnavailable;
            }

            var supportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return EditorCommanding.CommandState.CommandIsUnavailable;
            }

            return EditorCommanding.CommandState.CommandIsAvailable;
        }
        public bool ExecuteCommand(EncapsulateFieldCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var workspace = document.Project.Solution.Workspace;
            var supportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();
            if (!supportsFeatureService.SupportsRefactorings(document))
            {
                return false;
            }

            bool executed = false;
            _waitIndicator.Wait(
                title: EditorFeaturesResources.Encapsulate_Field,
                message: EditorFeaturesResources.Applying_Encapsulate_Field_refactoring,
                allowCancel: true,
                action: waitContext =>
                {
                    executed = Execute(args, waitContext);
                });

            return executed;
        }
    }
}
