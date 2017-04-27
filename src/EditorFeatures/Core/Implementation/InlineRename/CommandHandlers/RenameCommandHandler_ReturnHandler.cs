﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        public CommandState GetCommandState(ReturnKeyCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(ReturnKeyCommandArgs args)
        {
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Commit();
                (args.TextView as IWpfTextView).VisualElement.Focus();
                return true;
            }
            else
            {
                return false;   
            }
        }
    }
}
