// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Essentially the model of a custom editor menu.
    /// </summary>
    /// <seealso cref="IEditorMenuPresenter"/>
    /// <seealso cref="EditorMenuPresenter_GenericMenu"/>
    public class EditorMenu
    {
        private readonly List<IEditorMenuEntry> entries = new List<IEditorMenuEntry>();
        private readonly IEditorMenuPresenter presenter;


        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenu"/> class.
        /// </summary>
        /// <param name="presenter">An object that somehow presents the <see cref="EditorMenu"/>
        /// to the end-user.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="presenter"/> is <c>null</c>.
        /// </exception>
        public EditorMenu(IEditorMenuPresenter presenter)
        {
            if (presenter == null) {
                throw new ArgumentNullException("presenter");
            }

            this.presenter = presenter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenu"/> class with the
        /// default <see cref="IEditorMenuPresenter"/> implementation.
        /// </summary>
        /// <remarks>
        /// <para>The default presenter can be overridden project-wide if desired. Refer
        /// to <see cref="EditorMenuSettings"/> for more information regarding this.</para>
        /// </remarks>
        public EditorMenu()
            : this(EditorMenuPresenterUtility.GetDefaultPresenterInstance())
        {
        }


        /// <summary>
        /// Gets the editable list of entries that make up the <see cref="EditorMenu"/>.
        /// </summary>
        /// <remarks>
        /// <para>Non-supported entry types may be ignored or highlighted.</para>
        /// </remarks>
        public IList<IEditorMenuEntry> Entries {
            get { return this.entries; }
        }


        /// <summary>
        /// Adds a command to the <see cref="EditorMenu"/>.
        /// </summary>
        /// <example>
        /// <para>Add command with a straightforward action:</para>
        /// <code language="csharp"><![CDATA[
        /// var menu = new EditorMenu();
        /// menu.AddCommand("Do Something!")
        ///     .Action(() => {
        ///         Debug.Log("Woot!");
        ///     });
        /// ]]></code>
        ///
        /// <para>Add a command that is always disabled:</para>
        /// <code language="csharp"><![CDATA[
        /// var menu = new EditorMenu();
        /// menu.AddCommand("Disabled Command - No Action");
        /// ]]></code>
        ///
        /// <para>Add a command that is conditionally disabled:</para>
        /// <code language="csharp"><![CDATA[
        /// var menu = new EditorMenu();
        /// menu.AddCommand("Delete Selected")
        ///     .Enabled(selectedObject != null)
        ///     .Action(() => {
        ///         Undo.DestroyObjectImmediate(selectedObject);
        ///     });
        /// ]]></code>
        ///
        /// <para>Add a command that is conditionally visible:</para>
        /// <code language="csharp"><![CDATA[
        /// var menu = new EditorMenu();
        /// menu.AddCommand("Edit Tileset...")
        ///     .Visible(selectedObject is TilesetBrush)
        ///     .Action(() => {
        ///         var tilesetBrush = selectedObject as TilesetBrush;
        ///         ToolUtility.ShowTilesetInDesigner(tilesetBrush.Tileset);
        ///     });
        /// ]]></code>
        ///
        /// <para>Add commands to a sub-menu:</para>
        /// <code language="csharp"><![CDATA[
        /// var menu = new EditorMenu();
        /// menu.AddCommand("Sort/Ascending")
        ///     .Checked(this.SortMode == SortMode.Ascending)
        ///     .Action(() => this.SortMode = SortMode.Ascending);
        /// menu.AddCommand("Sort/Descending")
        ///     .Checked(this.SortMode == SortMode.Descending)
        ///     .Action(() => this.SortMode = SortMode.Descending);
        /// ]]></code>
        /// </example>
        /// <param name="fullPath">Full path of the menu command.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="fullPath"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="fullPath"/> is an empty string or starts with a slash.
        /// </exception>
        /// <seealso cref="AddSeparator(string)"/>
        public virtual EditorMenuCommandBinder AddCommand(string fullPath)
        {
            var entry = new EditorMenuCommandEntry(fullPath);
            this.entries.Add(entry);
            return new EditorMenuCommandBinder(entry);
        }

        /// <summary>
        /// Adds a separator to the <see cref="EditorMenu"/>.
        /// </summary>
        /// <example>
        /// <para>Add separator between two logical groups of commands:</para>
        /// <code language="csharp"><![CDATA[
        /// var menu = new EditorMenu();
        /// menu.AddCommand("None");
        /// menu.AddSeparator();
        /// menu.AddCommand("Abc");
        /// menu.AddCommand("Def");
        /// menu.AddCommand("Ghi");
        /// ]]></code>
        ///
        /// <para>Add a separator in a sub-menu:</para>
        /// <code language="csharp"><![CDATA[
        /// var menu = new EditorMenu();
        /// menu.AddCommand("Sub-menu/None");
        /// menu.AddSeparator("Sub-menu/");
        /// menu.AddCommand("Sub-menu/Abc");
        /// menu.AddCommand("Sub-menu/Def");
        /// menu.AddCommand("Sub-menu/Ghi");
        /// ]]></code>
        /// </example>
        /// <param name="path">Path to the separator.</param>
        /// <returns>
        /// Fluid style API to further define the new separator entry.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="path"/> is an empty string or starts with a slash.
        /// </exception>
        /// <seealso cref="AddCommand(string)"/>
        public virtual EditorMenuSeparatorBinder AddSeparator(string path = "")
        {
            var entry = new EditorMenuSeparatorEntry(path);
            this.entries.Add(entry);
            return new EditorMenuSeparatorBinder(entry);
        }


        /// <summary>
        /// Derived <see cref="EditorMenu"/> implementations can override this method to
        /// perform custom actions each time before the <see cref="EditorMenu"/> is shown
        /// with <see cref="ShowAsContext()"/> or <see cref="ShowAsDropdown(Rect)"/>.
        /// </summary>
        protected virtual void PrepareToShow()
        {
        }


        /// <summary>
        /// Shows the <see cref="EditorMenu"/> as a context menu.
        /// </summary>
        /// <seealso cref="ShowAsDropdown(Rect)"/>
        public void ShowAsContext()
        {
            this.PrepareToShow();
            this.presenter.ShowAsContext(this);
        }

        /// <summary>
        /// Shows the <see cref="EditorMenu"/> as a drop-down from a GUI control.
        /// </summary>
        /// <param name="position">Absolute position of GUI drop-down control.</param>
        /// <seealso cref="ShowAsContext()"/>
        public void ShowAsDropdown(Rect position)
        {
            this.PrepareToShow();
            this.presenter.ShowAsDropdown(position, this);
        }
    }
}
