using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 点击的接口
    /// </summary>
    public class IComponentOnClick
    {
        public delegate void OnClickDelegete();

        public delegate void OnClickDelegeteWhithData(PointerEventData data);

        private OnClickDelegete          onclick         = null;
        private OnClickDelegeteWhithData onclickWithData = null;

        public void AddListener(OnClickDelegete action)
        {
            onclick += action;
        }

        public void AddListener(OnClickDelegeteWhithData action)
        {
            onclickWithData += action;
        }

        public void RemoveAllListeners()
        {
            onclick         = null;
            onclickWithData = null;
        }

        public void Invoke(PointerEventData data = null)
        {
            if (onclick != null)
            {
                onclick();
            }

            if (onclickWithData != null)
            {
                onclickWithData(data);
            }
        }
    }

    /// <summary>
    /// 点击的接口
    /// </summary>
    public class IComponentOnLongTimeClick
    {
        public delegate void OnClickDelegete();

        public delegate void OnClickDelegeteWhithData(PointerEventData data);


        public class LongTimePressData
        {
            public float                    Time;
            public OnClickDelegete          onclick         = null;
            public OnClickDelegeteWhithData onclickWithData = null;

            public bool IsTrigger { get; private set; }

            public void Trigger(PointerEventData data = null)
            {
                onclick?.Invoke();
                onclickWithData?.Invoke(data);
                IsTrigger = true;
            }

            public void Reset()
            {
                IsTrigger = false;
            }
        }


        private List<LongTimePressData> longTimePressDataList = new List<LongTimePressData>();

        /// <summary>
        /// 事件数量
        /// 没有触发的
        /// </summary>
        public int EventNum
        {
            get
            {
                if (longTimePressDataList.Count > 0)
                {
                    return longTimePressDataList.Count - triggeredEventCount;
                }
                
                return 0;
            }
        }

        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="pressTime"></param>
        /// <param name="action"></param>
        public void AddListener(float pressTime, OnClickDelegete action)
        {
            LongTimePressData pressData = new LongTimePressData() { Time = pressTime, onclick = action, };
            this.longTimePressDataList.Add(pressData);
        }

        public void AddListener(float pressTime, OnClickDelegeteWhithData action)
        {
            LongTimePressData pressData = new LongTimePressData() { Time = pressTime, onclickWithData = action, };
            this.longTimePressDataList.Add(pressData);
        }

        public void RemoveAllListeners()
        {
            longTimePressDataList.Clear();
        }


        /// <summary>
        /// 已触发事件的数量
        /// </summary>
        private int triggeredEventCount = 0;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="time"></param>
        /// <param name="data"></param>
        /// <returns>是否触发长摁，触发则取消点击事件</returns>
        public bool Invoke(float time, PointerEventData data = null)
        {
            bool isTrigger = false;
            foreach (var pressData in longTimePressDataList)
            {
                if (pressData.IsTrigger && time >= pressData.Time)
                {
                    pressData.Trigger(data);
                    triggeredEventCount++;
                    isTrigger = true;
                }
            }
            return isTrigger;
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
           
            foreach (var pressData in this.longTimePressDataList)
            {
                pressData.Reset();
            }
            triggeredEventCount = 0;
        }
    }

    /// <summary>
    /// CopygonCollider 点击的接口
    /// </summary>
    public class IComponentCopygonCollider
    {
        public delegate void OnColliderDelegete();

        private OnColliderDelegete oncollider = null;

        public void AddListener(OnColliderDelegete action)
        {
            oncollider += action;
        }

        public void RemoveAllListeners()
        {
            if (oncollider != null)
            {
                oncollider = null;
            }
        }

        public void Invoke()
        {
            if (oncollider != null)
            {
                oncollider();
            }
        }
    }

    public class IButton : UIBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
#if !UNITY_EDITOR && ( UNITY_ANDROID || UNITY_IOS)
,IPointerExitHandler ,IPointerUpHandler
#else
                       , IPointerExitHandler, IPointerUpHandler, IPointerClickHandler
