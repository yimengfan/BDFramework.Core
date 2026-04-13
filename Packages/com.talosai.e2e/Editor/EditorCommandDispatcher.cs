using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using LitJson;
using UnityEngine;

namespace Talos.E2E
{
    /// <summary>
    /// 编辑器反射调度器——为 Playwright 提供统一的 Editor 控制入口。
    /// 设计目标是让 Playwright 成为测试流程的主控制方，Unity 侧默认只保留通用反射网关，
    /// 由 Playwright 按 Unity 官方 API、框架 API 和项目 API 文档去组合测试流程。
    /// 只有明显跨多步、带状态协调的复杂流程，才应继续在 Unity 侧提供专用封装。
    /// </summary>
    public static class EditorCommandDispatcher
    {
        /// <summary>
        /// 类型缓存——避免反复遍历全部程序集查找类型。
        /// </summary>
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        /// <summary>
        /// 静态方法缓存——按“成员路径 + 参数签名”缓存解析结果。
        /// </summary>
        private static readonly Dictionary<string, MethodInfo> StaticMethodCache = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// 静态属性缓存。
        /// </summary>
        private static readonly Dictionary<string, PropertyInfo> StaticPropertyCache = new Dictionary<string, PropertyInfo>();

        /// <summary>
        /// 静态字段缓存。
        /// </summary>
        private static readonly Dictionary<string, FieldInfo> StaticFieldCache = new Dictionary<string, FieldInfo>();

        /// <summary>
        /// 调度并执行编辑器命令。
        /// 当前只保留反射调用与静态成员读取两类能力，避免继续在 Unity 侧堆积单 API 薄封装。
        /// </summary>
        /// <param name="command">命令名称。支持：reflect_invoke_static、reflect_get_static，以及兼容别名 execute_method。</param>
        /// <param name="parameters">命令参数。</param>
        /// <returns>序列化友好的结果对象。</returns>
        public static object Dispatch(string command, JsonData parameters)
        {
            Debug.Log($"[TalosE2E] EditorCommandDispatcher.Dispatch: command={command}");

            switch (command)
            {
                case "reflect_invoke_static":
                case "execute_method":
                    return CmdReflectInvokeStatic(parameters);

                case "reflect_get_static":
                    return CmdReflectGetStatic(parameters);

                default:
                    throw new ArgumentException($"未知的编辑器命令: {command}。当前仅支持 reflect_invoke_static / reflect_get_static");
            }
        }

        /// <summary>
        /// 通过反射执行任意 public static 方法。
        /// 参数格式：
        /// - memberPath: "Namespace.TypeName.MethodName"
        /// - args: []
        /// 兼容旧字段 methodPath，便于平滑迁移 Playwright 调用端。
        /// </summary>
        private static object CmdReflectInvokeStatic(JsonData parameters)
        {
            var memberPath = GetParamString(parameters, "memberPath") ?? GetParamString(parameters, "methodPath");
            if (string.IsNullOrEmpty(memberPath))
            {
                throw new ArgumentException("reflect_invoke_static 命令缺少 memberPath 参数");
            }

            var split = SplitMemberPath(memberPath);
            var targetType = ResolveType(split.Item1);
            var rawArgs = GetArgs(parameters);
            var cacheKey = BuildMethodCacheKey(memberPath, rawArgs);

            MethodInfo methodInfo;
            if (!StaticMethodCache.TryGetValue(cacheKey, out methodInfo))
            {
                object[] convertedArgs;
                methodInfo = ResolveStaticMethod(targetType, split.Item2, rawArgs, out convertedArgs);
                StaticMethodCache[cacheKey] = methodInfo;

                Debug.Log($"[TalosE2E] 反射方法缓存写入: member={memberPath}, key={cacheKey}");
                return InvokeResolvedStaticMethod(memberPath, methodInfo, convertedArgs);
            }

            object[] cachedConvertedArgs;
            ConvertArgumentsOrThrow(methodInfo, rawArgs, out cachedConvertedArgs);
            Debug.Log($"[TalosE2E] 反射方法命中缓存: member={memberPath}, key={cacheKey}");
            return InvokeResolvedStaticMethod(memberPath, methodInfo, cachedConvertedArgs);
        }

        /// <summary>
        /// 读取任意 public static 属性或字段。
        /// 参数格式：
        /// - memberPath: "Namespace.TypeName.MemberName"
        /// </summary>
        private static object CmdReflectGetStatic(JsonData parameters)
        {
            var memberPath = GetParamString(parameters, "memberPath");
            if (string.IsNullOrEmpty(memberPath))
            {
                throw new ArgumentException("reflect_get_static 命令缺少 memberPath 参数");
            }

