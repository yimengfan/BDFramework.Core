using UnityEngine;
using UnityEditor;

using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;

[CustomModifier ("Set Reference Variable", typeof(GameObject))]
public class SetReference : IModifier
{

    enum AttachPolicy
    {
        RootObject = 1,
        MiddleObject = 2,
        LeafObject = 4
    }

    public enum Kind
    {
        Field,
        Property
    }

    public enum SearchDomain
    {
        FromRoot,
        FromMyself,
        FromParent,
        FromSiblings,
        FromChildrens
    }

    public enum TypeMatchPolicy
    {
        ExactMatch,
        AllAssignableTypes,
        SpecificTypes,
    }

    [Serializable]
    public class FieldEditInfo
    {
        public Kind kind;
        public string name;
        public string niceName;

        public FieldEditInfo(FieldInfo info) {
            name = info.Name;
            niceName = ObjectNames.NicifyVariableName(name);
            kind = Kind.Field;
        }

        public FieldEditInfo(PropertyInfo info) {
            name = info.Name;
            niceName = ObjectNames.NicifyVariableName(name);
            kind = Kind.Property;
        }

        public override string ToString() {
            return $"{niceName} ({((kind == Kind.Field) ? "Field" : "Property")})";
        }
    }

    // base object info
    [SerializeField] private AttachPolicy m_attachPolicy = AttachPolicy.RootObject | AttachPolicy.MiddleObject | AttachPolicy.LeafObject;
    [SerializeField] private string m_targetNamePattern;
    [SerializeField] private string m_targetComponentTypeName;

    [SerializeField] private Kind m_fieldKind;
    [SerializeField] private string m_fieldName;
    [SerializeField] private string m_nicifiedFieldName;

    [SerializeField] private SearchDomain m_searchDomain = SearchDomain.FromMyself;
    [SerializeField] private TypeMatchPolicy m_typeMatchPolicy = TypeMatchPolicy.ExactMatch;
    [SerializeField] private string m_searchingNamePattern;

    [SerializeField] private List<string> m_selectedMachingTypeNames;

    private System.Reflection.FieldInfo m_fieldInfo;
    private System.Reflection.PropertyInfo m_propertyInfo;
    private Type m_targetComponentType;
    private Type m_fieldValueType;
    private Type m_fieldValueElementType;
    private List<Type> m_matchingTypes;
    private List<Type> m_selectedMatchingTypes;

    private List<FieldEditInfo> m_properties;
    private string[] m_propNames;

    private string m_niceTypeName;

    private int m_componentSelectedIndex = -1;
    private int m_fieldSelectedIndex = -1;

    private void Invalidate(bool resetComponent) {
        m_fieldName = string.Empty;
        m_nicifiedFieldName = string.Empty;
        m_fieldInfo = null;
        m_propertyInfo = null;
        m_fieldValueType = null;
        m_properties = null;
        m_propNames = null;
        m_matchingTypes = null;
        m_selectedMachingTypeNames = null;
        m_niceTypeName = null;

        if (resetComponent) {
            m_targetComponentType = null;
            m_targetComponentTypeName = string.Empty;
        }
    }

