using BDFramework.UI;
using Code.Core.BDFramework.SimpleGenCSharpCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.Editor.UI
{
    public class Editor_UITool
    {
        private static string windowPath = "/Code/Game/Windows/";
        private static string createPath = "/Code/Game/Windows/Window_MVC/";

        private static string overrideContent = @"
//[Note]
public override [return type] [method name] ([params])
{
   [method content]
}
";


        public static void CreateViewCS(List<RegistViewItem> itemList, string goName, string root)
        {
            MyClass mc = new MyClass("View_" + goName + ":AViewBase");
            mc.AddNameSpace(new string[2] { "BDFramework.UI", "UnityEngine" });
            mc.SetSelfNameSpace("Code.Game.Windows");
            foreach (RegistViewItem item in itemList)
            {
                MyField f = new MyField();
                f.SetType(GetUIType(item.gameObject));
                f.SetFieldName(item.gameObject.name);
                if (item.isBindPath) f.AddAttribute(GetBindPath(item.gameObject, root));
                string tp = GetBindDataName(item);
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
            string path = Application.dataPath + createPath;
            if (!Directory.Exists(path))
            {
                Debug.LogError(string.Format("文件夹不存在！路径:{0}", path));
                return;
            }
            path = path + "View_" + goName + ".cs";
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
                Type t = GetUIType(item.gameObject);
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
                else
                {
                    methodName = "On_" + item.name;
                    methodParams = "";
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


            string path = Application.dataPath + createPath;
            if (!Directory.Exists(path))
            {
                Debug.LogError(string.Format("文件夹不存在！路径:{0}", path));
                return;
            }
            path = path + "Contrl_" + goName + ".cs";
            File.WriteAllText(path, mc.ToString());
            Debug.Log(string.Format("生成成功！路径:{0}", path));
        }

        public static void CreateWindowCS(string goName, string prefabName)
        {
            MyClass mc = new MyClass("Window_" + goName + ": AWindow ");
            mc.AddAttribute(string.Format("UI(0,\"{0}\")", "Windows/" + prefabName));
            mc.AddNameSpace(new string[3] { "BDFramework.UI", "UnityEngine", "BDFramework.UI" });
            MyMethod construct = new MyMethod();
            construct.OverwriteContent(@"//[Note]
        public  [method name](string path) : base(path)
        {
            
        }");
            construct.SetMethSign(null, "Window_" + goName, null);
            mc.AddMethod(construct);
            MyMethod init = new MyMethod();
            init.OverwriteContent(overrideContent);
            init.SetMethSign(null, "Init", null);
            init.SetMethodContent("base.Init();");
            mc.AddMethod(init);

            MyMethod close = new MyMethod();
            close.OverwriteContent(overrideContent);
            close.SetMethSign(null, "Close", null);
            close.SetMethodContent("base.Close();");
            mc.AddMethod(close);
            MyMethod open = new MyMethod();
            open.OverwriteContent(overrideContent);
            open.SetMethSign(null, "Open", null);
            open.SetMethodContent("base.Open();");
            mc.AddMethod(open);

            MyMethod destroy = new MyMethod();
            destroy.OverwriteContent(overrideContent);
            destroy.SetMethSign(null, "Destroy", null);
            destroy.SetMethodContent("base.Destroy();");
            mc.AddMethod(destroy);

            string path = Application.dataPath + windowPath;
            if (!Directory.Exists(path))
            {
                Debug.LogError(string.Format("文件夹不存在！路径:{0}", path));
                return;
            }
            path = path + "Window_" + goName + ".cs";
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
                if (type != null)
                {
                    object[] records = type.GetCustomAttributes(typeof(UIAttribute), false);
                    if (records != null && records.Length > 0)
                        pbPaths.Add(className, (records[0] as UIAttribute).ResourcePath);
                }
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


        public static Type GetUIType(GameObject gameObject)
        {
            UIBehaviour[] uiBehaviours = gameObject.GetComponents<UIBehaviour>();
            for (int i = 0; i < uiBehaviours.Length; i++)
            {
                if (uiBehaviours[i].GetType().Equals(typeof(UnityEngine.UI.Image))) continue;
                return uiBehaviours[i].GetType();
            }
            return typeof(UnityEngine.UI.Image);
        }

        public static string GetBindPath(GameObject gameObject, string _root)
        {
            string path = gameObject.name;
            Transform tsTp = gameObject.transform;
            while (tsTp.parent && !tsTp.parent.name.Equals(_root))
            {
                path = path.Insert(0, tsTp.parent.name + "/");
                tsTp = tsTp.parent;
            }
            return "BSetTransform(\"" + path + "\")";
        }

        //public string GetPath(bool isGet)
        //{
        //    string path = gameObject.name;
        //    Transform tsTp = gameObject.transform;
        //    if (isGet)
        //    {
        //        while (tsTp.parent && !tsTp.parent.name.Equals(_root))
        //        {
        //            path = path.Insert(0, tsTp.parent.name + "/");
        //            tsTp = tsTp.parent;
        //        }
        //        return "BSetTransform(\"" + path + "\")";
        //    }
        //    return "";
        //}

        public static string GetBindDataName(RegistViewItem item)
        {
            if (!string.IsNullOrEmpty(item.bindDataName))
            {
                return "BBindData(\"" + item.bindDataName.Trim() + "\")";
            }
            return null;
        }
    }
}