            var split = SplitMemberPath(memberPath);
            var targetType = ResolveType(split.Item1);

            PropertyInfo propertyInfo;
            if (StaticPropertyCache.TryGetValue(memberPath, out propertyInfo))
            {
                var propertyValue = propertyInfo.GetValue(null, null);
                return BuildMemberResult(memberPath, targetType, propertyInfo.PropertyType, propertyValue);
            }

            FieldInfo fieldInfo;
            if (StaticFieldCache.TryGetValue(memberPath, out fieldInfo))
            {
                var fieldValue = fieldInfo.GetValue(null);
                return BuildMemberResult(memberPath, targetType, fieldInfo.FieldType, fieldValue);
            }

            propertyInfo = targetType.GetProperty(split.Item2,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (propertyInfo != null && propertyInfo.CanRead)
            {
                StaticPropertyCache[memberPath] = propertyInfo;
                var propertyValue = propertyInfo.GetValue(null, null);
                Debug.Log($"[TalosE2E] 静态属性缓存写入: member={memberPath}");
                return BuildMemberResult(memberPath, targetType, propertyInfo.PropertyType, propertyValue);
            }

            fieldInfo = targetType.GetField(split.Item2,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (fieldInfo != null)
            {
                StaticFieldCache[memberPath] = fieldInfo;
                var fieldValue = fieldInfo.GetValue(null);
                Debug.Log($"[TalosE2E] 静态字段缓存写入: member={memberPath}");
                return BuildMemberResult(memberPath, targetType, fieldInfo.FieldType, fieldValue);
            }

            throw new MissingMemberException($"未找到可读的 public static 属性或字段: {memberPath}");
        }

        /// <summary>
        /// 调用已解析的方法，并构造统一返回结构。
        /// </summary>
        private static object InvokeResolvedStaticMethod(string memberPath, MethodInfo methodInfo, object[] convertedArgs)
        {
            Debug.Log($"[TalosE2E] 反射执行静态方法: {memberPath}, 参数个数={convertedArgs.Length}");
            var rawValue = methodInfo.Invoke(null, convertedArgs.Length > 0 ? convertedArgs : null);

            return new
            {
                kind = "method",
                memberPath,
                declaringType = methodInfo.DeclaringType != null ? methodInfo.DeclaringType.FullName : null,
                returnType = methodInfo.ReturnType.FullName,
                value = NormalizeValue(rawValue),
            };
        }

        /// <summary>
        /// 构造静态成员读取结果。
        /// </summary>
        private static object BuildMemberResult(string memberPath, Type targetType, Type valueType, object value)
        {
            return new
            {
                kind = "member",
                memberPath,
                declaringType = targetType.FullName,
                valueType = valueType.FullName,
                value = NormalizeValue(value),
            };
        }

        /// <summary>
        /// 解析类型，并使用缓存避免重复查找。
        /// </summary>
        private static Type ResolveType(string typeName)
        {
            Type cachedType;
            if (TypeCache.TryGetValue(typeName, out cachedType))
            {
                return cachedType;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var targetType = assembly.GetType(typeName);
                if (targetType == null)
                {
                    continue;
                }

                TypeCache[typeName] = targetType;
                Debug.Log($"[TalosE2E] 类型缓存写入: type={typeName}, assembly={assembly.GetName().Name}");
                return targetType;
            }

            throw new TypeLoadException($"未找到类型: {typeName}");
        }

        /// <summary>
        /// 解析最佳匹配的 public static 方法，并完成参数转换。
        /// </summary>
        private static MethodInfo ResolveStaticMethod(Type targetType, string methodName, object[] rawArgs, out object[] convertedArgs)
        {
            var candidates = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(method => method.Name == methodName)
                .ToArray();

            if (candidates.Length == 0)
            {
                throw new MissingMethodException($"未找到 public static 方法: {targetType.FullName}.{methodName}");
            }

            MethodInfo bestMethod = null;
            object[] bestArgs = null;
            var bestScore = int.MaxValue;

            foreach (var candidate in candidates)
            {
                object[] candidateArgs;
                int candidateScore;
                if (!TryConvertArguments(candidate, rawArgs, out candidateArgs, out candidateScore))
                {
                    continue;
                }

                if (candidateScore < bestScore)
                {
                    bestMethod = candidate;
                    bestArgs = candidateArgs;
                    bestScore = candidateScore;
                }
            }

            if (bestMethod == null)
            {
                var available = string.Join(", ", candidates.Select(FormatMethodSignature).ToArray());
                throw new MissingMethodException(
                    $"未找到匹配的方法: {targetType.FullName}.{methodName}({rawArgs.Length} 个参数)。可用签名: {available}");
            }

            convertedArgs = bestArgs;
            return bestMethod;
        }

        /// <summary>
        /// 使用缓存的方法签名再次转换参数，确保缓存命中时调用参数依然合法。
        /// </summary>
        private static void ConvertArgumentsOrThrow(MethodInfo methodInfo, object[] rawArgs, out object[] convertedArgs)
        {
            int score;
            if (!TryConvertArguments(methodInfo, rawArgs, out convertedArgs, out score))
            {
                throw new ArgumentException($"缓存的方法签名与当前参数不匹配: {FormatMethodSignature(methodInfo)}");
            }
        }

        /// <summary>
        /// 尝试将原始参数转换为目标方法签名。
        /// 分数越低代表匹配越精确，用于重载决策。
        /// </summary>
        private static bool TryConvertArguments(MethodInfo methodInfo, object[] rawArgs, out object[] convertedArgs, out int score)
        {
            var parameters = methodInfo.GetParameters();
            if (parameters.Length != rawArgs.Length)
            {
                convertedArgs = null;
                score = int.MaxValue;
                return false;
            }

            convertedArgs = new object[parameters.Length];
            score = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                object convertedValue;
                int itemScore;
                if (!TryConvertValue(rawArgs[i], parameters[i].ParameterType, out convertedValue, out itemScore))
                {
                    convertedArgs = null;
                    score = int.MaxValue;
                    return false;
                }

                convertedArgs[i] = convertedValue;
                score += itemScore;
            }

            return true;
        }

