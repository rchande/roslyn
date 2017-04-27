﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<AutomaticLineEnderCommandArgs>.GetCommandState(AutomaticLineEnderCommandArgs args)
        {
            AssertIsForeground();
            return CommandState.CommandIsUnavailable;
        }

        bool ICommandHandler<AutomaticLineEnderCommandArgs>.ExecuteCommand(AutomaticLineEnderCommandArgs args)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // No computation.  Nothing to do.  Just let the editor handle this.
                return false;
            }

            CommitOnEnter(out var sendThrough, out var committed);

            // We did not commit based on enter.  So our computation will still be running.  Stop it now.
            if (!committed)
            {
                this.DismissSessionIfActive();
                return false;
            }

            return true;
        }
    }
}
