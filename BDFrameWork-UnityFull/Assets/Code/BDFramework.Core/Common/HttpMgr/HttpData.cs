using System;
using UnityEngine;
namespace BDFramework.Net
{
    /// <summary>
    /// description：http post请求客户端
    /// </summary>
    public class HttpForm
    {

        public WWWForm data
        {
            get;
            private set;
        }
        public HttpForm()
        {
            data = new WWWForm();
        }
        /// <summary>
        /// 设置表单数据字段
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="fieldValue">字段值</param>
        /// <returns></returns>
        public void SetFieldByValue(String fieldName, String fieldValue)
        {
            data.AddField(fieldName, fieldValue);
        }

       
    }
}