        /// <summary>
        /// 将单个参数转换为目标类型。
        /// </summary>
        private static bool TryConvertValue(object value, Type targetType, out object converted, out int score)
        {
            var nullableTargetType = Nullable.GetUnderlyingType(targetType);
            var effectiveTargetType = nullableTargetType ?? targetType;

            if (value == null)
            {
                if (targetType.IsValueType && nullableTargetType == null)
                {
                    converted = null;
                    score = int.MaxValue;
                    return false;
                }

                converted = null;
                score = 1;
                return true;
            }

            if (effectiveTargetType.IsInstanceOfType(value))
            {
                converted = value;
                score = 0;
                return true;
            }

            try
            {
                if (effectiveTargetType == typeof(string))
                {
                    converted = value.ToString();
                    score = 1;
                    return true;
                }

                if (effectiveTargetType.IsEnum)
                {
                    if (value is string)
                    {
                        converted = Enum.Parse(effectiveTargetType, (string)value, true);
                        score = 1;
                        return true;
                    }

                    var enumUnderlyingType = Enum.GetUnderlyingType(effectiveTargetType);
                    var enumValue = Convert.ChangeType(value, enumUnderlyingType, CultureInfo.InvariantCulture);
                    converted = Enum.ToObject(effectiveTargetType, enumValue);
                    score = 2;
                    return true;
                }

                if (effectiveTargetType == typeof(bool))
                {
                    if (value is string)
                    {
                        converted = bool.Parse((string)value);
                        score = 1;
                        return true;
                    }

                    converted = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    score = 1;
                    return true;
                }

                if (typeof(IConvertible).IsAssignableFrom(effectiveTargetType) && value is IConvertible)
                {
                    converted = Convert.ChangeType(value, effectiveTargetType, CultureInfo.InvariantCulture);
                    score = 1;
                    return true;
                }
            }
            catch
            {
                // 保持静默并返回 false，让上层继续尝试其他重载。
            }

            converted = null;
            score = int.MaxValue;
            return false;
        }

        /// <summary>
        /// 从参数中提取调用参数数组。
        /// </summary>
        private static object[] GetArgs(JsonData parameters)
        {
            if (parameters == null)
            {
                return new object[0];
            }

            try
            {
                var argsData = parameters["args"];
                if (argsData == null || !argsData.IsArray)
                {
                    return new object[0];
                }

                var result = new object[argsData.Count];
                for (int i = 0; i < argsData.Count; i++)
                {
                    result[i] = ConvertJsonData(argsData[i]);
                }

                return result;
            }
            catch
            {
                return new object[0];
            }
        }

        /// <summary>
        /// 生成方法缓存键。
        /// </summary>
        private static string BuildMethodCacheKey(string memberPath, object[] rawArgs)
        {
            var signature = string.Join(",", rawArgs.Select(arg => arg != null ? arg.GetType().FullName : "null").ToArray());
            return string.Format("{0}::{1}", memberPath, signature);
        }

