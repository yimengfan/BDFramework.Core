using System.Net.Mime;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UI
{
    public class BindContextWarpper
    {
        public string Name;
        public IViewControl ViewControl;
        public IView view;
    }
   static public class VCContainer
    {
        /// <summary>
        /// 创建一个mvx模块
        /// </summary>
        /// <param name="viewControl"></param>
        /// <param name="view"></param>
        static public BindContextWarpper Create(string name, IViewControl viewControl, IView view)
        {
            ViewAutoSet(view);
            BindEvnet(viewControl, view);
            //
            viewControl.BindData();
            view.BindData();
            //
            var bind = new BindContextWarpper()
            {
                Name = name,
                ViewControl = viewControl,
                view = view
            };
            //
            return bind;
        }

        #region vc的操作

        /// <summary>
        /// view层自动设置
        /// </summary>
        /// <param name="view"></param>
        static private void ViewAutoSet(IView view)
        {
            var vt = view.GetType();
            var fields =vt.GetFields(BindingFlags.NonPublic | BindingFlags.Instance |BindingFlags.Public);

            var vTransform = vt.GetProperty("Transform").GetValue(view) as Transform;
            
            foreach (var f in fields)
            {
                //1.自动获取节点
                var attr = f.GetCustomAttribute<BSetTransform>();
                if (attr != null)
                {
                    //获取节点,并且获取组件
                    var trans = vTransform.Find(attr.Path);
                    if (trans == null)
                    {
                        BDeBug.I.LogError(string.Format("自动设置节点失败：{0} - {1}", vt.FullName , attr.Path));
                    }
                    var comp = trans.GetComponent(f.FieldType);
                    if (comp == null)
                    {
                        BDeBug.I.LogError(string.Format("节点没有对应组件：type【{0}】 - {1}",  f.FieldType , attr.Path));
                    }
                    //设置属性
                    f.SetValue(view,comp);
                }
                
                //2.自动绑定数据驱动
            }
        }
        
       
        
        /// <summary>
        /// VC进行数据绑定
        /// </summary>
        /// <param name="viewControl"></param>
        /// <param name="view"></param>
       static private void BindEvnet(IViewControl viewControl, IView view)
        {
            //开始反射所有的UI组件,自动注册Ctrl下面 OnEvent_+字段名
            var viewType = view.GetType();
            var controlType = viewControl.GetType();
            var vfields = viewType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance |BindingFlags.Public);
            foreach (var f in vfields)
            {
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
                        BDeBug.I.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
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
                        BDeBug.I.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
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
                        BDeBug.I.Log(string.Format("ui事件未实现:{0} - {1}" ,viewType.FullName , f.Name )  , "yellow");
                    }
                }            
            }
        }
        

        #endregion

    }
}