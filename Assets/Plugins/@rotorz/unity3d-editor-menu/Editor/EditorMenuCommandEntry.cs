// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// An <see cref="EditorMenu"/> entry that describes a command.
    /// </summary>
    public class EditorMenuCommandEntry : EditorMenuEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenuCommandEntry"/> class.
        /// </summary>
        /// <param name="fullPath">Full path of the command including its path, its label
        /// and any shortcut keys.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="fullPath"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="fullPath"/> is an invalid full command path.
        /// </exception>
        public EditorMenuCommandEntry(string fullPath)
        {
            ExceptionUtility.CheckExpectedStringArgument(fullPath, "fullPath");
            EditorMenuInternalUtility.CheckPathArgument(fullPath, "fullPath");

            int labelStartIndex = fullPath.LastIndexOf('/') + 1;

            this.Label = fullPath.Substring(labelStartIndex);
            this.Path = fullPath.Substring(0, labelStartIndex);
            this.FullPath = fullPath;

            this.IsEnabledPredicate = EditorMenuInternalUtility.AlwaysTruePredicate;
            this.IsCheckedPredicate = EditorMenuInternalUtility.AlwaysFalsePredicate;
        }


        /// <summary>
        /// Gets the label of the command entry; for instance, "Create Preset...".
        /// </summary>
        public string Label { get; protected set; }

        /// <summary>
        /// Gets the full path of the command entry; for instance, "Presets/Create Preset...".
        /// </summary>
        public string FullPath { get; protected set; }


        /// <summary>
        /// Gets or sets the predicate that is used to determine whether the command entry
        /// is enabled or disabled.
        /// </summary>
        public Func<bool> IsEnabledPredicate { get; set; }

        /// <summary>
        /// Gets or sets the predicate that is used to determine whether the command entry
        /// is checked or unchecked.
        /// </summary>
        public Func<bool> IsCheckedPredicate { get; set; }


        /// <summary>
        /// Gets or sets the predicate that is invoked when the command is actuated.
        /// </summary>
        public Action Action { get; set; }


        /// <summary>
        /// Evaluates whether or not the command is enabled.
        /// </summary>
        /// <remarks>
        /// <para>In addition to using the provided <see cref="IsEnabledPredicate"/> this
        /// method can also incorporate other factors such as <see cref="Action"/> being
        /// set to a value of <c>null</c>.</para>
        /// </remarks>
        /// <returns>
        /// A value of <c>true</c> if the command is enabled; otherwise, a value of <c>false</c>.
        /// </returns>
        public virtual bool EvaluateIsEnabled()
        {
            if (this.Action == null) {
                return false;
            }

            var d = this.IsEnabledPredicate;
            return d != null && d.Invoke() == true;
        }

        /// <summary>
        /// Evaluates whether or not the command is checked (ticked / selected / on).
        /// </summary>
        /// <remarks>
        /// <para>In addition to user the provided <see cref="IsCheckedPredicate"/> this
        /// method can also incorporate other factors.</para>
        /// </remarks>
        /// <returns>
        /// A value of <c>true</c> if the command is checkedl otherwise, a value of <c>false</c>.
        /// </returns>
        public virtual bool EvaluateIsChecked()
        {
            var d = this.IsCheckedPredicate;
            return d != null && d.Invoke() == true;
        }


        /// <summary>
        /// Verifies that <see cref="Action"/> can be invoked by throwing an exception if
        /// there is a problem.
        /// </summary>
        protected virtual void CheckCanInvokeAction()
        {
            if (!this.EvaluateIsVisible()) {
                throw new InvalidOperationException("Cannot invoke command because not visible.");
            }
            if (!this.EvaluateIsEnabled()) {
                throw new InvalidOperationException("Cannot invoke command because not enabled.");
            }
        }

        /// <summary>
        /// Invokes the command action.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// If the command cannot be invoked; for instance, if the command is not visible
        /// or is disabled.
        /// </exception>
        public virtual void InvokeAction()
        {
            this.CheckCanInvokeAction();

            var d = this.Action;
            if (d != null) {
                d.Invoke();
            }
        }
    }
}
