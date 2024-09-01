using UnityEditor;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    public class AssetPostprocessorContext {
        private List<AssetReference> m_importedAssets;
        private List<AssetReference> m_movedAssets;

        private readonly string[] m_importedAssetPaths;
        private readonly string[] m_deletedAssetPaths;
        private readonly string[] m_movedAssetPaths;
        private readonly string[] m_movedFromAssetPaths;
        private bool m_isAdhoc;

        public List<AssetReference> ImportedAssets {
            get {
                return m_importedAssets;
            }
        }

        public List<AssetReference> MovedAssets {
            get {
                return m_movedAssets;
            }
        }

        public string[] ImportedAssetPaths {
            get {
                return m_importedAssetPaths;
            }
        }

        public string[] DeletedAssetPaths {
            get {
                return m_deletedAssetPaths;
            }
        }

        public string[] MovedAssetPaths {
            get {
                return m_movedAssetPaths;
            }
        }

        public string[] MovedFromAssetPaths {
            get {
                return m_movedFromAssetPaths;
            }
        }

        public bool IsAdhoc {
            get {
                return m_isAdhoc;
            }
        }

        public AssetPostprocessorContext(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets, 
            string[] movedFromAssetPaths,
            Stack<AssetPostprocessorContext> ctxStack) 
        {
            m_importedAssetPaths = importedAssets;
            m_deletedAssetPaths = deletedAssets;
            m_movedAssetPaths = movedAssets;
            m_movedFromAssetPaths = movedFromAssetPaths;
            m_isAdhoc = false;

            Init (ctxStack);
        }

        public AssetPostprocessorContext() 
        {
            m_importedAssets = new List<AssetReference>();
            m_movedAssets = new List<AssetReference>();

            string[] empty = new string[0];

            m_importedAssetPaths = empty;
            m_deletedAssetPaths = empty;
            m_movedAssetPaths = empty;
            m_movedFromAssetPaths = empty;
            m_isAdhoc = true;
        }

        public void Init(Stack<AssetPostprocessorContext> ctxStack) 
        {
            m_importedAssets = new List<AssetReference>();
            m_movedAssets = new List<AssetReference>();

            foreach(var path in m_importedAssetPaths) {
                if(!TypeUtility.IsLoadingAsset(path)) {
                    continue;
                }
                bool isAssetFoundInStack = false;

                foreach (var ctx in ctxStack) {
                    if (ArrayUtility.Contains(ctx.ImportedAssetPaths, path) || 
                        ctx.ImportedAssets.Find (a => a.importFrom == path) != null) 
                    {
                        isAssetFoundInStack = true;
                        break;
                    }
                }
                if (!isAssetFoundInStack) {
                    var a = AssetReferenceDatabase.GetReference (path);
                    m_importedAssets.Add (a);
                }
            }

            for(int i = 0; i < m_movedAssetPaths.Length; ++i) {
                var movedTo = m_movedAssetPaths [i];

                if(!TypeUtility.IsLoadingAsset(movedTo)) {
                    continue;
                }

                var a = AssetReferenceDatabase.GetReference (movedTo);
                m_movedAssets.Add (a);
            }
        }

        public bool HasValidAssetToPostprocess() {
            return m_importedAssets.Count > 0 || m_movedAssets.Count > 0 || m_deletedAssetPaths.Length > 0;
        }
    }
}
