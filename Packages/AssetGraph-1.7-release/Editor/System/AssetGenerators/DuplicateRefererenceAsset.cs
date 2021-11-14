using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[System.Serializable]
[CustomAssetGenerator("Duplicate Reference Asset", "v1.0", 1)]
public class DuplicateReferenceAsset : IAssetGenerator {

    [SerializeField] private ObjectReference m_asset;

    public void OnValidate () {
        if(m_asset == null ||  m_asset.Object == null) {
            throw new NodeException ("Reference Asset not set", "Configure reference asset from inspector");
        }
    }

    public string GetAssetExtension (AssetReference asset) {

        var ext = Path.GetExtension(AssetDatabase.GetAssetPath (m_asset.Object));
        if (string.IsNullOrEmpty (ext)) {
            return null;
        }

        return ext;
    }

    public Type GetAssetType(AssetReference asset) {
        if (m_asset == null || m_asset.Object == null) {
            return null;
        }

        return FilterTypeUtility.FindAssetFilterType (AssetDatabase.GetAssetPath (m_asset.Object));
    }

    public bool CanGenerateAsset (AssetReference asset) {
        return true;
    }

    public bool GenerateAsset (AssetReference asset, string generateAssetPath) {

        bool generated = false;

        try {
            string referenceAssetPath = AssetDatabase.GetAssetPath (m_asset.Object);
            string srcFullPath = FileUtility.PathCombine (Directory.GetParent(Application.dataPath).ToString(), referenceAssetPath);
            string dstFullPath = FileUtility.PathCombine (Directory.GetParent(Application.dataPath).ToString(), generateAssetPath);

            File.Copy(srcFullPath, dstFullPath, true);
            generated = true;
        } catch(Exception e) {
            LogUtility.Logger.LogError ("DuplicateReferenceAsset", e.Message);
        }

        return generated;
    }

    public void OnInspectorGUI (Action onValueChanged) {

        if (m_asset == null) {
            m_asset = new ObjectReference ();
        }

        var refObj = EditorGUILayout.ObjectField ("Reference Asset", m_asset.Object, typeof(UnityEngine.Object), false);
        if (refObj != m_asset.Object) {
            m_asset.Object = refObj;
            onValueChanged ();
        }
    }
}
