using System.IO;
using System.Net;
using System.Threading.Tasks;
using BDFramework.Core.Tools;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// Assetbundle文件服务器
    /// </summary>
    public class WP_LocalABFileServer:IEditorWebApiProcessor
    {
        /// <summary>
        /// 监听""不需要api
        /// </summary>
        public string WebApiName { get; set; } = "Assetbundle";

        /// <summary>
        /// AB服务器
        /// </summary>
        /// <param name="apiParams"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public Task<EditorHttpResonseData> WebAPIProcessor(string apiParams, HttpListenerContext ctx)
        {
            string filePath = IPath.Combine( BApplication.DevOpsPublishAssetsPath, PublishPipelineTools.UPLOAD_FOLDER_SUFFIX,apiParams);
            if (!File.Exists(filePath))
            {
                ctx.Response.ContentLength64 = 0;
                ctx.Response.StatusCode = 400;
                ctx.Response.Abort();
            }
            else
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/octet-stream";
                FileStream fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                int byteLength = (int) fileStream.Length;
                byte[] fileBytes = new byte[byteLength];
                fileStream.Read(fileBytes, 0, byteLength);
                fileStream.Close();
                fileStream.Dispose();
                ctx.Response.ContentLength64 = byteLength;
                ctx.Response.OutputStream.Write(fileBytes, 0, byteLength);
                ctx.Response.OutputStream.Close();
            }

            //表示不需要后续处理
            return null;
        }
    }
}
