// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// The interface of an object that presents an <see cref="EditorMenu"/> data
    /// structure to the end-user.
    /// </summary>
    /// <seealso cref="EditorMenu"/>
    /// <seealso cref="EditorMenuPresenter_GenericMenu"/>
    public interface IEditorMenuPresenter
    {
        /// <summary>
        /// Shows the <see cref="EditorMenu"/> as a context menu.
        /// </summary>
        /// <param name="menu">The editor menu.</param>
        void ShowAsContext(EditorMenu menu);

        /// <summary>
        /// Shows the <see cref="EditorMenu"/> as a drop-down from a GUI control.
        /// </summary>
        /// <param name="position">Absolute position of GUI drop-down control.</param>
        /// <param name="menu">The editor menu.</param>
        void ShowAsDropdown(Rect position, EditorMenu menu);
    }
}
