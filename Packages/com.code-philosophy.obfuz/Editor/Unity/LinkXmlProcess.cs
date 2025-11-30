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

﻿using Obfuz.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;
using FileUtil = Obfuz.Utils.FileUtil;

namespace Obfuz.Unity
{
    public class LinkXmlProcess : IUnityLinkerProcessor
    {
        public int callbackOrder => ObfuzSettings.Instance.buildPipelineSettings.linkXmlProcessCallbackOrder;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            return GenerateAdditionalLinkXmlFile(data.target);
        }

#if !UNITY_2021_2_OR_NEWER

        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {

        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {

        }
#endif

        public static string GenerateAdditionalLinkXmlFile(BuildTarget target)
        {
            ObfuzSettings settings = ObfuzSettings.Instance;
            if (!settings.buildPipelineSettings.enable)
            {
                Debug.Log("Obfuscation is disabled. Skipping link.xml generation.");
                return null;
            }
            string symbolMappingFile = settings.symbolObfusSettings.GetSymbolMappingFile();
            if (!File.Exists(symbolMappingFile))
            {
                Debug.LogWarning($"Symbol mapping file not found: {symbolMappingFile}. Skipping link.xml generation.");
                return null;
            }
            string linkXmlPath = settings.GetObfuscatedLinkXmlPath(target);
            FileUtil.CreateParentDir(linkXmlPath);

            var writer = System.Xml.XmlWriter.Create(linkXmlPath,
                new System.Xml.XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true });
            try
            {
                var symbolMapping = new LiteSymbolMappingReader(symbolMappingFile);
                string[] linkGuids = AssetDatabase.FindAssets("t:TextAsset");
                var linkXmlPaths = linkGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(f => Path.GetFileName(f) == "link.xml")
                    .ToArray();

                var assembliesToObfuscated = new HashSet<string>(settings.assemblySettings.GetAssembliesToObfuscate());

                writer.WriteStartDocument();
                writer.WriteStartElement("linker");

                // Preserve Obfuz.Runtime assembly
                writer.WriteStartElement("assembly");
                writer.WriteAttributeString("fullname", "Obfuz.Runtime");
                writer.WriteAttributeString("preserve", "all");
                writer.WriteEndElement();

                foreach (string linkPath in linkXmlPaths)
                {
                    TransformLinkXml(linkPath, symbolMapping, assembliesToObfuscated, writer);
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            finally
            {
                writer.Close();
            }
            Debug.Log($"LinkXmlProcess write {Path.GetFullPath(linkXmlPath)}");
            return Path.GetFullPath(linkXmlPath);
        }

        private static void TransformLinkXml(string xmlFile, LiteSymbolMappingReader symbolMapping, HashSet<string> assembliesToObfuscated, XmlWriter writer)
        {
            Debug.Log($"LinkXmlProcess transform link.xml:{xmlFile}");
            var doc = new XmlDocument();
            doc.Load(xmlFile);
            var root = doc.DocumentElement;
            foreach (XmlNode assNode in root.ChildNodes)
            {
                if (!(assNode is XmlElement assElement))
                {
                    continue;
                }
                if (assElement.Name == "assembly")
                {
                    string assemblyName = assElement.GetAttribute("fullname");
                    if (string.IsNullOrEmpty(assemblyName))
                    {
                        throw new Exception($"Invalid node name: {assElement.Name}. attribute 'fullname' missing.");
                    }
                    if (!assembliesToObfuscated.Contains(assemblyName))
                    {
                        continue; // Skip assemblies that are not to be obfuscated
                    }
                    writer.WriteStartElement("assembly");
                    writer.WriteAttributeString("fullname", assemblyName);
                    if (assElement.HasAttribute("preserve"))
                    {
                        writer.WriteAttributeString("preserve", assElement.GetAttribute("preserve"));
                    }

                    foreach (XmlNode typeNode in assElement.ChildNodes)
                    {
                        if (typeNode is XmlElement typeElement)
                        {
                            if (typeElement.Name == "type")
                            {
                                string typeName = typeElement.GetAttribute("fullname");
                                if (string.IsNullOrEmpty(typeName))
                                {
                                    throw new Exception($"Invalid node name: {typeElement.Name}. attribute 'fullname' missing.");
                                }
                                if (!symbolMapping.TryGetNewTypeName(assemblyName, typeName, out string newTypeName))
                                {
                                    continue;
                                }

                                writer.WriteStartElement("type");
                                writer.WriteAttributeString("fullname", newTypeName);
                                if (typeElement.HasAttribute("preserve"))
                                {
                                    writer.WriteAttributeString("preserve", typeElement.GetAttribute("preserve"));
                                }
                                writer.WriteEndElement();
                            }
                        }
                    }

                    writer.WriteEndElement();
                }
            }
        }
    }
}
