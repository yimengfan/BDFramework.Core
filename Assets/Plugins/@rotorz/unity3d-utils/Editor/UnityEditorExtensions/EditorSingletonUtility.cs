// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System.Linq;
using UnityEditor;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Singleton utility functions for custom Unity editor extensions.
    /// </summary>
    public static class EditorSingletonUtility
    {
        /// <summary>
        /// Gets the one-and-only instance for a custom <see cref="IEditorSingleton"/>
        /// implementation.
        /// </summary>
        /// <typeparam name="T">Implementation type.</typeparam>
        /// <param name="instance">Reference for the one-and-only shared instance of the
        /// specified implementation type.</param>
        /// <param name="reinitializeOnReload">Indicates if singleton should be
        /// initialized again when Unity reloads it's assemblies.</param>
        public static void GetAssetInstance<T>(ref T instance, bool reinitializeOnReload = true)
            where T : EditorSingletonScriptableObject
        {
            if (instance == null) {
                string assetGuid = AssetDatabase.FindAssets("t:" + typeof(T).FullName).FirstOrDefault();
                if (!string.IsNullOrEmpty(assetGuid)) {
                    string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                    instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    if (reinitializeOnReload && instance.HasInitialized) {
                        instance.Reinitialize();
                    }
                }
            }

            if (!instance.HasInitialized) {
                instance.Initialize();
            }
        }
    }
}
