using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEngine.AssetGraph {

    public class ComponentMenuUtility {
        private static List<Type> s_componentTypes;
        private static string[] s_componentNames;

        private static List<Type> s_componentTypesWithObjRefProp;
        private static string[] s_componentNamesWithObjRefProp;

        public static List<Type> GetComponentTypes() {

            if(s_componentTypes == null) {
                s_componentTypes = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var components = assembly.GetTypes ()
                        .Where (t => 
                            t.IsPublic && 
                            !t.IsAbstract &&
                            t != typeof(UnityEngine.Component) &&
                            t != typeof(UnityEngine.MonoBehaviour) &&
                            typeof(UnityEngine.Component).IsAssignableFrom (t)
                        );
                    s_componentTypes.AddRange (components);
                }
            }
            return s_componentTypes;
        }

        public static string[] GetComponentNames() {
            if (s_componentNames == null) {
                var types = GetComponentTypes ();
                s_componentNames = types.Select (t => t.Name).ToArray ();
            }
            return s_componentNames;
        }


        public static List<Type> GetScriptComponentTypesWithObjectReferenceProperty() {

            if(s_componentTypesWithObjRefProp == null) {
                s_componentTypesWithObjRefProp = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var components = assembly.GetTypes ()
                        .Where (t => 
                            t.IsPublic && 
                            !t.IsAbstract &&
                            t != typeof(UnityEngine.MonoBehaviour) &&
                            typeof(UnityEngine.MonoBehaviour).IsAssignableFrom (t)
                        );

                    foreach (var componentType in components) {
                        var infos = componentType.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        bool added = false;
                        foreach (var info in infos) {
                            if (!info.IsPublic) {
                                var attr = info.GetCustomAttributes (typeof(SerializeField), true);
                                if (attr == null || attr.Length == 0) {
                                    continue;
                                }
                            }
                            Type fieldType = info.FieldType;
                            Type elementType = fieldType.IsArray ? fieldType.GetElementType() : fieldType;

                            if (typeof(UnityEngine.Component).IsAssignableFrom (elementType)) {
                                s_componentTypesWithObjRefProp.Add (componentType);
                                added = true;
                                break;
                            }
                        }
                        if (added) {
                            continue;
                        }

                        var props = componentType.GetProperties (BindingFlags.Instance);
                        foreach (var p in props) {
                            if (!p.CanWrite) {
                                continue;
                            }

                            Type fieldType = p.PropertyType;
                            Type elementType = fieldType.IsArray ? fieldType.GetElementType() : fieldType;

                            if (typeof(UnityEngine.Component).IsAssignableFrom (elementType)) {
                                s_componentTypesWithObjRefProp.Add (componentType);
                                break;
                            }
                        }
                    }
                }
            }
            return s_componentTypesWithObjRefProp;
        }

        public static string[] GetScriptComponentNamesWithObjectReferenceProperty() {
            if (s_componentNamesWithObjRefProp == null) {
                var types = GetScriptComponentTypesWithObjectReferenceProperty ();
                s_componentNamesWithObjRefProp = types.Select (t => t.Name).ToArray ();
            }
            return s_componentNamesWithObjRefProp;
        }
    }
}

