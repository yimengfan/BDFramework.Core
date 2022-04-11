using System;
using System.Linq;
using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    /// <summary>
    /// IPrefabBuilder is an interface to create Prefab AssetReference from incoming asset group.
    /// Subclass of IPrefabBuilder must have CUstomPrefabBuilder attribute.
    /// </summary>
	public interface IPrefabBuilder {

        /// <summary>
        /// Called when validating this prefabBuilder.
        /// NodeException should be thrown if this modifier is not ready to be used for building.
        /// </summary>
        void OnValidate ();

		/**
		 * 
		 * @result Name of prefab file if prefab can be created. null if not.
		 */
        /// <summary>
        /// Determines whether this instance can create prefab with the specified groupKey objects.
        /// </summary>
        /// <returns><c>true</c> if this instance can create prefab the specified groupKey objects; otherwise, <c>false</c>.</returns>
        /// <param name="groupKey">Group key.</param>
        /// <param name="objects">list of Objects passed from node to create a prefab.</param>
        /// <param name="description">Prefab descriptions to create with this IPrefabBuilder. if the method is returning true, valid descriptions should be set.</param>
        bool CanCreatePrefab (string groupKey, List<Object> objects, ref PrefabCreateDescription description);

        /// <summary>
        /// Creates the prefab.
        /// </summary>
        /// <returns>The prefab.</returns>
        /// <param name="groupKey">Group key.</param>
        /// <param name="objects">Objects.</param>
        /// <param name="previous">Previous prefab.</param>
        GameObject CreatePrefab(string groupKey, List<Object> objects, GameObject previous);

        /// <summary>
        /// Draw Inspector GUI for this PrefabBuilder.
        /// </summary>
        /// <param name="onValueChanged">On value changed.</param>
		void OnInspectorGUI (Action onValueChanged);
	}

    /// <summary>
    /// Custom prefab builder attribute.
    /// </summary>
	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomPrefabBuilder : Attribute {
		private string m_name;
		private string m_version;
		private int m_assetThreshold;

		private const int kDEFAULT_ASSET_THRES = 10;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
		public string Name {
			get {
				return m_name;
			}
		}

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
		public string Version {
			get {
				return m_version;
			}
		}

        /// <summary>
        /// Gets the asset threshold.
        /// </summary>
        /// <value>The asset threshold.</value>
		public int AssetThreshold {
			get {
				return m_assetThreshold;
			}
		}

		public CustomPrefabBuilder (string name) {
			m_name = name;
			m_version = string.Empty;
			m_assetThreshold = kDEFAULT_ASSET_THRES;
		}

		public CustomPrefabBuilder (string name, string version) {
			m_name = name;
			m_version = version;
			m_assetThreshold = kDEFAULT_ASSET_THRES;
		}

		public CustomPrefabBuilder (string name, string version, int itemThreashold) {
			m_name = name;
			m_version = version;
			m_assetThreshold = itemThreashold;
		}
	}

	public partial class PrefabBuilderUtility {

        private static  Dictionary<string, string> s_attributeAssemblyQualifiedNameMap;

		public static Dictionary<string, string> GetAttributeAssemblyQualifiedNameMap () {

			if(s_attributeAssemblyQualifiedNameMap == null) {
				// attribute name or class name : class name
				s_attributeAssemblyQualifiedNameMap = new Dictionary<string, string>(); 

                var allBuilders = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var builders = assembly.GetTypes()
                        .Where(t => !t.IsInterface)
                        .Where(t => typeof(IPrefabBuilder).IsAssignableFrom(t));
                    allBuilders.AddRange (builders);
                }

                foreach (var type in allBuilders) {
					// set attribute-name as key of dict if atribute is exist.
					CustomPrefabBuilder attr = 
						type.GetCustomAttributes(typeof(CustomPrefabBuilder), true).FirstOrDefault() as CustomPrefabBuilder;

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

		public static string GetPrefabBuilderGUIName(IPrefabBuilder builder) {
			CustomPrefabBuilder attr = 
				builder.GetType().GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
			return attr.Name;
		}

		public static bool HasValidCustomPrefabBuilderAttribute(Type t) {
			CustomPrefabBuilder attr = 
				t.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static string GetPrefabBuilderGUIName(string className) {
			if(className != null) {
				var type = Type.GetType(className);
				if(type != null) {
					CustomPrefabBuilder attr = 
                        type.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
					if(attr != null) {
						return attr.Name;
					}
				}
			}
			return string.Empty;
		}

		public static string GetPrefabBuilderVersion(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomPrefabBuilder attr = 
                    type.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
				if(attr != null) {
					return attr.Version;
				}
			}
			return string.Empty;
		}

		public static int GetPrefabBuilderAssetThreshold(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomPrefabBuilder attr = 
                    type.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
				if(attr != null) {
					return attr.AssetThreshold;
				}
			}
			return 0;
		}

		public static string GUINameToAssemblyQualifiedName(string guiName) {
			var map = GetAttributeAssemblyQualifiedNameMap();

			if(map.ContainsKey(guiName)) {
				return map[guiName];
			}

			return null;
		}

		public static IPrefabBuilder CreatePrefabBuilder(string guiName) {
			var className = GUINameToAssemblyQualifiedName(guiName);
			if(className != null) {
                var type = Type.GetType(className);
                if (type == null) {
                    return null;
                }
                return (IPrefabBuilder) type.Assembly.CreateInstance(type.FullName);
			}
			return null;
		}

		public static IPrefabBuilder CreatePrefabBuilderByAssemblyQualifiedName(string assemblyQualifiedName) {

			if(assemblyQualifiedName == null) {
				return null;
			}

			Type t = Type.GetType(assemblyQualifiedName);
			if(t == null) {
				return null;
			}

			if(!HasValidCustomPrefabBuilderAttribute(t)) {
				return null;
			}

            return (IPrefabBuilder) t.Assembly.CreateInstance(t.FullName);
		}
	}
}