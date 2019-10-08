// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rotorz.Games.Reflection
{
    /// <summary>
    /// Utility functionality providing extra meta information about types.
    /// </summary>
    public static class TypeMeta
    {
        private static Dictionary<Type, Type[]> s_DiscoveredTypeCache = new Dictionary<Type, Type[]>();


        /// <summary>
        /// Discovers non-abstract types that implement the specified type.
        /// </summary>
        /// <typeparam name="T">The abstract or base type.</typeparam>
        /// <returns>
        /// An array of zero-or-more types.
        /// </returns>
        public static Type[] DiscoverImplementations<T>()
        {
            return DiscoverImplementations(typeof(T));
        }

        /// <summary>
        /// Discovers non-abstract types that implement the specified type.
        /// </summary>
        /// <param name="type">The abstract or base type.</param>
        /// <returns>
        /// An array of zero-or-more types.
        /// </returns>
        public static Type[] DiscoverImplementations(Type type)
        {
            Type[] cache;
            if (!s_DiscoveredTypeCache.TryGetValue(type, out cache)) {
                var discoveredTypes = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    discoveredTypes.AddRange(DiscoverImplementationsInternal(type, assembly));
                }

                //
                // Note: Might need to re-add this in the future if desirable assemblies
                //       are being lazy loaded for some reason.
                //
                //foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies()) {
                //    var referencedAssembly = Assembly.Load(referencedAssemblyName);
                //    discoveredTypes.AddRange(DiscoverImplementationsInternal(type, referencedAssembly));
                //}
                //

                discoveredTypes.Sort((a, b) => a.FullName.CompareTo(b.FullName));
                cache = discoveredTypes.ToArray();
                s_DiscoveredTypeCache[type] = cache;
            }

            return cache.ToArray();
        }

        private static IEnumerable<Type> DiscoverImplementationsInternal(Type type, Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(x => type.IsAssignableFrom(x))
                .Where(x => !x.IsAbstract && !x.IsInterface);
        }


        /// <summary>
        /// Gets the collection of types that a given type is dependent upon.
        /// </summary>
        /// <param name="type">Type that is being analyzed.</param>
        /// <returns>
        /// A collection of types.
        /// </returns>
        public static IEnumerable<Type> GetAnnotatedDependencies(Type type)
        {
            return Attribute.GetCustomAttributes(type, typeof(DependencyAttribute), true)
                .Cast<DependencyAttribute>()
                .Select(attribute => attribute.DependencyType)
                .Distinct();
        }


        public static string NicifyName(string typeName, string unwantedSuffix = null)
        {
            typeName = RemoveUnwantedSuffix(typeName, unwantedSuffix);

            var builder = new StringBuilder(typeName.Length);
            int upperCaseChain = 0;

            for (int i = 0; i < typeName.Length; ++i) {
                char c = typeName[i];
                if (char.IsUpper(c)) {
                    ++upperCaseChain;
                    if (upperCaseChain == 1 && i != 0) {
                        builder.Append(' ');
                    }
                }
                else {
                    upperCaseChain = 0;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        public static string NicifyCompoundName(string typeName, char sourceSeparator = '_', string targetSeparator = " - ", string unwantedSuffix = null)
        {
            typeName = RemoveUnwantedSuffix(typeName, unwantedSuffix);

            var nicifiedNameParts = typeName.Split(sourceSeparator)
                .Select(x => NicifyName(x))
                .ToArray();
            return string.Join(targetSeparator, nicifiedNameParts);
        }

        public static string NicifyNamespaceQualifiedName(string namespaceName, string name)
        {
            string result = name;
            if (!string.IsNullOrEmpty(namespaceName)) {
                result = namespaceName + " / " + result;
            }
            return result;
        }

        public static string RemoveUnwantedSuffix(string typeName, string unwantedSuffix)
        {
            if (!string.IsNullOrEmpty(unwantedSuffix)) {
                if (typeName.EndsWith(unwantedSuffix)) {
                    typeName = typeName.Substring(0, typeName.Length - unwantedSuffix.Length);
                }
            }
            return typeName;
        }
    }
}
