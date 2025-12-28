namespace  BDFramework.Assets.VersionContrller
{
    /// <summary>
    /// 版本号帮助
    /// </summary>
    static public class VersionNumHelper
    {
       public struct VersionNum
        {
           public int bigNum;
           public  int smallNum;
           public int additiveNum;

           public string ToString()
           {
               return $"{bigNum}.{smallNum}.{additiveNum}";
           }
        }
        /// <summary>
        ///  对比,并添加一个版本号
        /// 大版本.迭代版本.自增版本 =》1.0.53
        /// 当大版本，迭代版本修改时,自增版本自动归零,否则默认add 1
        /// </summary>
        static public string AddVersionNum(string lastVersionNum, string newVersionNum, int add = 1)
        {
           var  ver = ParseVersion(newVersionNum);
           return AddVersionNum(lastVersionNum, ver.bigNum, ver.smallNum, ver.additiveNum, add);
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
            var lastVer = ParseVersion(lastVersionNum);
            var retBigNum = bigNum > lastVer.bigNum ? bigNum : lastVer.bigNum;
            var retSmallNum = smallNum > lastVer.smallNum ? smallNum : lastVer.smallNum ;
            int retAdditiveNum = 0;
            //自增版本号赋值
            //当大版本，迭代版本修改时,自增版本自动归零,否则默认add 1
            if (bigNum == lastVer.bigNum && smallNum == lastVer.smallNum )
            {
                if (additiveNum > lastVer.additiveNum )
                {
                    retAdditiveNum = additiveNum;
                }
                else
                {
                    retAdditiveNum = lastVer.additiveNum + add;
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
        static public VersionNum ParseVersion(string version)
        {

            var ver = new VersionNum();
            //版本号解析，格式要求 1.0.53
            var ints = version.Split('.');
            if(ints.Length>0)
            int.TryParse(ints[0], out ver.bigNum);
            if(ints.Length>1)
            int.TryParse(ints[1], out ver.smallNum);
            if(ints.Length>2)
            int.TryParse(ints[2], out ver.additiveNum);

            return ver;
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
            var aver= ParseVersion(a);
            var bver = ParseVersion(b);
            if(aver.bigNum>=bver.bigNum && aver.smallNum>=bver.smallNum && aver.additiveNum>=bver.additiveNum)
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
            var aver = ParseVersion(a);
            var bver = ParseVersion(b);
            if(aver.bigNum>bver.bigNum)
            {
                return true;
            }
            else if(aver.bigNum == bver.bigNum &&aver.smallNum > bver.smallNum )
            {
                return true;
            }
            else if(aver.bigNum == bver.bigNum &&aver.smallNum == bver.smallNum  && aver.additiveNum >= bver.additiveNum)
            {
                return true;
            }

            return false;
        }
    }
}