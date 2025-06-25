using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HybridCLR.Editor.ReversePInvokeWrap
{
    public class Generator
    {
        public void Generate(List<ABIReversePInvokeMethodInfo> methods, string outputFile)
        {
            string template = File.ReadAllText(outputFile, Encoding.UTF8);
            var frr = new FileRegionReplace(template);
            var codes = new List<string>();

            int methodIndex = 0;
            var stubCodes = new List<string>();
            foreach(var methodInfo in methods)
            {
                MethodDesc method = methodInfo.Method;
                string paramDeclaringListWithoutMethodInfoStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()} __arg{p.Index}"));
                string paramNameListWithoutMethodInfoStr = string.Join(", ", method.ParamInfos.Select(p => $"__arg{p.Index}").Concat(new string[] { "method" }));
                string paramTypeListWithMethodInfoStr = string.Join(", ", method.ParamInfos.Select(p => $"{p.Type.GetTypeName()}").Concat(new string[] { "const MethodInfo*" }));
                string methodTypeDef = $"typedef {method.ReturnInfo.Type.GetTypeName()} (*Callback)({paramTypeListWithMethodInfoStr})";
                for (int i = 0; i < methodInfo.Count; i++, methodIndex++)
                {
                    codes.Add($@"
	{method.ReturnInfo.Type.GetTypeName()} __ReversePInvokeMethod_{methodIndex}({paramDeclaringListWithoutMethodInfoStr})
	{{
        const MethodInfo* method = MetadataModule::GetMethodInfoByReversePInvokeWrapperIndex({methodIndex});
        {methodTypeDef};
		{(method.ReturnInfo.IsVoid ? "" : "return ")}((Callback)(method->methodPointerCallByInterp))({paramNameListWithoutMethodInfoStr});
	}}
");
                    stubCodes.Add($"\t\t{{\"{method.Sig}\", (Il2CppMethodPointer)__ReversePInvokeMethod_{methodIndex}}},\n");
                }
                Debug.Log($"[ReversePInvokeWrap.Generator] method:{method.MethodDef} wrapperCount:{methodInfo.Count}");
            }

            codes.Add(@"
    ReversePInvokeMethodData g_reversePInvokeMethodStub[]
	{
");
            codes.AddRange(stubCodes);

            codes.Add(@"
		{nullptr, nullptr},
	};
");

            frr.Replace("CODE", string.Join("", codes));
            frr.Commit(outputFile);
        }
    }
}
