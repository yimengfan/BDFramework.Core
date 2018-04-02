using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BGameObjectPools
{
    public class GameObjectRecord
    {
        private GameObject source = null;
        List<int> unUseList = new List<int>();
        List<GameObject> gameobjList = new List<GameObject>();

        public GameObjectRecord(string name )
        {
            source = BResources.Load<GameObject>(name); 
        }
        //
        public void GetObject(Action<int,GameObject> callback)
        {
            if(unUseList.Count <= 0)
            {
                AddObject();
            }
            var id = unUseList[0];
            unUseList.RemoveAt(0);
            callback(id, gameobjList[id]);
        }
        //
        public void ReturnGameObject(int id)
        {
            var gameobj = gameobjList[id];
            gameobj.SetActive(false);
            //GameObject.Destroy(gameobj.GetComponent<EntityBase>());
          
            unUseList.Add(id);
        }
        //
        public void AddObject()
        {
            unUseList.Add(gameobjList.Count);
            gameobjList.Add(GameObject.Instantiate(source));
        }

        public void Clear()
        {
            foreach(var o in gameobjList)
            {
                GameObject.Destroy(o);
            }
        }
    }

    static private Dictionary<string, GameObjectRecord> gameObjPoolsMap = new Dictionary<string, GameObjectRecord>();

    /// <summary>
    /// 预先load
    /// </summary>
    /// <param name="name"></param>
    /// <param name="count"></param>
   static public void PreLoadFromPools(string name ,int count)
    {

        CheckRes(name);
        //
        var record = gameObjPoolsMap[name];
        for(int i = 0;i<count;i++)
        {
            record.AddObject();
        }

    }
    /// <summary>
    /// 从pool取出
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callback"></param>
   static public void  LoadFormPools(string name,Action<int ,GameObject> callback)
    {
        CheckRes(name);
        var record = gameObjPoolsMap[name];

        record.GetObject((int id, GameObject o) =>
        {
            if (id < 0) //拿到空，循环一次
            {
                PreLoadFromPools(name, 1);
                LoadFormPools(name, callback);
            }
            else
            {
                record.GetObject(callback);
            }
        });
    }

    /// <summary>
    /// 还给pools
    /// </summary>
    /// <param name="name"></param>
    /// <param name="id"></param>
  static  public void ReturnToPools(string name,int id)
    {

        CheckRes(name);
        var record = gameObjPoolsMap[name];
        record.ReturnGameObject(id);
    }

    /// <summary>
    /// 检测资源名字
    /// </summary>
    /// <param name="name"></param>
   static private void CheckRes(string name)
    {
        if (gameObjPoolsMap.ContainsKey(name) == false)
        {

            gameObjPoolsMap[name] = new GameObjectRecord(name);
        }

    }

   static public void Clear()
    {
        //卸载复制体
        foreach(var v in gameObjPoolsMap.Values)
        {
            v.Clear();
        }
        gameObjPoolsMap = new Dictionary<string, GameObjectRecord>();
        //卸载源数据
        foreach (var m in gameObjPoolsMap)
        {
            BResources.UnloadAsset(m.Key);
        }


    }
}

