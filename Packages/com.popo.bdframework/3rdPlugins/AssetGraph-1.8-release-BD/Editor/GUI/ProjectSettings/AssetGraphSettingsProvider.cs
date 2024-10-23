using UnityEditor;
using System.Linq;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	
	class AssetGraphSettingsProvider : SettingsProvider
	{
		private AssetBundlesSettingsTab     m_abTab;
		private ExecutionOrderSettingsTab   m_execTab;
		private Mode m_mode;

		enum Mode : int {
			AssetBundleSettings,
			ExecutionOrderSettings
		}
		
		public AssetGraphSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
			: base(path, scope)
		{
			m_abTab = new AssetBundlesSettingsTab ();
			m_execTab = new ExecutionOrderSettingsTab ();
			m_mode = Mode.AssetBundleSettings;			
		}

		public override void OnGUI(string searchContext)
		{
			DrawToolBar ();

			switch (m_mode) {
				case Mode.AssetBundleSettings:
					m_abTab.OnGUI ();
					break;
				case Mode.ExecutionOrderSettings:
					m_execTab.OnGUI ();
					break;
			}
		}
		
		private void DrawToolBar() {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			float toolbarWidth = 300f;
			string[] labels = new string[] { "Asset Bundles", "Execution Order" };
			m_mode = (Mode)GUILayout.Toolbar((int)m_mode, labels, "LargeButton", GUILayout.Width(toolbarWidth) );
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(8f);
		}
		

		// Register the SettingsProvider
		[SettingsProvider]
		public static SettingsProvider CreateAssetGraphSettingsProvider()
		{
			var provider = new AssetGraphSettingsProvider("Project/Asset Graph")
			{
				keywords = 
					GetSearchKeywordsFromGUIContentProperties<AssetBundlesSettingsTab.Styles>()
					.Concat(GetSearchKeywordsFromGUIContentProperties<ExecutionOrderSettingsTab.Styles>())
			};			

			// Automatically extract all keywords from the Styles.
			return provider;
		}
	}
}
