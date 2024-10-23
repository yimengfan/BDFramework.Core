using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.AssetGraph;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

[System.Serializable]
[CustomAssetGenerator("Create Material From Texture", "v1.0")]
public class CreateMaterialFromTexture : IAssetGenerator
{
    public enum PropertyType
    {
        Texture,
        Int,
        Float,
        TextureScale,
        TextureOffset,
        Vector2,
        Vector3,
        Vector4,
        Color
    }

    [System.Serializable]
    public class PropertyField
    {
        [SerializeField] public string propertyName;
        [SerializeField] public TextureReference texture;
        [SerializeField] public Vector4 vectorValue;
        [SerializeField] public Color colorValue;
        [SerializeField] public int intValue;
        [SerializeField] public PropertyType propertyType;

        public PropertyField(PropertyType type)
        {
            texture = new TextureReference();
            vectorValue = Vector4.zero;
            colorValue = Color.black;
            propertyType = type;
            intValue = 0;
        }

        public float FloatValue
        {
            get { return vectorValue.x; }
            set { vectorValue = new Vector4(value, vectorValue.y, vectorValue.z); }
        }
    }

    [SerializeField] public MaterialReference m_referenceMat;
    [SerializeField] public string m_propertyName;
    [SerializeField] public List<PropertyField> m_properties;

    public void OnValidate()
    {
        if (m_referenceMat == null || m_referenceMat.Empty)
        {
            throw new NodeException("Reference Material is empty", "Set Reference Material from inspector.");
        }

        if (string.IsNullOrEmpty(m_propertyName))
        {
            throw new NodeException("Property name is empty", "Set property name for incoming texture from inspector.");
        }
    }

    public string GetAssetExtension(AssetReference asset)
    {
        return ".mat";
    }

    public Type GetAssetType(AssetReference asset)
    {
        return typeof(Material);
    }

    public bool CanGenerateAsset(AssetReference asset)
    {
        if (asset.importerType != typeof(TextureImporter))
        {
            throw new NodeException("CreateMaterialFromTexture needs texture for source asset.",
                $"Remove {asset.fileNameAndExtension} from input.");
        }

        return true;
    }

    /**
     * Generate asset.
     */
    public bool GenerateAsset(AssetReference asset, string generateAssetPath)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(asset.importFrom);
        if (tex == null)
        {
            return false;
        }

        var newMat = new Material(m_referenceMat.Object);

        newMat.SetTexture(m_propertyName, tex);

        if (m_properties != null)
        {
            foreach (var p in m_properties)
            {
                switch (p.propertyType)
                {
                    case PropertyType.Texture:
                    {
                        newMat.SetTexture(p.propertyName, p.texture.Object);
                    }
                        break;
                    case PropertyType.Int:
                    {
                        newMat.SetInt(p.propertyName, p.intValue);
                    }
                        break;
                    case PropertyType.TextureOffset:
                    {
                        newMat.SetTextureOffset(p.propertyName, new Vector2(p.vectorValue.x, p.vectorValue.y));
                    }
                        break;
                    case PropertyType.TextureScale:
                    {
                        newMat.SetTextureScale(p.propertyName, new Vector2(p.vectorValue.x, p.vectorValue.y));
                    }
                        break;
                    case PropertyType.Float:
                    {
                        newMat.SetFloat(p.propertyName, p.FloatValue);
                    }
                        break;
                    case PropertyType.Vector2:
                    case PropertyType.Vector3:
                    case PropertyType.Vector4:
                    {
                        newMat.SetVector(p.propertyName, p.vectorValue);
                    }
                        break;
                    case PropertyType.Color:
                    {
                        newMat.SetColor(p.propertyName, p.colorValue);
                    }
                        break;
                }
            }
        }

        AssetDatabase.CreateAsset(newMat, generateAssetPath);

