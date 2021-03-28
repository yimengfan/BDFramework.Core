using BDFramework.Editor.EditorLife;
using UnityEditor;

namespace Game.Editor
{
    [InitializeOnLoad]
    public class BDFrameworkEditorHook
    {
        static BDFrameworkEditorHook()
        {
            EditorApplication.delayCall += OnCompileCode;
            EditorApplication.playModeStateChanged += OnPlayExit;
        }


        /// <summary>
        /// 退出播放模式
        /// </summary>
        /// <param name="state"></param>
        static private void OnPlayExit(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RegisterAssembly();
            }
        }

        /// <summary>
        /// Editor代码刷新后执行
        /// </summary>
        static public void OnCompileCode()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            RegisterAssembly();
        }


        /// <summary>
        /// 注册Assembly
        /// </summary>
        static private void RegisterAssembly()
        {
            var logicAssembly = typeof(LocalGameStart).Assembly;
            var editorAssembly = typeof(BDFrameworkEditorHook).Assembly;
            BDFrameEditorLife.RegisterMainProjectAssembly(logicAssembly, editorAssembly);
        }
    }
}