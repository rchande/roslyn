﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal override void OnCaretPositionChanged(object sender, EventArgs args)
        {
            AssertIsForeground();

            if (!IsSessionActive)
            {
                // No session, so we don't need to do anything.
                return;
            }

            // If we have a session active then it may be in the process of computing results.  If it
            // has computed the results, then compare where the caret is with all the items.  If the
            // caret isn't within the bounds of the items, then we dismiss completion.
            var caretPoint = this.GetCaretPointInViewBuffer();
            var model = sessionOpt.Computation.InitialUnfilteredModel;
            if (model == null ||
                this.IsCaretOutsideAllItemBounds(model, caretPoint))
            {
                // Completions hadn't even been computed yet or the caret is out of bounds.  
                // Just cancel everything we're doing.
                this.StopComputationAndDismissPresentation();
            }
        }

        internal bool IsCaretOutsideAllItemBounds(Model model, SnapshotPoint caretPoint)
        {
            var textSpanToTextCache = new Dictionary<TextSpan, string>();
            var textSpanToViewSpanCache = new Dictionary<TextSpan, ViewTextSpan>();

            foreach (var item in model.TotalItems)
            {
                if (!IsCaretOutsideItemBounds(model, caretPoint, item, textSpanToTextCache, textSpanToViewSpanCache))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsCaretOutsideItemBounds(
            Model model,
            SnapshotPoint caretPoint,
            CompletionItem item,
            Dictionary<TextSpan, string> textSpanToText,
            Dictionary<TextSpan, ViewTextSpan> textSpanToViewSpan)
        {
            // Easy first check.  See if the caret point is before the start of the item.
            ViewTextSpan filterSpanInViewBuffer;
            if (!textSpanToViewSpan.TryGetValue(item.FilterSpan, out filterSpanInViewBuffer))
            {
                filterSpanInViewBuffer = model.GetSubjectBufferFilterSpanInViewBuffer(item.FilterSpan);
                textSpanToViewSpan[item.FilterSpan] = filterSpanInViewBuffer;
            }

            if (caretPoint < filterSpanInViewBuffer.TextSpan.Start)
            {
                return true;
            }

            var textSnapshot = caretPoint.Snapshot;

            var currentText = model.GetCurrentTextInSnapshot(item.FilterSpan, textSnapshot, textSpanToText);
            var currentTextSpan = new TextSpan(filterSpanInViewBuffer.TextSpan.Start, currentText.Length);

            return !currentTextSpan.IntersectsWith(caretPoint);
        }
    }
}
