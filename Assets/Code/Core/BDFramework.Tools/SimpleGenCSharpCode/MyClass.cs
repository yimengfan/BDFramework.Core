using System.Collections.Generic;

namespace Code.Core.BDFramework.SimpleGenCSharpCode
{
    public class MyClass
    {
        private string CodeContent =@"
//工具生成代码,请勿删除标签，否则无法进行添加操作
//[namespace]
public class [ClassName]
{
   //------[Field end]------
   //------[Propties end]------
   //------[Method end]------
}
";

        public List<MyField> FieldsList;
        public List<MyMethod> MethodList;


        public string ToString()
        {

            return "";
        }
    }
}