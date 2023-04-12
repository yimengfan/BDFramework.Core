using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HybridCLR
{
    public static class RuntimeApi
    {
#if UNITY_STANDALONE_WIN
        private const string dllName = "GameAssembly";
#elif UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_WEBGL
    private const string dllName = "__Internal";
#else
    private const string dllName = "il2cpp";
#endif

        /// <summary>
        /// 加载补充元数据assembly
        /// </summary>
        /// <param name="dllBytes"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static unsafe LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode)
        {
#if UNITY_EDITOR
            return LoadImageErrorCode.OK;
#else
            fixed(byte* data = dllBytes)
            {
                return (LoadImageErrorCode)LoadMetadataForAOTAssembly(data, dllBytes.Length, (int)mode);
            }
#endif
        }

        /// <summary>
        /// 加载补充元数据assembly
        /// </summary>
        /// <param name="dllBytes"></param>
        /// <param name="dllSize"></param>
        /// <returns></returns>
        [DllImport(dllName, EntryPoint = "RuntimeApi_LoadMetadataForAOTAssembly")]
        public static extern unsafe int LoadMetadataForAOTAssembly(byte* dllBytes, int dllSize, int mode);


        /// <summary>
        /// 获取解释器线程栈的最大StackObject个数(size*8 为最终占用的内存大小)
        /// </summary>
        /// <returns></returns>
        [DllImport(dllName, EntryPoint = "RuntimeApi_GetInterpreterThreadObjectStackSize")]
        public static extern int GetInterpreterThreadObjectStackSize();

        /// <summary>
        /// 设置解释器线程栈的最大StackObject个数(size*8 为最终占用的内存大小)
        /// </summary>
        /// <param name="size"></param>
        [DllImport(dllName, EntryPoint = "RuntimeApi_SetInterpreterThreadObjectStackSize")]
        public static extern void SetInterpreterThreadObjectStackSize(int size);

        /// <summary>
        /// 获取解释器线程函数帧数量(sizeof(InterpreterFrame)*size 为最终占用的内存大小)
        /// </summary>
        /// <returns></returns>
        [DllImport(dllName, EntryPoint = "RuntimeApi_GetInterpreterThreadFrameStackSize")]
        public static extern int GetInterpreterThreadFrameStackSize();

        /// <summary>
        /// 设置解释器线程函数帧数量(sizeof(InterpreterFrame)*size 为最终占用的内存大小)
        /// </summary>
        /// <param name="size"></param>
        [DllImport(dllName, EntryPoint = "RuntimeApi_SetInterpreterThreadFrameStackSize")]
        public static extern void SetInterpreterThreadFrameStackSize(int size);
    }
}
