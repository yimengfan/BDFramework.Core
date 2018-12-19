using System.Collections.Generic;
using System.Linq;

namespace BDFramework.UI
{
    public class WindowData
    {

        public Dictionary<string, object> DataMap { get; private set; }
        private WindowData()
        {
            this.DataMap = new Dictionary<string, object>();
        }

        static public WindowData Create()
        {
            return new WindowData();
        }

        public void AddData(string name , object value)
        {
            this.DataMap[name] = value;
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