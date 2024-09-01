using System;
using System.Linq;
using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    /// <summary>
    /// IModifier is an interface which modifies incoming assets.
    /// Subclass of IModifier must have <c>CustomModifier</c> attribute.
    /// </summary>
	public interface IModifier {

        /// <summary>
        /// Called when validating this prefabBuilder.
        /// NodeException should be thrown if this modifier is not ready to be used for building.
        /// </summary>
        void OnValidate ();

        /// <summary>
        /// Test if incoming assset is different from this IModifier's setting.
        /// </summary>
        /// <returns><c>true</c> if this instance is modified the specified assets; otherwise, <c>false</c>.</returns>
        /// <param name="assets">Assets.</param>
        bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group);

        /// <summary>
        /// Modify incoming assets.
        /// </summary>
        /// <param name="assets">Assets.</param>
        void Modify (UnityEngine.Object[] assets, List<AssetReference> group);

        /// <summary>
        /// Draw Inspector GUI for this Modifier.
        /// </summary>
        /// <param name="onValueChanged">On value changed.</param>
		void OnInspectorGUI (Action onValueChanged);
	}

    /// <summary>
    /// CustomModifier attribute is to declare the class is used as a IModifier. 
    /// </summary>
	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomModifier : Attribute {
		private string m_name;
		private Type m_modifyFor;

        /// <summary>
        /// Name of Modifier appears on GUI.
        /// </summary>
        /// <value>The name.</value>
		public string Name {
			get {
				return m_name;
			}
		}

        /// <summary>
        /// Type of asset Modifier modifies.
        /// </summary>
        /// <value>For.</value>
		public Type For {
			get {
				return m_modifyFor;
			}
		}

        /// <summary>
        /// CustomModifier declares the class is used as a IModifier.
        /// </summary>
        /// <param name="name">Name of Modifier appears on GUI.</param>
        /// <param name="modifyFor">Type of asset Modifier modifies.</param>
		public CustomModifier (string name, Type modifyFor) {
			m_name = name;
			m_modifyFor = modifyFor;
		}
	}

	public class ModifierUtility {
        private static Dictionary<Type, Dictionary<string, string>> s_attributeAssemblyQualifiedNameMap;

        private static Dictionary<Type, Dictionary<string, string>> GetAttributeAssemblyQualifiedNameMap() {

            if(s_attributeAssemblyQualifiedNameMap == null) {
                s_attributeAssemblyQualifiedNameMap =  new Dictionary<Type, Dictionary<string, string>>();

                var allBuilders = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var builders = assembly.GetTypes()
                        .Where(t => t != typeof(IModifier))
                        .Where(t => typeof(IModifier).IsAssignableFrom(t));
                    allBuilders.AddRange (builders);
                }

                foreach (var builder in allBuilders) {
                    CustomModifier attr = 
                        builder.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
                    
                    if (attr != null) {
                        if (!s_attributeAssemblyQualifiedNameMap.ContainsKey (attr.For)) {
                            s_attributeAssemblyQualifiedNameMap[attr.For] = new Dictionary<string, string>();
                        }
                        var map = s_attributeAssemblyQualifiedNameMap[attr.For];

                        if (!map.ContainsKey(attr.Name)) {
                            map[attr.Name] = builder.AssemblyQualifiedName;
                        } else {
                            LogUtility.Logger.LogWarning(LogUtility.kTag, "Multiple CustomModifier class with the same name/type found. Ignoring " + builder.Name);
                        }
                    }
                }
            }
            return s_attributeAssemblyQualifiedNameMap;
        }

        public static IEnumerable<Type> GetModifyableTypes () {
            return GetAttributeAssemblyQualifiedNameMap().Keys.AsEnumerable();
        }

		public static Dictionary<string, string> GetAttributeAssemblyQualifiedNameMap (Type targetType) {
			UnityEngine.Assertions.Assert.IsNotNull(targetType);
            return GetAttributeAssemblyQualifiedNameMap()[targetType];
		}

		public static string GetModifierGUIName(IModifier m) {
			CustomModifier attr = 
				m.GetType().GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
			return attr.Name;
		}

		public static string GetModifierGUIName(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomModifier attr = type.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
				if(attr != null) {
					return attr.Name;
				}
			}
			return string.Empty;
		}

		public static string GUINameToAssemblyQualifiedName(string guiName, Type targetType) {
			var map = GetAttributeAssemblyQualifiedNameMap(targetType);

			if(map.ContainsKey(guiName)) {
				return map[guiName];
			}

			return null;
		}

		public static Type GetModifierTargetType(IModifier m) {
			CustomModifier attr = 
				m.GetType().GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
			UnityEngine.Assertions.Assert.IsNotNull(attr);
			return attr.For;
		}

		public static Type GetModifierTargetType(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomModifier attr = type.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;
				if(attr != null) {
					return attr.For;
				}
			}
			return null;
		}

		public static bool HasValidCustomModifierAttribute(Type t) {
			CustomModifier attr = 
				t.GetCustomAttributes(typeof(CustomModifier), false).FirstOrDefault() as CustomModifier;

			if(attr != null) {
				return !string.IsNullOrEmpty(attr.Name) && attr.For != null;
			}
			return false;
		}

		public static IModifier CreateModifier(string guiName, Type targetType) {
            var assemblyQualifiedName = GUINameToAssemblyQualifiedName(guiName, targetType);
			if(assemblyQualifiedName != null) {
                var type = Type.GetType(assemblyQualifiedName);
                if (type == null) {
                    return null;
                }

                return (IModifier) type.Assembly.CreateInstance(type.FullName);
			}
			return null;
		}

		public static IModifier CreateModifier(string assemblyQualifiedName) {

			if(assemblyQualifiedName == null) {
				return null;
			}

			Type t = Type.GetType(assemblyQualifiedName);
			if(t == null) {
				return null;
			}

			if(!HasValidCustomModifierAttribute(t)) {
				return null;
			}

            return (IModifier) t.Assembly.CreateInstance(t.FullName);
		}
	}
}