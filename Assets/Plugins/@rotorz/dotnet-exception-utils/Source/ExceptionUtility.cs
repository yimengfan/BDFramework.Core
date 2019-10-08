// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games
{
    /// <summary>
    /// Utility functionality for exceptions.
    /// </summary>
    public static class ExceptionUtility
    {
        /// <summary>
        /// Checks that a string argument is non-null and non-empty.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="paramName">Name of the parameter</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="arg"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="arg"/> is an empty string.
        /// </exception>
        public static void CheckExpectedStringArgument(string arg, string paramName)
        {
            if (arg == null) {
                throw new ArgumentNullException(paramName);
            }
            if (arg == "") {
                throw new ArgumentException("Was empty string.", paramName);
            }
        }

        /// <summary>
        /// Checks that an object argument is non-null.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="paramName">Name of the parameter</param>
        public static void CheckArgumentNotNull(object arg, string paramName)
        {
            if (ReferenceEquals(arg, null)) {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
