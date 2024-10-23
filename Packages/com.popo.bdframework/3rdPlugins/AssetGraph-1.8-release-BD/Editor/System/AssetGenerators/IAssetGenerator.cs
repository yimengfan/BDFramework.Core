using System;
using System.Linq;
using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    /// <summary>
    /// IAssetGenerator is an interface to generate new asset from incoming asset.
    /// Subclass of IAssetGenerator must have CustomAssetGenerator attribute.
    /// </summary>
	public interface IAssetGenerator {

        /// <summary>
        /// Called when validating this prefabBuilder.
        /// NodeException should be thrown if this modifier is not ready to be used for building.
        /// </summary>
        void OnValidate ();

        /// <summary>
        /// Gets the asset extension of generating asset.
        /// </summary>
        /// <returns>The extension in string format (e.g. ".png").</returns>
        /// <param name="asset">The source asset to generate from.</param>
        string GetAssetExtension (AssetReference asset);

        /// <summary>
        /// Gets the type of the asset.
        /// For type of assets that have associated importers, return type of Importer.
        /// Textures = TextureImporter, Audio = AudioImporter, Video = VideoClipImporter
        /// </summary>
        /// <returns>The asset type.</returns>
        /// <param name="asset">The source asset to generate from.</param>
        Type GetAssetType(AssetReference asset);

        /// <summary>
        /// Test if generator can generate new asset with given asset.
        /// NodeException should be thrown if there is any error that user should know about.
        /// </summary>
        /// <returns><c>true</c> if this instance can generate asset; otherwise, <c>false</c>.</returns>
        /// <param name="asset">Asset to examine if derivertive asset can be generated.</param>
        bool CanGenerateAsset (AssetReference asset);

        /// <summary>
        /// Generates the asset.
        /// </summary>
        /// <returns><c>true</c>, if asset was generated, <c>false</c> otherwise.</returns>
        /// <param name="asset">Asset to generate derivertive asset from.</param>
        /// <param name="generateAssetPath">Path to save generated derivertive asset.</param>
        bool GenerateAsset (AssetReference asset, string generateAssetPath);

        /// <summary>
        /// Draw Inspector GUI for this AssetGenerator.
        /// Make sure to call <c>onValueChanged</c>() when inspector values are modified. 
        /// It will save state of AssetGenerator object.
        /// </summary>
        /// <param name="onValueChanged">Action to call when inspector value changed.</param>
		void OnInspectorGUI (Action onValueChanged);
	}

    /// <summary>
    /// Attribute for Custom Asset Generator.
    /// </summary>
	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomAssetGenerator : Attribute {
		private string m_name;
		private string m_version;

		private const int kDEFAULT_ASSET_THRES = 10;

        /// <summary>
        /// GUI name of the generator.
        /// </summary>
        /// <value>The GUI name of the generator.</value>
		public string Name {
			get {
				return m_name;
			}
		}

        /// <summary>
        /// Version string of the generator.
        /// Version string is useful to force update all generated assets
        /// when generator have catastrophic changes.
        /// </summary>
        /// <value>The version string.</value>
		public string Version {
			get {
				return m_version;
			}
		}

        public CustomAssetGenerator (string name) {
			m_name = name;
			m_version = string.Empty;
		}

        public CustomAssetGenerator (string name, string version) {
			m_name = name;
			m_version = version;
		}

        public CustomAssetGenerator (string name, string version, int itemThreashold) {
			m_name = name;
			m_version = version;
		}
	}

	public class AssetGeneratorUtility {

        private static  Dictionary<string, string> s_attributeAssemblyQualifiedNameMap;

		public static Dictionary<string, string> GetAttributeAssemblyQualifiedNameMap () {

			if(s_attributeAssemblyQualifiedNameMap == null) {
				// attribute name or class name : class name
				s_attributeAssemblyQualifiedNameMap = new Dictionary<string, string>(); 

                var allBuilders = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var builders = assembly.GetTypes()
                        .Where(t => !t.IsInterface)
                        .Where(t => typeof(IAssetGenerator).IsAssignableFrom(t));
                    allBuilders.AddRange (builders);
                }

                foreach (var type in allBuilders) {
					// set attribute-name as key of dict if atribute is exist.
                    CustomAssetGenerator attr = 
                        type.GetCustomAttributes(typeof(CustomAssetGenerator), true).FirstOrDefault() as CustomAssetGenerator;

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

        public static string GetGUIName(IAssetGenerator generator) {
            CustomAssetGenerator attr = 
                generator.GetType().GetCustomAttributes(typeof(CustomAssetGenerator), false).FirstOrDefault() as CustomAssetGenerator;
			return attr.Name;
		}

		public static bool HasValidAttribute(Type t) {
            CustomAssetGenerator attr = 
                t.GetCustomAttributes(typeof(CustomAssetGenerator), false).FirstOrDefault() as CustomAssetGenerator;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static string GetGUIName(string className) {
			if(className != null) {
				var type = Type.GetType(className);
				if(type != null) {
                    CustomAssetGenerator attr = 
                        type.GetCustomAttributes(typeof(CustomAssetGenerator), false).FirstOrDefault() as CustomAssetGenerator;
					if(attr != null) {
						return attr.Name;
					}
				}
			}
			return string.Empty;
		}

		public static string GetVersion(string className) {
			var type = Type.GetType(className);
			if(type != null) {
                CustomAssetGenerator attr = 
                    type.GetCustomAttributes(typeof(CustomAssetGenerator), false).FirstOrDefault() as CustomAssetGenerator;
				if(attr != null) {
					return attr.Version;
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

        public static IAssetGenerator CreateGenerator(string guiName) {
			var className = GUINameToAssemblyQualifiedName(guiName);
			if(className != null) {
                var type = Type.GetType(className);
                if (type == null) {
                    return null;
                }
                return (IAssetGenerator) type.Assembly.CreateInstance(type.FullName);
			}
			return null;
		}

        public static IAssetGenerator CreateByAssemblyQualifiedName(string assemblyQualifiedName) {

			if(assemblyQualifiedName == null) {
				return null;
			}

			Type t = Type.GetType(assemblyQualifiedName);
			if(t == null) {
				return null;
			}

			if(!HasValidAttribute(t)) {
				return null;
			}

            return (IAssetGenerator) t.Assembly.CreateInstance(t.FullName);
		}
	}
}