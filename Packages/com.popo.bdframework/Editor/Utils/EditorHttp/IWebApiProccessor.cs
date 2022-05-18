using System.Net;
using System.Threading.Tasks;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// IWebApiProccesor接口
    /// </summary>
    public interface IWebApiProccessor
    {
        string WebApiName { get; }
        Task<EditorHttpResonseData> WebAPIProccessor(string apiParams, HttpListenerResponse response);
    }
}
