using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDFramework.Mgr
{

    public class MgrBase<T>:IMgr  where T: IMgr, new()
    {

        static T i;
        static public T Inst
        {
            get
            {
                if (i == null)
                {
                    i = new T();
                }
                return i;
            }
        }
        
        protected MgrBase ()
        {
            this.ClassDataMap = new Dictionary<string, ClassData>();
        }
        
        protected Dictionary<string, ClassData> ClassDataMap
        {
            get;
            set;
        }

        virtual  public void CheckType(Type type)
        {
        }

        public ClassData GetCalssData(string typeName)
        {
            ClassData classData = null;
            this.ClassDataMap.TryGetValue(typeName, out classData);
            return classData;
        }

        public void SaveAttribute(string name, ClassData data)
        {
            this.ClassDataMap[name] = data;
        }

        public T2 GetTypeInst<T2>(string typeName) where T2 : class
        {
            var type = GetCalssData(typeName).Type;
            if (type != null)
            {
                var o = Activator.CreateInstance(type);
                return o as T2;
            }
            else
            {
                return null;
            }
        }


        virtual  public void Awake()
        {}
        virtual public void Update()
        {
            
        }
    }
}
