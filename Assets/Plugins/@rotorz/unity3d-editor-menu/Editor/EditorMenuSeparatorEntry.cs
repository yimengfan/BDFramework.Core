// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// An <see cref="EditorMenu"/> entry that describes a separator.
    /// </summary>
    public class EditorMenuSeparatorEntry : EditorMenuEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenuSeparatorEntry"/> class.
        /// </summary>
        /// <param name="path">Path of the separator.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="path"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="path"/> is an invalid full command path.
        /// </exception>
        public EditorMenuSeparatorEntry(string path)
        {
            ExceptionUtility.CheckArgumentNotNull(path, "path");
            EditorMenuInternalUtility.CheckPathArgument(path, "path");

            this.Path = path;
        }
    }
}
