using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class PageListDemo : MonoBehaviour
{
    public PageList list;

    public class TestData
    {
        public int id;
        public string name;

        public TestData(int id)
        {
            this.id = id;
            this.name = "szc" + this.id;
        }
    }

    private List<TestData> datas;

    private void Start()
    {
        datas = new List<TestData>();
        for (int i = 100; i <= 200; i++)
        {
            TestData data = new TestData(i);
            datas.Add(data);
        }

        list.onItemUpdateAction = this.OnTestUpdate;
        list.Data(datas.Count, "UI/TpItem");
    }

    public void OnTestUpdate(int index, GameObject go)
    {
        TestData data = datas[index];
        var txt = go.transform.Find("txt_t").GetComponent<Text>();
        txt.text = "index = " + index;
        var btn = go.transform.Find("btn_b").GetComponent<Button>();
        btn.onClick.AddListener(() => { Debug.Log("I'm " + data.name); });
    }
}