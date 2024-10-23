using UnityEngine;
using NUnit.Framework;
using UnityEngine.AssetGraph;

internal class GroupBySizeTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("Prefabs/", "prefab01", PrimitiveType.Cube);
		CreateTestPrefab("Prefabs/", "prefab02", PrimitiveType.Capsule);
		CreateTestPrefab("Prefabs/", "prefab03", PrimitiveType.Cylinder);
		CreateTestPrefab("Prefabs/", "prefab04", PrimitiveType.Sphere);
		CreateTestPrefab("Prefabs/", "prefab05", PrimitiveType.Quad);
		CreateTestMaterial("Materials/", "linedraw", "Hidden/AssetGraph/LineDraw");
		CreateTestTexture("Textures/", "tex_s01", 128, 128, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_s02", 128, 128, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_s03", 128, 128, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_s04", 128, 128, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_s05", 128, 128, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_m01", 512, 512, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_m02", 512, 512, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_m03", 512, 512, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_m04", 512, 512, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_m05", 512, 512, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_l01", 2048, 2048, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_l02", 2048, 2048, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_l03", 2048, 2048, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_l04", 2048, 2048, TextureFormat.ARGB32, true, true);
		CreateTestTexture("Textures/", "tex_l05", 2048, 2048, TextureFormat.ARGB32, true, true);
	}

	[Test]
	public void TestGroupNameFormat()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestGroupByFileSize()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestGroupByRuntimeMemorySize()
	{
		AssertGraphExecuteWithNoIssue();
	}

	[Test]
	public void TestFreezeGroupOnBuild()
	{
		AssertGraphExecuteWithNoIssue();
	}

}
