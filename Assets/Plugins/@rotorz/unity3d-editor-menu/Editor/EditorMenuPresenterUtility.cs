// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Utility functions to help with <see cref="IEditorMenuPresenter"/> usage.
    /// </summary>
    public static class EditorMenuPresenterUtility
    {
        /// <summary>
        /// Gets a default <see cref="IEditorMenuPresenter"/> instance.
        /// </summary>
        /// <remarks>
        /// <para>Unless the end-user project has a settings file that overrides the
        /// default <see cref="IEditorMenuPresenter"/> implementation; this will simply
        /// return an <see cref="EditorMenuPresenter_GenericMenu"/> instance.</para>
        /// </remarks>
        /// <returns>
        /// A <see cref="IEditorMenuPresenter"/> instance.
        /// </returns>
        /// <seealso cref="EditorMenuSettings.DefaultPresenterType"/>
        public static IEditorMenuPresenter GetDefaultPresenterInstance()
        {
            return (IEditorMenuPresenter)Activator.CreateInstance(EditorMenuSettings.Instance.DefaultPresenterType);
        }
    }
}
