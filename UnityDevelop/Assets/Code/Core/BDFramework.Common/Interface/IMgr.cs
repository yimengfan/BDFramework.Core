using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
namespace BDFramework.Mgr
{
    public class ClassData
    {
        public Attribute Attribute;
        public Type Type;
    }
    public  interface IMgr
    {
        void Awake();
        void Update();
        void CheckType(Type type);
        T GetTypeInst<T>(string typeName) where T : class;
        ClassData GetCalssData(string typeName);
    }
}
