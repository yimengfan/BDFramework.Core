using System.Collections.Generic;
using System.Linq;

namespace BDFramework.UI
{
    public class WinData
    {

        public Dictionary<string, object> DataMap { get; private set; }
        private WinData()
        {
            this.DataMap = new Dictionary<string, object>();
        }

        static public WinData Create()
        {
            return new WinData();
        }

        public void AddData(string name , object value)
        {
            this.DataMap[name] = value;
        }

        public void AddEvnet(string name)
        {
            this.DataMap[name] = null;
        }
    }
}