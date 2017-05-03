// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.Text.UI.Commanding;
using EditorCommands = Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using EditorCommanding = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<ReorderParametersCommandArgs>,
        ICommandHandler<RemoveParametersCommandArgs>,
        ICommandHandler<ExtractInterfaceCommandArgs>,
        EditorCommanding.ICommandHandler<EditorCommands.EncapsulateFieldCommandArgs>
    {
        public bool InterestedInReadOnlyBuffer => throw new NotImplementedException();

        public CommandState GetCommandState(ReorderParametersCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(ReorderParametersCommandArgs args, Action nextHandler)
        {
            CommitIfActiveAndCallNextHandler(args, nextHandler);
        }

        public CommandState GetCommandState(RemoveParametersCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(RemoveParametersCommandArgs args, Action nextHandler)
        {
            CommitIfActiveAndCallNextHandler(args, nextHandler);
        }

        public CommandState GetCommandState(ExtractInterfaceCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(ExtractInterfaceCommandArgs args, Action nextHandler)
        {
            CommitIfActiveAndCallNextHandler(args, nextHandler);
        }

        public VisualStudio.Text.UI.Commanding.CommandState GetCommandState(EditorCommands.EncapsulateFieldCommandArgs args) => EditorCommanding.CommandState.CommandIsUnavailable;
        public bool ExecuteCommand(EditorCommands.EncapsulateFieldCommandArgs args)
        {
            CommitIfActive(args.TextView);
            return false;
        }
    }
}
