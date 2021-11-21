using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;

internal class GroupByFilePathTest : AssetGraphEditorBaseTest
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

		CreateTestPrefab("c1834/v0001/", "body", PrimitiveType.Cube);
		CreateTestPrefab("c1834/v0001/", "head01", PrimitiveType.Cube);
		CreateTestPrefab("c1834/v0001/", "head02", PrimitiveType.Cube);
		CreateTestPrefab("c1834/v0001/", "weapon", PrimitiveType.Cube);
		CreateTestPrefab("c1834/v0002/", "body", PrimitiveType.Cube);
		CreateTestPrefab("c1834/v0002/", "head01", PrimitiveType.Cube);
		
		CreateTestPrefab("boss/", "body", PrimitiveType.Cube);
		CreateTestPrefab("boss/", "head01", PrimitiveType.Cube);
		CreateTestPrefab("boss/", "weapon", PrimitiveType.Cube);
	}

	[Test]
	public void TestGroupByWildcard()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestGroupByRegex()
	{
		AssertGraphExecuteWithNoIssue();
	}
	
	[Test]
	public void TestSubgrouping()
	{
		AssertGraphExecuteWithNoIssue();
	}
}
