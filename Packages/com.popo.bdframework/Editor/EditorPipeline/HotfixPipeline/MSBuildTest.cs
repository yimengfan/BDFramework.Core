//
// using System;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using System.Xml;
// using Microsoft.Build.Construction;
// using Microsoft.Build.Evaluation;
// using Microsoft.Build.Locator;
// using Microsoft.CodeAnalysis.MSBuild;
//
// // I use this so I don't get confused with the Roslyn Project type
// using MSBuildProject = Microsoft.Build.Evaluation.Project;
//
// namespace loadProject {
//     class Program {
//         static async Task Main(string[] args) {
//             MSBuildWorkspaceSetup();
//             // NOTE: we need to make sure we call MSBuildLocator.RegisterInstance
//             // before we ask the CLR to load any MSBuild types. Therefore we moved
//             // the code that uses MSBuild types to its own method (instead of being in
//             // Main) so the CLR is not forced to load them on startup.
//             await DoAnalysisAsync(args[0]);
//         }
//
//         private static async Task DoAnalysisAsync(string solutionPath) {
//              var workspace = MSBuildWorkspace.Create();
//
//             // Print message for WorkspaceFailed event to help diagnosing project load failures.
//             workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
//
//             Console.WriteLine($"Loading solution '{solutionPath}'");
//
//             // Attach progress reporter so we print projects as they are loaded.
//             var solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());
//             Console.WriteLine($"Finished loading solution '{solutionPath}'");
//
//             // We just select the first project as a demo
//             // you will want to use your own logic here
//             var project = solution.Projects.First();
//             
//             project.
//
//             // Now we use the MSBuild apis to load and evaluate our project file
//             using var xmlReader = XmlReader.Create(File.OpenRead(project.FilePath));
//             ProjectRootElement root = ProjectRootElement.Create(xmlReader, new ProjectCollection(), preserveFormatting: true);
//             MSBuildProject msbuildProject = new MSBuildProject(root);
//
//             // We can now ask any question about the properties or items in our project file
//             // and get the correct answer
//             string spaRootValue = msbuildProject.GetPropertyValue("SpaRoot");
//         }
//
//         private static void MSBuildWorkspaceSetup() {
//             // Attempt to set the version of MSBuild.
//             var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
//             var instance = visualStudioInstances.Length == 1
//                 // If there is only one instance of MSBuild on this machine, set that as the one to use.
//                 ? visualStudioInstances[0]
//                 // Handle selecting the version of MSBuild you want to use.
//                 : SelectVisualStudioInstance(visualStudioInstances);
//
//             Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");
//
//             // NOTE: Be sure to register an instance with the MSBuildLocator 
//             //       before calling MSBuildWorkspace.Create()
//             //       otherwise, MSBuildWorkspace won't MEF compose.
//             MSBuildLocator.RegisterInstance(instance);
//         }
//
//         private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances) {
//             Console.WriteLine("Multiple installs of MSBuild detected please select one:");
//             for (int i = 0; i < visualStudioInstances.Length; i++) {
//                 Console.WriteLine($"Instance {i + 1}");
//                 Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
//                 Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
//                 Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
//             }
//
//             while (true) {
//                 var userResponse = Console.ReadLine();
//                 if (int.TryParse(userResponse, out int instanceNumber) &&
//                     instanceNumber > 0 &&
//                     instanceNumber <= visualStudioInstances.Length) {
//                     return visualStudioInstances[instanceNumber - 1];
//                 }
//                 Console.WriteLine("Input not accepted, try again.");
//             }
//         }
//
//         private class ConsoleProgressReporter : IProgress<ProjectLoadProgress> {
//             public void Report(ProjectLoadProgress loadProgress) {
//                 var projectDisplay = Path.GetFileName(loadProgress.FilePath);
//                 if (loadProgress.TargetFramework != null) {
//                     projectDisplay += $" ({loadProgress.TargetFramework})";
//                 }
//
//                 Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
//             }
//         }
//     }
// }
//
//
// namespace BDFramework.Editor.WorkFollow
// {
//     public class MSBuildTest
//     {
//         
//     }
// }