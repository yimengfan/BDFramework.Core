using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;

internal class SplitByFilterTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("SubFolder/", "prefab01", PrimitiveType.Cube);
		CreateTestPrefab("SubFolder/", "foovar", PrimitiveType.Cylinder);
		CreateTestPrefab("", "prefab02", PrimitiveType.Cube);		
		CreateTestMaterial("", "mat01", "Hidden/AssetGraph/LineDraw");
		CreateTestTexture("OtherSub/", "tex01", 128, 128, TextureFormat.ARGB32, true, false);
	}

	[Test]
	public void TestRegexFilter()
	{
		AssertGraphExecuteWithNoIssue();
	}
	
	[Test]
	public void TestCombinedFilter()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestFilterByType()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestFilterByName()
	{
		AssertGraphExecuteWithNoIssue();
	}
}
