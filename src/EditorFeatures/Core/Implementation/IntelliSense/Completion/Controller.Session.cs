﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session : Session<Controller, Model, ICompletionPresenterSession>
        {
            #region Fields that can be accessed from either thread

            private readonly CompletionRules _completionRules;

            // When we issue filter tasks, provide them with a (monotonically increasing) id.  That
            // way, when they run we can bail on computation if they've been superceded by another
            // filter task.  
            private int _filterId;

            #endregion

            public Session(Controller controller, ModelComputation<Model> computation, CompletionRules completionRules, ICompletionPresenterSession presenterSession)
                : base(controller, computation, presenterSession)
            {
                _completionRules = completionRules;

                this.PresenterSession.ItemCommitted += OnPresenterSessionItemCommitted;
                this.PresenterSession.ItemSelected += OnPresenterSessionItemSelected;

                // We need to track which (if any) of our completion sets is selected
                this.PresenterSession.CompletionListSelected += OnPresenterCompletionListSelected;
            }

            private void OnPresenterCompletionListSelected(object sender, CompletionListSelectedEventArgs e)
            {
                throw new System.NotImplementedException();
            }

            private ITextBuffer SubjectBuffer
            {
                get
                {
                    AssertIsForeground();
                    return this.Controller.SubjectBuffer;
                }
            }

            internal void UpdateModelTrackingSpan(SnapshotPoint initialPosition)
            {
                AssertIsForeground();
                var currentPosition = Controller.GetCaretPointInViewBuffer();

                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    model =>
                {
                    if (model == null)
                    {
                        return model;
                    }

                    // If our tracking point maps to the caret before the edit, it should also map to the 
                    // caret after the edit.  This is for the automatic brace completion scenario where
                    // we don't want the completion commit span to include the auto-inserted ')'
                    if (model.CommitTrackingSpanEndPoint.GetPosition(initialPosition.Snapshot) == initialPosition.Position)
                    {
                        return model.WithTrackingSpanEnd(currentPosition.Snapshot.Version.CreateTrackingPoint(currentPosition.Position, PointTrackingMode.Positive));
                    }

                    return model;
                });
            }

            public override void Stop()
            {
                AssertIsForeground();
                this.PresenterSession.ItemSelected -= OnPresenterSessionItemSelected;
                this.PresenterSession.ItemCommitted -= OnPresenterSessionItemCommitted;
                base.Stop();
            }

            private SnapshotPoint GetCaretPointInViewBuffer()
            {
                AssertIsForeground();
                return Controller.GetCaretPointInViewBuffer();
            }

            private void OnPresenterSessionItemCommitted(object sender, CompletionItemEventArgs e)
            {
                AssertIsForeground();
                Contract.ThrowIfFalse(ReferenceEquals(this.PresenterSession, sender));

                this.Controller.CommitItem(e.CompletionItem);
            }

            private void OnPresenterSessionItemSelected(object sender, CompletionItemEventArgs e)
            {
                AssertIsForeground();
                Contract.ThrowIfFalse(ReferenceEquals(this.PresenterSession, sender));

                SetModelSelectedItem(m => e.CompletionItem.IsBuilder ? m.DefaultBuilder : e.CompletionItem);
            }
        }
    }
}
