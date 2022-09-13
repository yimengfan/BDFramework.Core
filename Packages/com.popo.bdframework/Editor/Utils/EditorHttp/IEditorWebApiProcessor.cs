using System.Net;
using System.Threading.Tasks;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// Editor下webapi接口
    /// </summary>
    public interface IEditorWebApiProcessor
    {
        /// <summary>
        /// 协议名，
        /// 如设置为 test ，则通过127.0.0.1:9999/test访问
        /// </summary>
        string WebApiName { get; }

        /// <summary>
        /// 协议执行
        /// </summary>
        /// <param name="apiParams">Get参数返回</param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        Task<EditorHttpResonseData> WebAPIProcessor(string apiParams, HttpListenerContext ctx);
    }
}
