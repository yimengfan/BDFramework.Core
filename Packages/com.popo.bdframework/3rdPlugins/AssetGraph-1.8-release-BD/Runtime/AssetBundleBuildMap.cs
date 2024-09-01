using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace UnityEngine.AssetGraph
{
    /// <summary>
    /// AssetBundleBuildMap is look-up data to find asset vs assetbundle relationship. 
    /// </summary>
    public class AssetBundleBuildMap : ScriptableObject
    {
        [SerializeField] private List<AssetBundleEntry> m_assetBundles = null;
        
#if UNITY_EDITOR
        [SerializeField] private int m_version;
        private const int VERSION = 1;
        private static AssetBundleBuildMap s_map;

        public static class UserSettings
        {
            private const string PREFKEY_AB_BUILDMAP_PATH = "UnityEngine.AssetGraph.BuildMapPath";

            public static string AssetBundleBuildMapPath
            {
                get
                {
                    var path = EditorUserSettings.GetConfigValue(PREFKEY_AB_BUILDMAP_PATH);
                    if (string.IsNullOrEmpty(path))
                    {
                        return Path.Combine("Assets/AssetGraph/Cache/", "AssetBundleBuildMap.asset");
                    }

                    return path;
                }

                set { EditorUserSettings.SetConfigValue(PREFKEY_AB_BUILDMAP_PATH, value); }
            }
        }

        public static AssetBundleBuildMap GetBuildMap()
        {
            if (s_map == null)
            {
                if (!Load())
                {
                    // Create vanilla db
                    s_map = ScriptableObject.CreateInstance<AssetBundleBuildMap>();
                    s_map.m_assetBundles = new List<AssetBundleEntry>();
                    s_map.m_version = VERSION;

                    var filePath = UserSettings.AssetBundleBuildMapPath;
                    var dirPath = Path.GetDirectoryName(filePath);

                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }

                    AssetDatabase.CreateAsset(s_map, filePath);
                }
            }

            return s_map;
        }

        private static bool Load()
        {
            bool loaded = false;

            try
            {
                var filePath = UserSettings.AssetBundleBuildMapPath;

                if (File.Exists(filePath))
                {
                    AssetBundleBuildMap m = AssetDatabase.LoadAssetAtPath<AssetBundleBuildMap>(filePath);

                    if (m != null && m.m_version == VERSION)
                    {
                        s_map = m;
                        loaded = true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return loaded;
        }

        public static void SetMapDirty()
        {
            EditorUtility.SetDirty(s_map);
        }

        internal static string MakeFullName(string assetBundleName, string variantName)
        {
            if (string.IsNullOrEmpty(assetBundleName))
            {
                return "";
            }

            if (string.IsNullOrEmpty(variantName))
            {
                return assetBundleName.ToLower();
            }

            return $"{assetBundleName.ToLower()}.{variantName.ToLower()}";
        }

        internal static string[] FullNameToNameAndVariant(string assetBundleFullName)
        {
            assetBundleFullName = assetBundleFullName.ToLower();
            var vIndex = assetBundleFullName.LastIndexOf('.');
            if (vIndex > 0 && vIndex + 1 < assetBundleFullName.Length)
            {
                var bundleName = assetBundleFullName.Substring(0, vIndex);
                var bundleVariant = assetBundleFullName.Substring(vIndex + 1);
                return new string[] {bundleName, bundleVariant};
            }

            return new string[] {assetBundleFullName, ""};
        }
        
        public void Clear()
        {
            m_assetBundles.Clear();
            SetMapDirty();
        }

        public void ClearFromId(string id)
        {
            m_assetBundles.RemoveAll(e => e.m_registererId == id);
        }  
#endif

        [Serializable]
        public class AssetBundleEntry
        {
            [Serializable]
            internal struct AssetPathString
            {
                [SerializeField] public string original;
                [SerializeField] public string lower;

                internal AssetPathString(string s)
                {
                    original = s;
                    if (!string.IsNullOrEmpty(s))
                    {
                        lower = s.ToLower();
                    }
                    else
                    {
                        lower = s;
                    }
                }
            }

            [SerializeField] internal string m_assetBundleName = string.Empty;
            [SerializeField] internal string m_assetBundleVariantName = string.Empty;
            [SerializeField] internal string m_fullName = string.Empty;
            [SerializeField] internal List<AssetPathString> m_assets = null;
            [SerializeField] public string m_registererId = string.Empty;

            public string Name
            {
                get { return m_assetBundleName; }
            }

            public string Variant
            {
                get { return m_assetBundleVariantName; }
            }

            public string FullName
            {
                get { return m_fullName; }
            }

#if UNITY_EDITOR
            public AssetBundleEntry(string registererId, string assetBundleName, string variantName)
            {
                m_registererId = registererId;
                m_assetBundleName = assetBundleName.ToLower();
                m_assetBundleVariantName = variantName.ToLower();
                m_fullName = AssetBundleBuildMap.MakeFullName(assetBundleName, variantName);
                m_assets = new List<AssetPathString>();
            }

            public void Clear()
            {
                m_assets.Clear();
                AssetBundleBuildMap.SetMapDirty();
            }


            public void AddAssets(string id, IEnumerable<string> assets)
            {
                foreach (var a in assets)
                {
                    m_assets.Add(new AssetPathString(a));
                }

                AssetBundleBuildMap.SetMapDirty();
            }
#endif
            
            public IEnumerable<string> GetAssetFromAssetName(string assetName)
            {
                assetName = assetName.ToLower();
                return m_assets.Where(a => Path.GetFileNameWithoutExtension(a.lower) == assetName)
                    .Select(s => s.original);
            }
        }

#if UNITY_EDITOR
        public AssetBundleEntry GetAssetBundle(string registererId, string assetBundleFullName)
        {
            var entry = m_assetBundles.Find(v => v.m_fullName == assetBundleFullName);
            if (entry == null)
            {
                string[] names = AssetBundleBuildMap.FullNameToNameAndVariant(assetBundleFullName);
                entry = new AssetBundleEntry(registererId, names[0], names[1]);
                m_assetBundles.Add(entry);
                SetMapDirty();
            }

            return entry;
        }

        public AssetBundleEntry GetAssetBundleWithNameAndVariant(string registererId, string assetBundleName,
            string variantName)
        {
            return GetAssetBundle(registererId, AssetBundleBuildMap.MakeFullName(assetBundleName, variantName));
        }
#endif

        public string[] GetAssetPathsFromAssetBundleAndAssetName(string assetbundleName, string assetName)
        {
            assetName = assetName.ToLower();
            return m_assetBundles.Where(ab => ab.m_fullName == assetbundleName)
                .SelectMany(ab => ab.GetAssetFromAssetName(assetName))
                .ToArray();
        }

        public string[] GetAssetPathsFromAssetBundle(string assetBundleName)
        {
            assetBundleName = assetBundleName.ToLower();
            return m_assetBundles.Where(e => e.m_fullName == assetBundleName).SelectMany(e => e.m_assets)
                .Select(s => s.original).ToArray();
        }

        public string GetAssetBundleName(string assetPath)
        {
            var entry = m_assetBundles.Find(e => e.m_assets.Contains(new AssetBundleEntry.AssetPathString(assetPath)));
            if (entry != null)
            {
                return entry.m_fullName;
            }

            return string.Empty;
        }

        public string GetImplicitAssetBundleName(string assetPath)
        {
            return GetAssetBundleName(assetPath);
        }

        public string[] GetAllAssetBundleNames()
        {
            return m_assetBundles.Select(e => e.m_fullName).ToArray();
        }
    }
}