using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEngine.AssetGraph {

    [Serializable]
	public class SerializedComponent {
        [Serializable]
        public class ComponentInfo {
            [SerializeField] private string m_typeInfo;
            [SerializeField] private string m_componentData;

            private Type m_componentType;
            private Component m_component;
            private Editor m_componentEditor;

            static Texture2D s_helpIcon;
            static Texture2D s_popupIcon;

            public Type ComponentType {
                get {
                    return m_componentType;
                }
            }

            public Component Component {
                get {
                    return m_component;
                }
            }

            public Editor Editor {
                get {
                    return m_componentEditor;
                }
            }

            public string Data {
                get {
                    return m_componentData;
                }
            }

            public ComponentInfo(Type t, Component c) {
                m_typeInfo = t.AssemblyQualifiedName;
                m_componentType = t;
                m_component = c;
                m_componentEditor = Editor.CreateEditor (m_component);
                Save();
            }

            public ComponentInfo(ComponentInfo info) {
                m_typeInfo = info.m_typeInfo;
                m_componentType = info.m_componentType;
                m_componentData = info.m_componentData;
            }

            public void Save() {
                if (m_component != null) {
                    m_componentData = CustomScriptUtility.EncodeString(EditorJsonUtility.ToJson(m_component));
                }
            }

            public void Restore(GameObject o) {

                Invalidate (false);

                UnityEngine.Assertions.Assert.IsNotNull (m_typeInfo);

                if (m_componentType == null) {
                    m_componentType = Type.GetType (m_typeInfo);
                }
                UnityEngine.Assertions.Assert.IsNotNull (m_componentType);

                m_component = o.GetComponent (m_componentType);
                if (m_component == null) {
                    m_component = o.AddComponent (m_componentType);
                    if (m_componentData != null) {
                        EditorJsonUtility.FromJsonOverwrite (CustomScriptUtility.DecodeString (m_componentData), m_component);
                    }
                }

                m_componentEditor = Editor.CreateEditor (m_component);
            }

            public void Invalidate(bool destroyComponent) {
                if (destroyComponent && m_component != null) {
                    Component.DestroyImmediate (m_component);
                }
                if (m_componentEditor != null) {
                    Editor.DestroyImmediate (m_componentEditor);
                    m_componentEditor = null;
                }
                m_componentType = null;
                m_component = null;
            }

            public void DrawDefaultInspector() {
                if (m_componentEditor != null) {
                    m_componentEditor.DrawDefaultInspector ();
                }
            }

            public void DrawHeader(SerializedComponent parent) {

                if (s_popupIcon == null) {
                    s_popupIcon = EditorGUIUtility.Load ("icons/_Popup.png") as Texture2D;
                }

                if (s_helpIcon == null) {
                    s_helpIcon = EditorGUIUtility.Load ("icons/_Help.png") as Texture2D;
                }

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                using (new EditorGUILayout.HorizontalScope ()) {
                    var thumbnail = AssetPreview.GetMiniTypeThumbnail (m_componentType);
                    if (thumbnail == null) {
                        if (typeof(MonoBehaviour).IsAssignableFrom (m_componentType)) {
                            thumbnail = AssetPreview.GetMiniTypeThumbnail (typeof(MonoScript));
                        } else {
                            thumbnail = AssetPreview.GetMiniTypeThumbnail (typeof(UnityEngine.Object));
                        }
                    }

                    GUILayout.Label(thumbnail, GUILayout.Width(32f), GUILayout.Height(32f));
                    if (m_component is Behaviour) {
                        Behaviour b = m_component as Behaviour;
                        b.enabled = EditorGUILayout.ToggleLeft (m_componentType.Name, b.enabled, EditorStyles.boldLabel);
                    } else {
                        GUILayout.Label (m_componentType.Name, EditorStyles.boldLabel);
                    }

                    GUILayout.FlexibleSpace ();

                    if (Help.HasHelpForObject (m_component)) {
                        var tooltip = $"Open Reference for {m_componentType.Name}.";
                        if(GUILayout.Button(new GUIContent(s_helpIcon, tooltip), EditorStyles.miniLabel, GUILayout.Width(20f), GUILayout.Height(20f))) {
                            Help.ShowHelpForObject (m_component);
                        }
                    }

                    if(GUILayout.Button(s_popupIcon, EditorStyles.miniLabel, GUILayout.Width(20f), GUILayout.Height(20f))) {
                        GenericMenu m = new GenericMenu ();
                        m.AddItem (new GUIContent ("Copy Component"), false, () => {
                            UnityEditorInternal.ComponentUtility.CopyComponent(m_component);
                        });

                        var pasteLabel = new GUIContent ("Paste Component Values");
                        m.AddItem (pasteLabel, false, () => {
                            UnityEditorInternal.ComponentUtility.PasteComponentValues(m_component);
                        });

                        m.AddItem (new GUIContent ("Remove Component"), false, () => {
                            parent.RemoveComponent(this);
                        });

                        MonoScript s = TypeUtility.LoadMonoScript(m_componentType.AssemblyQualifiedName);
                        if(s != null) {
                            m.AddSeparator ("");
                            m.AddItem(
                                new GUIContent("Edit Script"),
                                false, 
                                () => {
                                    AssetDatabase.OpenAsset(s, 0);
                                }
                            );
                        }

                        m.ShowAsContext ();
                    }
                }
                GUILayout.Space (4f);
            }

            public void OnInspectorGUI(SerializedComponent parent) {
                DrawHeader (parent);

                // indent inspector
                GUILayout.BeginHorizontal();
                GUILayout.Space(16f);
                GUILayout.BeginVertical();
                DrawDefaultInspector ();
                GUILayout.EndVertical ();
                GUILayout.EndHorizontal ();
            }
        }

        [SerializeField] private List<ComponentInfo> m_attachedComponents;
        [SerializeField] private string m_instanceData;
        private string m_newGameObjectName;

        private GameObject m_gameObject;
        private Editor m_gameObjectEditor;

        public bool IsInvalidated {
            get {
                return m_gameObject == null;
            }
        }

		public string Data {
			get {
				return m_instanceData;
			}
		}

        public List<ComponentInfo> Components {
            get {
                return m_attachedComponents;
            }
        }

        public GameObject InternalGameObject {
            get {
                if (m_gameObject == null) {
                    Deserialize ();
                }
                return m_gameObject;
            }
        }

        public Editor InternalGameObjectEditor {
            get {
                return m_gameObjectEditor;
            }
        }

        public SerializedComponent(string gameObjectName = null) {
			m_instanceData = string.Empty;
            m_attachedComponents = new List<ComponentInfo> ();
            m_newGameObjectName = gameObjectName;
		}

        public SerializedComponent(SerializedComponent c) {
			m_instanceData = c.m_instanceData;
            m_attachedComponents = new List<ComponentInfo> ();
            for (int i = 0; i < c.m_attachedComponents.Count; ++i) {
                m_attachedComponents.Add (new ComponentInfo (c.m_attachedComponents [i]));
            }
		}

        public void Invalidate() {
            if (m_gameObject != null) {
                GameObject.DestroyImmediate (m_gameObject);
                m_gameObject = null;
            }
            if (m_gameObjectEditor != null) {
                Editor.DestroyImmediate (m_gameObjectEditor);
                m_gameObjectEditor = null;
            }
            foreach (var info in m_attachedComponents) {
                info.Invalidate (false);
            }
        }

        public void Restore() {
            if (m_gameObject == null) {
                Deserialize ();
            }
        }

        private void Deserialize() {

            m_gameObject = new GameObject ();
            m_gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            m_gameObject.name = (string.IsNullOrEmpty(m_newGameObjectName)) ? "New GameObject" : m_newGameObjectName;

            if (!string.IsNullOrEmpty (m_instanceData)) {
                var decoded = CustomScriptUtility.DecodeString (m_instanceData);
                EditorJsonUtility.FromJsonOverwrite (decoded, m_gameObject);
            }

            if (m_attachedComponents == null) {
                m_attachedComponents = new List<ComponentInfo> ();
            }
            SortComponents ();

            foreach (var info in m_attachedComponents) {
                info.Restore (m_gameObject);
            }

            if (m_gameObjectEditor == null) {
                m_gameObjectEditor = Editor.CreateEditor (m_gameObjectEditor);
            }
		}

		public void Save() {
            if(m_gameObject != null) {
                m_instanceData = CustomScriptUtility.EncodeString(EditorJsonUtility.ToJson(m_gameObject));
			}
            SortComponents ();
            foreach (var info in m_attachedComponents) {
                info.Save ();
            }
		}

        public SerializedComponent Clone() {
			Save();
            return new SerializedComponent(this);
		}

        public T GetComponent<T> () where T: UnityEngine.Component {
            var c = InternalGameObject.GetComponent<T> ();
            if (c != null) {
                if (!m_attachedComponents.Where (i => i.Component == c).Any ()) {
                    m_attachedComponents.Add (new ComponentInfo (typeof(T), c));
                    Save ();
                }
            }
            return c;
        }

        public UnityEngine.Component GetComponent (Type t)  {
            var c = InternalGameObject.GetComponent (t);
            if (c != null) {
                if (!m_attachedComponents.Where (i => i.Component == c).Any ()) {
                    m_attachedComponents.Add (new ComponentInfo (t, c));
                    Save ();
                }
            }
            return c;
        }

        public T AddComponent<T> () where T: UnityEngine.Component {
            var c = InternalGameObject.AddComponent<T> ();
            if (c != null) {
                m_attachedComponents.Add (new ComponentInfo (typeof(T), c));
                Save ();
            }
            return c;
        }

        public UnityEngine.Component AddComponent (Type t)  {
            var c = InternalGameObject.AddComponent (t);
            if (c != null) {
                m_attachedComponents.Add (new ComponentInfo (t, c));
                Save ();
            }
            return c;
        }

        public void SyncAttachedComponents ()  {
            var added = false;
            var components = InternalGameObject.GetComponents<Component> ();
            foreach(var c in components) {
                if (!m_attachedComponents.Where (i => i.Component == c).Any ()) {
                    m_attachedComponents.Add (new ComponentInfo (c.GetType(), c));
                    added = true;
                }
            }

            // sometimes component changes by newly added component (i.e. Transform->RectTransform)
            for (int i = 0; i < m_attachedComponents.Count; ++i) {
                if (!components.Contains(m_attachedComponents[i].Component)) {
                    m_attachedComponents.RemoveAt (i);
                }
            }

            if (added) {
                Save ();
            }
        }

        private void SortComponents() {
            m_attachedComponents.Sort ((a, b) => {
                if(a.ComponentType == b.ComponentType) {
                    return 0;
                }
                if(a.ComponentType == null) {
                    return 1;
                }
                if(b.ComponentType == null) {
                    return -1;
                }

                if(a.ComponentType == typeof(Transform) || a.ComponentType.IsSubclassOf(typeof(Transform))) {
                    return -1;
                }
                if(b.ComponentType == typeof(Transform) || b.ComponentType.IsSubclassOf(typeof(Transform))) {
                    return 1;
                }

                return a.ComponentType.GetHashCode() - b.ComponentType.GetHashCode();
            });
        }

        public void OnInspectorGUI() {
            foreach (var info in m_attachedComponents) {
                info.OnInspectorGUI (this);
            }
        }

        public void RemoveComponent(ComponentInfo info) {
            info.Invalidate (true);
            m_attachedComponents.Remove (info);
        }

		public override bool Equals(object rhs)
		{
            SerializedComponent other = rhs as SerializedComponent; 
			if (other == null) {
				return false;
			} else {
				return other == this;
			}
		}

		public override int GetHashCode()
		{
			return (m_instanceData == null)? base.GetHashCode() : m_instanceData.GetHashCode();
		}

        public static bool operator == (SerializedComponent lhs, SerializedComponent rhs) {

			object lobj = lhs;
			object robj = rhs;

			if(lobj == null && robj == null) {
				return true;
			}
			if(lobj == null || robj == null) {
				return false;
			}

            if (lhs.m_instanceData != rhs.m_instanceData) {
                return false;
            }

            if (lhs.m_attachedComponents.Count != rhs.m_attachedComponents.Count) {
                return false;
            }

            for(int i = 0; i < lhs.m_attachedComponents.Count; ++i) {
                if (lhs.m_attachedComponents [i].Data != rhs.m_attachedComponents [i].Data) 
                {
                    return false;
                }
            }

            return true;
		}

        public static bool operator != (SerializedComponent lhs, SerializedComponent rhs) {
			return !(lhs == rhs);
		}
	}
}

