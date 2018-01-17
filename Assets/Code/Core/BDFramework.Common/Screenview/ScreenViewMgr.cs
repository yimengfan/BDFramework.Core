using System;
using System.Runtime.CompilerServices;
using BDFramework.Mgr;
using UnityEditor.Graphs;
using UnityEngine;

namespace BDFramework.ScreenView
{
    public class ScreenViewMgr : MgrBase<ScreenViewMgr>
    {
        ScreenViewCenter screenViewCenter = new ScreenViewCenter();
        private ScreenViewLayer mainLayer;
        /// <summary>
        ///唤起
        /// </summary>
        public override void Awake()
        {
            if (mainLayer != null)
            {
                Debug.LogError("已经执行过Awake");
                return;
            }
            base.Awake();
            screenViewCenter.AddLayer();
            mainLayer = screenViewCenter.GetLayer(0);
            //
            string defaultLayer = null;
            //
            foreach (var  classData in  this.ClassDataMap.Values)
            {
                var attr = classData.Attribute as ScreenViewAttribute;

                var sv = GetTypeInst<IScreenView>(attr.Name);
                //设置name属性
                sv.GetType().GetProperty("Name").SetValue(sv, attr.Name, null);
                mainLayer.RegScreen(sv);
                //
                BDeBug.I.Log("创建screen:" + attr.Name , Styles.Color.Green);
                //
                if (attr.isDefault)
                {
                    defaultLayer = attr.Name;
                }
            }

            if (string.IsNullOrEmpty(defaultLayer) == false)
            {
                mainLayer.BeginNavTo(defaultLayer);
            }
            else
            {
                BDeBug.I.Log("没有默认导航的ScreenView");
            }
        }

        public override void Update()
        {
            base.Update();
            screenViewCenter.Update(Time.deltaTime);
        }

        public override void CheckType(Type type)
        {
            base.CheckType(type);
            var attrs = type.GetCustomAttributes(typeof(ScreenViewAttribute), false);
            if (attrs.Length > 0)
            {
                foreach (var attr in attrs)
                {
                    var _attr = (ScreenViewAttribute)attr;
                    SaveAttribute(_attr.Name, new ClassData() { Attribute = _attr, Type = type });
                }
            }
        }


        public void BeginNav(string name)
        {
            mainLayer.BeginNavTo(name);
        }
    }
}