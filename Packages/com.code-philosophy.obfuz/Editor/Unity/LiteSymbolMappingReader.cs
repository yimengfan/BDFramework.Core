// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.


using System.Collections.Generic;
using System.Xml;

namespace Obfuz.Unity
{

    public class LiteSymbolMappingReader
    {
        class TypeMappingInfo
        {
            public string oldFullName;
            public string newFullName;

            //public Dictionary<string, string> MethodMappings = new Dictionary<string, string>();
        }

        class AssemblyMappingInfo
        {
            public Dictionary<string, string> TypeMappings = new Dictionary<string, string>();
            public Dictionary<string, string> MethodMappings = new Dictionary<string, string>();
        }

        private readonly Dictionary<string, AssemblyMappingInfo> _assemblies = new Dictionary<string, AssemblyMappingInfo>();

        public LiteSymbolMappingReader(string mappingFile)
        {
            LoadXmlMappingFile(mappingFile);
        }

        private void LoadXmlMappingFile(string mappingFile)
        {
            var doc = new XmlDocument();
            doc.Load(mappingFile);
            var root = doc.DocumentElement;
            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }
                LoadAssemblyMapping(element);
            }
        }

        private void LoadAssemblyMapping(XmlElement ele)
        {
            if (ele.Name != "assembly")
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. Expected 'assembly'.");
            }
            string assName = ele.GetAttribute("name");
            if (string.IsNullOrEmpty(assName))
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. attribute 'name' missing.");
            }
            if (!_assemblies.TryGetValue(assName, out var assemblyMappingInfo))
            {
                assemblyMappingInfo = new AssemblyMappingInfo();
                _assemblies[assName] = assemblyMappingInfo;
            }

            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }
                if (element.Name == "type")
                {
                    LoadTypeMapping(element, assemblyMappingInfo);
                }
            }
        }

        private void LoadTypeMapping(XmlElement ele, AssemblyMappingInfo assemblyMappingInfo)
        {
            string oldFullName = ele.GetAttribute("fullName");
            string newFullName = ele.GetAttribute("newFullName");
            string status = ele.GetAttribute("status");
            if (status == "Renamed")
            {
                if (string.IsNullOrEmpty(oldFullName) || string.IsNullOrEmpty(newFullName))
                {
                    throw new System.Exception($"Invalid node name: {ele.Name}. attributes 'fullName' or 'newFullName' missing.");
                }
                assemblyMappingInfo.TypeMappings[oldFullName] = newFullName;
            }
            //foreach (XmlNode node in ele.ChildNodes)
            //{
            //    if (!(node is XmlElement c))
            //    {
            //        continue;
            //    }
            //    if (node.Name == "method")
            //    {
            //        LoadMethodMapping(c);
            //    }
            //}
        }

        public bool TryGetNewTypeName(string assemblyName, string oldFullName, out string newFullName)
        {
            newFullName = null;
            if (_assemblies.TryGetValue(assemblyName, out var assemblyMappingInfo))
            {
                if (assemblyMappingInfo.TypeMappings.TryGetValue(oldFullName, out newFullName))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