        /// <summary>
        /// 将方法签名格式化为可读字符串，便于错误提示。
        /// </summary>
        private static string FormatMethodSignature(MethodInfo methodInfo)
        {
            var parameterText = string.Join(", ", methodInfo.GetParameters().Select(parameter => string.Format("{0} {1}", parameter.ParameterType.Name, parameter.Name)).ToArray());
            return string.Format("{0}.{1}({2})", methodInfo.DeclaringType != null ? methodInfo.DeclaringType.FullName : "<null>", methodInfo.Name, parameterText);
        }

        /// <summary>
        /// 拆分成员路径为类型名和成员名。
        /// </summary>
        private static Tuple<string, string> SplitMemberPath(string memberPath)
        {
            var lastDot = memberPath.LastIndexOf('.');
            if (lastDot < 0)
            {
                throw new ArgumentException($"成员路径格式错误，期望 'Namespace.TypeName.MemberName': {memberPath}");
            }

            return Tuple.Create(memberPath.Substring(0, lastDot), memberPath.Substring(lastDot + 1));
        }

        /// <summary>
        /// 将返回值转换为可 JSON 序列化的结构。
        /// 这里优先保留基础类型，对复杂对象则展开公共可读属性，避免 Scene 等结构体在协议层变成空对象。
        /// </summary>
        private static object NormalizeValue(object value, int depth = 0)
        {
            if (value == null)
            {
                return null;
            }

            if (depth > 3)
            {
                return value.ToString();
            }

            var valueType = value.GetType();
            if (valueType.IsPrimitive || value is string || value is decimal || value is DateTime || value is Guid)
            {
                return value;
            }

            if (valueType.IsEnum)
            {
                return value.ToString();
            }

            if (value is UnityEngine.Object)
            {
                var unityObject = (UnityEngine.Object)value;
                return new Dictionary<string, object>
                {
                    ["name"] = unityObject.name,
                    ["instanceId"] = unityObject.GetInstanceID(),
                    ["unityType"] = unityObject.GetType().FullName,
                };
            }

            if (value is Type)
            {
                return ((Type)value).FullName;
            }

            if (value is MemberInfo)
            {
                return value.ToString();
            }

            if (value is IDictionary)
            {
                var dictionary = (IDictionary)value;
                var map = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    map[entry.Key != null ? entry.Key.ToString() : string.Empty] = NormalizeValue(entry.Value, depth + 1);
                }

                return map;
            }

            if (value is IEnumerable && !(value is string))
            {
                var enumerable = (IEnumerable)value;
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    list.Add(NormalizeValue(item, depth + 1));
                }

                return list;
            }

            var properties = valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .ToArray();
            if (properties.Length > 0)
            {
                var result = new Dictionary<string, object>
                {
                    ["$type"] = valueType.FullName,
                };

                foreach (var property in properties)
                {
                    try
                    {
                        result[property.Name] = NormalizeValue(property.GetValue(value, null), depth + 1);
                    }
                    catch (Exception ex)
                    {
                        result[property.Name] = string.Format("<读取失败: {0}>", ex.GetType().Name);
                    }
                }

                return result;
            }

            var fields = valueType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length > 0)
            {
                var result = new Dictionary<string, object>
                {
                    ["$type"] = valueType.FullName,
                };

                foreach (var field in fields)
                {
                    result[field.Name] = NormalizeValue(field.GetValue(value), depth + 1);
                }

                return result;
            }

            return value.ToString();
        }

        /// <summary>
        /// 安全读取字符串参数。
        /// </summary>
        private static string GetParamString(JsonData parameters, string key)
        {
            if (parameters == null)
            {
                return null;
            }

            try
            {
                var value = parameters[key];
                if (value == null)
                {
                    return null;
                }

                return (string)value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 将 LitJson 数据递归转换为普通 .NET 对象，便于做参数匹配与缓存签名。
        /// </summary>
        private static object ConvertJsonData(JsonData data)
        {
            if (data == null)
            {
                return null;
            }

            if (data.IsString)
            {
                return (string)data;
            }

            if (data.IsBoolean)
            {
                return (bool)data;
            }

            if (data.IsInt)
            {
                return (int)data;
            }

            if (data.IsLong)
            {
                return (long)data;
            }

            if (data.IsDouble)
            {
                return (double)data;
            }

            if (data.IsArray)
            {
                var list = new List<object>();
                for (int i = 0; i < data.Count; i++)
                {
                    list.Add(ConvertJsonData(data[i]));
                }

                return list;
            }

            if (data.IsObject)
            {
                var map = new Dictionary<string, object>();
                foreach (DictionaryEntry entry in (IDictionary)data)
                {
                    map[entry.Key != null ? entry.Key.ToString() : string.Empty] = ConvertJsonData((JsonData)entry.Value);
                }

                return map;
            }

            return data.ToString();
        }
    }
}
