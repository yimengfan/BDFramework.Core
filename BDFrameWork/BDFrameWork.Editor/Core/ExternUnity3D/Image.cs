using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//namespace BDFramework.Helper
//{
    public static class ImageHelper
    {
        public static void ChangeToGray(this Image img)
        {
            Material material = BResources.Load<Material>("Shader/UI_Gary") as Material;
            if (img != null && material != null)
            {
                img.material = material;
            }
        }
        public static void ChangeToNormal(this Image img)
        {
            img.material = null;
        }
    }
//}