// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games
{
    /// <summary>
    /// Declares that the annotated class is dependent upon another class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class DependencyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyAttribute"/> class.
        /// </summary>
        /// <param name="dependencyType">Type that the annotated class is dependent upon.</param>
        public DependencyAttribute(Type dependencyType)
        {
            this.DependencyType = dependencyType;
        }


        /// <summary>
        /// Gets the <see cref="System.Type"/> that the annotated class is dependent upon.
        /// </summary>
        public Type DependencyType { get; private set; }
    }
}
