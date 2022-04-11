using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;

internal class LoadBySearchFilterTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("", "LoadBySearchFilterTestPrefab01", PrimitiveType.Cube);
		CreateTestMaterial("", "LoadBySearchFilterTestMaterial01", "Hidden/AssetGraph/LineDraw");
	}

	[Test]
	public void TestSearchFilterWithName()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestSearchFilterWithTypeAndName()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestEmptySearchCondition()
	{
		var result = AssertGraphExecuteWithIssue();
		
		foreach (var e in result.Issues)
		{
			Assert.AreEqual(e.Node.Operation.ClassName, typeof(LoaderBySearch).AssemblyQualifiedName);
		}
	}
}
