/**
 * EditorCommandDispatcher —— 编辑器命令调度器。
 *
 * 设计角色：
 * - 作为 Playwright → Unity Editor 的万能操作通道。
 * - Playwright 端发送 editor_command 消息，此调度器在主线程上执行对应操作。
 * - 内置常用命令（场景操作、PlayMode 控制、菜单触发等）。
 * - 提供 execute_method 万能反射接口，允许 Playwright 调用任意静态方法。
 *
 * 反射使用说明：
 * - 此类位于基础设施层（Talos.E2E.Editor），非业务代码。
 * - 反射仅用于 Playwright E2E 编排，是受控的扩展点。
 * - execute_method 仅在 Editor 模式下可用，不会进入 Runtime 构建。
 *
 * 使用示例（Playwright 端）：
 * ```typescript
 * // 打开场景
 * await editorOps.scene.openScene('Assets/Scenes/BDFrame.unity');
 * // 进入 PlayMode
 * await editorOps.editor.enterPlayMode();
 * // 万能反射调用
 * await editorOps.executeMethod('SomeNamespace.SomeClass.SomeMethod', { arg1: 'value' });
 * ```
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LitJson;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Talos.E2E
{
    /// <summary>
    /// 编辑器命令调度器——在主线程上执行 Playwright 端请求的编辑器操作。
    /// 内置命令通过 switch-case 路由，同时提供 execute_method 万能反射接口。
    /// </summary>
    static public class EditorCommandDispatcher
    {
        /// <summary>
        /// 调度并执行编辑器命令。
        /// </summary>
        /// <param name="command">命令名称，如 "open_scene"、"enter_playmode"。</param>
        /// <param name="parameters">命令参数（可为 null）。</param>
        /// <returns>命令执行结果，序列化为 JSON 返回给 Playwright。</returns>
        public static object Dispatch(string command, JsonData parameters)
        {
            Debug.Log($"[TalosE2E] EditorCommandDispatcher.Dispatch: command={command}");

            switch (command)
            {
                // ======== 场景操作 ========
                case "open_scene":
                    return CmdOpenScene(parameters);
                case "get_current_scene":
                    return CmdGetCurrentScene();
                case "save_scene":
                    return CmdSaveScene();

                // ======== PlayMode 控制 ========
                case "enter_playmode":
                    return CmdEnterPlayMode();
                case "exit_playmode":
                    return CmdExitPlayMode();
                case "is_playing":
                    return CmdIsPlaying();

                // ======== 编辑器通用操作 ========
                case "execute_menu_item":
                    return CmdExecuteMenuItem(parameters);
                case "delay_frames":
                    return CmdDelayFrames(parameters);
                case "get_editor_state":
                    return CmdGetEditorState();

                // ======== 万能反射接口 ========
                case "execute_method":
                    return CmdExecuteMethod(parameters);

                default:
                    throw new ArgumentException($"未知的编辑器命令: {command}");
            }
        }

        // ====================================================================
        // 场景操作
        // ====================================================================

        /// <summary>
        /// 打开指定场景。
        /// params: { scenePath: "Assets/Scenes/BDFrame.unity" }
        /// </summary>
        private static object CmdOpenScene(JsonData parameters)
        {
            var scenePath = GetParamString(parameters, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new ArgumentException("open_scene 命令缺少 scenePath 参数");
            }

            Debug.Log($"[TalosE2E] 打开场景: {scenePath}");
            var scene = EditorSceneManager.OpenScene(scenePath);
            Debug.Log($"[TalosE2E] 场景已打开: {scene.name}, 路径: {scene.path}");

            return new { sceneName = scene.name, scenePath = scene.path };
        }

        /// <summary>
        /// 获取当前活动场景信息。
        /// </summary>
        private static object CmdGetCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            return new
            {
                name = scene.name,
                path = scene.path,
                isLoaded = scene.isLoaded,
                rootCount = scene.rootCount,
            };
        }

        /// <summary>
        /// 保存当前场景。
        /// </summary>
        private static object CmdSaveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                return new { saved = false, reason = "场景未保存到磁盘（可能是新建未保存场景）" };
            }

            var success = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TalosE2E] 保存场景: {scene.path}, 成功={success}");
            return new { saved = success, scenePath = scene.path };
        }

        // ====================================================================
        // PlayMode 控制
        // ====================================================================

        /// <summary>
        /// 进入 PlayMode。
        /// 注意：此命令发送后 Unity 会异步进入 PlayMode，调用方需要轮询 is_playing 确认。
        /// </summary>
        private static object CmdEnterPlayMode()
        {
            Debug.Log("[TalosE2E] 进入 PlayMode...");
            EditorApplication.EnterPlaymode();
            return new { entering = true };
        }

        /// <summary>
        /// 退出 PlayMode。
        /// 注意：此命令发送后 Unity 会异步退出 PlayMode，调用方需要轮询 is_playing 确认。
        /// </summary>
        private static object CmdExitPlayMode()
        {
            Debug.Log("[TalosE2E] 退出 PlayMode...");
            EditorApplication.ExitPlaymode();
            return new { exiting = true };
        }

        /// <summary>
        /// 查询当前是否处于 PlayMode。
        /// </summary>
        private static object CmdIsPlaying()
        {
            return new { isPlaying = EditorApplication.isPlaying };
        }

        // ====================================================================
        // 编辑器通用操作
        // ====================================================================

        /// <summary>
        /// 执行 Unity 菜单项——等效于点击菜单。
        /// params: { menuPath: "Talos/E2E Test/检查 DEBUG 状态" }
        /// </summary>
        private static object CmdExecuteMenuItem(JsonData parameters)
        {
            var menuPath = GetParamString(parameters, "menuPath");
            if (string.IsNullOrEmpty(menuPath))
            {
                throw new ArgumentException("execute_menu_item 命令缺少 menuPath 参数");
            }

            Debug.Log($"[TalosE2E] 执行菜单项: {menuPath}");
            EditorApplication.ExecuteMenuItem(menuPath);
            return new { executed = true, menuPath };
        }

        /// <summary>
        /// 延迟指定帧数后返回——用于等待 Unity 完成异步操作。
        /// 实际实现为同步等待（在主线程上），仅占用当前帧处理。
        /// params: { description: "等待场景加载" }
        /// </summary>
        private static object CmdDelayFrames(JsonData parameters)
        {
            // 在命令模式中 delay_frames 是一个标记命令，
            // 实际的等待由 Playwright 端通过轮询实现。
            var description = GetParamString(parameters, "description") ?? "无描述";
            Debug.Log($"[TalosE2E] delay_frames 标记: {description}");
            return new { noted = true, description };
        }

        /// <summary>
        /// 获取编辑器当前状态快照——用于 Playwright 端判断 Editor 是否就绪。
        /// </summary>
        private static object CmdGetEditorState()
        {
            var scene = SceneManager.GetActiveScene();
            return new
            {
                isPlaying = EditorApplication.isPlaying,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                currentScene = scene.name,
                currentScenePath = scene.path,
                timeSinceStartup = EditorApplication.timeSinceStartup,
            };
        }

        // ====================================================================
        // 万能反射接口——执行任意静态方法
        // ====================================================================

        /// <summary>
        /// 通过反射执行任意静态方法——Playwright 端的万能操作通道。
        ///
        /// 反射使用说明（基础设施层受控扩展点）：
        /// - 仅限 Editor 模式，不会进入 Runtime 构建。
        /// - 仅支持 public static 方法。
        /// - 用于 E2E 编排，非业务逻辑。
        ///
        /// params:
        ///   - methodPath: "Namespace.ClassName.MethodName"（完整类型名+方法名）
        ///   - args: []（可选，参数数组）
        ///
        /// 使用示例（Playwright 端）：
        ///   executeMethod('Talos.E2E.Editor.E2EEditorTools.CheckDebugStatus')
        ///   executeMethod('SomeClass.SomeMethod', { args: ["param1", 42] })
        /// </summary>
        private static object CmdExecuteMethod(JsonData parameters)
        {
            var methodPath = GetParamString(parameters, "methodPath");
            if (string.IsNullOrEmpty(methodPath))
            {
                throw new ArgumentException("execute_method 命令缺少 methodPath 参数");
            }

            // 解析 "Namespace.ClassName.MethodName"
            var lastDot = methodPath.LastIndexOf('.');
            if (lastDot < 0)
            {
                throw new ArgumentException($"methodPath 格式错误，期望 'Namespace.ClassName.MethodName': {methodPath}");
            }

            var typeName = methodPath.Substring(0, lastDot);
            var methodName = methodPath.Substring(lastDot + 1);

            Debug.Log($"[TalosE2E] 反射调用: type={typeName}, method={methodName}");

            // 查找类型——遍历所有已加载的程序集
            Type targetType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                targetType = assembly.GetType(typeName);
                if (targetType != null) break;
            }

            if (targetType == null)
            {
                throw new TypeLoadException($"未找到类型: {typeName}");
            }

            // 提取参数
            var argsList = new List<object>();
            if (parameters != null)
            {
                try
                {
                    var argsData = parameters["args"];
                    if (argsData != null && argsData.IsArray)
                    {
                        for (int i = 0; i < argsData.Count; i++)
                        {
                            argsList.Add(ConvertJsonData(argsData[i]));
                        }
                    }
                }
                catch
                {
                    // "args" 字段不存在，忽略
                }
            }

            // 查找方法
            var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            var method = methods.FirstOrDefault(m =>
            {
                if (m.Name != methodName) return false;
                var ps = m.GetParameters();
                if (ps.Length != argsList.Count) return false;
                return true;
            });

            if (method == null)
            {
                // 尝试无参版本
                if (argsList.Count == 0)
                {
                    method = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                }

                if (method == null)
                {
                    var available = string.Join(", ", methods.Select(m => $"{m.Name}({m.GetParameters().Length} params)"));
                    throw new MissingMethodException(
                        $"未找到匹配的方法: {typeName}.{methodName}({argsList.Count} 个参数)。可用方法: {available}");
                }
            }

            // 执行
            Debug.Log($"[TalosE2E] 执行: {typeName}.{methodName}({argsList.Count} 个参数)");
            var result = method.Invoke(null, argsList.Count > 0 ? argsList.ToArray() : null);

            Debug.Log($"[TalosE2E] 反射调用完成: {typeName}.{methodName}, 返回类型={result?.GetType().Name ?? "void"}");

            return new
            {
                invoked = true,
                methodPath,
                returnType = method.ReturnType.Name,
                result = result,
            };
        }

        // ====================================================================
        // 辅助方法
        // ====================================================================

        /// <summary>
        /// 从 JsonData 中安全获取字符串参数。
        /// LitJson 的 JsonData 不提供 ContainsKey 方法，使用 try-catch 安全访问。
        /// </summary>
        private static string GetParamString(JsonData parameters, string key)
        {
            if (parameters == null) return null;
            try
            {
                var val = parameters[key];
                if (val == null) return null;
                return (string)val;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 将 LitJson 的 JsonData 值转换为 C# 基本类型。
        /// LitJson 版本限制：不支持 ContainsKey、IsNull 等方法，使用类型检查替代。
        /// </summary>
        private static object ConvertJsonData(JsonData data)
        {
            if (data == null) return null;
            if (data.IsString) return (string)data;
            if (data.IsInt) return (int)data;
            if (data.IsLong) return (long)data;
            if (data.IsDouble) return (double)data;
            if (data.IsBoolean) return (bool)data;
            // 复杂类型（数组、对象）保留为 JsonData 字符串
            return data.ToString();
        }
    }
}
