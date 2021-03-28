using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
namespace BDFramework.Mgr
{
    public class ClassData
    {
        public ManagerAtrribute Attribute;
        public Type Type;
    }
    public  interface IMgr
    {
        
        void Init();
        void Start();
        void CheckType(Type type, IEnumerable<Attribute> attributes);
        T2 CreateInstance<T2>(int tag , params object[] args)  where T2 : class;
        ClassData GetClassData(int typeName);
    }
}
