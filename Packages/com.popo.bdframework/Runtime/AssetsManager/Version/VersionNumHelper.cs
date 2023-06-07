namespace  BDFramework.Assets.VersionContrller
{
    /// <summary>
    /// 版本号帮助
    /// </summary>
    static public class VersionNumHelper
    {
        /// <summary>
        ///  对比,并添加一个版本号
        /// 大版本.迭代版本.自增版本 =》1.0.53
        /// 当大版本，迭代版本修改时,自增版本自动归零,否则默认add 1
        /// </summary>
        static public string AddVersionNum(string lastVersionNum, string newVersionNum, int add = 1)
        {
           var (newBigNum, newSmallNum, newAdditiveNum) = ParseVersion(newVersionNum);
           return AddVersionNum(lastVersionNum, newBigNum, newSmallNum, newAdditiveNum, add);
        }

        /// <summary>
        /// 添加一个版本号
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
            //版本号赋值
            var (lastBigNum, lastSmallNum, lastAdditiveNum) = ParseVersion(lastVersionNum);
            var retBigNum = bigNum > lastBigNum ? bigNum : lastBigNum;
            var retSmallNum = smallNum > lastSmallNum ? smallNum : lastSmallNum;
            int retAdditiveNum = 0;
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


        /// <summary>
        /// 解析版本号
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        static (int, int, int) ParseVersion(string version)
        {
            int bigNum = 0;
            int smallNum = 0;
            int additiveNum = 0;
            //版本号解析，格式要求 1.0.53
            var ints = version.Split('.');
            if(ints.Length>0)
            int.TryParse(ints[0], out bigNum);
            if(ints.Length>1)
            int.TryParse(ints[1], out smallNum);
            if(ints.Length>2)
            int.TryParse(ints[2], out additiveNum);

            return (bigNum, smallNum, additiveNum);
        }

        /// <summary>
        /// 对比版本号,想同等于true
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public bool Compare(string a, string b)
        {
            //a==b  true
            var (ab,@as,aa) = ParseVersion(a);
            var (bb, bs, ba) = ParseVersion(b);
            if(ab>=bb && @as>=bs && aa>=ba)
            {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 大于或者等于>=
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public bool GT(string a, string b)
        {
            //a>=b  true
            //a<b  false
            var (ab,@as,aa) = ParseVersion(a);
            var (bb, bs, ba) = ParseVersion(b);
            if(ab>bb)
            {
                return true;
            }
            else if(ab == bb &&@as > bs)
            {
                return true;
            }
            else if(ab ==bb && @as == bs && aa >= ba)
            {
                return true;
            }

            return false;
        }
    }
}