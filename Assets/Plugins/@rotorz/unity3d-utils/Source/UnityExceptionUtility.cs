// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using Object = UnityEngine.Object;

namespace Rotorz.Games
{
    /// <summary>
    /// Utility functionality for exceptions.
    /// </summary>
    public static class UnityExceptionUtility
    {
        /// <summary>
        /// Checks that an object argument is non-null and in the case of Unity objects
        /// that they haven't been destroyed.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="paramName">Name of the parameter</param>
        public static void CheckArgumentObjectValid(Object arg, string paramName)
        {
            if (ReferenceEquals(arg, null)) {
                throw new ArgumentNullException(paramName);
            }
            if (arg == null) {
                throw new ArgumentException("Object has been destroyed.", paramName);
            }
        }
    }
}
