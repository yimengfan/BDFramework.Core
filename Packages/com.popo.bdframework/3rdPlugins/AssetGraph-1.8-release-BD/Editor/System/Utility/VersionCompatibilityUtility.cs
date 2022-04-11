using System;
using System.Linq;

namespace UnityEngine.AssetGraph {

    public static class VersionCompatibilityUtility {

        public static string UpdateClassName(string className) {
            if (string.IsNullOrEmpty(className))
            {
                return className;
            }
            
            if (!className.StartsWith("UnityEngine.AssetGraph."))
            {
                className = className
                    .Replace("UnityEngine.AssetBundles.GraphTool.", "UnityEngine.AssetGraph.") // v1.3 -> 1.5
                    .Replace("Unity.AssetGraph.", "UnityEngine.AssetGraph."); // v1.4 -> 1.5
            }

            // test remapped type class.
            var typeGetTest = Type.GetType(className);
            if (null == typeGetTest)
            {
                var fullname = className.Split(',')[0];
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var matchingType = assembly.GetTypes().FirstOrDefault(t => t.FullName == fullname);
                    if (matchingType != null)
                    {
                        return matchingType.AssemblyQualifiedName;
                    }
                }
                
                Debug.LogWarningFormat( "[VersionCompatibilityUtility] Type not found for class: {0}.", className );						
            }

            return className;
        }
    }
}
