using System.Collections.Generic;
using System.Linq;

namespace Game.UI
{
    public class M_WindowData
    {

        public Dictionary<string, object> DataMap { get; private set; }
        private M_WindowData()
        {
            this.DataMap = new Dictionary<string, object>();
        }

        static public M_WindowData Create()
        {
            return new M_WindowData();
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
        public void MergeData(M_WindowData data)
        {
            if(data!= null)
            foreach (var d in  data.DataMap)
            {
                this.DataMap[d.Key] = d.Value;
            }
        }
    }
}