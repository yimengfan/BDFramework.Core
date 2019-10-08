// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <exclude/>
    internal static class EditorMenuInternalUtility
    {
        public static readonly Func<bool> AlwaysTruePredicate = () => true;
        public static readonly Func<bool> AlwaysFalsePredicate = () => false;


        public static void CheckPathArgument(string path, string argumentName)
        {
            if (path.StartsWith("/")) {
                throw new ArgumentException("Invalid menu path.", argumentName);
            }
        }
    }
}
