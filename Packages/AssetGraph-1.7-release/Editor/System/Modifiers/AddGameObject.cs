using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[CustomModifier("Add GameObject", typeof(GameObject))]
public class AddGameObject : IModifier {

    enum AttachPolicy {
        RootObject = 1,
        MiddleObject = 2,
        LeafObject = 4
    }

    enum GameObjectPropertyPolicy {
        UseThisValue,
        CopyParentValue,
        CopyRootValue
    }

    enum ComponentSyncPolicy {
        LeaveExtraComponents,
        RemoveExtraComponents
    }


    [SerializeField] private SerializedComponent m_component;
    [SerializeField] private AttachPolicy m_attachPolicy = AttachPolicy.RootObject | AttachPolicy.MiddleObject | AttachPolicy.LeafObject;
    [SerializeField] private string m_nameFormat;
    [SerializeField] private GameObjectPropertyPolicy m_tagPolicy;
    [SerializeField] private GameObjectPropertyPolicy m_layerPolicy;
    [SerializeField] private GameObjectPropertyPolicy m_staticPolicy;
    [SerializeField] private GameObjectPropertyPolicy m_activePolicy;
    [SerializeField] private ComponentSyncPolicy m_componentSyncPolicy;

    private int m_selectedIndex = -1;
    private Texture2D m_popupIcon;
    private Texture2D m_helpIcon;

    private Editor m_goEditor;

    public void OnValidate () {
    }

