using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.Meta
{
    public class PathAssemblyResolver : AssemblyResolverBase
    {
        private readonly string[] _searchPaths;
        public PathAssemblyResolver(params string[] searchPaths)
        {
            _searchPaths = searchPaths;
        }

        protected override bool TryResolveAssembly(string assemblyName, out string assemblyPath)
        {
            foreach(var path in _searchPaths)
            {
                assemblyPath = Path.Combine(path, $"{assemblyName}.dll");
                if (File.Exists(assemblyPath))
                {
                    Debug.Log($"resolve {assemblyName} at {assemblyPath}");
                    return true;
                }
                assemblyPath = Path.Combine(path, $"{assemblyName}.dll.bytes");
                if (File.Exists(assemblyPath))
                {
                    Debug.Log($"resolve {assemblyName} at {assemblyPath}");
                    return true;
                }
            }
            assemblyPath = null;
            return false;
        }
    }
}
