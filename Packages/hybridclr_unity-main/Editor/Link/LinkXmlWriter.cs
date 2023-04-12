using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Link
{
    internal class LinkXmlWriter
    {
        public void Write(string outputLinkXmlFile, HashSet<TypeRef> refTypes)
        {
            string parentDir = Directory.GetParent(outputLinkXmlFile).FullName;
            Directory.CreateDirectory(parentDir);
            var writer = System.Xml.XmlWriter.Create(outputLinkXmlFile,
                new System.Xml.XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true});

            writer.WriteStartDocument();
            writer.WriteStartElement("linker");

            var typesByAssembly = refTypes.GroupBy(t => t.DefinitionAssembly.Name.String).ToList();
            typesByAssembly.Sort((a, b) => a.Key.CompareTo(b.Key));

            foreach(var assembly in typesByAssembly)
            {
                writer.WriteStartElement("assembly");
                writer.WriteAttributeString("fullname", assembly.Key);
                List<TypeRef> assTypes = assembly.ToList();
                assTypes.Sort((a, b) => a.FullName.CompareTo(b.FullName));
                foreach(var type in assTypes)
                {
                    writer.WriteStartElement("type");
                    writer.WriteAttributeString("fullname", type.FullName);
                    writer.WriteAttributeString("preserve", "all");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }
    }
}
