using System;
using System.Collections.Generic;
using System.Linq;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    public class FilterUtility {

        private static  Dictionary<string, string> s_attributeAssemblyQualifiedNameMap;

        public static Dictionary<string, string> GetAttributeAssemblyQualifiedNameMap () {

            if(s_attributeAssemblyQualifiedNameMap == null) {
                // attribute name or class name : class name
                s_attributeAssemblyQualifiedNameMap = new Dictionary<string, string>(); 

                var allFilters = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var filters = assembly.GetTypes()
                        .Where(t => !t.IsInterface)
                        .Where(t => typeof(IFilter).IsAssignableFrom(t));
                    allFilters.AddRange (filters);
                }

                foreach (var type in allFilters) {
                    // set attribute-name as key of dict if atribute is exist.
                    CustomFilter attr = 
                        type.GetCustomAttributes(typeof(CustomFilter), true).FirstOrDefault() as CustomFilter;

                    var typename = type.AssemblyQualifiedName;

                    if (attr != null) {
                        if (!s_attributeAssemblyQualifiedNameMap.ContainsKey(attr.Name)) {
                            s_attributeAssemblyQualifiedNameMap[attr.Name] = typename;
                        }
                    } else {
                        s_attributeAssemblyQualifiedNameMap[typename] = typename;
                    }
                }
            }
            return s_attributeAssemblyQualifiedNameMap;
        }

        public static string GetFilterGUIName(IFilter filter) {
            CustomFilter attr = 
                filter.GetType().GetCustomAttributes(typeof(CustomFilter), false).FirstOrDefault() as CustomFilter;
            return attr.Name;
        }

        public static string GetPrefabBuilderGUIName(string className) {
            if(className != null) {
                var type = Type.GetType(className);
                if(type != null) {
                    CustomFilter attr = 
                        type.GetCustomAttributes(typeof(CustomFilter), false).FirstOrDefault() as CustomFilter;
                    if(attr != null) {
                        return attr.Name;
                    }
                }
            }
            return string.Empty;
        }

        public static string GUINameToAssemblyQualifiedName(string guiName) {
            var map = GetAttributeAssemblyQualifiedNameMap();

            if(map.ContainsKey(guiName)) {
                return map[guiName];
            }

            return null;
        }

        public static IFilter CreateFilter(string guiName) {
            var assemblyQualifiedName = GUINameToAssemblyQualifiedName(guiName);
            if(assemblyQualifiedName != null) {
                Type t = Type.GetType(assemblyQualifiedName);
                if(t == null) {
                    return null;
                }

                return (IFilter) t.Assembly.CreateInstance(t.FullName);
            }
            return null;
        }

        public static bool HasValidCustomFilterAttribute(Type t) {
            CustomFilter attr = 
                t.GetCustomAttributes(typeof(CustomFilter), false).FirstOrDefault() as CustomFilter;
            return attr != null && !string.IsNullOrEmpty(attr.Name);
        }

        public static IFilter CreateFilterByAssemblyQualifiedName(string assemblyQualifiedName) {

            if(assemblyQualifiedName == null) {
                return null;
            }

            Type t = Type.GetType(assemblyQualifiedName);
            if(t == null) {
                return null;
            }

            if(!HasValidCustomFilterAttribute(t)) {
                return null;
            }

            return (IFilter) t.Assembly.CreateInstance(t.FullName);
        }
    }
}
