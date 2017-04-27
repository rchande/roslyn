﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Represents the various states that a command might have.
    /// </summary>
    internal struct CommandState2
    {
        /// <summary>
        /// If true, the command should be visible and enabled in the UI.
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        /// If true, the command should appear as checked (i.e. toggled) in the UI.
        /// </summary>
        public bool IsChecked { get; }

        /// <summary>
        /// If specified, returns the custom text that should be displayed in the UI.
        /// </summary>
        public string DisplayText { get; }

        public CommandState2(bool isAvailable = false, bool isChecked = false, string displayText = null)
            : this()
        {
            this.IsAvailable = isAvailable;
            this.IsChecked = isChecked;
            this.DisplayText = displayText;
        }

        public static CommandState2 Available
        {
            get { return new CommandState2(isAvailable: true); }
        }

        public static CommandState2 Unavailable
        {
            get { return new CommandState2(isAvailable: false); }
        }
    }
}
