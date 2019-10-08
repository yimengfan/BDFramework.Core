// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Additional flags which can be passed into reorderable list field.
    /// </summary>
    /// <example>
    /// <para>Multiple flags can be specified if desired:</para>
    /// <code language="csharp"><![CDATA[
    /// var flags = ReorderableListFlags.HideAddButton | ReorderableListFlags.HideRemoveButtons;
    /// ReorderableListGUI.ListField(list, flags);
    /// ]]></code>
    /// </example>
    [Flags]
    public enum ReorderableListFlags
    {
        /// <summary>
        /// Hide grab handles and disable reordering of list items.
        /// </summary>
        DisableReordering = 0x0001,
        /// <summary>
        /// Hide add button at base of control.
        /// </summary>
        HideAddButton = 0x0002,
        /// <summary>
        /// Hide remove buttons from list items.
        /// </summary>
        HideRemoveButtons = 0x0004,
        /// <summary>
        /// Do not display context menu upon right-clicking grab handle.
        /// </summary>
        DisableContextMenu = 0x0008,
        /// <summary>
        /// Hide "Duplicate" option from context menu.
        /// </summary>
        DisableDuplicateCommand = 0x0010,
        /// <summary>
        /// Do not automatically focus first control of newly added items.
        /// </summary>
        DisableAutoFocus = 0x0020,
        /// <summary>
        /// Show zero-based index of array elements.
        /// </summary>
        ShowIndices = 0x0040,
        /// <summary>
        /// Do not attempt to automatically scroll when list is inside a scroll view and
        /// the mouse pointer is dragged outside of the visible portion of the list.
        /// </summary>
        DisableAutoScroll = 0x0100,
        /// <summary>
        /// Show "Size" field at base of list control.
        /// </summary>
        ShowSizeField = 0x0200,
    }
}
