using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using 维修公司.Dll.data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Utils
{
    /// <summary>
    /// 项目通用工具类 —— 含稳定字符串哈希等
    /// </summary>
    public static class CatUtils
    {
        /// <summary>
        /// 注：获取与运行时无关的稳定哈希码（djb2 变体，种子使用项目端口 3043）。
        /// 用于 RPC 方法名、变量名等需要网络一致的哈希。
        /// </summary>
        public static int GetStableHashCode(this string str)
        {
            unchecked
            {
                int hash = 3043;
                int hash2 = 3043;
                int length = str.Length;

                for (int i = 0; i < length; i += 2)
                {
                    hash = ((hash << 5) + hash) ^ str[i];

                    if (i + 1 < length)
                        hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash + hash2 * 686421487;
            }
        }


        /// <summary>从资源路径提取名称</summary>
        /// <param name="resourcePath">资源路径</param>
        /// <returns>返回资源名，失败时为空</returns>
        public static string GetResourceName(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                GD.PrintErr("[ToolUtils.GetResourceName] 资源路径为空");
                return null;
            }

            // 截取最后一个/后的文件名（如手电筒.tscn）
            int lastSlashIndex = resourcePath.LastIndexOf('/');
            if (lastSlashIndex == -1)
            {
                GD.PrintErr($"[ToolUtils.GetResourceName] 路径 {resourcePath} 格式异常（无/分隔符）");
                return null;
            }

            string fileNameWithExt = resourcePath.Substring(lastSlashIndex + 1);
            string resourceName = fileNameWithExt.Replace(".tscn", "");

            // 提取结果为空则返回null
            if (string.IsNullOrEmpty(resourceName))
            {
                GD.PrintErr($"[ToolUtils.GetResourceName] 路径 {resourcePath} 提取名称为空");
                return null;
            }

            return resourceName;
        }

        public static void StopAndExit(Node node)
        {
            node.SetProcess(false);
            node.SetPhysicsProcess(false);
            node.QueueFree();
        }







        public static Dictionary<string, Variant> SerializerToDict<T>(T obj) where T : Resource
        {
            if (obj == null) return null;

            Type type = obj.GetType();
            Dictionary<string, Variant> dict = [];
            PropertyInfo[] Properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in Properties)
            {
                if (!prop.CanRead) continue;

                string key = prop.Name;
                object value = prop.GetValue(obj);
                if (value == null) continue;

                if (IsVariant(value.GetType())) dict[key] = Variant.From(value);

            }
            return dict;
        }

        // 修正方法名，并补全所有 Variant 兼容类型
        private static bool IsVariant(Type type)
        {
            // 内置值类型和 string (根据文档)
            if (type == typeof(bool) || type == typeof(long) || type == typeof(double) 
                || type == typeof(string) || type == typeof(float) || type == typeof(int)) return true;

            // Godot 结构体 (根据文档)
            if (type == typeof(Vector2) || type == typeof(Vector2I) || type == typeof(Vector3) || type == typeof(Vector3I)) return true;
            if (type == typeof(Rect2) || type == typeof(Rect2I) || type == typeof(Transform2D) || type == typeof(Transform3D)) return true;
            if (type == typeof(Vector4) || type == typeof(Vector4I) || type == typeof(Plane) || type == typeof(Quaternion)) return true;
            if (type == typeof(Aabb) || type == typeof(Basis) || type == typeof(Projection)) return true;
            if (type == typeof(Color) || type == typeof(StringName) || type == typeof(NodePath) || type == typeof(Rid)) return true;
            if (type == typeof(Callable) || type == typeof(Signal)) return true;

            // Godot 集合类型 (根据文档)
            if (type == typeof(Godot.Collections.Dictionary) || type == typeof(Godot.Collections.Array)) return true;

            // Packed Arrays (根据文档)
            if (type == typeof(byte[]) || type == typeof(int[]) || type == typeof(long[]) ||
                type == typeof(float[]) || type == typeof(double[]) || type == typeof(string[]) ||
                type == typeof(Vector2[]) || type == typeof(Vector3[]) || type == typeof(Vector4[]) || type == typeof(Color[]))
                return true;


            return false;
        }




    }
}