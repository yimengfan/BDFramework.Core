namespace Editor.EditorPipeline.PublishPipeline
{
    /// <summary>
    /// 版本号帮助
    /// </summary>
    static public class VersionNumHelper
    {
        /// <summary>
        ///  添加一个版本号
        /// 大版本.迭代版本.自增版本 =》1.0.53
        /// 当大版本，迭代版本修改时,自增版本自动归零,否则默认add 1
        /// </summary>
        static public string AddVersionNum(string lastVersionNum, string newVersionNum, int add = 1)
        {
            int newBigNum = 0;
            int newSmallNum = 0;
            int newAdditiveNum = 0;

            //版本号解析，格式要求 1.0.53
            var ints = newVersionNum.Split('.');
            int.TryParse(ints[0], out newBigNum);
            int.TryParse(ints[1], out newSmallNum);
            int.TryParse(ints[2], out newAdditiveNum);

            //调用既有函数
            return AddVersionNum(lastVersionNum, newBigNum, newSmallNum, newAdditiveNum, add);
        }

        /// <summary>
        ///  添加一个版本号
        /// 大版本.迭代版本.自增版本 =》1.0.53
        /// 当大版本，迭代版本修改时,自增版本自动归零,否则默认add 1
        /// </summary>
        /// <param name="lastVersionNum"></param>
        /// <param name="bigNum">设置大版本号</param>
        /// <param name="smallNum">设置小版本号</param>
        /// <param name="additiveNum">自增版本号</param>
        /// <param name="add">增加数量</param>
        /// <returns></returns>
        static public string AddVersionNum(string lastVersionNum, int bigNum = 0, int smallNum = 0, int additiveNum = 0, int add = 1)
        {
            int lastBigNum = 0;
            int lastSmallNum = 0;
            int lastAdditiveNum = 0;

            //版本号解析，格式要求 1.0.53
            var ints = lastVersionNum.Split('.');
            int.TryParse(ints[0], out lastBigNum);
            int.TryParse(ints[1], out lastSmallNum);
            int.TryParse(ints[2], out lastAdditiveNum);
            //版本号赋值
            int retBigNum = 0;
            int retSmallNum = 0;
            int retAdditiveNum = 0;

            retBigNum = bigNum > lastBigNum ? bigNum : lastBigNum;
            retSmallNum = smallNum > lastSmallNum ? smallNum : lastSmallNum;

            //自增版本号赋值
            //当大版本，迭代版本修改时,自增版本自动归零,否则默认add 1
            if (bigNum == lastBigNum && smallNum == lastSmallNum)
            {
                if (additiveNum > lastAdditiveNum)
                {
                    retAdditiveNum = additiveNum;
                }
                else
                {
                    retAdditiveNum = lastAdditiveNum + add;
                }
            }
            else
            {
                retAdditiveNum = 0;
            }

            //版本号自增
            return $"{retBigNum}.{retSmallNum}.{retAdditiveNum}";
        }
    }
}
