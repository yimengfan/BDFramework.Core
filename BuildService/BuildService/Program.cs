using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
      
        static void Main(string[] args)
        {
            Console.WriteLine("Made By  B.道友");
            Console.WriteLine("=>[BDFramework]");
            Console.WriteLine("******************************编译服务启动******************************");
            //udp
            UDPClient udpmain  = new UDPClient();
            udpmain.Start();
            //build server
            ScriptBiuld_Service buildserver = new ScriptBiuld_Service();
            buildserver.BuildDll(new string[] { "d:/demo" },"d:/111.dll");

            Console.ReadKey();
        }
       
    }
}
