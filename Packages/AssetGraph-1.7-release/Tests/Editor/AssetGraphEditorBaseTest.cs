using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

internal abstract class AssetGraphEditorBaseTest: IPrebuildSetup
{

	// Set this to true when you need to debug graph and also create reference data for regression tests.
	// Do not commit code with this switch set to true.
	private const bool kDebugGraph = false;

	// Root folder of each tests to use assets. 
	protected string RootFolder => $"Assets/{GetType().Name}_AssetsToDelete";

	public void Setup()
	{
		if (!Directory.Exists(RootFolder))
		{
			Directory.CreateDirectory(RootFolder);
		}
	}

	[OneTimeTearDown]
	public void Cleanup()
	{
		CleanupTest();
		if (!kDebugGraph)
		{
			AssetDatabase.DeleteAsset(RootFolder);
		}
	}

	[OneTimeSetUp]
	public void OneTimeSetup()
	{
		AssetDatabase.StartAssetEditing();
		CreateResourcesForTests();
		AssetDatabase.StopAssetEditing();
		PrepareTest();
	}

	protected virtual void PrepareTest()
	{
		//do nothing
	}

	protected virtual void CleanupTest()
	{
		//do nothing
	}

	protected abstract void CreateResourcesForTests();
	
	[MethodImpl(MethodImplOptions.NoInlining)]
	protected static string GetCurrentMethodName (int stackOffset)
	{
		var st = new StackTrace ();
		var sf = st.GetFrame (1 + stackOffset);
		return sf.GetMethod().Name;
	}

	protected Model.ConfigGraph LoadGraphForTest(int stackoffset=1, bool fixLoadPath = true)
	{
		var graph = LoadGraphByName("__hidden__" + GetType().Name + "_" + GetCurrentMethodName(stackoffset));
		if (fixLoadPath)
		{
			SetLoaderPathToRootDirectory(graph);
		}
		return graph;
	}

	protected static Model.ConfigGraph LoadGraphByName(string name)
	{
		var guids = AssetDatabase.FindAssets (Model.Settings.GRAPH_SEARCH_CONDITION + " " + name);
		
		Assert.IsFalse(guids.Length == 0, "Graph for test not found.");
		Assert.AreEqual(1, guids.Length, "Multiple graphs for test found.");
		
		var path = AssetDatabase.GUIDToAssetPath (guids[0]);

		return AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
	}

	protected void SetLoaderPathToRootDirectory(Model.ConfigGraph graph)
	{
		foreach (var n in graph.Nodes.Where(n => n.Operation.ClassName.StartsWith(typeof(UnityEngine.AssetGraph.Loader).FullName + ",")))
		{
			var loader = n.Operation.Object as UnityEngine.AssetGraph.Loader;
			loader.LoadPath = RootFolder;
		}
	}

	protected static void EnsureDirectoryExists(string assetPath)
	{
		var dir = Path.GetDirectoryName(assetPath);
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
	}

	protected void AssertGraphExecuteWithNoIssue()
	{
		var result = AssetGraphUtility.ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, LoadGraphForTest(2));
		
		Assert.False(result.IsAnyIssueFound);
	}
	
	protected ExecuteGraphResult AssertGraphExecuteWithIssue()
	{
		ExecuteGraphResult result;
		using (new DisableAssetProcessEventRecordScope())
		{
			result = AssetGraphUtility.ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, LoadGraphForTest(2));		
			Assert.True(result.IsAnyIssueFound);
		}
		return result;
	}

	internal void CreateTestPrefab(string assetPrefix, string objectName, PrimitiveType t)
	{
		var assetPath = RootFolder + "/" + assetPrefix + objectName + ".prefab";
		EnsureDirectoryExists(assetPath);

		var go = GameObject.CreatePrimitive(t);
		go.name = objectName;
		PrefabUtility.SaveAsPrefabAsset(go, assetPath);
		Object.DestroyImmediate(go);
	}
	
	internal void CreateTestTexture(string assetPrefix, string objectName, int width, int height, TextureFormat format, bool mipmap, bool linear)
	{
		var assetPath = RootFolder + "/" + assetPrefix + objectName + ".png";
		EnsureDirectoryExists(assetPath);

		var t = new Texture2D(width, height, format, mipmap, linear);
		var bytes = t.EncodeToPNG();
		
		var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), assetPath);
		File.WriteAllBytes(fullPath, bytes);

		Object.DestroyImmediate(t);
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	internal void CreateTestMaterial(string assetPrefix, string objectName, string shaderName)
	{
		var assetPath = RootFolder + "/" + assetPrefix + objectName + ".mat";
		EnsureDirectoryExists(assetPath);

		var shader = Shader.Find (shaderName);
		var m = new Material(shader);

		AssetDatabase.CreateAsset(m, assetPath);
	}
}
