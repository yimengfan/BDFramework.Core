using System.Net;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// IWebApiProccesor接口
    /// </summary>
    public interface IWebApiProccessor
    {
        string WebApiName { get; }
        void WebAPIProccessor(string apiParams, HttpListenerResponse response);
    }
}
