using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR.Editor.Meta
{
    public abstract class AssemblyResolverBase : IAssemblyResolver
    {
        public string ResolveAssembly(string assemblyName, bool throwExIfNotFind)
        {
            if (TryResolveAssembly(assemblyName, out string assemblyPath))
            {
                return assemblyPath;
            }
            if (throwExIfNotFind)
            {
#if UNITY_2021_1_OR_NEWER && UNITY_IOS
                throw new Exception($"resolve assembly:{assemblyName} 失败! 请按照Install文档正确替换了UnityEditor.CoreModule.dll或者升级hybridclr_unity到2.0.1及更高版本");
#else
                throw new Exception($"resolve assembly:{assemblyName} 失败! 请参阅常见错误文档");
#endif
            }
            return null;
        }

        protected abstract bool TryResolveAssembly(string assemblyName, out string assemblyPath);
    }
}
