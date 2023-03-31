using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BDFramework.ResourceMgr.V2;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BDFramework.ResourceMgr.V2
{
    public class ShaderLoder : AssetLoder
    {
        public ShaderLoder(UnityEngine.AssetBundle ab) : base(ab)
        {
        }

        /// <summary>
        /// 所有的shader
        /// </summary>
        private List<Shader> shaders = null;

        /// <summary>
        /// 加载所有shader
        /// </summary>
        public void LoadAllShaders()
        {
            IEnumeratorTool.StartCoroutine(this.WarmUpShaders());
        }

        /// <summary>
        /// 获取shader
        /// </summary>
        /// <returns></returns>
        public Shader FindShader(string shaderName)
        {
            var shader =
                this.shaders.FirstOrDefault((s) => s.name.Equals(shaderName, StringComparison.OrdinalIgnoreCase));
            return shader;
        }


        /// <summary>
        /// 预热shader
        /// </summary>
        /// <returns></returns>
        IEnumerator WarmUpShaders()
        {
            var svcs = this.AssetBundle.LoadAllAssets<ShaderVariantCollection>();
            foreach (var svc in svcs)
            {
                if (!svc.isWarmedUp)
                {
                    svc.WarmUp();
                }

                BDebug.Log($"warmup shader:{svc.name}", Color.yellow);
                yield return null;
            }

            //最后加载
            BDebug.LogWatchBegin("Load shaders");
            //获取shader
            var shaders = this.AssetBundle.LoadAllAssets<Shader>();
            this.shaders = new List<Shader>(shaders);
            BDebug.LogWatchEnd("Load shaders");
        }
    }
}