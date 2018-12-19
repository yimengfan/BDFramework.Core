using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Mgr
{
    public class M_ManagerAtrribute : Attribute
    {
        public string Tag { get; private set; }
        public M_ManagerAtrribute(string Id)
        {
            this.Tag = Id;
        }
    }
    
    public class M_ManagerBase<T,V>:M_IMgr  where T: M_IMgr, new() 
                                        where V: M_ManagerAtrribute
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
        
        protected M_ManagerBase ()
        {
            this.ClassDataMap = new Dictionary<string, M_ClassData>();
        }
        
        protected Dictionary<string, M_ClassData> ClassDataMap
        {
            get;
            set;
        }

        virtual  public void CheckType(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(V), false);
            if (attrs.Length > 0)
            {
                var attr = attrs[0];
                if (attr is V)
                {   
                    var _attr = (V)attr;
                    SaveAttribute(_attr.Tag, new M_ClassData() { Attribute = _attr, Type = type });
                }             
            }
        }




        virtual public void Init()
        {
            
        }

        virtual public void Start()
        {
            
        }

        virtual public void Update()
        {
            
        }
        
        
        public M_ClassData GetCalssData(string typeName)
        {
            M_ClassData mClassData = null;
            this.ClassDataMap.TryGetValue(typeName, out mClassData);
            return mClassData;
        }

        public void SaveAttribute(string name, M_ClassData data)
        {
            this.ClassDataMap[name] = data;
        }

        public T2 CreateInstance<T2>(string typeName , params object[] args) where T2 : class
        {
            var type = GetCalssData(typeName).Type;
            if (type != null)
            {
                if (args.Length == 0)
                {
                    return Activator.CreateInstance(type) as T2;
                }
                else
                {
                    return Activator.CreateInstance(type,args) as T2;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
