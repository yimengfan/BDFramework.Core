using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;
using UnityEngine.AssetGraph;
using Object = UnityEngine.Object;

[CustomPrefabBuilder("[Experimental]Replace GameObject By Name", "v1.0", 1)]
public class ReplaceGameObjectByName : IPrefabBuilder {

    [Serializable]
    public class ReplaceEntry {
        [SerializeField] public string      name;
        [SerializeField] public GameObjectReference  dstObject;
    }

    [SerializeField] List<ReplaceEntry> m_replaceEntries;

    public List<ReplaceEntry> ReplaceEntries
    {
        get { return m_replaceEntries; }
    }

    private string GetPrefabName(string srcGameObjectName, string groupKeyName) {
        return $"{srcGameObjectName}_{groupKeyName}";
    }

    public void OnValidate () {

        if (m_replaceEntries != null) {
            foreach (var e in m_replaceEntries) {
                if (e.dstObject == null || e.dstObject.Empty) {
                    throw new NodeException ("Replacing GameObject is not set.", "Configure Replacing Object from inspector.");
                }
            }
        }

    }

	/**
		 * Test if prefab can be created with incoming assets.
		 * @result Name of prefab file if prefab can be created. null if not.
		 */
    public bool CanCreatePrefab (string groupKey, List<UnityEngine.Object> objects, ref PrefabCreateDescription description) {

        var go = objects.Find(o => o.GetType() == typeof(UnityEngine.GameObject) &&
            ((GameObject)o).transform.parent == null );

		if(go != null) {
            description.prefabName = GetPrefabName (go.name, groupKey);
		}

		return go != null;
	}

	/**
	 * Create Prefab.
	 */ 
    public GameObject CreatePrefab(string groupKey, List<Object> objects, GameObject previous) {

        GameObject src = (GameObject)objects.Find(o => o.GetType() == typeof(UnityEngine.GameObject) &&
            ((GameObject)o).transform.parent == null );

        GameObject go = GameObject.Instantiate (src);

        go.name = GetPrefabName (src.name, groupKey);

        if (m_replaceEntries != null) {
            ReplaceChildRecursively(go);
        }

		return go;
	}

    private void ReplaceChildRecursively(GameObject parent) {
        for (int i = 0; i < parent.transform.childCount; ++i) {
            var childTransform = parent.transform.GetChild (i);
            foreach(var r in m_replaceEntries) {
                if (childTransform.gameObject.name == r.name) {
                    var newObj = GameObject.Instantiate (r.dstObject.Object, 
                        childTransform.position, 
                        childTransform.rotation, 
                        parent.transform) as GameObject;
                    newObj.SetActive (childTransform.gameObject.activeSelf);
                    newObj.name = childTransform.gameObject.name; // suppress "(Clone)"
                    UnityEngine.Object.DestroyImmediate (childTransform.gameObject);
                }
            }
            if (childTransform != null) {
                if (childTransform.childCount > 0) {
                    ReplaceChildRecursively (childTransform.gameObject);
                }
            }
        }
    }

	/**
	 * Draw Inspector GUI for this PrefabBuilder.
	 */ 
	public void OnInspectorGUI (Action onValueChanged) {

        EditorGUILayout.HelpBox ("Replace Game Object By Name create prefab by replacing children of incoming GameObject using assigned names and Prefabs.", MessageType.Info);

        if (m_replaceEntries == null) {
            m_replaceEntries = new List<ReplaceEntry> ();
            onValueChanged ();
        }

        using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {

            for (int i = 0; i < m_replaceEntries.Count; ++i) {
                using (new EditorGUILayout.HorizontalScope ()) {
                    if(GUILayout.Button("-")) {
                        m_replaceEntries.RemoveAt (i);
                        onValueChanged ();
                        return;
                    }
                    var newName = EditorGUILayout.TextField(m_replaceEntries[i].name);
                    if (newName != m_replaceEntries [i].name) {
                        m_replaceEntries [i].name = newName;
                        onValueChanged ();
                    }

                    var newObj  = (UnityEngine.GameObject)EditorGUILayout.ObjectField(m_replaceEntries[i].dstObject.Object, 
                        typeof(UnityEngine.GameObject), false);
                    
                    if (newObj != m_replaceEntries [i].dstObject.Object) {
                        m_replaceEntries [i].dstObject.Object = newObj;
                        onValueChanged ();
                    }
                }
            }


            GUILayout.Space(10f);

            if(GUILayout.Button("+")) {
                m_replaceEntries.Add (new ReplaceEntry() );
                onValueChanged ();
            }
        }
	}
}
