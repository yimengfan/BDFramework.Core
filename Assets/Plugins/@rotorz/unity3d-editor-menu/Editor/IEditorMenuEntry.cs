// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// The interface of an <see cref="EditorMenu"/> entry.
    /// </summary>
    public interface IEditorMenuEntry
    {
        /// <summary>
        /// Gets the path of the menu entry; for instance, the menu item <b>Sub-Menu > Abc</b>
        /// would have a path of "Sub-Menu".
        /// </summary>
        string Path { get; }


        /// <summary>
        /// Gets or sets a predicate that determines whether the entry is visible.
        /// </summary>
        Func<bool> IsVisiblePredicate { get; set; }


        /// <summary>
        /// Gets an editable collection of custom parameters that can assist <see cref="IEditorMenuPresenter"/>
        /// implementations by allowing additional information to plugged into entries.
        /// </summary>
        /// <seealso cref="HasParameters"/>
        ICollection<IEditorMenuEntryParameter> Parameters { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IEditorMenuEntry"/> has any
        /// parameters. This property should be tested before accessing <see cref="Parameters"/>.
        /// </summary>
        /// <seealso cref="Parameters"/>
        bool HasParameters { get; }


        /// <summary>
        /// Evaluates whether the entry is visible.
        /// </summary>
        /// <returns>
        /// A value of <c>true</c> if the entry is visible; otherwise, a value of <c>false</c>.
        /// </returns>
        bool EvaluateIsVisible();
    }
}
