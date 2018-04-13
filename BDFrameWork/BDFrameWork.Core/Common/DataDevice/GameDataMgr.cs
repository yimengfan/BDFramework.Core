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

    private static GameDataMgr instance = null;

    public static GameDataMgr Inst
    {
       get
        {
            if(instance == null)
            {
                instance = new GameDataMgr();
            }

            return instance;
        }
    }
}

