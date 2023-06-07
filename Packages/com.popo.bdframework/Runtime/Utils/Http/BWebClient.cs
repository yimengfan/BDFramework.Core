using System;
using System.Net;
using System.Threading.Tasks;

namespace BDFramework.Core.Tools.Http
{
    /// <summary>
    /// 封装过的WebClient
    /// </summary>
    public class BWebClient : WebClient
    {

        /// <summary>
        /// 带重试的下载
        /// </summary>
        /// <param name="address"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> DownloadStringTaskAsync(string address ,int retryCount)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                  var task =  await this.DownloadStringTaskAsync(address);
                  return task;
                }
                catch (Exception e)
                {
                    if (i == retryCount - 1)
                    {
                        throw e;
                    }
                }
            }

            return null;
        }
    }
}