using UnityEditor;

namespace BDFramework.Editor.Unity3dEx
{
    /// <summary>
    /// unity3d的editor扩展函数
    /// </summary>
    static public class Unity3dEditorEx
    {
        #region 宏处理
        /// <summary>
        /// 添加宏
        /// </summary>
        /// <param name="symbol"></param>
        public static void AddSymbols(BuildTargetGroup bt,string symbol)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bt);
            if (!symbols.Contains(symbol))
            {
                string str = "";
                if (!string.IsNullOrEmpty(symbols))
                {
                    if (!str.EndsWith(";"))
                    {
                        str = symbols + ";" + symbol;
                    }
                    else
                    {
                        str = symbols + symbol;
                    }
                }
                else
                {
                    str = symbol;
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(bt, str);
            }
        }

        /// <summary>
        /// 移除宏
        /// </summary>
        /// <param name="symbol"></param>
        public static void RemoveSymbols(BuildTargetGroup bt,string symbol)
        {
            //移除宏
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bt);
                if (symbols.Contains(symbol + ";"))
                {
                    symbols = symbols.Replace(symbol + ";", "");
                }
                else if (symbols.Contains(symbol))
                {
                    symbols = symbols.Replace(symbol, "");
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(bt, symbols);
        }

        #endregion
    }
}