    private void Restore ()
    {
        if (m_targetComponentType == null && !string.IsNullOrEmpty (m_targetComponentTypeName)) {
            m_targetComponentType = Type.GetType (m_targetComponentTypeName);
            SetupPropertySelector ();
        }

        if (m_fieldValueType == null && !string.IsNullOrEmpty (m_fieldName)) {
            if (m_fieldKind == Kind.Field) {
                m_fieldInfo = m_targetComponentType.GetField (m_fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (m_fieldInfo == null) {
                    Invalidate (false);
                    return;
                }
                m_fieldValueType = m_fieldInfo.FieldType;
            } else {
                m_propertyInfo = m_targetComponentType.GetProperty (m_fieldName);
                if (m_propertyInfo == null) {
                    Invalidate (false);
                    return;
                }
                m_fieldValueType = m_propertyInfo.PropertyType;
            }
            m_fieldValueElementType = (m_fieldValueType.IsArray) ? 
                m_fieldValueType.GetElementType () : m_fieldValueType;
        }

        if (string.IsNullOrEmpty (m_nicifiedFieldName)) {
            m_nicifiedFieldName = ObjectNames.NicifyVariableName (m_fieldName);
        }

        if (m_selectedMachingTypeNames == null) {
            m_selectedMachingTypeNames = new List<string> ();
        }

        if (m_selectedMatchingTypes == null) {
            m_selectedMatchingTypes = new List<Type> ();
        }

        if(m_selectedMachingTypeNames.Count != m_selectedMatchingTypes.Count) {
            m_selectedMatchingTypes.Clear ();
            foreach (var aqn in m_selectedMachingTypeNames) {
                var t = Type.GetType (aqn);
                if (t != null) {
                    m_selectedMatchingTypes.Add(t);
                }
            }
        }
    }

    private void SetupPropertySelector() {

        m_properties = new List<FieldEditInfo> ();

        var infos = m_targetComponentType.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        foreach (var info in infos) {
            if (!info.IsPublic) {
                var attr = info.GetCustomAttributes (typeof(SerializeField), true);
                if (attr == null || attr.Length == 0) {
                    continue;
                }
            }

            if (IsTargetType(info.FieldType)) {
                m_properties.Add (new FieldEditInfo(info));
            }
        }

        var props = m_targetComponentType.GetProperties (BindingFlags.Instance);
        foreach (var p in props) {

            if (!p.CanWrite) {
                continue;
            }

            if (IsTargetType(p.PropertyType)) {
                m_properties.Add (new FieldEditInfo (p));
            }
        }

        m_propNames = m_properties.Select (p => p.ToString ()).ToArray ();
    }

    private static bool IsTargetType(Type t) {
        if (t.IsArray) {
            return typeof(UnityEngine.Component).IsAssignableFrom(t.GetElementType());
        } else {
            return typeof(UnityEngine.Component).IsAssignableFrom(t);
        }
    }

    public void OnValidate () {
        Restore ();

        if (m_targetComponentType == null) {
            throw new NodeException ("Component type is not set.", "Select Component type from inspector.");
        }
        if (m_fieldValueType == null) {
            throw new NodeException ("Field variable is not set.", "Select editing variable from inspector.");
        }
    }

    // Test if asset is different from intended configuration
    public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group)
    {
        Restore ();

        return assets.Where (a => a is GameObject).Any ();
    }

