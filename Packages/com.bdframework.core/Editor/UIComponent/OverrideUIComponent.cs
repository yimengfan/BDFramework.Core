using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.Editor
{
    public class OverrideUIComponent
    {
        [MenuItem("GameObject/UI/Text")]
        static void CreateText()
        {
            var text = CreateComponent<Text>("text_defaultName");
            text.raycastTarget = false;
            text.supportRichText = false;
        }

        [MenuItem("GameObject/UI/Image")]
        static void CreateImage()
        {
            var image = CreateComponent<Image>("img_defaultName");
            image.raycastTarget = false;
            image.maskable = false;
        }
        
        [MenuItem("GameObject/UI/Raw Image")]
        static void CreateRawImage()
        {
            var image = CreateComponent<RawImage>("rawimg_defaultName");
            image.raycastTarget = false;
            image.maskable = false;
        }
        
        /// <summary>
        /// 创建ui组件
        /// </summary>
        /// <param name="defaultName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T CreateComponent<T>(string defaultName) where T : UIBehaviour
        {
            GameObject go = new GameObject(defaultName, typeof(T));
            go.transform.SetParent(Selection.activeTransform);
            Selection.activeGameObject = go;
            return go.GetComponent<T>();
        }
    }
}