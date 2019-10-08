// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Binds additional information to an <see cref="EditorMenu"/> command entry.
    /// </summary>
    public class EditorMenuCommandBinder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenuCommandBinder"/> class.
        /// </summary>
        /// <param name="entry">The associated command entry.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="entry"/> is <c>null</c>.
        /// </exception>
        public EditorMenuCommandBinder(EditorMenuCommandEntry entry)
        {
            ExceptionUtility.CheckArgumentNotNull(entry, "entry");

            this.Entry = entry;
        }


        /// <summary>
        /// Gets the associated <see cref="EditorMenuCommandEntry"/> which can be
        /// modified as further information is provided.
        /// </summary>
        protected EditorMenuCommandEntry Entry { get; private set; }


        /// <summary>
        /// Sets whether the <see cref="EditorMenu"/> command entry is visible.
        /// </summary>
        /// <param name="isVisible">Value indicating visibility.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        public EditorMenuCommandBinder Visible(bool isVisible)
        {
            return this.Visible(isVisible
                ? EditorMenuInternalUtility.AlwaysTruePredicate
                : EditorMenuInternalUtility.AlwaysFalsePredicate
            );
        }

        /// <summary>
        /// Sets the predicate that is used to determine whether the <see cref="EditorMenu"/>
        /// command entry is visible.
        /// </summary>
        /// <param name="predicate">Predicate that returns a value of <c>true</c> for a
        /// visible command or a value of <c>false</c> to hide it.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        public EditorMenuCommandBinder Visible(Func<bool> predicate)
        {
            this.Entry.IsVisiblePredicate = predicate;
            return this;
        }


        /// <summary>
        /// Sets whether the <see cref="EditorMenu"/> command entry is enabled.
        /// </summary>
        /// <remarks>
        /// <para>A disabled command is typically presented "greyed out" such that it
        /// cannot be actuated.</para>
        /// </remarks>
        /// <param name="isEnabled">Value indicating whether the command is enabled.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        public EditorMenuCommandBinder Enabled(bool isEnabled)
        {
            return this.Enabled(isEnabled
                ? EditorMenuInternalUtility.AlwaysTruePredicate
                : EditorMenuInternalUtility.AlwaysFalsePredicate
            );
        }

        /// <summary>
        /// Sets the predicate that is used to determine whether the <see cref="EditorMenu"/>
        /// command entry is enabled.
        /// </summary>
        /// <remarks>
        /// <para>A disabled command is typically presented "greyed out" such that it
        /// cannot be actuated.</para>
        /// </remarks>
        /// <param name="predicate">Predicate that returns a value of <c>true</c> for an
        /// enabled command or a value of <c>false</c> to "grey it out".</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        public EditorMenuCommandBinder Enabled(Func<bool> predicate)
        {
            this.Entry.IsEnabledPredicate = predicate;
            return this;
        }


        /// <summary>
        /// Sets whether the <see cref="EditorMenu"/> command entry is checked (ticked /
        /// selected / active / on).
        /// </summary>
        /// <remarks>
        /// <para>A check mark is typically presented alongside command entries that have
        /// been checked.</para>
        /// </remarks>
        /// <param name="isChecked">Value indicating whether the command is checked.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        public EditorMenuCommandBinder Checked(bool isChecked)
        {
            return this.Checked(isChecked
                ? EditorMenuInternalUtility.AlwaysTruePredicate
                : EditorMenuInternalUtility.AlwaysFalsePredicate
            );
        }

        /// <summary>
        /// Sets the predicate that is used to determine whether the <see cref="EditorMenu"/>
        /// command entry is checked.
        /// </summary>
        /// <remarks>
        /// <para>A check mark is typically presented alongside command entries that have
        /// been checked.</para>
        /// </remarks>
        /// <param name="predicate">Predicate that returns a value of <c>true</c> for a
        /// checked command or a value of <c>false</c> for a regular non-checked command.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        public EditorMenuCommandBinder Checked(Func<bool> predicate)
        {
            this.Entry.IsCheckedPredicate = predicate;
            return this;
        }


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
        public EditorMenuCommandBinder Parameter(IEditorMenuEntryParameter parameter)
        {
            ExceptionUtility.CheckArgumentNotNull(parameter, "parameter");

            this.Entry.Parameters.Add(parameter);
            return this;
        }


        /// <summary>
        /// Adds an action to the <see cref="EditorMenu"/> command entry.
        /// </summary>
        /// <remarks>
        /// <para>Specifying a value of <c>null</c> or simply omitting to specify an
        /// action will simply result in the <see cref="EditorMenu"/> command being
        /// disabled ("greyed out").</para>
        /// </remarks>
        /// <param name="action">The action to have the command invoke.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="action"/> is <c>null</c>.
        /// </exception>
        public EditorMenuCommandBinder Action(Action action)
        {
            ExceptionUtility.CheckArgumentNotNull(action, "action");

            this.Entry.Action += action;
            return this;
        }

        /// <summary>
        /// Adds a parameterized action to the <see cref="EditorMenu"/> command entry.
        /// </summary>
        /// <remarks>
        /// <para>Specifying a value of <c>null</c> or simply omitting to specify an
        /// action will simply result in the <see cref="EditorMenu"/> command being
        /// disabled ("greyed out").</para>
        /// </remarks>
        /// <typeparam name="TParam">Type of command parameter.</typeparam>
        /// <param name="action">The action to have the command invoke.</param>
        /// <param name="param">Parameter for command.</param>
        /// <returns>
        /// Fluid style API to further define the new command entry.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="action"/> is <c>null</c>.
        /// </exception>
        public EditorMenuCommandBinder Action<TParam>(Action<TParam> action, TParam param)
        {
            ExceptionUtility.CheckArgumentNotNull(action, "action");

            this.Entry.Action += () => action(param);
            return this;
        }
    }
}
