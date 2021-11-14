using System.Diagnostics;

namespace BDFramework.Editor.AssetGraph.Node
{
    public class StopwatchTools
    {
        static private Stopwatch sw = new Stopwatch();

        static public void Begin()
        {
            sw.Restart();
        }


        static public void End(string title = "")
        {
            sw.Stop();

            UnityEngine.Debug.LogFormat("{0}耗时:{1}ms", title, sw.ElapsedMilliseconds);
        }
    }
}
