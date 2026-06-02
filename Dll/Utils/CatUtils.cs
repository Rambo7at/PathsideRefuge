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
using 途畔归所.Dll.NetWork;

namespace 途畔归所.Dll.Utils
{
    /// <summary>注：通用工具类</summary>
    public static class CatUtils
    {
        /// <summary>注：计算字符串的稳定哈希值。</summary>
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

        /// <summary>注：从资源路径提取名称，若路径异常或提取失败返回空并打印错误。</summary>
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

        /// <summary>注：停止节点的处理并将其从场景中移除。</summary>
        public static void StopAndExit(Node node)
        {
            node.SetProcess(false);
            node.SetPhysicsProcess(false);
            node.QueueFree();
        }

        /// <summary>注：在子节点中查找指定类型的子节点并返回</summary>
        public static T FindChildNode<T>(Node node) where T : Node
        {
            if (node == null) return null;
            foreach (var comp in node.GetChildren()) if (comp is T netsyncbase) return netsyncbase;
            return null;
        }

    }
}