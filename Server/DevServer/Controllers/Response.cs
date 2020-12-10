namespace DevServer.Controllers
{
    public class Response
    {
        /// <summary>
        /// 返回码
        /// 0=失败
        /// 1=成功
        /// </summary>
        public int Code { get; private set; }
        /// <summary>
        /// 返回的消息
        /// </summary>
        public string Msg { get; private set; }
        /// <summary>
        /// 返回的内容
        /// </summary>
        public object Content { get; private set; }

        public void Fail(int code = 0, string msg = null)
        {
            this.Code = code;
            this.Msg  = msg;
        }

        public void Success(int code =1,string msg = null, object content = null)
        {
            this.Code    = code;
            this.Msg     = msg;
            this.Content = content;
        }
    }
}