using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;

internal class LoadFromDirectoryTest : AssetGraphEditorBaseTest {

	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("", "prefab01", PrimitiveType.Cube);
		CreateTestPrefab("", "prefab02", PrimitiveType.Cube);
		CreateTestPrefab("", "prefab03", PrimitiveType.Cube);
		CreateTestPrefab("", "prefab04", PrimitiveType.Cube);
		CreateTestPrefab("Ignore/", "prefab05", PrimitiveType.Cube);
		CreateTestPrefab("Ignore/", "prefab06", PrimitiveType.Cube);
	}

	[Test]
	public void TestLoadAsset()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestIgnoreSettings()
	{
		AssertGraphExecuteWithNoIssue();
	}
	
	[Test]
	public void TestDirectoryNotExist()
	{
		ExecuteGraphResult result;
		using (new DisableAssetProcessEventRecordScope())
		{
			result = AssetGraphUtility.ExecuteGraph(UnityEditor.EditorUserBuildSettings.activeBuildTarget, LoadGraphForTest(1, false));		
		}
				
		Assert.True(result.IsAnyIssueFound);
		
		foreach (var e in result.Issues)
		{
			Assert.AreEqual(e.Node.Operation.ClassName, typeof(Loader).AssemblyQualifiedName);
		}
	}
}
