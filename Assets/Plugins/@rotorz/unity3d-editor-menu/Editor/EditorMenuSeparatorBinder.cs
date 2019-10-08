// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Binds additional information to an <see cref="EditorMenu"/> separator entry.
    /// </summary>
    public class EditorMenuSeparatorBinder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenuSeparatorBinder"/> class.
        /// </summary>
        /// <param name="entry">The associated separator entry.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="entry"/> is <c>null</c>.
        /// </exception>
        public EditorMenuSeparatorBinder(EditorMenuSeparatorEntry entry)
        {
            ExceptionUtility.CheckArgumentNotNull(entry, "entry");

            this.Entry = entry;
        }


        /// <summary>
        /// Gets the associated <see cref="EditorMenuSeparatorEntry"/> which can be
        /// modified as further information is provided.
        /// </summary>
        protected EditorMenuSeparatorEntry Entry { get; private set; }


        /// <summary>
        /// Adds an arbitary parameter to the <see cref="IEditorMenuEntry"/> that can
        /// assist <see cref="IEditorMenuPresenter"/> implementations by providing them
        /// with additional per-entry information.
        /// </summary>
        /// <param name="parameter">Custom parameter.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="parameter"/> is <c>null</c>.
        /// </exception>
        public EditorMenuSeparatorBinder Parameter(IEditorMenuEntryParameter parameter)
        {
            ExceptionUtility.CheckArgumentNotNull(parameter, "parameter");

            this.Entry.Parameters.Add(parameter);
            return this;
        }
    }
}
