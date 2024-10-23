using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;

internal class GroupByFileTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("c0001/v0001/", "body", PrimitiveType.Cube);
		CreateTestPrefab("c0001/v0001/", "head01", PrimitiveType.Cube);
		CreateTestPrefab("c0001/v0002/", "body", PrimitiveType.Cube);
		CreateTestPrefab("c0001/v0002/", "head01", PrimitiveType.Cube);
		CreateTestPrefab("c0001/v0003/", "body", PrimitiveType.Cube);
		CreateTestPrefab("c0001/v0003/", "head01", PrimitiveType.Cube);
		CreateTestPrefab("c0001/v0003/", "head02", PrimitiveType.Cube);
	}

	[Test]
	public void TestGroupNameFormat()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestGroupByFile()
	{
		AssertGraphExecuteWithNoIssue();
	}
}
