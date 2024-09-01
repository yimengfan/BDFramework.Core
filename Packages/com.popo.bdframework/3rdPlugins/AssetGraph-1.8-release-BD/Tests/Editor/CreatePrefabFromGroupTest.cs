using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.AssetGraph;


internal class CreatePrefabFromGroupTest : AssetGraphEditorBaseTest
{
	private void CreateTestPrefabForBuilders(string assetPrefix, string objectName, PrimitiveType t, GameObject childObject)
	{
		var assetPath = RootFolder + "/" + assetPrefix + objectName + ".prefab";
		EnsureDirectoryExists(assetPath);

		var root = GameObject.CreatePrimitive(t);
		root.name = objectName;

		for (var i = 0; i < 5; ++i)
		{
			var go = GameObject.Instantiate(childObject);
			go.name = string.Format("{0}{1}", childObject.name, i);
			go.transform.parent = root.transform;
		}
		
		PrefabUtility.SaveAsPrefabAsset(root, assetPath);
		Object.DestroyImmediate(root);
	}
	
	
	protected override void CreateResourcesForTests()
	{
		var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
		child.name = "child";		

		var replacing = GameObject.CreatePrimitive(PrimitiveType.Capsule);
		replacing.name = "rep";
		
		CreateTestPrefabForBuilders("Testing/", "replaceBy", PrimitiveType.Cube, child);
		CreateTestPrefabForBuilders("Testing/", "replaceWith", PrimitiveType.Cube, child);
		CreateTestPrefabForBuilders("Reference/", "replaceBy", PrimitiveType.Capsule, replacing);
		CreateTestPrefabForBuilders("Reference/", "replaceWith", PrimitiveType.Capsule, replacing);
	}

//	private static void AssertGameObjectEqual(GameObject a, GameObject b)
//	{
//		Assert.AreEqual(a, b);
//	}
//
//	[Test]
//	public void TestReplaceGameObjectByName()
//	{
//		var graph = LoadGraphForTest();
//		
//		var builderNode = graph.Nodes.First(n => n.Operation.ClassName == typeof(PrefabBuilder).AssemblyQualifiedName).Operation.Object as PrefabBuilder;
//		var builder = builderNode.GetPrefabBuilder(EditorUserBuildSettings.activeBuildTarget) as ReplaceGameObjectByName;
//
//		var replacing = GameObject.CreatePrimitive(PrimitiveType.Capsule);
//		replacing.name = "rep";
//		
//		builder.ReplaceEntries[0].dstObject.Object = replacing;
//		builder.ReplaceEntries[0].name = "???";
//
//		using (new DisableAssetProcessEventRecordScope())
//		{
//			var result = AssetGraphUtility.ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graph);
//			Assert.False(result.IsAnyIssueFound);
//		}
//
//		AssetDatabase.Refresh();
//
//		var testObject = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(RootFolder, "replaceBy_0.prefab"));
//		var refObject = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(RootFolder, "Reference/replaceBy.prefab"));
//		
//		AssertGameObjectEqual(testObject, refObject);
//	}
//	
//	[Test]
//	public void TestReplaceWithIncomingGameObject()
//	{
//		var graph = LoadGraphForTest();
//		
//		var builderNode = graph.Nodes.First(n => n.Operation.ClassName == typeof(PrefabBuilder).AssemblyQualifiedName).Operation.Object as PrefabBuilder;
//		var builder = builderNode.GetPrefabBuilder(EditorUserBuildSettings.activeBuildTarget) as ReplaceWithIncomingGameObject;
//
//		var replacing = GameObject.CreatePrimitive(PrimitiveType.Capsule);
//		replacing.name = "rep";
//
//		builder.ReplacingObject = replacing;
//
//		using (new DisableAssetProcessEventRecordScope())
//		{
//			var result = AssetGraphUtility.ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graph);
//			Assert.False(result.IsAnyIssueFound);
//		}
//		
//		AssetDatabase.Refresh();
//
//		var testObject = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(RootFolder, "replaceWith_0.prefab"));
//		var refObject = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(RootFolder, "Reference/replaceWith.prefab"));
//		
//		AssertGameObjectEqual(testObject, refObject);
//	}
}
