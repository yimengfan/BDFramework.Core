using System.Collections.Generic;
using System.Linq;
using BDFramework.UI;
using DG.Tweening.Plugins.Core.PathCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BDFramework.UI.UIWidget
{
    public class Widget_Paginator
    {
        public delegate void SelectPageDelegate(string name);
       // public delegate 
        public class Page
        {
            public Sprite NormalSprite;
            public Sprite PressSprite;
            public Button Button;
            public SubWindow Window;
            public SelectPageDelegate OnSelectPage;
            //
            public void SetClick()
            {
                this.Button.GetComponent<Image>().sprite = this.PressSprite;
                Window.Open();
                this.Button.enabled = false;
            }

            public void SetNormal()
            {
                this.Button.GetComponent<Image>().sprite = this.NormalSprite;
                Window.Close();
                this.Button.enabled = true;
            }
        }



     

        private string curSelectPage;
        //
        public Dictionary<string, Page>  PageDictionary = new Dictionary<string, Page>();
        public void AddPage(string name, Page page)
        {
            PageDictionary[name] = page;
            page.Window.Close();
            //
            page.Button.onClick.AddListener(() =>
            {
                if (curSelectPage == name)
                {
                    Debug.Log("当前处于该状态:" +  curSelectPage);
                }
                //恢复上个page
                var lastPage = PageDictionary[curSelectPage];
                lastPage.Window.Close();
                lastPage.SetNormal();
                //打开当前的window
                curSelectPage = name;
                var curPage = PageDictionary[name];
                curPage.SetClick();
               // curPage.Window.Open();
                //回调
                if (curPage.OnSelectPage != null)
                {
                    curPage.OnSelectPage(name);
                }
            });
             
        }

        public void RemovePage(string name)
        {
            PageDictionary.Remove(name);
        }

        public void Reset()
        {
            var key = PageDictionary.Keys.ToArray()[0];
            curSelectPage = key;
            //
            var page = PageDictionary[key];
            page.SetClick();
            page.Window.Transform.gameObject.SetActive(true);
            //page.Window.Open();
        }
    }
}