// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// An <see cref="IEditorMenuPresenter"/> that presents an <see cref="EditorMenu"/>
    /// using the Unity <see cref="GenericMenu"/> API.
    /// </summary>
    /// <remarks>
    /// <para>This is the default implementation of <see cref="IEditorMenuPresenter"/>
    /// that is used unless an alternative implementation is supplied when constructing
    /// an <see cref="EditorMenu"/> or the project-wide setting <see cref="EditorMenuSettings.DefaultPresenterType"/>
    /// is set by the end-user.</para>
    /// <para>With this implementation adjacent separators are reduced into a single
    /// separator since it looks kinda goofy otherwise.</para>
    /// </remarks>
    public class EditorMenuPresenter_GenericMenu : IEditorMenuPresenter
    {
        private const string UNKNOWN_ENTRY_TYPE_LABEL = "«Unknown Entry Type»";


        /// <inheritdoc/>
        public void ShowAsContext(EditorMenu menu)
        {
            var genericMenu = this.BuildMenu(menu);
            if (genericMenu.GetItemCount() > 0) {
                genericMenu.ShowAsContext();
            }
        }

        /// <inheritdoc/>
        public void ShowAsDropdown(Rect position, EditorMenu menu)
        {
            var genericMenu = this.BuildMenu(menu);
            if (genericMenu.GetItemCount() > 0) {
                genericMenu.DropDown(position);
            }
        }


        /// <summary>
        /// Builds and returns a <see cref="GenericMenu"/> for the given <see cref="EditorMenu"/>.
        /// </summary>
        /// <param name="menu">Input menu data structure.</param>
        /// <returns>
        /// A new <see cref="GenericMenu"/> instance.
        /// </returns>
        public virtual GenericMenu BuildMenu(EditorMenu menu)
        {
            var genericMenu = new GenericMenu();
            var populatedPaths = new HashSet<string>();

            int entryIndex = -1;
            foreach (var entry in menu.Entries) {
                ++entryIndex;

                if (entry.EvaluateIsVisible() == false) {
                    continue;
                }

                var separatorEntry = entry as EditorMenuSeparatorEntry;
                if (separatorEntry != null) {
                    bool isFirstAtPath = !populatedPaths.Contains(entry.Path);
                    if (this.CanAddSeparatorEntryToMenu(menu, entryIndex, entry.Path, isFirstAtPath)) {
                        this.AddSeparatorEntryToMenu(genericMenu, separatorEntry);
                    }
                    continue;
                }

                populatedPaths.Add(entry.Path);

                this.AddEntryToMenu(genericMenu, entry);
            }

            return genericMenu;
        }

        /// <summary>
        /// Adds an entry to the <see cref="GenericMenu"/>.
        /// </summary>
        /// <param name="genericMenu">Output menu.</param>
        /// <param name="entry">Menu entry.</param>
        protected virtual void AddEntryToMenu(GenericMenu genericMenu, IEditorMenuEntry entry)
        {
            var commandEntry = entry as EditorMenuCommandEntry;
            if (commandEntry != null) {
                this.AddCommandEntryToMenu(genericMenu, commandEntry);
                return;
            }

            this.AddUnknownEntryToMenu(genericMenu, entry);
        }


        /// <summary>
        /// Adds a command entry to the <see cref="GenericMenu"/>.
        /// </summary>
        /// <example>
        /// <para>Can be overriden to provide special <see cref="EditorMenuCommandEntry"/>
        /// implementations:</para>
        /// <code language="csharp"><![CDATA[
        /// protected override void AddCommandEntryToMenu(GenericMenu menu, EditorMenuCommandEntry entry)
        /// {
        ///     var specialCommandEntry = entry as EditorMenuSpecialCommandEntry;
        ///     if (specialCommandEntry != null) {
        ///         this.AddSpecialCommandEntryToMenu(menu, specialCommandEntry);
        ///         return;
        ///     }
        ///
        ///     // Assume the default implementation.
        ///     base.AddCommandEntryToMenu(menu, entry);
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="genericMenu">Input menu data structure.</param>
        /// <param name="entry">Menu entry that is currently being added.</param>
        protected virtual void AddCommandEntryToMenu(GenericMenu genericMenu, EditorMenuCommandEntry entry)
        {
            var content = new GUIContent(entry.FullPath);
            if (entry.EvaluateIsEnabled()) {
                genericMenu.AddItem(content, entry.EvaluateIsChecked(), () => entry.InvokeAction());
            }
            else {
                genericMenu.AddDisabledItem(content);
            }
        }


        /// <summary>
        /// Finds the next visible menu entry at the given path.
        /// </summary>
        /// <param name="menu">Input menu data structure.</param>
        /// <param name="path">Path of the entry.</param>
        /// <param name="fromEntryIndex">Zero-based index of the starting entry (excluded).</param>
        /// <returns>
        /// The next visible <see cref="IEditorMenuEntry"/> instance when found;
        /// otherwise, a value of <c>null</c>.
        /// </returns>
        protected static IEditorMenuEntry FindNextVisibleMenuEntryAtPath(EditorMenu menu, string path, int fromEntryIndex)
        {
            for (int entryIndex = fromEntryIndex + 1; entryIndex < menu.Entries.Count; ++entryIndex) {
                var entry = menu.Entries[entryIndex];
                if (entry.Path == path && entry.EvaluateIsVisible()) {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines whether a separator can be added to the <see cref="GenericMenu"/>.
        /// </summary>
        /// <param name="menu">Input menu data structure.</param>
        /// <param name="entryIndex">Zero-based index of the separator entry.</param>
        /// <param name="path">Path of the separator entry.</param>
        /// <param name="isFirstAtPath">Indicates if the separator would be the first
        /// output entry at that path.</param>
        /// <returns>
        /// A value of <c>true</c> if the separator entry can be added to the output
        /// <see cref="GenericMenu"/>; else, a value of <c>false</c>.
        /// </returns>
        protected virtual bool CanAddSeparatorEntryToMenu(EditorMenu menu, int entryIndex, string path, bool isFirstAtPath)
        {
            // Don't output separator at the start of a path... it just doesn't look good!
            if (isFirstAtPath) {
                return false;
            }

            var nextEntryAtPath = FindNextVisibleMenuEntryAtPath(menu, path, entryIndex);

            // Don't output separator if no entry follows in same menu path.
            if (nextEntryAtPath == null) {
                return false;
            }
            // Don't output separator if next entry is also separator... it doesn't look good!
            if (nextEntryAtPath is EditorMenuSeparatorEntry) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a separator entry to the <see cref="GenericMenu"/>.
        /// </summary>
        /// <param name="genericMenu">Input menu data structure.</param>
        /// <param name="entry">Menu entry that is currently being added.</param>
        protected virtual void AddSeparatorEntryToMenu(GenericMenu genericMenu, EditorMenuSeparatorEntry entry)
        {
            genericMenu.AddSeparator(entry.Path);
        }


        /// <summary>
        /// Adds a placeholder for an unknown type of entry to the <see cref="GenericMenu"/>.
        /// </summary>
        /// <param name="genericMenu">Input menu data structure.</param>
        /// <param name="entry">Menu entry that is currently being added.</param>
        protected virtual void AddUnknownEntryToMenu(GenericMenu genericMenu, IEditorMenuEntry entry)
        {
            string label = UNKNOWN_ENTRY_TYPE_LABEL;

            if (entry.Path != "") {
                label = entry.Path + "/" + label;
            }

            genericMenu.AddDisabledItem(new GUIContent(label));
        }
    }
}
