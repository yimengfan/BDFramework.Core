using UnityEditor;
using System;
using System.IO;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    public class JSONGraphUtility {

        public static void ExportGraphToJSONFromDialog(Model.ConfigGraph graph) {

            string path =
                EditorUtility.SaveFilePanelInProject(
                    $"Export {graph.name} to JSON file", 
                    graph.name, "json", 
                    "Export to:");
            if(string.IsNullOrEmpty(path)) {
                return;
            }

            string jsonString = EditorJsonUtility.ToJson (graph, true);

            File.WriteAllText (path, jsonString, System.Text.Encoding.UTF8);

            AssetDatabase.Refresh();
		}

        public static void ExportAllGraphsToJSONFromDialog() {

            var folderSelected = 
                EditorUtility.OpenFolderPanel("Select folder to export all graphs", Application.dataPath + "..", "");
            if(string.IsNullOrEmpty(folderSelected)) {
                return;
            }

            var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

            foreach(var guid in guids) {
                string graphPath = AssetDatabase.GUIDToAssetPath(guid);
                string graphName = Path.GetFileNameWithoutExtension(graphPath);

                string jsonFilePath = Path.Combine (folderSelected, $"{graphName}.json");

                var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphPath);
                string jsonString = EditorJsonUtility.ToJson (graph, true);

                File.WriteAllText (jsonFilePath, jsonString, System.Text.Encoding.UTF8);
            }

            AssetDatabase.Refresh();
        }

        public static Model.ConfigGraph ImportJSONToGraphFromDialog(Model.ConfigGraph graph) {

            string fileSelected = EditorUtility.OpenFilePanelWithFilters("Select JSON files to import", Application.dataPath, new string[] {"JSON files", "json", "All files", "*"});
            if(string.IsNullOrEmpty(fileSelected)) {
                return null;
            }

            string name = Path.GetFileNameWithoutExtension(fileSelected);

            var jsonContent = File.ReadAllText (fileSelected, System.Text.Encoding.UTF8);

            if (graph != null) {
                Undo.RecordObject(graph, "Import");
                EditorJsonUtility.FromJsonOverwrite (jsonContent, graph);
            } else {
                graph = ScriptableObject.CreateInstance<Model.ConfigGraph>();
                EditorJsonUtility.FromJsonOverwrite (jsonContent, graph);
                var newAssetFolder = CreateFolderForImportedAssets ();
                var graphPath = FileUtility.PathCombine(newAssetFolder, $"{name}.asset");
                AssetDatabase.CreateAsset (graph, graphPath);
            }
            return graph;
        }

        public static void ImportAllJSONInDirectoryToGraphFromDialog() {
            var folderSelected = 
                EditorUtility.OpenFolderPanel("Select folder contains JSON files to import", Application.dataPath + "..", "");
            if(string.IsNullOrEmpty(folderSelected)) {
                return;
            }

            var newAssetFolder = CreateFolderForImportedAssets ();

            var filePaths = FileUtility.GetAllFilePathsInFolder (folderSelected);
            foreach (var path in filePaths) {
                var ext = Path.GetExtension (path).ToLower ();
                if (ext != ".json") {
                    continue;
                }
                var jsonContent = File.ReadAllText (path, System.Text.Encoding.UTF8);
                var name = Path.GetFileNameWithoutExtension (path);

                var graph = ScriptableObject.CreateInstance<Model.ConfigGraph>();
                EditorJsonUtility.FromJsonOverwrite (jsonContent, graph);
                var graphPath = FileUtility.PathCombine(newAssetFolder, $"{name}.asset");
                AssetDatabase.CreateAsset (graph, graphPath);
            }
        }

        private static string CreateFolderForImportedAssets() {
            var t = DateTime.Now;
            var folderName =
                $"ImportedGraphs_{t.Year:D4}-{t.Month:D2}_{t.Day:D2}_{t.Hour:D2}{t.Minute:D2}{t.Second:D2}";

            AssetDatabase.CreateFolder ("Assets", folderName);

            return $"Assets/{folderName}";
        }
	}
}
