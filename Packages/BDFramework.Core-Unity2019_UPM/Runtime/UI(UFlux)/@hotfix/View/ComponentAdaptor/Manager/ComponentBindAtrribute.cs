using System;
using BDFramework.Mgr;
using ILRuntime.CLR.TypeSystem;

namespace BDFramework.UFlux
{
    public class ComponentBindAttribute : ManagerAttribute
    {
        public Type Type { get; private set; }
        public ComponentBindAttribute(string typeName) : base(typeName)
        {
            Type type;
            //组件逻辑，
            if(!ILRuntimeHelper.UIComponentTypes.TryGetValue(typeName, out type))
            {
                //自定义绑定逻辑
                //限制typename的命名空间,增加查询速度
                var fullname =  "BDFramework.UFlux." + typeName;
                IType ilrtype;
                if (ILRuntimeHelper.AppDomain!=null&&
                    ILRuntimeHelper.AppDomain.LoadedTypes!=null && //这两个判断防止编辑器下报错
                    ILRuntimeHelper.AppDomain.LoadedTypes.TryGetValue(fullname, out ilrtype)) 
                {
                    this.Type = ilrtype.ReflectionType;
                }
                else
                {
                    BDebug.LogError("【UFlux】不存在ComponentBindAdaptor:" + fullname);
                }
            }

            this.Type = type;

        }
    }

}