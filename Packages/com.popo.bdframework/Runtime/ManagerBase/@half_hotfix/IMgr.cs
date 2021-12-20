using System;

namespace BDFramework.Mgr
{
    public class ClassData
    {
        public ManagerAttribute Attribute;
        public Type Type;
    }
    public  interface IMgr
    {
        
        void Init();
        void Start();
        void CheckType(Type type, ManagerAttribute attribute);
        T2 CreateInstance<T2>(int tag , params object[] args)  where T2 : class;
        ClassData GetClassData(int typeName);
    }
}