    // Actually change asset configurations.
    public void Modify (UnityEngine.Object[] assets, List<AssetReference> group)
    {
        Regex r = new Regex (m_targetNamePattern);
        bool isRootObjTargeting = (m_attachPolicy & AttachPolicy.RootObject) > 0;
        bool isLeafObjTargeting = (m_attachPolicy & AttachPolicy.LeafObject) > 0;
        bool isMiddleObjTargeting = (m_attachPolicy & AttachPolicy.MiddleObject) > 0;

        foreach (var o in assets) {
            GameObject go = o as GameObject;
            if (go == null) {
                continue;
            }

            var c = go.GetComponent (m_targetComponentType);
            if (c == null) {
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

            DoSetReference(go, c);
        }
    }

    private void DoSetReference(GameObject target, Component cmp) {

        List<UnityEngine.Object> foundObjects = new List<UnityEngine.Object> ();
        List<GameObject> objectsToSearch = new List<GameObject> ();

        switch (m_searchDomain) {
        case SearchDomain.FromRoot:
            {
                GameObject o = target;
                while (o.transform.parent != null) {
                    o = o.transform.parent.gameObject;
                }
                objectsToSearch.Add (o);
            }
            break;
        case SearchDomain.FromMyself:
            objectsToSearch.Add (target);
            break;
        case SearchDomain.FromParent:
            if (target.transform.parent != null) {
                objectsToSearch.Add (target.transform.parent.gameObject);
            } else {
                objectsToSearch.Add (target);
            }
            break;
        case SearchDomain.FromSiblings:
            if (target.transform.parent != null) {
                foreach (var sibling in target.transform.parent) {
                    Transform t = sibling as Transform;
                    if (t != target.transform) {
                        objectsToSearch.Add (t.gameObject);
                    }
                }
            } else {
                objectsToSearch.Add (target);
            }
            break;
        case SearchDomain.FromChildrens:
            foreach (var child in target.transform) {
                Transform t = child as Transform;
                objectsToSearch.Add (t.gameObject);
            }
            break;
        }

        var r = new Regex (m_searchingNamePattern);

        foreach (var go in objectsToSearch) {
            CollectRecursive (go, r, foundObjects);
        }

        Array a = null;

        if (m_fieldValueType.IsArray) {
            a = Array.CreateInstance (m_fieldValueElementType, foundObjects.Count);
            for (int i = 0; i < foundObjects.Count; ++i) {
                a.SetValue (foundObjects [i], i);
            }
        }

        if (m_fieldKind == Kind.Field) {
            if (m_fieldValueType.IsArray) {
                m_fieldInfo.SetValue (cmp, a);
            } else {
                m_fieldInfo.SetValue (cmp, (foundObjects.Count == 0) ? null : foundObjects [0]);
            }
        } else {
            if (m_fieldValueType.IsArray) {
                m_propertyInfo.SetValue (cmp, a, null);
            } else {
                m_propertyInfo.SetValue (cmp, (foundObjects.Count == 0) ? null : foundObjects [0], null);
            }
        }
    }

    private void CollectRecursive(GameObject go, Regex namePattern, List<UnityEngine.Object> foundObjects) 
    {
        
        if(namePattern.IsMatch(go.name)) {
            switch (m_typeMatchPolicy) {
            case TypeMatchPolicy.ExactMatch:
                {
                    var c = go.GetComponent (m_fieldValueElementType);
                    if (c != null) {
                        foundObjects.Add (c);
                    }
                }
                break;
            case TypeMatchPolicy.AllAssignableTypes:
                foreach (var t in m_matchingTypes) {
                    var c = go.GetComponent (t);
                    if (c != null) {
                        foundObjects.Add (c);
                    }
                }
                break;
            case TypeMatchPolicy.SpecificTypes:
                foreach (var t in m_selectedMatchingTypes) {
                    var c = go.GetComponent (t);
                    if (c != null) {
                        foundObjects.Add (c);
                    }
                }
                break;
            }
        }

        foreach (var child in go.transform) {
            Transform t = child as Transform;
            CollectRecursive (t.gameObject, namePattern, foundObjects);
        }
    }


    private void DrawPropertySelector (Action onValueChanged) {
        
        if (m_targetComponentType == null) {
            return;
        }

        if (m_fieldValueType != null) {
            using (new EditorGUILayout.HorizontalScope ()) {
                GUILayout.Label (
                    $"Editing Field: {m_nicifiedFieldName} ({ObjectNames.NicifyVariableName(m_fieldValueType.Name)})", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace ();
                if (GUILayout.Button ("Clear", GUILayout.Width (60))) {
                    if (EditorUtility.DisplayDialog ("Reset Property Setting", 
                        string.Format ("Do you want to clear property setting?"), "OK", "Cancel")) 
                    {
                        Invalidate (false);
                        SetupPropertySelector ();
                        onValueChanged ();
                    }
                }
            }
        } else {
            using (new EditorGUILayout.HorizontalScope ()) {
                if (m_propNames != null && m_propNames.Length > 0) {
                    m_fieldSelectedIndex = EditorGUILayout.Popup ("Field", m_fieldSelectedIndex, m_propNames);

                    using (new EditorGUI.DisabledScope (m_fieldSelectedIndex < 0)) {
                        if (GUILayout.Button ("Select", GUILayout.Width (60))) 
                        {
                            var info = m_properties[m_fieldSelectedIndex];
                            m_fieldKind = info.kind;
                            m_fieldName = info.name;
                            m_nicifiedFieldName = info.niceName;

                            if (m_fieldKind == Kind.Field) {
                                m_fieldInfo = m_targetComponentType.GetField (m_fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                                m_fieldValueType = m_fieldInfo.FieldType;
                            } else {
                                m_propertyInfo = m_targetComponentType.GetProperty (m_fieldName);
                                m_fieldValueType = m_propertyInfo.PropertyType;
                            }
                            m_fieldValueElementType = (m_fieldValueType.IsArray) ? m_fieldValueType.GetElementType () : m_fieldValueType;

                            SetupPropertySelector ();

                            onValueChanged ();
                        }
                    }
                } else {
                    EditorGUILayout.LabelField ("Field", "No selectable field");
                }
            }
        }
    }

    private void DrawPropertyConfiguration (Action onValueChanged) {

        if (m_fieldValueType == null) {
            return;
        }

        if (m_matchingTypes == null) {
            m_matchingTypes = new List<Type> ();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var matchingTypes = assembly.GetTypes ()
                    .Where (t => !t.IsInterface)
                    .Where (t => m_fieldValueElementType.IsAssignableFrom (t));

                foreach (var t in matchingTypes) {
                    if (!m_matchingTypes.Contains (t)) {
                        m_matchingTypes.Add (t);
                    }
                }
            }
        }

        var newSearchDomain = (SearchDomain)EditorGUILayout.EnumPopup ("Search Domain", m_searchDomain);
        if (newSearchDomain != m_searchDomain) {
            m_searchDomain = newSearchDomain;
            onValueChanged ();
        }

        var newTypeMatch = (TypeMatchPolicy)EditorGUILayout.EnumPopup ("Type Match", m_typeMatchPolicy);
        if (newTypeMatch != m_typeMatchPolicy) {
            m_typeMatchPolicy = newTypeMatch;
            onValueChanged ();
        }

        var newSearchName = EditorGUILayout.TextField ("Name Pattern", m_searchingNamePattern);
        if (newSearchName != m_searchingNamePattern) {
            m_searchingNamePattern = newSearchName;
            onValueChanged ();
        }

        using (new EditorGUI.DisabledScope (newTypeMatch != TypeMatchPolicy.SpecificTypes)) {
            string label = string.Empty;
            if (m_selectedMachingTypeNames.Count > 1) {
                label = "Mixed...";
            } else if( m_selectedMachingTypeNames.Count == 1 ) {
                if (m_niceTypeName == null) {
                    Type t = Type.GetType (m_selectedMachingTypeNames[0]);
                    m_niceTypeName = ObjectNames.NicifyVariableName (t.Name);
                }
                label = m_niceTypeName;
            }

            using (new GUILayout.HorizontalScope ()) {
                GUILayout.Label ("Maching Types");
                if (EditorGUILayout.DropdownButton (new GUIContent(label), FocusType.Passive)) {

                    var menu = new GenericMenu ();

                    foreach (var t in m_matchingTypes) {
                        string name = t.Name;
                        string aqn = t.AssemblyQualifiedName;
                        var isChecked = m_selectedMachingTypeNames.Contains(aqn);
                        menu.AddItem (new GUIContent(ObjectNames.NicifyVariableName(name)), 
                            isChecked,
                            () => {
                                m_niceTypeName = null;
                                if(m_selectedMachingTypeNames.Contains(aqn)) {
                                    m_selectedMachingTypeNames.Remove(aqn);
                                } else {
                                    m_selectedMachingTypeNames.Add(aqn);
                                }
                                if(m_selectedMatchingTypes != null) {
                                    m_selectedMatchingTypes.Clear();
                                }
                            });
                    }

                    menu.ShowAsContext ();
                }
            }
        }
    }

    private void DrawComponentSelector (Action onValueChanged)
    {
        if (m_targetComponentType != null) {
            using (new EditorGUILayout.HorizontalScope ()) {
                GUILayout.Label ($"Component: {m_targetComponentType.Name}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace ();
                if (GUILayout.Button ("Clear", GUILayout.Width (60))) {
                    if (EditorUtility.DisplayDialog ("Clear Properties Setting", 
                            string.Format ("Do you want to reset component to set properties for?"), "OK", "Cancel")) 
                    {
                        Invalidate (true);
                        onValueChanged ();
                    }
                }
            }
        } else {
            using (new EditorGUILayout.HorizontalScope ()) {
                m_componentSelectedIndex = EditorGUILayout.Popup ("Component", m_componentSelectedIndex, ComponentMenuUtility.GetScriptComponentNamesWithObjectReferenceProperty ());

                using (new EditorGUI.DisabledScope (m_componentSelectedIndex < 0)) {
                    if (GUILayout.Button ("Select", GUILayout.Width (60))) 
                    {
                        m_targetComponentType = ComponentMenuUtility.GetScriptComponentTypesWithObjectReferenceProperty () [m_componentSelectedIndex];
                        m_targetComponentTypeName = m_targetComponentType.AssemblyQualifiedName;
                        Invalidate (false);

                        SetupPropertySelector ();

                        onValueChanged ();
                    }
                }
            }
        }
    }

    // Draw inspector gui
    public void OnInspectorGUI (Action onValueChanged)
    {

        Restore ();

        var newAttachPolicy = (AttachPolicy)EditorGUILayout.EnumFlagsField ("Edit Policy", m_attachPolicy);
        if (newAttachPolicy != m_attachPolicy) {
            m_attachPolicy = newAttachPolicy;
            onValueChanged ();
        }

        var newNameFormat = EditorGUILayout.TextField ("Name Pattern", m_targetNamePattern);
        if (newNameFormat != m_targetNamePattern) {
            m_targetNamePattern = newNameFormat;
            onValueChanged ();
        }

        GUILayout.Space (8f);

        DrawComponentSelector (onValueChanged);

        GUILayout.Space (4f);

        DrawPropertySelector (onValueChanged);

        GUILayout.Space (4f);

        EditorGUI.BeginChangeCheck ();

        DrawPropertyConfiguration (onValueChanged);

        if (EditorGUI.EndChangeCheck ()) {
            onValueChanged ();
        }
    }
}
