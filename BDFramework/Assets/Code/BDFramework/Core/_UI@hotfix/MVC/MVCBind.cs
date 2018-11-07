using System;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using LitJson;
using Microsoft.Cci.Pdb;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UI
{
    public class MvcWarpper
    {
        public string Name;
        public IViewControl ViewControl;
        public IView View;

        /// <summary>
        /// 进入
        /// </summary>
        public void Enter()
        {
            this.View.Show();
            this.ViewControl.Enter();
        }

        /// <summary>
        /// 退出
        /// </summary>
        public void Exit()
        {
            this.View.Hide();
            this.ViewControl.Exit();
        }
    }
    static public class MVCBind
    {
        /// <summary>
        /// 创建一个mvc模块
        /// </summary>
        /// <param name="viewControl"></param>
        /// <param name="view"></param>
        static public MvcWarpper Create(string name, IViewControl viewControl, IView view)
        {
            //1.自动设置节点
            ViewAutoSet(view);
            //2.绑定数据model
            viewControl.BindModel();
            view.BindModel();
            //3.自动绑定事件
            BindEvnet(viewControl, view);
            //
            var bind = new MvcWarpper()
            {
                Name = name,
                ViewControl = viewControl,
                View = view
            };
            //
            return bind;
        }

        #region vc的操作

        private static Type checkType = typeof(Behaviour);
        /// <summary>
        /// view层自动设置
        /// </summary>
        /// <param name="view"></param>
        static private void ViewAutoSet(IView view)
        {
            var vt = view.GetType();
            var fields =vt.GetFields(BindingFlags.NonPublic | BindingFlags.Instance |BindingFlags.Public);

            var vTransform = view.Transform;
                
            foreach (var f in fields)
            {
                if (f.FieldType.IsSubclassOf(checkType) == false)
                {
                    continue;
                }
                //1.自动获取节点
                //TODO 热更层必须这样获取属性
                var _attrs = f.GetCustomAttributes(typeof(TransformPath), false); //as Attribute[];
                if (_attrs!=null && _attrs.Length>0 )
                {
                    var attr = _attrs.ToList().Find((a)=> a is TransformPath) as TransformPath;
                    if(attr==null) continue;
                    //获取节点,并且获取组件
                    var trans = vTransform.Find(attr.Path);
                    if (trans == null)
                    { 
                        BDebug.LogError(string.Format("自动设置节点失败：{0} - {1}", vt.FullName , attr.Path));
                    }
                    var comp = trans.GetComponent(f.FieldType);
                    if (comp == null)
                    {
                        BDebug.LogError(string.Format("节点没有对应组件：type【{0}】 - {1}",  f.FieldType , attr.Path));
                    }
                    //设置属性
                    f.SetValue(view,comp);
                    //Debug.LogFormat("字段{0}获取到setTransform ，path：{1}" , f.Name , attr.Path);
                }
                else
                {
                    //Debug.LogFormat("字段{0}没找到setTransform" , f.Name);
                }
                
                //2.自动绑定数据驱动
                 _attrs = f.GetCustomAttributes(typeof(BindModel), false); //as Attribute[];
                if (_attrs!=null && _attrs.Length>0 )
                {
                    var bAttr = _attrs.ToList().Find((a)=> a is BindModel) as BindModel;
                    if(bAttr==null) continue;
                    var name = bAttr.Name;
                    var _Model = view.Model;
                    //注册属性名
                    _Model.AddData(bAttr.Name);
                    //注册事件监听
                    _Model.AddListener(bAttr.Name , (v) =>
                    {
                        if (f.FieldType == typeof(Text))
                        {
                            var c = f.GetValue(view) as Text;
                            c.text = v.ToString();
                        }
                        
                        else if (f.FieldType == typeof(Slider))
                        {
                            var c = f.GetValue(view) as Slider;
                            c.value = (float)v;
                        }
                        else if (f.FieldType == typeof(Scrollbar))
                        {
                            var c = f.GetValue(view) as Scrollbar;
                            c.value = (float)v;
                        }
                        else if (f.FieldType == typeof(Toggle))
                        {
                            var c = f.GetValue(view) as Toggle;
                            c.isOn = (bool)v;
                        }
                        else
                        {
                            BDebug.LogError("不支持类型,请扩展：" + f.Name  + "-"+ vt.FullName);
                        }
                    });
                }
            }
        }
        
       
        
        /// <summary>
        /// VC进行数据绑定
        /// </summary>
        /// <param name="viewControl"></param>
        /// <param name="view"></param>
       static private void BindEvnet(IViewControl viewControl, IView view)
        {
            //开始反射所有的UI组件,自动注册Ctrl下面 OnEvent_+ 字段名
            var viewType = view.GetType();
            var controlType = viewControl.GetType();
            var vfields = viewType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance |BindingFlags.Public);
            foreach (var f in vfields)
            {
                if (!f.FieldType.IsSubclassOf(typeof( UnityEngine.Object)))
                {
                    continue;   
                }
                //所有ui组件
                if (f.FieldType == typeof(Button))//主动注册OnClick、
                {
                    //
                    var fClick = controlType.GetMethod(string.Format("OnClick_{0}", f.Name ),BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fClick!=null)
                    {
                        var o = f.GetValue(view) as Button;
                        o.onClick.AddListener(() =>
                        {
                            fClick.Invoke(viewControl, new object[]{});
                        });
                    }
                    else
                    {
                        BDebug.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
                    }
                }
                else if (f.FieldType == typeof(Scrollbar))//主动注册OnValueChange
                {
                    var o = f.GetValue(view) as Scrollbar;
                    var fValueChange = controlType.GetMethod(string.Format("OnValueChange_{0}", f.Name),BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fValueChange != null)
                    {
                       
                        o.onValueChanged.AddListener((value) =>
                        {
                            fValueChange.Invoke(viewControl, new object[] {value});
                        });
                    }
                    else
                    {
                        BDebug.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
                    }
                }
                else if (f.FieldType == typeof(Slider)) //自动注册OnValueChange
                {               
                    var o = f.GetValue(view) as Slider;
                    var fValueChange = controlType.GetMethod(string.Format("OnValueChange_{0}", f.Name),BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fValueChange != null)
                    {
                       
                        o.onValueChanged.AddListener((value) =>
                        {
                            fValueChange.Invoke(viewControl, new object[] {value});
                        });
                    }
                    else
                    {
                        BDebug.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
                    }
                }            
            }
        }
        

        #endregion

    }
}