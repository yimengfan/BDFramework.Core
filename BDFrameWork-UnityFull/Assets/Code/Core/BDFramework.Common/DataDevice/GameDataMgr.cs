using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/// <summary>
/// 游戏数据管理
/// </summary>
public class GameDataMgr
{
    private GameDataMgr()
    {
    }

    private static GameDataMgr _instance = null;

    public static GameDataMgr I
    {
       get
        {
            if(_instance == null)
            {
                _instance = new GameDataMgr();
            }

            return _instance;
        }
    }
}

