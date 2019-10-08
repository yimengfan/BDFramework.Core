// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Can be implemented along with <see cref="IReorderableListAdaptor"/> when drop
    /// insertion or ordering is desired.
    /// </summary>
    /// <remarks>
    /// <para>This type of "drop" functionality can occur when the "drag" phase of the
    /// drag and drop operation was initiated elsewhere. For example, a custom
    /// <see cref="IReorderableListAdaptor"/> could insert entirely new items by
    /// dragging and dropping from the Unity "Project" window.</para>
    /// </remarks>
    /// <see cref="IReorderableListAdaptor"/>
    public interface IReorderableListDropTarget
    {
        /// <summary>
        /// Determines whether an item is being dragged and that it can be inserted
        /// or moved by dropping somewhere into the reorderable list control.
        /// </summary>
        /// <remarks>
        /// <para>This method is always called whilst drawing an editor GUI.</para>
        /// </remarks>
        /// <param name="insertionIndex">Zero-based index of insertion.</param>
        /// <returns>
        /// A value of <c>true</c> if item can be dropped; otherwise <c>false</c>.
        /// </returns>
        /// <see cref="UnityEditor.DragAndDrop"/>
        bool CanDropInsert(int insertionIndex);

        /// <summary>
        /// Processes the current drop insertion operation when <see cref="CanDropInsert(int)"/>
        /// returns a value of <c>true</c> to process, accept or cancel.
        /// </summary>
        /// <remarks>
        /// <para>This method is always called whilst drawing an editor GUI.</para>
        /// <para>This method is only called when <see cref="CanDropInsert(int)"/>
        /// returns a value of <c>true</c>.</para>
        /// </remarks>
        /// <param name="insertionIndex">Zero-based index of insertion.</param>
        /// <see cref="ReorderableListGUI.CurrentListControlID"/>
        /// <see cref="UnityEditor.DragAndDrop"/>
        void ProcessDropInsertion(int insertionIndex);
    }
}
