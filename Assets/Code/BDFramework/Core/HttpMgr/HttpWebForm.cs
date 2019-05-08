using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace BDFramework.Http
{
    /// <summary>
    /// http post请求客户端
    /// </summary>
    public class HttpWebForm
    {
        #region //字段

        private  ArrayList bytesArray;

        private Encoding encoding = Encoding.UTF8;

        private string boundary = String.Empty;

        #endregion

        #region //构造方法

        public HttpWebForm()
        {
            bytesArray = new ArrayList();
            string flag = DateTime.Now.Ticks.ToString("x");
            boundary = "---------------------------" + flag;
        }

        #endregion

        #region //方法

        /// <summary>
        /// 合并请求数据
        /// </summary>
        /// <returns></returns>
        private byte[] MergeContent()
        {
            int length = 0;
            int readLength = 0;
            string endBoundary = "--"+ boundary + "--\r\n";
            byte[] endBoundaryBytes = encoding.GetBytes(endBoundary);
            bytesArray.Add(endBoundaryBytes);
            foreach (byte[] b in bytesArray)
            {
                length += b.Length;
            }

            byte[] bytes = new byte[length];
            foreach (byte[] b in bytesArray)
            {
                b.CopyTo(bytes, readLength);
                readLength += b.Length;
            }

            return
                bytes;
        }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="requestUrl">请求url</param>
        /// <param name="responseText">响应</param>
        /// <returns></returns>
        async public Task Upload(String url)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);

            await webClient.UploadDataTaskAsync(url, MergeContent());
        }

        /// <summary>
        /// 设置表单数据字段
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="fieldValue">字段值</param>
        /// <returns></returns>
        public void AddFieldValue(String fieldName, String fieldValue)
        {
            string httpRow = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
            string httpRowData = String.Format(httpRow, fieldName, fieldValue);
            bytesArray.Add(encoding.GetBytes(httpRowData));
        }

        /// <summary>
        /// 设置表单文件数据
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="filename">字段值</param>
        /// <param name="contentType">内容内型</param>
        /// <param name="fileBytes">文件字节流</param>
        /// <returns></returns>
        public void AddFileValue(String fieldName, String filename, String contentType = "application/octet-stream")
        {
            string end = "\r\n";
            string httpRow = "--"
                          + boundary +
                          "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            //拼头部
            string httpRowData = String.Format(httpRow, fieldName, Path.GetFileName(filename), contentType);
            byte[] headerBytes = encoding.GetBytes(httpRowData);
            byte[] endBytes = encoding.GetBytes(end);
            //拼byte
            var fileBytes = File.ReadAllBytes(filename);
            byte[] fileDataBytes = new byte[headerBytes.Length + fileBytes.Length + endBytes.Length];
            headerBytes.CopyTo(fileDataBytes, 0);
            fileBytes.CopyTo(fileDataBytes, headerBytes.Length);
            endBytes.CopyTo(fileDataBytes, headerBytes.Length + fileBytes.Length);
            bytesArray.Add(fileDataBytes);
        }

        #endregion
    }
}