using UnityEditor;
using System;

using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
	public static class BuildTargetUtility
	{

		public const BuildTargetGroup DefaultTarget = BuildTargetGroup.Unknown;

		/**
		 *  from build target to human friendly string for display purpose.
		 */
		public static string TargetToHumaneString (BuildTarget t)
		{

			switch (t) {
			case BuildTarget.Android:
				return "Android";
			case BuildTarget.iOS:
				return "iOS";
			case BuildTarget.PS4:
				return "PlayStation 4";
#if !UNITY_2019_2_OR_NEWER
			case BuildTarget.StandaloneLinux:
				return "Linux Standalone";
			case BuildTarget.StandaloneLinuxUniversal:
				return "Linux Standalone(Universal)";
#endif
			case BuildTarget.StandaloneLinux64:
				return "Linux Standalone(64-bit)";
			case BuildTarget.StandaloneOSX:
				return "OSX Standalone(Universal)";
			case BuildTarget.StandaloneWindows:
				return "Windows Standalone";
			case BuildTarget.StandaloneWindows64:
				return "Windows Standalone(64-bit)";
			case BuildTarget.tvOS:
				return "tvOS";
			case BuildTarget.WebGL:
				return "WebGL";
			case BuildTarget.WSAPlayer:
				return "Windows Store Apps";
			case BuildTarget.XboxOne:
				return "Xbox One";
			case BuildTarget.Switch:
				return "Nintendo Switch";

			default:
				return t + "(deprecated)";
			}
		}

		public enum PlatformNameType
		{
			Default,
			TextureImporter,
			AudioImporter,
			VideoClipImporter
		}

		public static string TargetToAssetBundlePlatformName (BuildTargetGroup g, PlatformNameType pnt = PlatformNameType.Default)
		{
			return TargetToAssetBundlePlatformName (GroupToTarget (g), pnt);
		}

		//returns the same value defined in AssetBundleManager
		public static string TargetToAssetBundlePlatformName (BuildTarget t, PlatformNameType pnt = PlatformNameType.Default)
		{
			switch (t) {
			case BuildTarget.Android:
				return "Android";
			case BuildTarget.iOS:
				switch (pnt) {
				case PlatformNameType.TextureImporter:
					return "iPhone";
				}
				return "iOS";
			case BuildTarget.PS4:
				return "PS4";
#if !UNITY_2019_2_OR_NEWER
			case BuildTarget.StandaloneLinux:
			case BuildTarget.StandaloneLinuxUniversal:
#endif
			case BuildTarget.StandaloneLinux64:
				return "Linux";
			case BuildTarget.StandaloneOSX:
				return "OSX";
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				switch (pnt) {
				case PlatformNameType.AudioImporter:
					return "Standalone";
				case PlatformNameType.TextureImporter:
					return "Standalone";
				case PlatformNameType.VideoClipImporter:
					return "Standalone";
				}
				return "Windows";
			case BuildTarget.tvOS:
				return "tvOS";
			case BuildTarget.WebGL:
				return "WebGL";
			case BuildTarget.WSAPlayer:
				switch (pnt) {
				case PlatformNameType.AudioImporter:
					return "WSA";
				case PlatformNameType.VideoClipImporter:
					return "WSA";
				}
				return "WindowsStoreApps";
			case BuildTarget.XboxOne:
				return "XboxOne";
			case BuildTarget.Switch:
				return "Switch";

			default:
				return t.ToString () + "(deprecated)";
			}
		}

		/**
		 *  from build target group to human friendly string for display purpose.
		 */
		public static string GroupToHumaneString (BuildTargetGroup g)
		{

			switch (g) {
			case BuildTargetGroup.Android:
				return "Android";
			case BuildTargetGroup.iOS:
				return "iOS";
			case BuildTargetGroup.PS4:
				return "PlayStation 4";
			case BuildTargetGroup.Standalone:
				return "PC/Mac/Linux Standalone";
			case BuildTargetGroup.tvOS:
				return "tvOS";
			case BuildTargetGroup.WebGL:
				return "WebGL";
			case BuildTargetGroup.WSA:
				return "Windows Store Apps";
			case BuildTargetGroup.XboxOne:
				return "Xbox One";
			case BuildTargetGroup.Unknown:
				return "Unknown";
			#if !UNITY_2019_3_OR_NEWER
			case BuildTargetGroup.Facebook:
				return "Facebook";
			#endif
			case BuildTargetGroup.Switch:
				return "Nintendo Switch";
			default:
				return g.ToString () + "(deprecated)";
			}
		}


		public static BuildTargetGroup TargetToGroup (BuildTarget t)
		{

			if ((int)t == int.MaxValue) {
				return BuildTargetGroup.Unknown;
			}

			switch (t) {
			case BuildTarget.Android:
				return BuildTargetGroup.Android;
			case BuildTarget.iOS:
				return BuildTargetGroup.iOS;
			case BuildTarget.PS4:
				return BuildTargetGroup.PS4;
#if !UNITY_2019_2_OR_NEWER
			case BuildTarget.StandaloneLinux:
			case BuildTarget.StandaloneLinuxUniversal:
#endif
			case BuildTarget.StandaloneLinux64:
			case BuildTarget.StandaloneOSX:
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return BuildTargetGroup.Standalone;
			case BuildTarget.tvOS:
				return BuildTargetGroup.tvOS;
			case BuildTarget.WebGL:
				return BuildTargetGroup.WebGL;
			case BuildTarget.WSAPlayer:
				return BuildTargetGroup.WSA;
			case BuildTarget.XboxOne:
				return BuildTargetGroup.XboxOne;
			case BuildTarget.Switch:
				return BuildTargetGroup.Switch;
			default:
				return BuildTargetGroup.Unknown;
			}
		}

		public static BuildTarget GroupToTarget (BuildTargetGroup g)
		{

			switch (g) {
			case BuildTargetGroup.Android:
				return BuildTarget.Android;
			case BuildTargetGroup.iOS:
				return BuildTarget.iOS;
			case BuildTargetGroup.PS4:
				return BuildTarget.PS4;
			case BuildTargetGroup.Standalone:
				return BuildTarget.StandaloneWindows;
			case BuildTargetGroup.tvOS:
				return BuildTarget.tvOS;
			case BuildTargetGroup.WebGL:
				return BuildTarget.WebGL;
			case BuildTargetGroup.WSA:
				return BuildTarget.WSAPlayer;
			case BuildTargetGroup.XboxOne:
				return BuildTarget.XboxOne;
			case BuildTargetGroup.Switch:
				return BuildTarget.Switch;
			#if !UNITY_2019_3_OR_NEWER
			case BuildTargetGroup.Facebook:
				return BuildTarget.StandaloneWindows; // TODO: Facebook can be StandardWindows or WebGL
			#endif
			default:
				// temporarily assigned for default value (BuildTargetGroup.Unknown)
				return (BuildTarget)int.MaxValue;
			}
		}

		public static BuildTarget BuildTargetFromString (string val) {
			return (BuildTarget)Enum.Parse(typeof(BuildTarget), val);
		}

		public static bool IsBuildTargetSupported(BuildTarget t) {

            var g = TargetToGroup(t);

            return BuildPipeline.IsBuildTargetSupported(g, t);
		}
	}
}
