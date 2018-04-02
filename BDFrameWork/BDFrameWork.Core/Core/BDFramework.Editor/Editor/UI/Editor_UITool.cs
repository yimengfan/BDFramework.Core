using BDFramework.UI;
using Code.Core.BDFramework.SimpleGenCSharpCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BDFramework.Editor.UI
{
    public class Editor_UITool
    {
        private static string windowPath = "/Code/Game/Windows/";
        private static string createPath = "/Code/Game/Windows/Window_MVC/";


        public static void CreateViewCS(List<RegistViewItem> itemList, string goName)
        {
            MyClass mc = new MyClass("View_" + goName + ":AViewBase");
            mc.AddNameSpace(new string[2] { "BDFramework.UI", "UnityEngine" });
            mc.SetSelfNameSpace("Code.Game.Windows");
            foreach (RegistViewItem item in itemList)
            {
                MyField f = new MyField();
                f.SetType(item.GetUIType());
                f.SetFieldName(item.gameObject.name);
                string tp = item.GetBindPath();
                if (!string.IsNullOrEmpty(tp)) f.AddAttribute(tp);
                tp = item.GetBindDataName();
                if (!string.IsNullOrEmpty(tp)) f.AddAttribute(tp);
                mc.AddField(f);
            }

            MyMethod construct = new MyMethod();
            construct.OverwriteContent(@"//[Note]
        public  [method name](Transform t, DataDrive_Service service) : base(t, service)
        {
            
        }");
            construct.SetMethSign(null, "View_" + goName, null);
            mc.AddMethod(construct);

            MyMethod bindData = new MyMethod();
            bindData.OverwriteContent(@"//[Note]
        public override void BindData()
        {
            base.BindData();
        }");
            mc.AddMethod(bindData);
            string path = Application.dataPath + createPath + "View_" + goName + ".cs";
            File.WriteAllText(path, mc.ToString());
            Debug.Log(string.Format("生成成功！路径:{0}", path));
        }

        public static void CreateContrlCS(List<RegistViewItem> itemList, string goName)
        {
            MyClass mc = new MyClass("Contrl_" + goName + ":AViewContrlBase");
            mc.AddNameSpace(new string[2] { "BDFramework.UI", "UnityEngine" });
            mc.SetSelfNameSpace("Code.Game.Windows.MCX"); MyMethod construct = new MyMethod();
            construct.OverwriteContent(@"//[Note]
        public [method name](DataDrive_Service data) : base(data)
        {
            
        } ");
            construct.SetMethSign(null, "Contrl_" + goName, null);
            mc.AddMethod(construct);
            foreach (RegistViewItem item in itemList)
            {
                if (string.IsNullOrEmpty(item.bindDataName)) continue;
                Type t = item.GetUIType();
                MyMethod bindData = new MyMethod();
                string methodName = ""; string methodParams = "";
                if (t.Equals(typeof(UnityEngine.UI.Button)))
                {
                    methodName = "OnClick_" + item.name;
                    methodParams = "";
                }
                else if (t.Equals(typeof(UnityEngine.UI.Slider)))
                {
                    methodName = "OnValueChange_" + item.name;
                    methodParams = "float value";
                }
                else if (t.Equals(typeof(UnityEngine.UI.Scrollbar)))
                {
                    methodName = "OnValueChange_" + item.name;
                    methodParams = "float value";
                }
                bindData.OverwriteContent(@"//[Note]
                private [return type] [method name] ([params])
                {
                   [method content]
                }
                ");
                bindData.SetMethSign(null, methodName, methodParams);
                bindData.SetMethodContent(string.Format("Debug.Log(\"use {0}\");", item.name));
                mc.AddMethod(bindData);
            }

            string path = Application.dataPath + createPath + "Contrl_" + goName + ".cs";
            File.WriteAllText(path, mc.ToString());
            Debug.Log(string.Format("生成成功！路径:{0}", path));
        }


        public static void FindWindows(ref Dictionary<string, string> pbPaths)
        {
            string path = Application.dataPath + windowPath;
            DirectoryInfo folder = new DirectoryInfo(path);
            foreach (FileInfo file in folder.GetFiles("*.cs"))
            {
                string className = file.Name.Substring(0, file.Name.LastIndexOf('.'));
                Assembly assembly = Assembly.Load("Assembly-CSharp");
                Type type = assembly.GetType(className);
                object[] records = type.GetCustomAttributes(typeof(UIAttribute), false);
                pbPaths.Add(className, (records[0] as UIAttribute).ResourcePath);
            }
        }

        public static void CloneValues(List<RegistViewItem> itemlist, ref List<string> nameList, ref List<bool> isBindPathList, ref List<string> bindNameList)
        {
            nameList.Clear();
            isBindPathList.Clear();
            bindNameList.Clear();
            foreach (RegistViewItem item in itemlist)
            {
                nameList.Add(item.name);
                isBindPathList.Add(item.isBindPath);
                bindNameList.Add(item.bindDataName);
            }
        }

        public static bool CheckRepeatName(List<RegistViewItem> itemlist)
        {
            return itemlist.GroupBy(x => x.name).Where(x => x.Count() > 1).ToList().Count() > 0;
        }
    }
}
