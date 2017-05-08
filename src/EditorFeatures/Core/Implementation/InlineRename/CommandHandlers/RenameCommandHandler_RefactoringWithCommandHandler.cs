// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using EditorCommands = Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSC.ICommandHandler<ReorderParametersCommandArgs>,
        VSC.ICommandHandler<RemoveParametersCommandArgs>,
        VSC.ICommandHandler<ExtractInterfaceCommandArgs>,
        VSC.ICommandHandler<EncapsulateFieldCommandArgs>
    {
        public bool InterestedInReadOnlyBuffer => throw new NotImplementedException();

        public VSC.CommandState GetCommandState(ReorderParametersCommandArgs argsr)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }

        public bool ExecuteCommand(ReorderParametersCommandArgs args)
        {
            CommitIfActive(args.TextView);
            return false;
        }

        public VSC.CommandState GetCommandState(RemoveParametersCommandArgs args)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }

        public bool ExecuteCommand(RemoveParametersCommandArgs args)
        {
            CommitIfActive(args.TextView);
            return false;
        }

        public VSC.CommandState GetCommandState(EditorCommands.ExtractInterfaceCommandArgs args)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }

        public bool ExecuteCommand(EditorCommands.ExtractInterfaceCommandArgs args)
        {
            CommitIfActive(args.TextView);
            return false;
        }

        public VisualStudio.Text.UI.Commanding.CommandState GetCommandState(EncapsulateFieldCommandArgs args) => VSC.CommandState.CommandIsUnavailable;
        public bool ExecuteCommand(EncapsulateFieldCommandArgs args)
        {
            CommitIfActive(args.TextView);
            return false;
        }
    }
}
