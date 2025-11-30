using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{

    public class AssemblySorter
    {
        class Node
        {
            public string Name;
            public List<Node> Dependencies = new List<Node>();

            public Node(string name)
            {
                Name = name;
            }
        }

        class TopologicalSorter
        {

            public static List<Node> Sort(List<Node> nodes)
            {
                List<Node> sorted = new List<Node>();
                HashSet<Node> visited = new HashSet<Node>();
                HashSet<Node> tempMarks = new HashSet<Node>();

                foreach (var node in nodes)
                {
                    if (!visited.Contains(node))
                    {
                        Visit(node, visited, tempMarks, sorted);
                    }
                }
                return sorted;
            }

            private static void Visit(Node node, HashSet<Node> visited, HashSet<Node> tempMarks, List<Node> sorted)
            {
                if (tempMarks.Contains(node))
                {
                    throw new Exception("Detected cyclic dependency!");
                }

                if (!visited.Contains(node))
                {
                    tempMarks.Add(node);
                    foreach (var dependency in node.Dependencies)
                    {
                        Visit(dependency, visited, tempMarks, sorted);
                    }
                    tempMarks.Remove(node);
                    visited.Add(node);
                    sorted.Add(node);
                }
            }
        }

        private static List<string> SortAssemblyByReferenceOrder(IEnumerable<string> assemblies, Dictionary<string, HashSet<string>> refs)
        {
            var nodes = new List<Node>();
            var nodeMap = new Dictionary<string, Node>();
            foreach (var assembly in assemblies)
            {
                var node = new Node(assembly);
                nodes.Add(node);
                nodeMap.Add(assembly, node);
            }
            foreach (var assembly in assemblies)
            {
                var node = nodeMap[assembly];
                foreach (var refAssembly in refs[assembly])
                {
                    node.Dependencies.Add(nodeMap[refAssembly]);
                }
            }
            var sortedNodes = TopologicalSorter.Sort(nodes);
            return sortedNodes.Select(node => node.Name).ToList();
        }

        public static List<string> SortAssemblyByReferenceOrder(IEnumerable<string> assemblies, IAssemblyResolver assemblyResolver)
        {
            var assCache = new AssemblyCache(assemblyResolver);
            var assRefAssemblies = new Dictionary<string, HashSet<string>>();
            foreach (var assName in assemblies)
            {
                var refAssemblies = new HashSet<string>();
                var mod = assCache.LoadModule(assName, false);
                foreach (var refAss in mod.GetAssemblyRefs())
                {
                    if (assemblies.Contains(refAss.Name.ToString()))
                    {
                        refAssemblies.Add(refAss.Name.ToString());
                    }
                }
                assRefAssemblies.Add(assName, refAssemblies);
            }
            return SortAssemblyByReferenceOrder(assemblies, assRefAssemblies);
        }
    }
}
