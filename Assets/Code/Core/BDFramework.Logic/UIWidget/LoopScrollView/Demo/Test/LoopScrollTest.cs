using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class LoopScrollTest : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        var trans_scroll = GameObject.Instantiate( Resources.Load<GameObject>("VerticalScroll")).transform;

        trans_scroll.SetParent(GameObject.Find("Canvas").transform, false);
        var scroll = trans_scroll.GetComponent<LoopVerticalScrollRect>();

        //资源路径
        scroll.prefabSource.prefabName = "ScrollCell1";
        //对象池大小
        scroll.prefabSource.poolSize = 10;

        //item总数
        scroll.totalCount = 50;
        //item上限
        scroll.threshold = 100; 
        //
        scroll.OnCellInit += (idx, trans) =>
        {
            Debug.Log("load:" + idx);
            //trans 是传回来的Item 可以对其进行替换资源等操作。
        };

        //填充所有的fill
        scroll.RefillCells();

	}
}
