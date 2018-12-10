using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
namespace Game.Mgr
{
    public class M_ClassData
    {
        public M_ManagerAtrribute Attribute;
        public Type Type;
    }
    public  interface M_IMgr
    {
        
        void Init();
        void Start();
        void Update();
        void CheckType(Type type);
        T2 CreateInstance<T2>(string typeName , params object[] args)  where T2 : class;
        M_ClassData GetCalssData(string typeName);
    }
}
