using UnityEngine;
using UnityEditor;

using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;

[CustomModifier ("Set Field Variables", typeof(GameObject))]
public class SetProperty : IModifier
{

    enum AttachPolicy
    {
        RootObject = 1,
        MiddleObject = 2,
        LeafObject = 4
    }

    [Serializable]
    public class FieldEditInfo
    {

        public enum Kind
        {
            Field,
            Property
        }

        [SerializeField] private Kind m_kind;
        [SerializeField] private string m_name;
        [SerializeField] private string m_nicifiedName;
        [SerializeField] private bool m_enabled;

        // stored values
        [SerializeField] private int m_intValue;
        [SerializeField] private float m_floatValue;
        [SerializeField] private long m_longValue;
        [SerializeField] private bool m_boolValue;
        [SerializeField] private string m_stringValue = string.Empty;
        [SerializeField] private Vector2 m_v2value;
        [SerializeField] private Vector3 m_v3value;
        [SerializeField] private Vector4 m_v4value;
        [SerializeField] private AnimationCurve m_curveValue;
        [SerializeField] private Color m_colorValue;

        private Type m_valueType;
        private System.Reflection.FieldInfo m_fieldInfo;
        private System.Reflection.PropertyInfo m_propertyInfo;
        private UnityEngine.Object m_objectValue;

        public string Name {
            get {
                return m_name;
            }
        }

        public FieldEditInfo (System.Reflection.FieldInfo info)
        {
            m_name = info.Name;
            m_kind = Kind.Field;
            m_nicifiedName = ObjectNames.NicifyVariableName (m_name);
            m_fieldInfo = info;
            m_valueType = info.FieldType;
            m_curveValue = new AnimationCurve ();
        }

        public FieldEditInfo (System.Reflection.PropertyInfo info)
        {
            m_name = info.Name;
            m_kind = Kind.Property;
            m_nicifiedName = ObjectNames.NicifyVariableName (m_name);
            m_propertyInfo = info;
            m_valueType = info.PropertyType;
            m_curveValue = new AnimationCurve ();
        }

        public FieldEditInfo (FieldEditInfo info)
        {
            m_name = info.m_name;
            m_nicifiedName = info.m_nicifiedName;
            m_fieldInfo = info.m_fieldInfo;
            m_propertyInfo = info.m_propertyInfo;
            m_kind = info.m_kind;
            m_objectValue = info.m_objectValue;
            m_valueType = info.m_valueType;

            m_intValue = info.m_intValue;
            m_floatValue = info.m_floatValue;
            m_longValue = info.m_longValue;
            m_boolValue = info.m_boolValue;
            m_stringValue = info.m_stringValue;
            m_v2value = info.m_v2value;
            m_v3value = info.m_v3value;
            m_v4value = info.m_v4value;
            m_curveValue = info.m_curveValue;
            m_colorValue = info.m_colorValue;
        }

