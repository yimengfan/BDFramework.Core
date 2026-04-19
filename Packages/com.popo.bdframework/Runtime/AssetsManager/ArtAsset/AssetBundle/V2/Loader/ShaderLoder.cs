using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BDFramework.ResourceMgr.V2;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BDFramework.ResourceMgr.V2
{
    /// <summary>
    /// Shader 资源加载器。
    /// Shader resource loader.
    /// 该类型负责持有 Shader AssetBundle 的运行时查询缓存，并在预热协程尚未完成时提供可退化的查找入口，
    /// 避免外部在 BaseFlow、母包启动或其他早期阶段调用 <c>FindShader</c> 时因为缓存未建立而直接空引用崩溃。
    /// This type owns the runtime lookup cache for the shader AssetBundle and provides a degradable query entry before the warmup coroutine completes,
    /// preventing early-stage callers such as BaseFlow or the host startup path from crashing with a null reference when <c>FindShader</c> is invoked before the cache exists.
    /// 使用说明：如果预热流程已经跑完，查找会复用现有缓存；如果尚未预热，则会按需同步填充一次 Shader 列表。
    /// Usage note: when warmup has already completed the lookup reuses the existing cache; otherwise it synchronously hydrates the shader list once on demand.
    /// </summary>
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
        /// 根据名称查找 Shader，并在首次查找时按需补齐缓存。
        /// Find a Shader by name and lazily hydrate the cache on the first lookup when necessary.
        /// </summary>
        /// <param name="shaderName">目标 Shader 名称。</param>
        /// <param name="shaderName">Target shader name.</param>
        /// <returns>命中时返回 Shader；未命中或当前无可用 AssetBundle 时返回 null。</returns>
        /// <returns>Returns the Shader when found; otherwise returns null when no match or no usable AssetBundle is available.</returns>
        public Shader FindShader(string shaderName)
        {
            EnsureShaderCacheReadyForLookup();
            var shader = this.shaders.FirstOrDefault((s) => s.name.Equals(shaderName, StringComparison.OrdinalIgnoreCase));
            return shader;
        }

        /// <summary>
        /// 在预热协程尚未完成时，为查询路径惰性建立 Shader 缓存。
        /// Lazily build the shader cache for the lookup path when the warmup coroutine has not completed yet.
        /// </summary>
        /// <remarks>
        /// 这里不主动执行 ShaderVariantCollection.WarmUp，只保证 <c>FindShader</c> 有稳定的可查询列表，
        /// 这样既保留原有预热流程，又避免查询接口在早期阶段直接空引用。
        /// This helper does not proactively run <c>ShaderVariantCollection.WarmUp</c>; it only guarantees that <c>FindShader</c> has a stable list to query,
        /// preserving the existing warmup flow while preventing null-reference failures for early lookups.
        /// </remarks>
        private void EnsureShaderCacheReadyForLookup()
        {
            if (this.shaders != null)
            {
                return;
            }

            if (!this.AssetBundle)
            {
                this.shaders = new List<Shader>(0);
                return;
            }

            var loadedShaders = this.AssetBundle.LoadAllAssets<Shader>();
            this.shaders = loadedShaders == null
                ? new List<Shader>(0)
                : new List<Shader>(loadedShaders);
        }

        /// <summary>
        /// 加载所有shader
        /// </summary>
        public void LoadAllShaders()
        {
            IEnumeratorTool.StartCoroutine(this.WarmUpShaders());
        }


        /// <summary>
        /// 加载所有shader
        /// </summary>
        public void LoadShader(string shaderName)
        {
            // var shader = this.FindShader(shaderName);
            // if (shader == null)
            // {
            //     BDebug.LogError($"shader:{shaderName} 不存在");
            // }
            //
            IEnumeratorTool.StartCoroutine(this.WarmUpShaders());
        }

        /// <summary>
        /// 预热所有shader
        /// </summary>
        /// <returns></returns>
        IEnumerator WarmUpShaders()
        {
            var svcs = this.AssetBundle.LoadAllAssets<ShaderVariantCollection>();
            foreach (var svc in svcs)
            {
                if (!svc.isWarmedUp)
                {
                    var log = "WarmUp shader:" + svc.name;
                    BDebug.LogWatchBegin(log);
                    svc.WarmUp();
                    BDebug.LogWatchEnd(log, "yellow");
                }
                
                yield return new  WaitForSeconds(0.1f);
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