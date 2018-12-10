using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class M_MvcWarpper
    {
        public string Name;
        public M_IViewControl MIViewControl;
        public M_IView MIView;

        /// <summary>
        /// 进入
        /// </summary>
        public void Enter()
        {
            this.MIView.Show();
            this.MIViewControl.Enter();
        }

        /// <summary>
        /// 退出
        /// </summary>
        public void Exit()
        {
            this.MIView.Hide();
            this.MIViewControl.Exit();
        }
    }
    static public class M_MVCBind
    {
        /// <summary>
        /// 创建一个mvc模块
        /// </summary>
        /// <param name="mIViewControl"></param>
        /// <param name="mIView"></param>
        static public M_MvcWarpper Create(string name, M_IViewControl mIViewControl, M_IView mIView)
        {
            //1.自动设置节点
            ViewAutoSet(mIView);
            //2.绑定数据model
            mIViewControl.BindModel();
            mIView.BindModel();
            //3.自动绑定事件
            BindEvnet(mIViewControl, mIView);
            //
            var bind = new M_MvcWarpper()
            {
                Name = name,
                MIViewControl = mIViewControl,
                MIView = mIView
            };
            //
            return bind;
        }

        #region vc的操作

        private static Type checkType = typeof(Behaviour);
        /// <summary>
        /// view层自动设置
        /// </summary>
        /// <param name="mIView"></param>
        static private void ViewAutoSet(M_IView mIView)
        {
            var vt = mIView.GetType();
            var fields =vt.GetFields(BindingFlags.NonPublic | BindingFlags.Instance |BindingFlags.Public);

            var vTransform = mIView.Transform;
                
            foreach (var f in fields)
            {
                if (f.FieldType.IsSubclassOf(checkType) == false)
                {
                    continue;
                }
                //1.自动获取节点
                //TODO 热更层必须这样获取属性
                var _attrs = f.GetCustomAttributes(typeof(M_TransformPath), false); //as Attribute[];
                if (_attrs!=null && _attrs.Length>0 )
                {
                    var attr = _attrs.ToList().Find((a)=> a is M_TransformPath) as M_TransformPath;
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
                    f.SetValue(mIView,comp);
                    //Debug.LogFormat("字段{0}获取到setTransform ，path：{1}" , f.Name , attr.Path);
                }
                else
                {
                    //Debug.LogFormat("字段{0}没找到setTransform" , f.Name);
                }
                
                //2.自动绑定数据驱动
                 _attrs = f.GetCustomAttributes(typeof(M_BindModel), false); //as Attribute[];
                if (_attrs!=null && _attrs.Length>0 )
                {
                    var bAttr = _attrs.ToList().Find((a)=> a is M_BindModel) as M_BindModel;
                    if(bAttr==null) continue;
                    var name = bAttr.Name;
                    var _Model = mIView.Model;
                    //注册属性名
                    _Model.AddData(bAttr.Name);
                    //注册事件监听
                    _Model.AddListener(bAttr.Name , (v) =>
                    {
                        if (f.FieldType == typeof(Text))
                        {
                            var c = f.GetValue(mIView) as Text;
                            c.text = v.ToString();
                        }
                        
                        else if (f.FieldType == typeof(Slider))
                        {
                            var c = f.GetValue(mIView) as Slider;
                            c.value = (float)v;
                        }
                        else if (f.FieldType == typeof(Scrollbar))
                        {
                            var c = f.GetValue(mIView) as Scrollbar;
                            c.value = (float)v;
                        }
                        else if (f.FieldType == typeof(Toggle))
                        {
                            var c = f.GetValue(mIView) as Toggle;
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
        /// <param name="mIViewControl"></param>
        /// <param name="mIView"></param>
       static private void BindEvnet(M_IViewControl mIViewControl, M_IView mIView)
        {
            //开始反射所有的UI组件,自动注册Ctrl下面 OnEvent_+ 字段名
            var viewType = mIView.GetType();
            var controlType = mIViewControl.GetType();
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
                        var o = f.GetValue(mIView) as Button;
                        o.onClick.AddListener(() =>
                        {
                            fClick.Invoke(mIViewControl, new object[]{});
                        });
                    }
                    else
                    {
                        BDebug.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
                    }
                }
                else if (f.FieldType == typeof(Scrollbar))//主动注册OnValueChange
                {
                    var o = f.GetValue(mIView) as Scrollbar;
                    var fValueChange = controlType.GetMethod(string.Format("OnValueChange_{0}", f.Name),BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fValueChange != null)
                    {
                       
                        o.onValueChanged.AddListener((value) =>
                        {
                            fValueChange.Invoke(mIViewControl, new object[] {value});
                        });
                    }
                    else
                    {
                        BDebug.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
                    }
                }
                else if (f.FieldType == typeof(Slider)) //自动注册OnValueChange
                {               
                    var o = f.GetValue(mIView) as Slider;
                    var fValueChange = controlType.GetMethod(string.Format("OnValueChange_{0}", f.Name),BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fValueChange != null)
                    {
                       
                        o.onValueChanged.AddListener((value) =>
                        {
                            fValueChange.Invoke(mIViewControl, new object[] {value});
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