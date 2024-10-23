using UnityEditor;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    public class ConnectionGUIInspector 
    {
        private GroupViewController m_groupViewController;



        public  void OnInspectorGUI(ConnectionGUI con )
        {

            if (m_groupViewController == null)
            {
                m_groupViewController = new GroupViewController(con.AssetGroupViewContext);
            }

            if (con == null)
            {
                return;
            }

            var count = 0;
            var assetGroups = con.AssetGroups;
            if (assetGroups == null)
            {
                return;
            }

            foreach (var assets in assetGroups.Values)
            {
                if (assets != null)
                {
                    count += assets.Count;
                }
            }

            var groupCount = assetGroups.Keys.Count;

            GUILayout.Label("Stats", "BoldLabel");
            EditorGUILayout.LabelField("Total groups", groupCount.ToString());
            EditorGUILayout.LabelField("Total items", count.ToString());
            GUILayout.Space(8f);

            m_groupViewController.SetGroups(assetGroups);
            m_groupViewController.OnGroupViewGUI();
        }
    }
}
