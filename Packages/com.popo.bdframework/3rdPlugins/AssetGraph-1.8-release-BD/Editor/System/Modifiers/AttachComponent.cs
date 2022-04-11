using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[CustomModifier("Attach Component", typeof(GameObject))]
public class AttachComponent : IModifier {

    enum AttachPolicy {
        RootObject = 1,
        MiddleObject = 2,
        LeafObject = 4
    }

    [SerializeField] private SerializedComponent m_component;
    [SerializeField] private AttachPolicy m_attachPolicy = AttachPolicy.RootObject | AttachPolicy.MiddleObject | AttachPolicy.LeafObject;
    [SerializeField] private string m_nameFormat;

    private int m_selectedIndex = -1;
    private Texture2D m_popupIcon;
    private Texture2D m_helpIcon;

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

        foreach (var o in assets) {
            GameObject go = o as GameObject;
            if (go == null) {
                continue;
            }

            if (!r.IsMatch (go.name)) {
                continue;
            }

            bool isRootObj = go.transform.parent == null;
            bool isLeafObj = go.transform.childCount == 0;
            bool isMiddleObj = !isRootObj && !isLeafObj;

            bool isTargeting = 
                (isRootObj && isRootObjTargeting) ||
                (isLeafObj && isLeafObjTargeting) ||
                (isMiddleObj && isMiddleObjTargeting);

            if (!isTargeting) {
                continue;
            }

            foreach (var info in m_component.Components) {
                var dst = go.GetComponent (info.ComponentType);
                if (dst == null) {
                    dst = go.AddComponent (info.ComponentType);
                }

                if (dst != null) {
                    EditorUtility.CopySerialized (info.Component, dst);
                }
            }
        }
	}

	// Draw inspector gui 
	public void OnInspectorGUI (Action onValueChanged) {

        if (m_component == null) {
            m_component = new SerializedComponent ("Attaching Components");
        }

        if (m_component.IsInvalidated) {
            m_component.Restore ();
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
                        m_component.Save ();
                        onValueChanged ();
                    }
                }
            }
        }
	}
}
