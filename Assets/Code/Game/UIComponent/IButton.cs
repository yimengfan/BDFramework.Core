using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UI
{
    /// <summary>
    /// 点击的接口
    /// </summary>
    public class IComponentOnClick
    {
        public delegate void OnClickDelegete();

        private OnClickDelegete onclick = null;

        public void AddListener(OnClickDelegete action)
        {
            onclick += action;
        }

        public void RemoveAllListeners()
        {
            onclick = null;
        }

        public void Invoke()
        {
            if (onclick != null)
            {
                onclick();
            }
        }
    }

    public class IButton : UIBehaviour,IPointerDownHandler 
#if !UNITY_EDITOR
         ,IPointerExitHandler ,IPointerUpHandler 
#else
         ,IPointerClickHandler
#endif
    {
        private Color normalColor = Color.white;
        public Color OnClickColor = new Color(195f / 255f, 195f / 255f, 195f / 255f);
        public IComponentOnClick onClick = new IComponentOnClick();
        private Image img;

        private void Awake()
        {
            img = this.GetComponent<Image>();
        }

        private bool lastIsdown = false;

        
        Vector2 startPos = Vector2.zero;
        public void OnPointerDown(PointerEventData eventData)
        {
//            string str = "------------pointer down----------\n";
//            str += eventData.lastPress == null ? "lastpress = null\n" : "lastpress = value\n";
//            str += eventData.pointerDrag == null ? "pointerDrag = null\n" : "pointerDrag = value\n";
//            str += eventData.rawPointerPress == null ? "rawPointerPress = null\n" : "rawPointerPress = value\n";
//            str += eventData.pointerEnter == null ? "pointerEnter = null\n" : "pointerEnter = value\n";
//            str += eventData.pointerPress == null ? "pointerPress = null\n" : "lastpress = value\n";     
//            Debug.Log(str);

            startPos = eventData.position;
            lastIsdown = true;
            if (img != null)
                img.color = this.OnClickColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            lastIsdown = false;
            if (img != null)
            {
                img.color = this.normalColor;
            }
            
            //触发drag 则取消点击事件
            if (Vector2.Distance(eventData.position , startPos) > 15)
            {
                return;
            }
            
            if (eventData.pointerDrag == null)
            {
                onClick.Invoke();
            }
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            onClick.Invoke();
            if (img != null)
            {
                img.color = this.normalColor;
            }
        }

    }
}