using UnityEngine;
using System.Collections;
using BDFramework;
using BDFramework.ResourceMgr;

public class Gun : MonoBehaviour
{
    /// <summary>
    /// 子弹路径
    /// </summary>
    public string bulletPath = "ObjectPool/Bullet";
    /// <summary>
    /// BResource的加载地址
    /// </summary>
    public AssetLoadPathType BAssetLoadPathType = AssetLoadPathType.Editor;
    void Start()
    {
        //加载接口初始化
        BResources.Init( BAssetLoadPathType);
        //预热
        BResources.WarmPool(bulletPath, 10);
        //测试删除
        this.StartCoroutine(TestDestory());
        //Notes
        // Make sure the prefab is inactive, or else it will run update before first use
    }

    void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            Vector3 pos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f);
            FireBullet(Camera.main.ScreenToWorldPoint(pos), Quaternion.identity);
        }
    }

    //Spawn pooled objects
    void FireBullet(Vector3 position, Quaternion rotation)
    {
        var bullet = BResources.LoadFormPool(bulletPath, position, rotation).GetComponent<Bullet>();

        //Notes:
        // bullet.gameObject.SetActive(true) is automatically called on spawn 
        // When done with the instance, you MUST release it!
        // If the number of objects in use exceeds the pool size, new objects will be created
    }


    IEnumerator TestDestory()
    {
        yield return new WaitForSeconds(10);
        BResources.DestroyPool(bulletPath);
    }
}