#endif
    {
        private Color normalColor;
        public  Color changedColor = Color.gray;
        public  bool  ischangeColor;
        public  Color OnClickColor = new Color(195f / 255f, 195f / 255f, 195f / 255f);

        //各种点击回调
        public IComponentOnClick onClick { get; private set; } = new IComponentOnClick();

        /// <summary>
        /// 长摁
        /// </summary>
        public IComponentOnLongTimeClick onLongTimePress { get; private set; } = new IComponentOnLongTimeClick();

        /// <summary>
        /// 摁下
        /// </summary>
        public IComponentOnClick onDownClick { get; private set; } = new IComponentOnClick();

        /// <summary>
        /// 抬起
        /// </summary>
        public IComponentOnClick onUpClick { get; private set; } = new IComponentOnClick();

        /// <summary>
        /// 滑动
        /// </summary>
        public IComponentOnClick onDrag { get; private set; } = new IComponentOnClick();

        /// <summary>
        /// 滑动结束
        /// </summary>
        public IComponentOnClick onDragEnd { get; private set; } = new IComponentOnClick();


        private Image img;

        private void Awake()
        {
            img         = this.GetComponent<Image>();
            normalColor = img.color;
        }




        /// <summary>
        /// 点摁下事件
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(PointerEventData eventData)
        {
            isCancelThisClick = false;

            if (img != null && img.sprite != null)
            {
                img.color = this.OnClickColor;
            }

            onDownClick.Invoke(eventData);
            LongPressStart(eventData);
        }


        /// <summary>
        /// 点抬起事件
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (img != null && img.sprite != null)
            {
                img.color = this.normalColor;
            }
            
            onUpClick.Invoke(eventData);
            LongPressEnd();
        }

        /// <summary>
        /// 点击退出事件
        /// 手机上这个生效
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (ischangeColor == false)
                {
                    if (img != null && img.sprite != null)
                    {
                        img.color = this.normalColor;
                    }
                }
                else
                {
                    if (img != null && img.sprite != null)
                    {
                        img.color = this.changedColor;
                    }
                }

                this.LongPressEnd();
                //触发drag 则取消点击事件
                if (Vector2.Distance(eventData.position, eventData.pressPosition) > 15)
                {
                    return;
                }

                if (eventData.pointerDrag == null && !isCancelThisClick)
                {
                    onClick.Invoke(eventData);
                }
            }
        }


        /// <summary>
        /// 点击事件
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (ischangeColor == false)
            {
                if (img != null)
                {
                    img.color = this.normalColor;
                }
            }
            else
            {
                if (img != null)
                {
                    img.color = this.changedColor;
                }
            }

            if (!this.isCancelThisClick)
            {
                onClick.Invoke(eventData);
            }

            onLongTimePress.Reset();
        }


        public void OnDrag(PointerEventData eventData)
        {
            onDrag.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            onDragEnd.Invoke(eventData);
        }


        #region 长摁相关逻辑

        private Coroutine coroutine;
        /// <summary>
        /// 长摁开始 开启协程计时逻辑
        /// </summary>
        /// <param name="eventData"></param>
        private void LongPressStart(PointerEventData eventData)
        {
            if (this.onLongTimePress.EventNum > 0)
            {
                coroutine = this.StartCoroutine(this.LongPressTimeCounter(eventData));
            }
        }

        /// <summary>
        /// 长按结束 重置所有状态
        /// </summary>
        private void LongPressEnd()
        {
            if (this.coroutine != null)
            {
                this.StopCoroutine(this.coroutine);
            }
            this.onLongTimePress.Reset();
        }

        /// <summary>
        /// 每0.x 一次
        /// </summary>
        /// <returns></returns>
        private IEnumerator LongPressTimeCounter(PointerEventData eventData)
        {
            float startTime = Time.realtimeSinceStartup;
            while (this.onLongTimePress.EventNum > 0)
            {
                //每3帧一次
                yield return null;
                yield return null;
                yield return null;
                var isTrigger = this.onLongTimePress.Invoke(Time.realtimeSinceStartup - startTime, eventData);
                //触发成功就要取消点击事件
                if (isTrigger)
                {
                    this.CancelThisClick();
                }
            }
        }

        #endregion


        private bool isCancelThisClick = false;

        private void CancelThisClick()
        {
            isCancelThisClick = true;
        }
    }
}