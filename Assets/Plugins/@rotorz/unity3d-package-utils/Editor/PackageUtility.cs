// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Rotorz.Games.EditorExtensions
{
    /// <summary>
    /// Utility functions for accessing package assets.
    /// </summary>
    public static class PackageUtility
    {
        private const string PackagesAssetPath = "Assets/Plugins/Packages";
        private const string PackageDataAssetPath = "Assets/Plugins/PackageData";


        private const string PackageNameRegex_ScopeName = @"[@a-z_\-][a-z0-9_\-]+";
        private const string PackageNameRegex_PackageName = @"[a-z_\-][a-z0-9_\-]+";
        private static Regex s_PackageNameRegex = new Regex(PackageNameRegex_ScopeName + "(/" + PackageNameRegex_PackageName + ")?");

        private const string AssetFileNameRegex = @"[A-Za-z0-9_\-\.]";
        private static Regex s_AssetFileNameRegex = new Regex(AssetFileNameRegex);
        private static Regex s_RelativeAssetPathRegex = new Regex(AssetFileNameRegex + "(/" + AssetFileNameRegex + ")*");


        /// <summary>
        /// Checks the value of a package name argument for validity.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/> has an invalid value.</c>.
        /// </exception>
        public static void CheckPackageNameArgument(string packageName)
        {
            if (packageName == null) {
                throw new ArgumentNullException("packageName");
            }
            if (!s_PackageNameRegex.IsMatch(packageName)) {
                throw new ArgumentException(string.Format("Invalid package name '{0}'.", packageName), "packageName");
            }
        }

        private static void CheckAssetFileNameArgument(string assetFileName)
        {
            if (assetFileName == null) {
                return; // It's fine!
            }
            if (!s_AssetFileNameRegex.IsMatch(assetFileName)) {
                throw new ArgumentException(string.Format("Invalid asset file name '{0}'.", assetFileName), "assetFileName");
            }
        }

        private static void CheckRelativeFolderPathArgument(string relativeFolderPath)
        {
            if (relativeFolderPath == null) {
                return; // It's fine!
            }
            if (!s_RelativeAssetPathRegex.IsMatch(relativeFolderPath)) {
                throw new ArgumentException(string.Format("Invalid relative asset folder path '{0}'.", relativeFolderPath), "relativeFolderPath");
            }
        }


        private static string NormalizeDirectorySeparatorForOS(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            return NormalizeDirectorySeparatorForOS(
                Path.Combine(Directory.GetCurrentDirectory(), assetPath)
            );
        }

        private static void EnsureFolderAssetPathExists(string folderAssetPath)
        {
            string[] parts = folderAssetPath.Split('/');

            if (parts[0] != "Assets") {
                throw new ArgumentException(string.Format("Invalid asset folder path '{0}'.", folderAssetPath), "folderAssetPath");
            }

            string currentAssetPath = "Assets";
            for (int i = 1; i < parts.Length; ++i) {
                string parentAssetPath = currentAssetPath;

                string folderName = parts[i];
                currentAssetPath += "/" + folderName;

                if (!AssetDatabase.IsValidFolder(currentAssetPath)) {
                    AssetDatabase.CreateFolder(parentAssetPath, folderName);
                }
            }
        }


        /// <summary>
        /// Resolves asset path of a package specific folder or asset.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// Debug.Log(PackageUtility.ResolveAssetPath("@vendor-name/package-name", "Language", "en-US.txt"));
        /// // Assets/Plugins/Packages/@vendor-name/package-name/Language/en-US.txt
        /// ]]></code>
        /// </example>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="relativeFolderPath">Relative folder path inside package data
        /// folder (optional).</param>
        /// <param name="assetFileName">Name of asset file (optional).</param>
        /// <returns>
        /// The resolved asset path of the package specific path.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/>, <paramref name="relativeFolderPath"/> or
        /// <paramref name="assetFileName"/> has an invalid value.</c>.
        /// </exception>
        public static string ResolveAssetPath(string packageName, string relativeFolderPath = null, string assetFileName = null)
        {
            CheckPackageNameArgument(packageName);
            CheckRelativeFolderPathArgument(relativeFolderPath);
            CheckAssetFileNameArgument(assetFileName);

            string assetPath = PackagesAssetPath + "/" + packageName;

            if (relativeFolderPath != null) {
                assetPath += "/" + relativeFolderPath;
            }
            if (assetFileName != null) {
                assetPath += "/" + assetFileName;
            }

            return assetPath;
        }

        /// <summary>
        /// Resolves absolute file system path of a package specific folder or asset.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// Debug.Log(PackageUtility.ResolveAssetPathAbsolute("@vendor-name/package-name", "Language", "en-US.txt"));
        /// // C:\MyProject\Assets\Plugins\Packages\@vendor-name\package-name\Language\en-US.txt
        /// ]]></code>
        /// </example>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="relativeFolderPath">Relative folder path inside package data
        /// folder (optional).</param>
        /// <param name="assetFileName">Name of asset file (optional).</param>
        /// <returns>
        /// The resolved asset path of the package specific path.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/>, <paramref name="relativeFolderPath"/> or
        /// <paramref name="assetFileName"/> has an invalid value.</c>.
        /// </exception>
        public static string ResolveAssetPathAbsolute(string packageName, string relativeFolderPath = null, string assetFileName = null)
        {
            string assetPath = ResolveAssetPath(packageName, relativeFolderPath, assetFileName);
            return AssetPathToAbsolutePath(assetPath);
        }


        /// <summary>
        /// Resolves asset path of a package specific data folder or asset.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// Debug.Log(PackageUtility.ResolveDataAssetPath("@vendor-name/package-name", "Presets", "NewPreset.asset"));
        /// // Assets/Plugins/PackageData/@vendor-name/package-name/Presets/NewPreset.asset
        /// ]]></code>
        /// </example>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="relativeFolderPath">Relative folder path inside package data
        /// folder (optional).</param>
        /// <param name="assetFileName">Name of asset file (optional).</param>
        /// <returns>
        /// The resolved asset path of the package specific data path.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/>, <paramref name="relativeFolderPath"/> or
        /// <paramref name="assetFileName"/> has an invalid value.</c>.
        /// </exception>
        public static string ResolveDataAssetPath(string packageName, string relativeFolderPath = null, string assetFileName = null)
        {
            CheckPackageNameArgument(packageName);
            CheckRelativeFolderPathArgument(relativeFolderPath);
            CheckAssetFileNameArgument(assetFileName);

            string assetPath = PackageDataAssetPath + "/" + packageName;

            if (relativeFolderPath != null) {
                assetPath += "/" + relativeFolderPath;
            }
            if (assetFileName != null) {
                assetPath += "/" + assetFileName;
            }

            return assetPath;
        }

        /// <summary>
        /// Resolves absolute file system path of a package specific data folder or asset.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// Debug.Log(PackageUtility.ResolveDataPathAbsolute("@vendor-name/package-name", "Presets", "NewPreset.asset"));
        /// // C:\MyProject\Assets\Plugins\PackageData\@vendor-name\package-name\Presets\NewPreset.asset
        /// ]]></code>
        /// </example>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="relativeFolderPath">Relative folder path inside package data
        /// folder (optional).</param>
        /// <param name="assetFileName">Name of asset file (optional).</param>
        /// <returns>
        /// The resolved asset path of the package specific data path.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/>, <paramref name="relativeFolderPath"/> or
        /// <paramref name="assetFileName"/> has an invalid value.</c>.
        /// </exception>
        public static string ResolveDataPathAbsolute(string packageName, string relativeFolderPath = null, string assetFileName = null)
        {
            string assetPath = ResolveDataAssetPath(packageName, relativeFolderPath, assetFileName);
            return AssetPathToAbsolutePath(assetPath);
        }


        /// <summary>
        /// Gets asset path of a package specific data folder or asset and ensures that
        /// the path exists on the file system.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// Debug.Log(PackageUtility.GetDataAssetPath("@vendor-name/package-name", "Presets", "NewPreset.asset"));
        /// // Assets/Plugins/PackageData/@vendor-name/package-name/Presets/NewPreset.asset
        /// ]]></code>
        /// </example>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="relativeFolderPath">Relative folder path inside package data
        /// folder (optional).</param>
        /// <param name="assetFileName">Name of asset file (optional).</param>
        /// <returns>
        /// The resolved asset path of the package specific data path.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/>, <paramref name="relativeFolderPath"/> or
        /// <paramref name="assetFileName"/> has an invalid value.</c>.
        /// </exception>
        public static string GetDataAssetPath(string packageName, string relativeFolderPath = null, string assetFileName = null)
        {
            string assetPath = ResolveDataAssetPath(packageName, relativeFolderPath, assetFileName);

            string folderAssetPath = ResolveDataAssetPath(packageName, relativeFolderPath);
            EnsureFolderAssetPathExists(folderAssetPath);

            return assetPath;
        }

        /// <summary>
        /// Gets absolute file system path of a package specific data folder or asset and
        /// ensures that the path exists on the file system.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// Debug.Log(PackageUtility.GetDataPathAbsolute("@vendor-name/package-name", "Presets", "NewPreset.asset"));
        /// // C:\MyProject\Assets\Plugins\PackageData\@vendor-name\package-name\Presets\NewPreset.asset
        /// ]]></code>
        /// </example>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="relativeFolderPath">Relative folder path inside package data
        /// folder (optional).</param>
        /// <param name="assetFileName">Name of asset file (optional).</param>
        /// <returns>
        /// The resolved asset path of the package specific data path.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/>, <paramref name="relativeFolderPath"/> or
        /// <paramref name="assetFileName"/> has an invalid value.</c>.
        /// </exception>
        public static string GetDataPathAbsolute(string packageName, string relativeFolderPath = null, string assetFileName = null)
        {
            string assetPath = GetDataAssetPath(packageName, relativeFolderPath, assetFileName);
            return AssetPathToAbsolutePath(assetPath);
        }


        /// <summary>
        /// Delete a data folder but only if it is empty.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="relativeFolderPath">Relative folder path inside package data
        /// folder.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="packageName"/> or <paramref name="relativeFolderPath"/> is
        /// <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// If <paramref name="packageName"/> or <paramref name="relativeFolderPath"/>
        /// has an invalid value.</c>.
        /// </exception>
        public static void DeleteDataFolderIfEmpty(string packageName, string relativeFolderPath)
        {
            if (relativeFolderPath == null) {
                throw new ArgumentNullException("relativeFolderPath");
            }

            string absoluteFolderPath = ResolveDataPathAbsolute(packageName, relativeFolderPath);

            // Bail if the directory doesn't actually exist; nothing to delete!
            if (!Directory.Exists(absoluteFolderPath)) {
                return;
            }
            // Bail if the directory contains one or more files or sub-directories.
            if (Directory.GetFiles(absoluteFolderPath).Length != 0 || Directory.GetDirectories(absoluteFolderPath).Length != 0) {
                return;
            }

            string folderAssetPath = ResolveDataAssetPath(packageName, relativeFolderPath);
            AssetDatabase.DeleteAsset(folderAssetPath);
        }
    }
}
