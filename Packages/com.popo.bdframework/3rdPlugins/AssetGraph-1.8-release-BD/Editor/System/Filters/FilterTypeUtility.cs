using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public static class FilterTypeUtility {
        
        private static readonly List<string> IgnoredAssetTypeExtension = new List<string>{
			string.Empty,
			".manifest",
			".assetbundle",
			".sample",
			".unitypackage",
			".cs",
			".sh",
            ".js",
            ".boo",
			".zip",
			".tar",
			".tgz"
		};

        private static readonly Dictionary<string, Type> s_defaultAssetFilterGUITypeMap = new Dictionary<string, Type> {

            {"Text or Binary", typeof(TextAsset)},
            {"Scene", typeof(UnityEditor.SceneAsset)},
            {"Animation", typeof(AnimationClip)},
            {"Animator Controller", typeof(AnimatorController)},
            {"Animator Override Controller", typeof(AnimatorOverrideController)},
            {"Avatar Mask", typeof(AvatarMask)},
            {"Custom Font", typeof(Font)},
            {"Physic Material", typeof(PhysicMaterial)},
            {"Physic Material 2D", typeof(PhysicsMaterial2D)},
            {"Shader", typeof(Shader)},
            {"Material", typeof(Material)},
            {"Render Texture", typeof(RenderTexture)},
            {"Custom Render Texture", typeof(CustomRenderTexture)},
            {"Lightmap Parameter", typeof(LightmapParameters)},
            {"Lens Flare", typeof(Flare)},
            {"Sprite Atlas", typeof(UnityEngine.U2D.SpriteAtlas)},
            {"Tilemap", typeof(UnityEngine.Tilemaps.Tile)},
            {"GUI Skin", typeof(GUISkin)},
            {"Legacy/Cubemap", typeof(Cubemap)},
        };

        private const string kPREFAB_KEY_NAME = "Prefab";
        private const string kPREFAB_IMPORTER_CLASS = "UnityEditor.PrefabImporter";

        private static List<string> s_filterKeyTypeList;
        private static Type s_prefabImporterClass;

        public static List<string> GetFilterGUINames() {
            if (s_filterKeyTypeList == null) {
                var typemap = ImporterConfiguratorUtility.GetImporterConfiguratorGuiNameTypeMap ();

                var keyList = new List<string> ();

                keyList.Add (Model.Settings.DEFAULT_FILTER_KEYTYPE);
                keyList.Add(kPREFAB_KEY_NAME);
                keyList.AddRange (typemap.Keys);
                keyList.AddRange (s_defaultAssetFilterGUITypeMap.Keys);

                s_filterKeyTypeList = keyList;
            }
            return s_filterKeyTypeList;
        }

        public static Type FindFilterTypeFromGUIName(string guiName) {
            if (guiName == Model.Settings.DEFAULT_FILTER_KEYTYPE) {
                return null; // or UnityEngine.Object ?
            }

            // since PrefabImporter is internal class and can't access
            if (guiName == kPREFAB_KEY_NAME)
            {
                if (s_prefabImporterClass == null)
                {
                    s_prefabImporterClass = Assembly.Load("UnityEditor").GetType(kPREFAB_IMPORTER_CLASS);
                }

                return s_prefabImporterClass;
            }

            var typemap = ImporterConfiguratorUtility.GetImporterConfiguratorGuiNameTypeMap ();
            if (typemap.ContainsKey (guiName)) {
                return typemap [guiName];
            }
            
            if (s_defaultAssetFilterGUITypeMap.ContainsKey (guiName)) {
                return s_defaultAssetFilterGUITypeMap [guiName];
            }

            return null;
        }

        public static string FindGUINameFromType(Type t) {

            UnityEngine.Assertions.Assert.IsNotNull (t);

            // since PrefabImporter is internal class and can't access
            if (t.FullName == kPREFAB_IMPORTER_CLASS)
            {
                return kPREFAB_KEY_NAME;
            }
            
            var typemap = ImporterConfiguratorUtility.GetImporterConfiguratorGuiNameTypeMap ();

            var elements = typemap.Where (v => v.Value == t);
            if (elements.Any ()) {
                return elements.First ().Key;
            }

            var elements2 = s_defaultAssetFilterGUITypeMap.Where (v => v.Value == t);
            if (elements2.Any ()) {
                return elements2.First ().Key;
            }

            return Model.Settings.DEFAULT_FILTER_KEYTYPE;
        }

		public static Type FindAssetFilterType (string assetPath) {
            var importerType = TypeUtility.GetAssetImporterTypeAtPath(assetPath);
            if (importerType != null) {
                return importerType;
            }

			// not specific type importer. should determine their type by extension.
			var extension = Path.GetExtension(assetPath).ToLower();

            if (IgnoredAssetTypeExtension.Contains(extension)) {
                return null;
            }

            return TypeUtility.GetMainAssetTypeAtPath (assetPath);
		}			
	}
}
