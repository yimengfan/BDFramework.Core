namespace BDFramework.UFlux.WindowStatus
{
    /// <summary>
    /// 窗口打开
    /// </summary>
    public class OnWindowOpen
    {
        public int WindowIdx { get; set; }
        
        public OnWindowOpen(int windowIdx)
        {
            this.WindowIdx = windowIdx;
        }
    }

    /// <summary>
    /// 窗口关闭
    /// </summary>
    public class OnWindowClose
    {
        public int WindowIdx { get; set; }
        
        public OnWindowClose(int windowIdx)
        {
            this.WindowIdx = windowIdx;
        }
    }

    /// <summary>
    /// 窗口重新激活
    /// 被遮挡=>重新激活
    /// </summary>
    public class OnWindowFocus
    {
        public int WindowIdx { get; set; }
        
        public OnWindowFocus(int windowIdx)
        {
            this.WindowIdx = windowIdx;
        }
    }
    
    /// <summary>
    /// 窗口丢失焦点
    /// </summary>
    public class OnWindowBlur
    {
        public int WindowIdx { get; set; }
        
        public OnWindowBlur(int windowIdx)
        {
            this.WindowIdx = windowIdx;
        }
    }
}