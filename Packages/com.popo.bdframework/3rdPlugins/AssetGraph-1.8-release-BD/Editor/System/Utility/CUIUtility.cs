using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class CUIUtility {

		private static readonly string kCommandMethod = "UnityEngine.AssetGraph.CUIUtility.BuildFromCommandline";

		private static readonly string kCommandStr = 
			"\"{0}\" -batchmode -quit -projectPath \"{1}\" -logFile abbuild.log -executeMethod {2} {3}";

		private static readonly string kCommandName = 
			"buildassetbundle.{0}";

		[MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_CUITOOL, priority = 14000 + 3)]
		private static void CreateCUITool() {

			var appPath = EditorApplication.applicationPath.Replace(Model.Settings.UNITY_FOLDER_SEPARATOR, Path.DirectorySeparatorChar);

            var appCmd =
	            $"{appPath}{((Application.platform == RuntimePlatform.WindowsEditor) ? "" : "/Contents/MacOS/Unity")}";
			var argPass = (Application.platform == RuntimePlatform.WindowsEditor)? "%1 %2 %3 %4 %5 %6 %7 %8 %9" : "$*";
			var cmd = string.Format(kCommandStr, appCmd, FileUtility.ProjectPathWithSlash(), kCommandMethod, argPass);
			var ext = (Application.platform == RuntimePlatform.WindowsEditor)? "bat" : "sh";
			var cmdFile = string.Format(kCommandName, ext );

            var destinationPath = FileUtility.PathCombine(Model.Settings.Path.CUISpacePath, cmdFile);

            Directory.CreateDirectory(Model.Settings.Path.CUISpacePath);
			File.WriteAllText(destinationPath, cmd);

			AssetDatabase.Refresh();
		}

		/**
		 * Build from commandline - entrypoint.
		 */ 
		public static void BuildFromCommandline(){
			try {
				var arguments = new List<string>(System.Environment.GetCommandLineArgs());

				Application.SetStackTraceLogType(LogType.Log, 		StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Error, 	StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Warning, 	StackTraceLogType.None);

				BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

				int targetIndex = arguments.FindIndex(a => a == "-target");

				if(targetIndex >= 0) {
					var targetStr = arguments[targetIndex+1];
					LogUtility.Logger.Log("Target specified:"+ targetStr);

					var newTarget = BuildTargetUtility.BuildTargetFromString(arguments[targetIndex+1]);
					if(!BuildTargetUtility.IsBuildTargetSupported(newTarget)) {
						throw new AssetGraphException(newTarget + " is not supported to build with this Unity. Please install platform support with installer(s).");
					}

					if(newTarget != target) {
						EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetUtility.TargetToGroup(newTarget), newTarget);
						target = newTarget;
					}
				}

				int graphIndex = arguments.FindIndex(a => a == "-graph");
				int collectionIndex = arguments.FindIndex(a => a == "-collection");

				if(graphIndex >= 0 && collectionIndex >= 0) {
					LogUtility.Logger.Log("-graph and -collection can not be used at once. Aborting...");
					return;
				}

				Model.ConfigGraph graph = null;

				if(graphIndex >= 0) {
					var graphPath = arguments[graphIndex+1];
					LogUtility.Logger.Log("Graph path:"+ graphPath);

					graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphPath);

					LogUtility.Logger.Log("AssetReference bundle building for:" + BuildTargetUtility.TargetToHumaneString(target));

					if (graph == null) {
						LogUtility.Logger.Log("Graph data not found. To specify graph to execute, use -graph [path]. Aborting...");
						return;
					}

					var result = AssetGraphUtility.ExecuteGraph(target, graph);

					if(result.IsAnyIssueFound) {
						LogUtility.Logger.Log("Building asset bundles terminated because of following errors. Please fix issues by opening editor.");
						foreach(var e in result.Issues) {
							LogUtility.Logger.LogError(LogUtility.kTag, e.Reason);
						}
					}
				}

				if(collectionIndex >= 0) {
					var collectionName = arguments[collectionIndex+1];
					LogUtility.Logger.Log("Collection Name:"+ collectionName);

					LogUtility.Logger.Log("AssetReference bundle building for:" + BuildTargetUtility.TargetToHumaneString(target));

					if (collectionName == null) {
						LogUtility.Logger.Log("Collection name not specified. To specify collection to execute, use -collection [name]. Aborting...");
						return;
					}
					BatchBuildConfig.GraphCollection c = BatchBuildConfig.GetConfig().Find(collectionName);
					if (c == null) {
						LogUtility.Logger.Log("Collection not found. Please open project and configure graph collection. Aborting...");
						return;
					}

					var result = AssetGraphUtility.ExecuteGraphCollection(target, c);

					foreach(var r in result) {
						if(r.IsAnyIssueFound) {
							foreach(var e in r.Issues) {
								LogUtility.Logger.LogError(LogUtility.kTag, r.Graph.name + ":" + e.Reason);
							}
						}
					}
				}
			} catch(Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, e);
				LogUtility.Logger.LogError(LogUtility.kTag, "Building asset bundles terminated due to unexpected error.");
			} finally {
				LogUtility.Logger.Log("End of build.");
			}
		}
	}
}
