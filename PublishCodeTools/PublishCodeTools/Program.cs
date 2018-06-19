using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PublishCodeTools
{
    class Program
    {
        static void Main(string[] args)
        {
            //1.先复制目录
            CopyCodeTools.Exec();
            //2.修改csproj 并编译
            Console.WriteLine("------------拷贝完毕---------------");
            Console.ReadLine();
        }
    }
}
