using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;


internal class ConfigureBundleFromGroupTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("Good/", "prefab01", PrimitiveType.Cube);
		CreateTestPrefab("Good/", "prefab02", PrimitiveType.Cube);
		CreateTestPrefab("Bad/", "prefab03", PrimitiveType.Cube);
		CreateTestPrefab("Bad/", "prefab04", PrimitiveType.Cube);
	}

}
