using System;
using System.Collections.Generic;
using System.Text;

namespace BDFramework.Http
{
    /// <summary>
    /// http的管理器
    /// </summary>
    public class HttpMgr
    {
        private HttpMgr()
        {
            layerMap = new Dictionary<int, HttpLayer>();
        }
        static private HttpMgr i;

        static public HttpMgr Inst
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

        /// <summary>
        /// 以分层 队列形式进行http操作
        /// </summary>
        /// <param name="layerid"></param>
        /// <returns></returns>
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

        private List<HttpClient> httpClients = new List<HttpClient>();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>     
        public HttpClient GetFreeHttpClient(Dictionary<string ,string> heads =null)
        {

            HttpClient client = null;

            //寻找一个不在忙的client
            client = httpClients.Find((c) => c.IsBusy == false);
            if (client == null)
            {
                client =  new HttpClient();
            }
            //
            if (heads != null)
            {
                foreach (var item in heads)
                {
                    client.Headers[item.Key] = item.Value;
                }
            }
            httpClients.Add(client);
            return client;
        }

    }
}