        return true;
    }

    /**
     * Draw Inspector GUI for this AssetGenerator.
     */
    public void OnInspectorGUI(Action onValueChanged)
    {
        if (m_referenceMat == null)
        {
            m_referenceMat = new MaterialReference();
            onValueChanged();
        }

        if (m_properties == null)
        {
            m_properties = new List<PropertyField>();
            onValueChanged();
        }

        var refMat =
            (Material) EditorGUILayout.ObjectField("Reference Material", m_referenceMat.Object, typeof(Material),
                false);
        if (refMat != m_referenceMat.Object)
        {
            m_referenceMat.Object = refMat;
            onValueChanged();
        }

        var newFieldName = EditorGUILayout.TextField("Property Name", m_propertyName);
        if (newFieldName != m_propertyName)
        {
            m_propertyName = newFieldName;
            onValueChanged();
        }

        GUILayout.Space(8f);

        PropertyField removing = null;

        foreach (var p in m_properties)
        {
            DrawPropertyGUI(p, onValueChanged, ref removing);
            GUILayout.Space(8f);
        }

        if (removing != null)
        {
            m_properties.Remove(removing);
            onValueChanged();
        }


        GUILayout.Space(8f);


        using (new GUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(20f)))
            {
                var menu = new GenericMenu();

                foreach (var v in (PropertyType[]) Enum.GetValues(typeof(PropertyType)))
                {
                    var propertyType = v;
                    menu.AddItem(new GUIContent(v.ToString()), false, () =>
                    {
                        m_properties.Add(new PropertyField(propertyType));
                        onValueChanged();
                    });
                }

                menu.ShowAsContext();
            }
        }
    }

    private void DrawPropertyGUI(PropertyField field, Action onValueChanged, ref PropertyField removing)
    {
        using (new GUILayout.HorizontalScope())
        {
            var n = EditorGUILayout.TextField("Property Name", field.propertyName);
            if (n != field.propertyName)
            {
                field.propertyName = n;
                onValueChanged();
            }

            if (GUILayout.Button("-", GUILayout.Width(20f)))
            {
                removing = field;
            }
        }

        switch (field.propertyType)
        {
            case PropertyType.Texture:
            {
                var t = (Texture) EditorGUILayout.ObjectField("Texture", field.texture.Object, typeof(Texture2D),
                    false);
                if (t != field.texture.Object)
                {
                    field.texture.Object = t;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.TextureOffset:
            {
                var v = EditorGUILayout.Vector2Field("Texture Offset", field.vectorValue);
                if (v != new Vector2(field.vectorValue.x, field.vectorValue.y))
                {
                    field.vectorValue = v;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.TextureScale:
            {
                var v = EditorGUILayout.Vector2Field("Texture Scale", field.vectorValue);
                if (v != new Vector2(field.vectorValue.x, field.vectorValue.y))
                {
                    field.vectorValue = v;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.Int:
            {
                var v = EditorGUILayout.IntField("Int", field.intValue);
                if (v != field.intValue)
                {
                    field.intValue = v;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.Float:
            {
                var v = EditorGUILayout.FloatField("Float", field.FloatValue);
                if (v != field.FloatValue)
                {
                    field.FloatValue = v;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.Vector2:
            {
                var v = EditorGUILayout.Vector2Field("Vector 2", field.vectorValue);
                if (v != new Vector2(field.vectorValue.x, field.vectorValue.y))
                {
                    field.vectorValue = v;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.Vector3:
            {
                var v = EditorGUILayout.Vector3Field("Vector 3", field.vectorValue);
                if (v != new Vector3(field.vectorValue.x, field.vectorValue.y, field.vectorValue.z))
                {
                    field.vectorValue = v;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.Vector4:
            {
                var v = EditorGUILayout.Vector4Field("Vector 4", field.vectorValue);
                if (v != field.vectorValue)
                {
                    field.vectorValue = v;
                    onValueChanged();
                }
            }
                break;
            case PropertyType.Color:
            {
                var v = EditorGUILayout.ColorField(new GUIContent("Color"), field.colorValue, true, true, true);
                if (v != field.colorValue)
                {
                    field.colorValue = v;
                    onValueChanged();
                }
            }
                break;
        }
    }
}