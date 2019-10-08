// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Interface of a Unity editor extension singleton.
    /// </summary>
    public interface IEditorSingleton
    {
        /// <summary>
        /// Gets a value indicating whether the singleton has already been initialized.
        /// </summary>
        bool HasInitialized { get; }


        /// <summary>
        /// Invoked to initialize the singleton.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// If the <see cref="IEditorSingleton"/> has already been initialized.
        /// </exception>
        void Initialize();
    }
}
