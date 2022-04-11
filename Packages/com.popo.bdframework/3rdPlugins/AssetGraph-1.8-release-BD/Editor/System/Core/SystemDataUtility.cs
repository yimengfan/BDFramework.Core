using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class SystemDataUtility {

		public static bool IsCached (AssetReference relatedAsset, List<string> alreadyCachedPath, string localAssetPath) {
			if (alreadyCachedPath.Contains(localAssetPath)) {
				// if source is exists, check hash.
				var sourceHash = GetHash(relatedAsset.absolutePath);
				var destHash = GetHash(localAssetPath);

				// completely hit.
				if (sourceHash.SequenceEqual(destHash)) {
					return true;
				}
			}

			return false;
		}

		public static byte[] GetHash (string filePath) {
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(filePath)) {
					return md5.ComputeHash(stream);
				}
			}
		}

		public static string GetPathSafeDefaultTargetName () {
			return GetPathSafeTargetGroupName(BuildTargetUtility.DefaultTarget);
		}

		public static string GetPathSafeTargetName (BuildTarget t) {
			return t.ToString();
		}

		public static string GetPathSafeTargetGroupName (BuildTargetGroup g) {
			return g.ToString();
		}

		public static string GetProjectName () {
			var assetsPath = Application.dataPath;
			var projectFolderNameArray = assetsPath.Split(Model.Settings.UNITY_FOLDER_SEPARATOR);
			var projectFolderName = projectFolderNameArray[projectFolderNameArray.Length - 2] + Model.Settings.UNITY_FOLDER_SEPARATOR;
			return projectFolderName;
		}

	}
}

