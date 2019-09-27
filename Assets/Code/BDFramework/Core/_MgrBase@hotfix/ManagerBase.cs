using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDFramework.Mgr
{
    public class ManagerAtrribute : Attribute
    {
        public string Tag { get; private set; }
        public ManagerAtrribute(string Id)
        {
            this.Tag = Id;
        }
    }
    
    public class ManagerBase<T,V>:IMgr  where T: IMgr, new() 
                                        where V: ManagerAtrribute
    {

        static private T i;

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
        
        protected ManagerBase ()
        {
            this.ClassDataMap = new Dictionary<string, ClassData>();
        }
        
        protected Dictionary<string, ClassData> ClassDataMap
        {
            get;
            set;
        }

        private Type vtype = null;
        virtual  public void CheckType(Type type)
        {
            if (vtype == null)
            {
                vtype = typeof(V);
            }
            var attrs = type.GetCustomAttributes(vtype, false);
            if (attrs.Length > 0)
            {
                var attr = attrs[0];
                if (attr is V)
                {   
                    var _attr = (V)attr;
                    SaveAttribute(_attr.Tag, new ClassData() { Attribute = _attr, Type = type });
                }             
            }
        }




        virtual public void Init()
        {
            
        }

        
        virtual public void Start()
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

        public T2 CreateInstance<T2>(string typeName , params object[] args) where T2 : class
        {
            var cd = GetCalssData(typeName);
            if (cd == null)
            {
                BDebug.LogError("没有找到:" + typeName + " -"  + typeof(T2).Name);
                return null;
            }
            if (cd.Type != null)
            {
                if (args.Length == 0)
                {
                    return Activator.CreateInstance(cd.Type) as T2;
                }
                else
                {
                    return Activator.CreateInstance(cd.Type,args) as T2;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
