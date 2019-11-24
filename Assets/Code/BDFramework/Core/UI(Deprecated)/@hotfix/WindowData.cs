using System;
using System.Collections.Generic;
using System.Linq;

namespace BDFramework.UI
{
    [Obsolete("please use new uiframe: uflux.")]
    public class WindowData
    {

        public string Name { get; private set; }
        public Dictionary<string, object> DataMap { get; private set; }
        private WindowData(string name)
        {
            this.Name = name;
            this.DataMap = new Dictionary<string, object>();
        }

        static public WindowData Create(string name)
        {
            return new WindowData(name);
        }

        public void AddData(string name , object value)
        {
            this.DataMap[name] = value;
        }

        public T GetData<T>(string name)
        {
            return (T) this.DataMap[name];
        }
        public void AddEvnet(string name)
        {
            this.DataMap[name] = null;
        }

        /// <summary>
        /// 合并数据
        /// </summary>
        /// <param name="data"></param>
        public void MergeData(WindowData data)
        {
            if(data!= null)
            foreach (var d in  data.DataMap)
            {
                this.DataMap[d.Key] = d.Value;
            }
        }
    }
}