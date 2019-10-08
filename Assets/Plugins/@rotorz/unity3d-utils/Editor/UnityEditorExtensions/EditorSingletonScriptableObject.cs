// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Base class of a <see cref="ScriptableObject"/> Unity editor extension singleton.
    /// </summary>
    public abstract class EditorSingletonScriptableObject : ScriptableObject, IEditorSingleton
    {
        /// <inheritdoc/>
        public bool HasInitialized { get; private set; }


        /// <summary>
        /// Occurs when the <see cref="EditorSingletonScriptableObject"/> is initialized.
        /// </summary>
        protected virtual void OnInitialize()
        {
        }


        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.HasInitialized) {
                throw new InvalidOperationException("Already initialized!");
            }

            this.HasInitialized = true;
            this.OnInitialize();
        }

        /// <summary>
        /// Reinitializes the singleton.
        /// </summary>
        public void Reinitialize()
        {
            this.HasInitialized = true;
            this.OnInitialize();
        }
    }
}
