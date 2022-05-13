using System.IO;
using System.Net;
using BDFramework.Core.Tools;

namespace BDFramework.Editor.Tools.EditorHttpServer
{
    /// <summary>
    /// Assetbundle文件服务器
    /// </summary>
    public class WP_LocalABFileServer:IWebApiProccessor
    {
        /// <summary>
        /// 监听""不需要api
        /// </summary>
        public string WebApiName { get; set; } = "Assetbundle";
        public void WebAPIProccessor(string apiParams, HttpListenerResponse response)
        {
            string filePath = IPath.Combine( BApplication.DevOpsPublishAssetsPath, PublishPipelineTools.UPLOAD_FOLDER_SUFFIX,apiParams);
            if (!File.Exists(filePath))
            {
                response.ContentLength64 = 0;
                response.StatusCode = 400;
                response.Abort();
            }
            else
            {
                response.StatusCode = 200;
                response.ContentType = "application/octet-stream";
                FileStream fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                int byteLength = (int) fileStream.Length;
                byte[] fileBytes = new byte[byteLength];
                fileStream.Read(fileBytes, 0, byteLength);
                fileStream.Close();
                fileStream.Dispose();
                response.ContentLength64 = byteLength;
                response.OutputStream.Write(fileBytes, 0, byteLength);
                response.OutputStream.Close();
            }
        }
    }
}
