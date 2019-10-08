// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Utility functions for asset files and folders.
    /// </summary>
    public static class AssetUtility
    {
        #region Asset Path Manipulation

        /// <summary>
        /// Combine one or more asset paths.
        /// </summary>
        /// <param name="firstPath">First asset path.</param>
        /// <param name="otherPaths">Other parts of asset path.</param>
        /// <returns>
        /// The combined asset path.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <list type="bullet">
        /// <item>If <paramref name="firstPath"/> is <c>null</c>.</item>
        /// <item>If one or more <paramref name="otherPaths"/> have a value of <c>null</c>.</item>
        /// </list>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <list type="bullet">
        /// <item>If <paramref name="firstPath"/> is not a valid asset path.</item>
        /// <item>If one or more <paramref name="otherPaths"/> are not valid.</item>
        /// </list>
        /// </exception>
        public static string CombineAssetPaths(string firstPath, params string[] otherPaths)
        {
            CheckAssetPathArgument(firstPath, "firstPath");

            if (otherPaths == null || otherPaths.Length == 0) {
                return firstPath;
            }

            var sb = new StringBuilder(firstPath);

            for (int i = 0; i < otherPaths.Length; ++i) {
                string otherPath = otherPaths[i];

                if (otherPath == null) {
                    throw new ArgumentNullException(string.Format("otherPaths[{0}]", i));
                }
                if (otherPath == "" || !PathRegex.IsMatch(otherPath)) {
                    throw new ArgumentException(string.Format("Invalid path '{0}'.", otherPath), string.Format("otherPaths[{0}]", i));
                }

                sb.Append('/');
                sb.Append(otherPath);
            }

            return sb.ToString();
        }

        #endregion


        #region Asset Path Validation

        private const string SlugPattern = @"[^\s/\\][^/\\]*";

        //private static readonly Regex SlugRegex = new Regex(SlugPattern, RegexOptions.CultureInvariant);
        private static readonly Regex PathRegex = new Regex(string.Format(@"^{0}(/{0})*$", SlugPattern), RegexOptions.CultureInvariant);

        private static string GetAssetPathError(string assetPath)
        {
            if (assetPath == null) {
                return "Asset path was null.";
            }
            if (!assetPath.StartsWith("Assets/")) {
                return "Asset path does not start with 'Assets/'.";
            }
            if (assetPath.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
                return string.Format("Asset path '{0}' contains one or more invalid characters.", assetPath);
            }
            if (!PathRegex.IsMatch(assetPath)) {
                return string.Format("Invalid asset path '{0}'.", assetPath);
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="assetPath"/> is valid.
        /// </summary>
        /// <param name="assetPath">Asset path.</param>
        /// <returns>
        /// A <see cref="bool"/> value of <c>true</c> if is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidAssetPath(string assetPath)
        {
            return GetAssetPathError(assetPath) == null;
        }

        /// <summary>
        /// Does nothing if the specified <paramref name="assetPath"/> is valid;
        /// otherwise throws a <see cref="System.ArgumentException"/> exception.
        /// </summary>
        /// <param name="assetPath">Asset path.</param>
        /// <param name="paramName">Name of the parameter</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="assetPath"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="assetPath"/> is not a valid asset path.
        /// </exception>
        public static void CheckAssetPathArgument(string assetPath, string paramName)
        {
            ExceptionUtility.CheckExpectedStringArgument(assetPath, paramName);

            string error = GetAssetPathError(assetPath);
            if (error != null) {
                throw new ArgumentException(error, paramName);
            }
        }

        #endregion


        #region Assets

        /// <summary>
        /// Creates an asset file from the specified object at the specified path.
        /// </summary>
        /// <example>
        /// <para>Create a new instance of a custom <see cref="ScriptableObject"/> and
        /// then save to an asset file:</para>
        /// <code language="csharp"><![CDATA[
        /// var newAsset = ScriptableObject.Create<MyAsset>();
        /// AssetUtility.CreateAsset(newAsset, "Assets/Some/Sub/Folder/MyAsset.asset");
        /// AssetDatabase.SaveAssets();
        /// ]]></code>
        /// </example>
        /// <param name="obj">Object that is to be saved.</param>
        /// <param name="assetPath">Path of new asset file.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <list type="bullet">
        /// <item>If <paramref name="obj"/> is <c>null</c>.</item>
        /// <item>If <paramref name="assetPath"/> is <c>null</c>.</item>
        /// </list>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <list type="bullet">
        /// <item>If <paramref name="obj"/> has already been destroyed.</item>
        /// <item>If <paramref name="assetPath"/> is not a valid asset path.</item>
        /// </list>
        /// </exception>
        public static void CreateAsset(Object obj, string assetPath)
        {
            UnityExceptionUtility.CheckArgumentObjectValid(obj, "obj");

            CheckAssetPathArgument(assetPath, "assetPath");
            if (!assetPath.EndsWith(".asset")) {
                throw new ArgumentException("Does not end with '.asset'.", "assetPath");
            }

            // Ensure that directory exists before proceeding to create asset.
            string[] assetPathFragments = assetPath.Split('/');
            if (assetPathFragments.Length > 2) {
                string assetDirectoryPath = string.Join("/", assetPathFragments, 0, assetPathFragments.Length - 1);
                CreateFolder(assetDirectoryPath);
            }

            AssetDatabase.CreateAsset(obj, assetPath);
        }

        #endregion


        #region Folders

        /// <summary>
        /// Ensures that all folders in the specified asset path exist.
        /// </summary>
        /// <param name="assetPath">Asset path.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="assetPath"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="assetPath"/> is not a valid asset path.
        /// </exception>
        public static void CreateFolder(string assetPath)
        {
            CheckAssetPathArgument(assetPath, "assetPath");

            string absoluteFolderPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Directory.CreateDirectory(absoluteFolderPath);
        }

        #endregion
    }
}
