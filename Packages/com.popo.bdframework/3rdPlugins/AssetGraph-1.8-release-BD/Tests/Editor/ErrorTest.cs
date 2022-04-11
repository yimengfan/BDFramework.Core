using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;

internal class ErrorTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("", "prefab01", PrimitiveType.Cube);
	}

	[Test]
	public void TestNoError()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestError()
	{
		var result = AssertGraphExecuteWithIssue();
		
		foreach (var e in result.Issues)
		{
			Assert.AreEqual(e.Node.Operation.ClassName, typeof(Error).AssemblyQualifiedName);
		}
	}
}
