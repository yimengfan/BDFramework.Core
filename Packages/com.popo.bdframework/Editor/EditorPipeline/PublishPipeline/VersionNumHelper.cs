namespace Editor.EditorPipeline.PublishPipeline
{
    /// <summary>
    /// 版本号帮助
    /// </summary>
    static public class VersionNumHelper
    {
        /// <summary>
        ///  添加一个版本号
        /// 大版本.迭代版本.自增版本 =》1.0.1
        /// </summary>
        /// <param name="lastVersionNum"></param>
        /// <param name="bigNum">设置大版本号</param>
        /// <param name="smallNum">设置小版本号</param>
        /// <param name="additiveNum">自增版本号</param>
        /// <param name="add">增加数量</param>
        /// <returns></returns>
        static public string AddVersionNum(string lastVersionNum, int bigNum = -1, int smallNum = -1, int additiveNum = -1, int add = 1)
        {
            int retBigNum = 0;
            int retSmallNum = 0;
            int retAdditiveNum = 0;

            //版本号解析
            var ints = lastVersionNum.Split('.');
            int.TryParse(ints[0], out retBigNum);
            int.TryParse(ints[1], out retSmallNum);
            int.TryParse(ints[2], out retAdditiveNum);
            //版本号赋值
            retBigNum = bigNum > retBigNum ? bigNum : retBigNum;
            retSmallNum = smallNum > retSmallNum ? smallNum : retSmallNum;

            if (additiveNum > retAdditiveNum)
            {
                retAdditiveNum = additiveNum;
            }
            else
            {
                retAdditiveNum += add;
            }
            //版本号自增
            
            
            return $"{retBigNum}.{retSmallNum}.{retAdditiveNum}";
        }
    }
}