        public bool Restore (Type baseType)
        {
            if (m_kind == Kind.Field) {
                if (m_fieldInfo == null) {
                    m_fieldInfo = baseType.GetField (m_name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (m_fieldInfo == null) {
                        return false;
                    }
                    m_valueType = m_fieldInfo.FieldType;
                }
            } else {
                if (m_propertyInfo == null) {
                    m_propertyInfo = baseType.GetProperty (m_name);
                    if (m_propertyInfo == null) {
                        return false;
                    }
                    m_valueType = m_propertyInfo.PropertyType;
                }
            }

            if (m_objectValue == null &&
                typeof(UnityEngine.Object).IsAssignableFrom(m_valueType)) 
            {
                if (!string.IsNullOrEmpty (m_stringValue)) {
                    m_objectValue = AssetDatabase.LoadMainAssetAtPath (AssetDatabase.GUIDToAssetPath (m_stringValue));
                }
            }

            if (m_curveValue == null) {
                m_curveValue = new AnimationCurve ();
            }

            return true;
        }

        public void Invalidate ()
        {
            m_fieldInfo = null;
            m_propertyInfo = null;
            m_valueType = null;
            m_objectValue = null;
        }

        public void OnInspectorGUI (Type baseType, Action onValueChanged)
        {

            Restore (baseType);

            EditorGUI.BeginChangeCheck ();

            using (new EditorGUILayout.HorizontalScope ()) {
                m_enabled = EditorGUILayout.Toggle (m_enabled, GUILayout.Width (15f));

                using (new EditorGUI.DisabledScope (!m_enabled)) {
                    if (m_valueType == typeof(int)) {
                        m_intValue = EditorGUILayout.IntField (m_nicifiedName, m_intValue);
                    } else if (m_valueType == typeof(float)) {
                        m_floatValue = EditorGUILayout.FloatField (m_nicifiedName, m_floatValue);
                    } else if (m_valueType == typeof(long)) {
                        m_longValue = EditorGUILayout.LongField (m_nicifiedName, m_longValue);
                    } else if (m_valueType == typeof(bool)) {
                        m_boolValue = EditorGUILayout.Toggle (m_nicifiedName, m_boolValue);
                    } else if (m_valueType == typeof(string)) {
                        m_stringValue = EditorGUILayout.TextField (m_nicifiedName, m_stringValue);
                    } else if (m_valueType == typeof(Vector2)) {
                        m_v2value = EditorGUILayout.Vector2Field (m_nicifiedName, m_v2value);
                    } else if (m_valueType == typeof(Vector3)) {
                        m_v3value = EditorGUILayout.Vector3Field (m_nicifiedName, m_v3value);
                    } else if (m_valueType == typeof(Vector4)) {
                        m_v4value = EditorGUILayout.Vector4Field (m_nicifiedName, m_v4value);
                    } else if (m_valueType == typeof(AnimationCurve)) {
                        m_curveValue = EditorGUILayout.CurveField (m_nicifiedName, m_curveValue);
                    } else if (m_valueType == typeof(Color)) {
                        m_colorValue = EditorGUILayout.ColorField (m_nicifiedName, m_colorValue);
                    } else if (m_valueType.IsEnum) {
                        var v = EditorGUILayout.EnumPopup (m_nicifiedName, (Enum)Enum.ToObject (m_valueType, m_intValue));
                        m_intValue = Convert.ToInt32 (v);
                    } else if (typeof(UnityEngine.Object).IsAssignableFrom(m_valueType)) {
                        m_objectValue = EditorGUILayout.ObjectField (m_nicifiedName, m_objectValue, m_valueType, false);
                    }
                }

                if (EditorGUI.EndChangeCheck ()) {
                    if (typeof(UnityEngine.Object).IsAssignableFrom(m_valueType)) {
                        m_stringValue = AssetDatabase.AssetPathToGUID (AssetDatabase.GetAssetPath (m_objectValue));
                    }
                    onValueChanged ();
                }
            }
        }

        public void Apply (UnityEngine.Object o)
        {
            if (!m_enabled) {
                return;
            }

            if (m_valueType == typeof(int)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_intValue);
                else
                    m_propertyInfo.SetValue (o, m_intValue, null);
            } else if (m_valueType == typeof(float)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_floatValue);
                else
                    m_propertyInfo.SetValue (o, m_floatValue, null);
            } else if (m_valueType == typeof(long)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_longValue);
                else
                    m_propertyInfo.SetValue (o, m_longValue, null);
            } else if (m_valueType == typeof(string)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_stringValue);
                else
                    m_propertyInfo.SetValue (o, m_stringValue, null);
            } else if (m_valueType == typeof(Vector2)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_v2value);
                else
                    m_propertyInfo.SetValue (o, m_v2value, null);
            } else if (m_valueType == typeof(Vector3)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_v3value);
                else
                    m_propertyInfo.SetValue (o, m_v3value, null);
            } else if (m_valueType == typeof(Vector4)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_v4value);
                else
                    m_propertyInfo.SetValue (o, m_v4value, null);
            } else if (m_valueType == typeof(AnimationCurve)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_curveValue);
                else
                    m_propertyInfo.SetValue (o, m_curveValue, null);
            } else if (m_valueType == typeof(Color)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_colorValue);
                else
                    m_propertyInfo.SetValue (o, m_colorValue, null);
            } else if (m_valueType.IsEnum) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, Enum.ToObject (m_valueType, m_intValue));
                else
                    m_propertyInfo.SetValue (o, Enum.ToObject (m_valueType, m_intValue), null);
            } else if (typeof(UnityEngine.Object).IsAssignableFrom(m_valueType)) {
                if (m_kind == Kind.Field)
                    m_fieldInfo.SetValue (o, m_objectValue);
                else
                    m_propertyInfo.SetValue (o, m_objectValue, null);
            }
        }
    }

    [SerializeField] private List<FieldEditInfo> m_fields;
    [SerializeField] private List<FieldEditInfo> m_properties;
    [SerializeField] private AttachPolicy m_attachPolicy = AttachPolicy.RootObject | AttachPolicy.MiddleObject | AttachPolicy.LeafObject;
    [SerializeField] private string m_nameFormat;
    [SerializeField] private string m_componentType;

    private Type m_type;
    private int m_selectedIndex = -1;
    private List<FieldEditInfo> m_removingItems;

    private void Restore ()
    {
        if (m_type == null && !string.IsNullOrEmpty (m_componentType)) {
            m_type = Type.GetType (m_componentType);
            if (m_type != null) {
                UpdateFieldInfo ();
            } else {
                m_type = null;
                m_componentType = string.Empty;
                m_fields.Clear ();
                m_properties.Clear ();
            }
        }

        if (m_fields == null) {
            m_fields = new List<FieldEditInfo> ();
        }

        if (m_properties == null) {
            m_properties = new List<FieldEditInfo> ();
        }

        if (m_removingItems == null) {
            m_removingItems = new List<FieldEditInfo> ();
        }

        foreach (var info in m_fields) {
            if (!info.Restore (m_type)) {
                m_removingItems.Add (info);
            }
        }

        if (m_removingItems.Count > 0) {
            foreach (var r in m_removingItems) {
                m_fields.Remove (r);
            }
            m_removingItems.Clear ();
        }

        foreach (var info in m_properties) {
            if(!info.Restore (m_type)) {
                m_removingItems.Add (info);
            }
        }

        if (m_removingItems.Count > 0) {
            foreach (var r in m_removingItems) {
                m_properties.Remove (r);
            }
            m_removingItems.Clear ();
        }
    }

    private void UpdateFieldInfo ()
    {
        if (m_fields == null) {
            m_fields = new List<FieldEditInfo> ();
        }

        if (m_properties == null) {
            m_properties = new List<FieldEditInfo> ();
        }

        var infos = m_type.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        foreach (var info in infos) {
            if (!info.IsPublic) {
                var attr = info.GetCustomAttributes (typeof(SerializeField), true);
                if (attr == null || attr.Length == 0) {
                    continue;
                }
            }

            if (m_fields.Where (f => f.Name == info.Name).Any ()) {
                continue;
            }

            if (IsTargetType(info.FieldType)) {
                m_fields.Add (new FieldEditInfo (info));
            }
        }

        var props = m_type.GetProperties (BindingFlags.Instance);
        foreach (var p in props) {

            if (!p.CanWrite) {
                continue;
            }

            if (m_properties.Where (f => f.Name == p.Name).Any ()) {
                continue;
            }

            if (IsTargetType(p.PropertyType)) {
                m_properties.Add (new FieldEditInfo (p));
            }
        }

        m_fields.Sort ((a, b) => string.Compare (a.Name, b.Name));
        m_properties.Sort ((a, b) => string.Compare (a.Name, b.Name));
    }

    private static bool IsTargetType(Type t) {
        return t.IsEnum ||
        t == typeof(int) ||
        t == typeof(float) ||
        t == typeof(long) ||
        t == typeof(bool) ||
        t == typeof(string) ||
        t == typeof(Vector2) ||
        t == typeof(Vector3) ||
        t == typeof(Vector4) ||
        t == typeof(AnimationCurve) ||
        t == typeof(Color) ||
        typeof(UnityEngine.Object).IsAssignableFrom (t);
    }

    // Validate this Modifier.
    public void OnValidate () {
        Restore ();
        if (m_type == null) {
            throw new NodeException ("Component type is not set.", "Select Component type from inspector.");
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
        Regex r = new Regex (m_nameFormat);
        bool isRootObjTargeting = (m_attachPolicy & AttachPolicy.RootObject) > 0;
        bool isLeafObjTargeting = (m_attachPolicy & AttachPolicy.LeafObject) > 0;
        bool isMiddleObjTargeting = (m_attachPolicy & AttachPolicy.MiddleObject) > 0;

        foreach (var o in assets) {
            GameObject go = o as GameObject;
            if (go == null) {
                continue;
            }

            var c = go.GetComponent (m_type);
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

            foreach (var info in m_fields) {
                info.Apply (c);
            }

            foreach (var info in m_properties) {
                info.Apply (c);
            }
        }
    }

    private void DrawComponentSelector (Action onValueChanged)
    {
        if (m_type != null) {
            using (new EditorGUILayout.HorizontalScope ()) {
                GUILayout.Label ($"{m_type.Name}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace ();
                if (GUILayout.Button ("Clear", GUILayout.Width (60))) {
                    if (EditorUtility.DisplayDialog ("Clear Properties Setting", 
                            string.Format ("Do you want to reset component to set properties for?"), "OK", "Cancel")) {
                        m_type = null;
                        m_componentType = string.Empty;
                        m_fields.Clear ();
                        m_properties.Clear ();
                        onValueChanged ();
                    }
                }
            }
        } else {
            using (new EditorGUILayout.HorizontalScope ()) {
                m_selectedIndex = EditorGUILayout.Popup ("Component", m_selectedIndex, ComponentMenuUtility.GetComponentNames ());

                using (new EditorGUI.DisabledScope (m_selectedIndex < 0)) {
                    if (GUILayout.Button ("Select", GUILayout.Width (60))) {
                        m_type = ComponentMenuUtility.GetComponentTypes () [m_selectedIndex];
                        m_componentType = m_type.AssemblyQualifiedName;
                        m_fields.Clear ();
                        m_properties.Clear ();
                        UpdateFieldInfo ();

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

        var newNameFormat = EditorGUILayout.TextField ("Name Pattern", m_nameFormat);
        if (newNameFormat != m_nameFormat) {
            m_nameFormat = newNameFormat;
            onValueChanged ();
        }

        GUILayout.Space (8f);

        DrawComponentSelector (onValueChanged);

        GUILayout.Space (4f);

        EditorGUI.BeginChangeCheck ();

        if (m_fields.Count > 0) {
            GUILayout.Label ("Fields:", EditorStyles.boldLabel);
            foreach (var info in m_fields) {
                info.OnInspectorGUI (m_type, onValueChanged);
            }
        }

        if (m_properties.Count > 0) {
            GUILayout.Space (4f);
            GUILayout.Label ("Properties:", EditorStyles.boldLabel);
            foreach (var info in m_properties) {
                info.OnInspectorGUI (m_type, onValueChanged);
            }
        }

        if (EditorGUI.EndChangeCheck ()) {
            onValueChanged ();
        }
    }
}