	// Test if asset is different from intended configuration 
	public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group) {

        if (m_component == null) {
            return false;
        }

        if (m_component.IsInvalidated) {
            m_component.Restore ();
        }

        return assets.Where (a => a is GameObject).Any ();
	}

	// Actually change asset configurations. 
	public void Modify (UnityEngine.Object[] assets, List<AssetReference> group) {

        Regex r = new Regex(m_nameFormat);
        bool isRootObjTargeting = (m_attachPolicy & AttachPolicy.RootObject) > 0;
        bool isLeafObjTargeting = (m_attachPolicy & AttachPolicy.LeafObject) > 0;
        bool isMiddleObjTargeting = (m_attachPolicy & AttachPolicy.MiddleObject) > 0;
        GameObject copyingObject = m_component.InternalGameObject;

        foreach (var o in assets) {
            GameObject go = o as GameObject;
            if (go == null) {
                continue;
            }

            if (!r.IsMatch (go.name)) {
                continue;
            }

            Transform t = go.transform;

            string name = copyingObject.name.Replace ("{ParentName}", go.name);

            bool isRootObj = t.parent == null;
            bool isLeafObj = t.childCount == 0 || 
                (t.childCount == 1 && t.GetChild(0).name == name);
            bool isMiddleObj = !isRootObj && !isLeafObj;

            bool isTargeting = 
                (isRootObj && isRootObjTargeting) ||
                (isLeafObj && isLeafObjTargeting) ||
                (isMiddleObj && isMiddleObjTargeting);

            if (!isTargeting) {
                continue;
            }

            GameObject childTarget = null;

            GameObject rootObject = go;
            while (rootObject.transform.parent != null) {
                rootObject = rootObject.transform.parent.gameObject;
            }

            for (int i = 0; i < t.childCount; ++i) {
                Transform child = t.GetChild (i);
                if (child.name == name) {
                    childTarget = child.gameObject;
                    break;
                }
            }

            if (childTarget != null) {
                if (m_componentSyncPolicy == ComponentSyncPolicy.RemoveExtraComponents) {
                    var components = childTarget.GetComponents<Component> ();
                    foreach (var c in components) {
                        if (!m_component.Components.Where (info => info.ComponentType == c.GetType ()).Any ()) {
                            Component.DestroyImmediate (c);
                        }
                    }
                }

                EditorUtility.CopySerializedIfDifferent (copyingObject, childTarget);

                foreach (var info in m_component.Components) {
                    var dstObj = childTarget.GetComponent (info.ComponentType);
                    if (dstObj == null) {
                        dstObj = childTarget.AddComponent (info.ComponentType);
                    }
                    EditorUtility.CopySerializedIfDifferent (info.Component, dstObj);
                }
            } else {
                childTarget = GameObject.Instantiate (copyingObject, t, false);
            }

            childTarget.name = name;

            if (m_tagPolicy != GameObjectPropertyPolicy.UseThisValue) {
                var srcObj = (m_tagPolicy == GameObjectPropertyPolicy.CopyParentValue) ? go : rootObject;
                childTarget.tag = srcObj.tag;
            }

            if (m_layerPolicy != GameObjectPropertyPolicy.UseThisValue) {
                var srcObj = (m_layerPolicy == GameObjectPropertyPolicy.CopyParentValue) ? go : rootObject;
                childTarget.layer = srcObj.layer;
            }

            if (m_staticPolicy != GameObjectPropertyPolicy.UseThisValue) {
                var srcObj = (m_staticPolicy == GameObjectPropertyPolicy.CopyParentValue) ? go : rootObject;
                var srcFlags = GameObjectUtility.GetStaticEditorFlags (srcObj);
                GameObjectUtility.SetStaticEditorFlags (childTarget, srcFlags);
            }

            if (m_activePolicy != GameObjectPropertyPolicy.UseThisValue) {
                var srcObj = (m_staticPolicy == GameObjectPropertyPolicy.CopyParentValue) ? go : rootObject;
                var activeFlag = srcObj.activeSelf;
                childTarget.SetActive (activeFlag);
            }
        }
	}

	// Draw inspector gui 
	public void OnInspectorGUI (Action onValueChanged) {

        if (m_component == null) {
            m_component = new SerializedComponent ();
        }

        if (m_component.IsInvalidated) {
            m_component.Restore ();
            m_component.SyncAttachedComponents ();

            if (m_goEditor == null) {
                m_goEditor = Editor.CreateEditor (m_component.InternalGameObject);
            }
        }

        var newAttachPolicy = (AttachPolicy)EditorGUILayout.EnumFlagsField ("Attach Policy", m_attachPolicy);
        if(newAttachPolicy != m_attachPolicy) {
            m_attachPolicy = newAttachPolicy;
            onValueChanged();
        }

        var newNameFormat = EditorGUILayout.TextField ("Name Pattern", m_nameFormat);
        if(newNameFormat != m_nameFormat) {
            m_nameFormat = newNameFormat;
            onValueChanged();
        }

        EditorGUI.BeginChangeCheck ();

        m_goEditor.DrawHeader ();

        GUILayout.Space (8f);

        EditorGUILayout.HelpBox ("You can use {ParentName} variable for GameObject's naming.", MessageType.Info);

        GUILayout.Space (4f);

        // reset label Width ()
        EditorGUIUtility.labelWidth = 0;

        m_activePolicy = (GameObjectPropertyPolicy)EditorGUILayout.EnumPopup ("Active Flag", m_activePolicy);
        m_tagPolicy = (GameObjectPropertyPolicy)EditorGUILayout.EnumPopup ("Tag", m_tagPolicy);
        m_layerPolicy = (GameObjectPropertyPolicy)EditorGUILayout.EnumPopup ("Layer", m_layerPolicy);
        m_staticPolicy = (GameObjectPropertyPolicy)EditorGUILayout.EnumPopup ("Static Flags", m_staticPolicy);
        m_componentSyncPolicy = (ComponentSyncPolicy)EditorGUILayout.EnumPopup ("Extra Component", m_componentSyncPolicy);

        GUILayout.Space (8f);

        m_component.OnInspectorGUI ();

        if (EditorGUI.EndChangeCheck ()) {
            m_component.Save ();
            onValueChanged ();
        }

        GUILayout.Space (10f);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

        using (new EditorGUILayout.HorizontalScope ()) {
            m_selectedIndex = EditorGUILayout.Popup ("Component", m_selectedIndex, ComponentMenuUtility.GetComponentNames ());

            using (new EditorGUI.DisabledScope (m_selectedIndex < 0)) {
                if (GUILayout.Button ("Add", GUILayout.Width(40))) {
                    var type = ComponentMenuUtility.GetComponentTypes()[m_selectedIndex];

                    var c = m_component.AddComponent (type);
                    if (c != null) {
                        // addition of some component implicitly adds other component by RequireComponent attribute, 
                        // so ensure to track everything added.
                        m_component.SyncAttachedComponents ();
                        m_component.Save ();
                        onValueChanged ();
                    }
                }
            }
        }
	}
}
