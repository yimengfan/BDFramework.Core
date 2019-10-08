// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Base class for a custom type of <see cref="EditorMenu"/> entry.
    /// </summary>
    public abstract class EditorMenuEntry : IEditorMenuEntry
    {
        private List<IEditorMenuEntryParameter> parameters = null;


        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenuEntry"/> class.
        /// </summary>
        public EditorMenuEntry()
        {
            this.Path = "";

            this.IsVisiblePredicate = EditorMenuInternalUtility.AlwaysTruePredicate;
        }


        /// <inheritdoc/>
        public string Path { get; protected set; }


        /// <inheritdoc/>
        public Func<bool> IsVisiblePredicate { get; set; }


        /// <inheritdoc/>
        public ICollection<IEditorMenuEntryParameter> Parameters {
            get {
                if (this.parameters == null) {
                    this.parameters = new List<IEditorMenuEntryParameter>();
                }
                return this.parameters;
            }
        }

        /// <inheritdoc/>
        public bool HasParameters {
            get { return this.parameters != null && this.parameters.Count > 0; }
        }


        /// <inheritdoc/>
        public virtual bool EvaluateIsVisible()
        {
            var d = this.IsVisiblePredicate;
            if (d == null) {
                return false;
            }

            return d.Invoke() == true;
        }
    }
}
