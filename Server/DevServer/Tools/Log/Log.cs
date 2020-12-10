using System;

namespace DevServer.Tools.Log
{
    static public class Log
    {

        public static void WriteLine(string name , ConsoleColor color =  ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(name);
        }
    }
}