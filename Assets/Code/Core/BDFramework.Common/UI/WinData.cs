using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BDFramework.UI
{
    public class WinData : ADataDrive
    {

        private WinData()
        {
            
        }

        static public WinData Create()
        {
            return new WinData();
        }
        //获取数据的所有键
        public List<string> GetDataKeys()
        {
            return new List<string>(this.dataMap.Keys.ToArray()) ;
        }
    }
}