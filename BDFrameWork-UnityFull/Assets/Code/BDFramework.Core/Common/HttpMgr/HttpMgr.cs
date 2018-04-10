using System;
using System.Collections.Generic;
using System.Text;

namespace BDFramework.Http
{
    public class HttpMgr
    {
        private HttpMgr()
        {
            layerMap = new Dictionary<int, HttpLayer>();
        }
        static private HttpMgr i;

        static public HttpMgr I
        {
            get
            {
                if (i == null)
                {
                    i = new HttpMgr();
                }
                return i;
            }
        }


        private Dictionary<int , HttpLayer> layerMap;

        public  HttpLayer GetLayer( int layerid)
        {

            HttpLayer layer = null;

            this.layerMap.TryGetValue(layerid, out layer);
            if (layer == null)
            {
                layer = new HttpLayer();
                layerMap[layerid] = layer;
            }
            
            return layer;
        }

    }
}
