using System.Collections.Generic;

namespace Code.Core.BDFramework.SimpleGenCSharpCode
{
    public class MyClass
    {
        private string CodeContent =@"
//工具生成代码,请勿删除标签，否则无法进行添加操作
[namespace]
//[Note]
public class [ClassName]
{
   //------[Field end]------
   //------[Propties end]------
   //------[Method end]------
}
";

        private List<MyField> FieldsList;
        private List<MyMethod> MethodList;
        private List<MyPropties> ProptiesList;
        private List<string> NamespaceList;

        public MyClass(string className)
        {
            this.FieldsList = new List<MyField>();
            this.MethodList = new List<MyMethod>();
            this.ProptiesList =  new List<MyPropties>();
            this.NamespaceList = new List<string>();
            //
            this.CodeContent =  this.CodeContent.Replace("[ClassName]", className);
        }

        public void AddNameSpace(params string[] names)
        {
            foreach (var n in names)
            {
                this.NamespaceList.Add(n);
            }
        }

        public void AddField(MyField f)
        {
            FieldsList.Add(f);
        }

        public void AddProties(MyPropties p)
        {
            ProptiesList.Add(p);
        }

        public void AddMethod(MyMethod m)
        {
           MethodList.Add(m);
        }
        
        //
        override public string ToString()
        {
            string fields = "";
            foreach (var f in FieldsList)
            {
                fields += (f.ToString() + "\n");
            }
            string propties = "";
            foreach (var p in ProptiesList)
            {
                propties += (p.ToString() + "\n");
            }
            string methods = "";
            foreach (var m in MethodList)
            {
                methods += (m.ToString() + "\n");
            }

            string namespaces = "";
            foreach (var n in NamespaceList)
            {
                namespaces += ("using " + n.ToString() + ";\n");
            }
            
            
            //
            this.CodeContent=  this.CodeContent.Replace("[namespace]", namespaces);
            
            var index = this.CodeContent.IndexOf("//------[Field end]------");
            this.CodeContent=   this.CodeContent.Insert( index, fields);
            
            index = this.CodeContent.IndexOf("//------[Propties end]------");
            this.CodeContent=   this.CodeContent.Insert( index, propties);
            
            index = this.CodeContent.IndexOf("//------[Method end]------");
            this.CodeContent=   this.CodeContent.Insert( index, methods);

            
            return this.CodeContent;
        }
    }
}