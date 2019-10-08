// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Interface for an object which adds elements to a context object of the type
    /// <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">Type of the context object that elements can be added to.</typeparam>
    public interface IElementAdder<TContext>
    {
        /// <summary>
        /// Gets the context object.
        /// </summary>
        TContext Object { get; }

        /// <summary>
        /// Determines whether a new element of the specified <paramref name="type"/> can
        /// be added to the associated context object.
        /// </summary>
        /// <param name="type">Type of element to add.</param>
        /// <returns>
        /// A value of <c>true</c> if an element of the specified type can be added;
        /// otherwise, a value of <c>false</c>.
        /// </returns>
        bool CanAddElement(Type type);

        /// <summary>
        /// Adds an element of the specified <paramref name="type"/> to the associated
        /// context object.
        /// </summary>
        /// <param name="type">Type of element to add.</param>
        /// <returns>
        /// The new element.
        /// </returns>
        object AddElement(Type type);
    }
}
